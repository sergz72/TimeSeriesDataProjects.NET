using SmartHome.entities;
using TimeSeriesData;

namespace SmartHome;

internal class BinaryDbConfiguration: IDbConfiguration
{
    public string DatesSuffix => "dates";
    public IDatedSource<SensorData> GetMainDataSource()
    {
        return new BinaryDatedSource();
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

internal class BinaryDatedSource() : IDatedSource<SensorData>
{
    private readonly BinaryClassLoader<SensorData> _loader = new(null);
    
    // parentFolder/year/fileName
    private static string GetFileName(string dataFolderPath, int key) => Path.Combine(dataFolderPath, (key / 10000).ToString(), key + ".bin");

    public SensorData Load(IEnumerable<DbFileWithDate> files)
    {
        return _loader.Load(files.First().FileName, false);
    }

    public void Save(SensorData value, string dataFolderPath, int key)
    {
        var folder = Path.Combine(dataFolderPath, (key / 10000).ToString());
        Directory.CreateDirectory(folder);
        _loader.Save(value, GetFileName(dataFolderPath, key), false);
    }

    public int GetDate(DbFileInfo fi)
    {
        return int.Parse(Path.GetFileNameWithoutExtension(fi.FileName));
    }

    public IEnumerable<DbFileWithDate> GetFileNames(string dataFolderPath, int key)
    {
        return [new DbFileWithDate(GetFileName(dataFolderPath, key), key * 100)];
    }
}
