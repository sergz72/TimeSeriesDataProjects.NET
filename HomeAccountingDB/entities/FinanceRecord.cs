using System.Text.Json;
using System.Text.Json.Serialization;
using TimeSeriesData;

namespace HomeAccountingDB.entities;

public sealed class FinanceRecord: IBinaryData<FinanceRecord>
{
    internal readonly List<FinanceOperation> Operations;
    internal Dictionary<int, long> Totals;

    private FinanceRecord(Dictionary<int, long> totals, List<FinanceOperation> operations)
    {
        Operations = operations;
        Totals = totals;
    }
    
    public FinanceRecord()
    {
        Operations = new List<FinanceOperation>();
        Totals = new Dictionary<int, long>();
    }

    internal FinanceRecord(List<FinanceOperation> operations)
    {
        Operations = operations;
        Totals = new Dictionary<int, long>();
    }

    internal FinanceRecord(int operations)
    {
        Totals = new Dictionary<int, long>();
        Operations = Enumerable.Range(0, operations).Select(_ => new FinanceOperation()).ToList();
    }
    
    internal void UpdateChanges(FinanceChanges changes, Accounts accounts, Subcategories subcategories)
    {
        Operations.ForEach(op => op.UpdateChanges(changes, accounts, subcategories));
    }
    
    internal FinanceChanges BuildChanges(Accounts accounts, Subcategories subcategories)
    {
        var changes = new FinanceChanges(Totals);
        Operations.ForEach(op => op.UpdateChanges(changes, accounts, subcategories));
        return changes;
    }

    public static FinanceRecord Create(BinaryReader stream)
    {
        var totals = ReadTotals(stream);
        var ops = ReadOperations(stream);
        return new FinanceRecord(totals, ops);
    }

    private static List<FinanceOperation> ReadOperations(BinaryReader stream)
    {
        var count = stream.ReadInt32();
        var result = new List<FinanceOperation>();
        while (count-- > 0)
            result.Add(FinanceOperation.Create(stream));
        return result;
    }

    private static Dictionary<int, long> ReadTotals(BinaryReader stream)
    {
        var count = stream.ReadInt32();
        var result = new Dictionary<int, long>();
        while (count-- > 0)
        {
            var key = stream.ReadInt32();
            var value = stream.ReadInt64();
            result.Add(key, value);
        }
        return result;
    }

    public void Save(BinaryWriter stream)
    {
        SaveTotals(stream);
        SaveOperations(stream);
    }

    private void SaveOperations(BinaryWriter stream)
    {
        stream.Write(Operations.Count);
        foreach (var op in Operations)
            op.Save(stream);
    }

    private void SaveTotals(BinaryWriter stream)
    {
        stream.Write(Totals.Count);
        foreach (var t in Totals)
        {
            stream.Write(t.Key);
            stream.Write(t.Value);
        }
    }
}

internal class FinanceChanges
{
    internal readonly Dictionary<int, FinanceChange> Changes;

    internal FinanceChanges(Dictionary<int, long> totals)
    {
        Changes = totals
            .Select(kv => (kv.Key, new FinanceChange(kv.Value)))
            .ToDictionary();
    }

    internal void Income(int account, long summa)
    {
        if (Changes.TryGetValue(account, out var change))
            change.Income += summa;
        else 
            Changes[account] = new FinanceChange(0, summa, 0);
    }
    
    internal void Expenditure(int account, long summa)
    {
        if (Changes.TryGetValue(account, out var change))
            change.Expenditure += summa;
        else 
            Changes[account] = new FinanceChange(0, 0, summa);
    }

    public Dictionary<int, long> BuildTotals()
    {
        return Changes
            .Select(kv => (kv.Key, kv.Value.GetEndSumma()))
            .ToDictionary();
    }
}

internal class FinanceChange
{
    internal readonly long Summa;
    internal long Income;
    internal long Expenditure;

    internal FinanceChange(long summa)
    {
        Summa = summa;
        Income = Expenditure = 0;
    }

    internal FinanceChange(long summa, long income, long expenditure)
    {
        Summa = summa;
        Income = income;
        Expenditure = expenditure;
    }

    internal long GetEndSumma()
    {
        return Summa + Income - Expenditure;
    }
}

public sealed class FinanceOperation: IBinaryData<FinanceOperation>
{
    [JsonIgnore]
    public int Date { get; set; }
    [JsonConverter(typeof(DbSumma3Converter))]
    public long? Amount { get; set; }
    [JsonConverter(typeof(DbSumma2Converter))]
    public long Summa { get; set; }
    [JsonPropertyName("subcategoryId")] public int Subcategory { get; set; }
    [JsonPropertyName("accountId")] public int Account { get; set; }
    [JsonPropertyName("finOpProperies")] public List<FinOpProperty>? FinOpProperties { get; set; }

