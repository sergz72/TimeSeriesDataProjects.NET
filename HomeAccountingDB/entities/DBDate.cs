using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeAccountingDB.entities;

internal class DbDateConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("cannot read DbDate - start array expected");
        if (!reader.Read())
            throw new JsonException("cannot read DbDate - year expected");
        var year = reader.GetInt32();
        if (!reader.Read())
            throw new JsonException("cannot read DbDate - month expected");
        var month = reader.GetInt32();
        if (!reader.Read())
            throw new JsonException("cannot read DbDate - day expected");
        var day = reader.GetInt32();
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("cannot read DbDate - end array expected");
        return year * 10000 + month * 100 + day;
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
