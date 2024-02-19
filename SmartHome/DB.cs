using SmartHome.entities;
using TimeSeriesData;

namespace SmartHome;

internal interface IDbConfiguration
{
    string DatesSuffix { get; }
    IDatedSource<SensorData, List<SensorDataItem>> GetMainDataSource();
    IDataSource<List<Location>> GetLocationsSource();
    IDataSource<List<Sensor>> GetSensorsSource();
}

internal sealed class Db: TimeSeriesData<SensorData, SensorDataItem>
{
    private readonly Locations _locations;
    private readonly Sensors _sensors;
    
    internal Db(string dataFolderPath, IDbConfiguration configuration, int maxItems):
        base(Path.Combine(dataFolderPath, configuration.DatesSuffix), configuration.GetMainDataSource(), maxItems)
    {
        _locations = new Locations(dataFolderPath, configuration.GetLocationsSource());
        _sensors = new Sensors(dataFolderPath, configuration.GetSensorsSource());
    }
    
    protected override int CalculateKey(int date)
    {
        return date;
    }

    internal void AggregateAll()
    {
        foreach (var v in Load(0, int.MaxValue))
            v.Item2.Aggregate();
    }
}