    public static FinanceOperation Create(BinaryReader stream)
    {
        var date = stream.ReadInt32();
        var amount = stream.ReadInt64();
        var summa = stream.ReadInt64();
        var subcategory = stream.ReadInt32();
        var account = stream.ReadInt32();
        var count = stream.ReadInt32();
        var properties = new List<FinOpProperty>();
        while (count > 0)
        {
            properties.Add(FinOpProperty.Create(stream));
            count--;
        }
        return new FinanceOperation
        {
            Date = date,
            Amount = amount == 0 ? null : amount,
            Summa = summa,
            Subcategory = subcategory,
            Account = account,
            FinOpProperties = properties.Count == 0 ? null : properties
        };
    }

    public void Save(BinaryWriter stream)
    {
        stream.Write(Date);
        stream.Write(Amount ?? 0);
        stream.Write(Summa);
        stream.Write(Subcategory);
        stream.Write(Account);
        if (FinOpProperties != null)
        {
            stream.Write(FinOpProperties.Count);
            foreach (var prop in FinOpProperties)
                prop.Save(stream);
        }
        else
            stream.Write(0);
    }

    internal void UpdateChanges(FinanceChanges changes, Accounts accounts, Subcategories subcategories)
    {
        var subcategory = subcategories.Get(Subcategory);
        switch (subcategory.OperationCode)
        {
            case SubcategoryOperationCode.Incm:
                changes.Income(Account, Summa);
                break;
            case SubcategoryOperationCode.Expn:
                changes.Expenditure(Account, Summa);
                break;
            case SubcategoryOperationCode.Spcl:
                switch (subcategory.Code)
                {
                    // Пополнение карточного счета наличными
                    case SubcategoryCode.Incc:
                        HandleIncc(changes, accounts);
                        break;
                    // Снятие наличных в банкомате
                    case SubcategoryCode.Expc:
                        HandleExpc(changes, accounts);
                        break;
                    // Обмен валюты
                    case SubcategoryCode.Exch:
                        HandleExch(changes);
                        break;
                    // Перевод средств между платежными картами
                    case SubcategoryCode.Trfr:
                        HandleTrfr(changes);
                        break;
                    default: throw new DbException("unknown subcategory code");
                }
                break;
            default: throw new DbException("unknown subcategory operation code");
        }
    }

    private void HandleTrfr(FinanceChanges changes)
    {
        HandleTrfrWithSumma(changes, Summa);
    }

    private void HandleExch(FinanceChanges changes)
    {
        if (Amount != null)
        {
            HandleTrfrWithSumma(changes, Amount.Value / 10);
        }
    }

    private void HandleTrfrWithSumma(FinanceChanges changes, long summa)
    {
        if (FinOpProperties == null) return;
        changes.Expenditure(Account, summa);
        var secondAccountProperty = FinOpProperties.Find(property => property.PropertyCode == FinOpPropertyCode.Seca);
        if (secondAccountProperty.NumericValue != null)
        {
            changes.Income((int)secondAccountProperty.NumericValue, Summa);
        }
    }
    
    private void HandleExpc(FinanceChanges changes, Accounts accounts)
    {
        changes.Expenditure(Account, Summa);
        // cash account for corresponding currency code
        var cashAccount = accounts.GetCashAccount(Account);
        changes.Income(cashAccount, Summa);
    }

    private void HandleIncc(FinanceChanges changes, Accounts accounts)
    {
        changes.Income(Account, Summa);
        // cash account for corresponding currency code
        var cashAccount = accounts.GetCashAccount(Account);
        changes.Expenditure(cashAccount, Summa);
    }
}

public enum FinOpPropertyCode
{
    Amou,
    Dist,
    Netw,
    Ppto,
    Seca,
    Typ
}

public struct FinOpProperty: IBinaryData<FinOpProperty>
{
    public long? NumericValue { get; set; }
    public string? StringValue { get; set; }
    public int? DateValue { get; set; }
    [JsonConverter(typeof(PropertyCodeConverter))]
    public FinOpPropertyCode PropertyCode { get; set; }

    public static FinOpProperty Create(BinaryReader stream)
    {
        var n = stream.ReadInt64();
        var s = stream.ReadString();
        var d = stream.ReadInt32();
        var code = (FinOpPropertyCode)stream.ReadByte();
        return new FinOpProperty
        {
            NumericValue = n == long.MaxValue ? null : n,
            StringValue = s.Length == 0 ? null : s,
            DateValue = d == 0 ? null : d,
            PropertyCode = code
        };
    }

    public void Save(BinaryWriter stream)
    {
        stream.Write(NumericValue ?? long.MaxValue);
        stream.Write(StringValue ?? "");
        stream.Write(DateValue ?? 0);
        stream.Write((byte)PropertyCode);
    }
}

internal class PropertyCodeConverter : JsonConverter<FinOpPropertyCode>
{
    public override FinOpPropertyCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var code = reader.GetString() ?? throw new JsonException("null finOpProperty code");
        return code switch
        {
            "AMOU" => FinOpPropertyCode.Amou,
            "DIST" => FinOpPropertyCode.Dist,
            "NETW" => FinOpPropertyCode.Netw,
            "PPTO" => FinOpPropertyCode.Ppto,
            "SECA" => FinOpPropertyCode.Seca,
            "TYPE" => FinOpPropertyCode.Typ,
            _ => throw new JsonException("unknown finOpProperty code")
        };
    }

    public override void Write(Utf8JsonWriter writer, FinOpPropertyCode value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
