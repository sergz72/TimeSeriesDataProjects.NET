using System.Text.Json;

namespace TimeSeriesData;

public sealed class JsonListLoader<T>(JsonSerializerOptions? options) : IDataSource<List<T>>
{
    public List<T> Load(string fileName, bool addExtension)
    {
        if (addExtension)
            fileName += ".json";
        using var stream = File.OpenRead(fileName);
        return JsonSerializer.Deserialize<List<T>>(stream, options) ??
                   throw new JsonException("deserialize returned null");
    }

    public void Save(List<T> value, string fileName, bool addExtension)
    {
        throw new NotImplementedException();
    }
}
