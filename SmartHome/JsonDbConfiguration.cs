using SmartHome.entities;
using TimeSeriesData;

namespace SmartHome;

internal class JsonDbConfiguration: IDbConfiguration
{
    public string DatesSuffix => "dates_new";

    public IDatedSource<SensorData> GetMainDataSource()
    {
        return new JsonDatedSource();
    }

    public IDataSource<List<Location>> GetLocationsSource()
    {
        return new JsonListLoader<Location>(null);
    }

    public IDataSource<List<Sensor>> GetSensorsSource()
    {
        return new JsonListLoader<Sensor>(null);
    }
}

internal class JsonDatedSource : IDatedSource<SensorData>
{
    private readonly JsonListLoader<SensorDataItem> _loader = new(null);

    public SensorData Load(IEnumerable<DbFileWithDate> files)
    {
        var data = files.
            Select(file => (int.Parse(Path.GetFileNameWithoutExtension(file.FileName)), _loader.Load(file.FileName, false)))
            .ToDictionary();
        return new SensorData(data);
    }

    public void Save(SensorData value, string dataFolderPath, int date)
    {
        throw new NotImplementedException();
    }

    public int GetDate(DbFileInfo fi)
    {
        return int.Parse(fi.Folder);
    }

    public IEnumerable<DbFileWithDate> GetFileNames(string dataFolderPath, int key)
    {
        throw new NotImplementedException();
    }
}