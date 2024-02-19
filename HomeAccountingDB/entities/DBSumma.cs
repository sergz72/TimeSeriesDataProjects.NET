using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeAccountingDB.entities;

internal class DbSumma2Converter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TryGetInt64(out var summa) ? summa : (long)Math.Round(reader.GetDouble() * 100);
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

internal class DbSumma3Converter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TryGetInt64(out var summa) ? summa : (long)Math.Round(reader.GetDouble() * 1000);
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}