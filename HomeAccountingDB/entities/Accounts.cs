using System.Text.Json;
using System.Text.Json.Serialization;
using TimeSeriesData;

namespace HomeAccountingDB.entities;

internal sealed class Accounts: DictionaryData<Account>
{
    internal Accounts(string dataFolderPath, IDataSource<List<Account>> source):
        base("accounts", dataFolderPath, source)
    {
        var cashAccounts = Data.Where(item => item.Value.CashAccount == null)
            .Select(item => (item.Value.Currency, item.Key))
            .ToDictionary();
        foreach (var keyValuePair in Data.Where(keyValuePair => keyValuePair.Value.CashAccount == 0))
            keyValuePair.Value.CashAccount = cashAccounts[keyValuePair.Value.Currency];
    }

    internal int GetCashAccount(int account)
    {
        return Data[account].CashAccount ?? throw new DbException("cash account not found");
    }
}

public sealed class Account: IIdentifiable, IBinaryData<Account>
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("valutaCode")] public string Currency { get; set; }
    [JsonPropertyName("activeTo")]
    [JsonConverter(typeof(DbDateConverter))]
    public int? ActiveTo { get; set; }
    [JsonPropertyName("isCash")]
    [JsonConverter(typeof(IsCashConverter))]
    public int? CashAccount { get; set; }

    public int GetId()
    {
        return Id;
    }

    public static Account Create(BinaryReader stream)
    {
        int id = stream.ReadInt32();
        string name = stream.ReadString();
        string currency = stream.ReadString();
        int activeTo = stream.ReadInt32();
        int cashAccount = stream.ReadInt32();
        return new Account
        {
            Id = id,
            Name = name,
            Currency = currency,
            ActiveTo = activeTo == 0 ? null : activeTo,
            CashAccount = cashAccount == 0 ? null : cashAccount
        };
    }

    public void Save(BinaryWriter stream)
    {
        stream.Write(Id);
        stream.Write(Name);
        stream.Write(Currency);
        stream.Write(ActiveTo ?? 0);
        stream.Write(CashAccount ?? 0);
    }
}

internal class IsCashConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var isCash = reader.GetBoolean();
        return isCash ? null : 0;
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
