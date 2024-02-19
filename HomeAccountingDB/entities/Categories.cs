using System.Text.Json;
using System.Text.Json.Serialization;
using TimeSeriesData;

namespace HomeAccountingDB.entities;

internal sealed class Subcategories: DictionaryData<Subcategory>
{
    internal Subcategories(string dataFolderPath, IDataSource<List<Subcategory>> source):
        base("subcategories", dataFolderPath, source)
    {
    }
}

public enum SubcategoryOperationCode
{
    Incm,
    Expn,
    Spcl
}

public enum SubcategoryCode
{
    Comb,
    Comc,
    Fuel,
    Prcn,
    Incc,
    Expc,
    Exch,
    Trfr,
    None
}

public sealed class Subcategory: IIdentifiable, IBinaryData<Subcategory>
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("code")]
    [JsonConverter(typeof(CodeConverter))]
    public SubcategoryCode Code { get; set; }
    [JsonPropertyName("operationCodeId")]
    [JsonConverter(typeof(OperationCodeConverter))]
    public SubcategoryOperationCode OperationCode { get; set; }
    [JsonPropertyName("categoryId")] public int Category { get; set; }

    public int GetId()
    {
        return Id;
    }

    public static Subcategory Create(BinaryReader stream)
    {
        var id = stream.ReadInt32();
        var name = stream.ReadString();
        var code = stream.ReadByte();
        var operationCode = stream.ReadByte();
        var category = stream.ReadInt32();
        return new Subcategory
        {
            Id = id,
            Name = name,
            Code = (SubcategoryCode)code,
            OperationCode = (SubcategoryOperationCode)operationCode,
            Category = category
        };
    }

    public void Save(BinaryWriter stream)
    {
        stream.Write(Id);
        stream.Write(Name);
        stream.Write((byte)Code);
        stream.Write((byte)OperationCode);
        stream.Write(Category);
    }
}

internal sealed class Categories: DictionaryData<Category>
{
    internal Categories(string dataFolderPath, IDataSource<List<Category>> source):
        base("categories", dataFolderPath, source)
    {
    }
}

public sealed class Category : IIdentifiable, IBinaryData<Category>
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }

    public int GetId()
    {
        return Id;
    }

    public static Category Create(BinaryReader stream)
    {
        var id = stream.ReadInt32();
        var name = stream.ReadString();
        return new Category { Id = id, Name = name };
    }

    public void Save(BinaryWriter stream)
    {
        stream.Write(Id);
        stream.Write(Name);
    }
}

internal class CodeConverter : JsonConverter<SubcategoryCode>
{
    public override SubcategoryCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var code = reader.GetString();
        return code switch
        {
            null => SubcategoryCode.None,
            "COMB" => SubcategoryCode.Comb,
            "COMC" => SubcategoryCode.Comc,
            "INCC" => SubcategoryCode.Incc,
            "EXPC" => SubcategoryCode.Expc,
            "EXCH" => SubcategoryCode.Exch,
            "TRFR" => SubcategoryCode.Trfr,
            "PRCN" => SubcategoryCode.Prcn,
            "FUEL" => SubcategoryCode.Fuel,
            _ => throw new JsonException("unknown subcategory code " + code)
        };
    }

    public override void Write(Utf8JsonWriter writer, SubcategoryCode value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

internal class OperationCodeConverter : JsonConverter<SubcategoryOperationCode>
{
    public override SubcategoryOperationCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var code = reader.GetString() ?? throw new JsonException("null subcategory operation code");
        return code switch
        {
            "INCM" => SubcategoryOperationCode.Incm,
            "EXPN" => SubcategoryOperationCode.Expn,
            "SPCL" => SubcategoryOperationCode.Spcl,
            _ => throw new JsonException("unknown subcategory operation code")
        };
    }

    public override void Write(Utf8JsonWriter writer, SubcategoryOperationCode value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
