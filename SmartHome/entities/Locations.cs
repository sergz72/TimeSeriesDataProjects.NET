using TimeSeriesData;

namespace SmartHome.entities;

internal sealed class Locations: DictionaryData<Location>
{
    internal Locations(string dataFolderPath, IDataSource<List<Location>> source):
        base("locations", dataFolderPath, source)
    {
    }
}

internal sealed class Location : IIdentifiable
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string LocationType { get; set; }

    public int GetId()
    {
        return Id;
    }
}