using System.Text.Json.Serialization;
using TimeSeriesData;

namespace SmartHome.entities;

internal class Sensors: DictionaryData<Sensor>
{
    internal Sensors(string dataFolderPath, IDataSource<List<Sensor>> source):
        base("sensors", dataFolderPath, source)
    {
    }
}

internal sealed class Sensor : IIdentifiable
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string DataType { get; set; }
    [JsonPropertyName("LocationId")] public int Location { get; set; }

    public int GetId()
    {
        return Id;
    }
}