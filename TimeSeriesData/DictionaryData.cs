namespace TimeSeriesData;

public interface IIdentifiable
{
    int GetId();
}

public class DictionaryData<T> where T: IIdentifiable
{
    protected readonly Dictionary<int, T> Data;
    protected readonly IDataSource<List<T>> Source;
    protected readonly string FileName;
    protected readonly string DataFolderPath;
    protected bool Modified;

    protected DictionaryData(string fileName, string dataFolderPath, IDataSource<List<T>> source)
    {
        FileName = fileName;
        DataFolderPath = dataFolderPath;
        Source = source;
        Data = source.Load(Path.Combine(dataFolderPath, fileName), true).Select(d => (d.GetId(), d)).ToDictionary();
        Modified = false;
    }

    public T Get(int idx)
    {
        return Data[idx];
    }

    public void Update(int idx, Action<T> action)
    {
        var v = Data[idx];
        action.Invoke(v);
        Modified = true;
    }

    public virtual void Delete(int idx)
    {
        Data.Remove(idx);
        Modified = true;
    }

    public void Add(int idx, T value)
    {
        Data[idx] = value;
        Modified = true;
    }
    
    public void Save(IDataSource<List<T>> source, string dataFolderPath)
    {
        if (!Modified) return;
        source.Save(Data.Select(kv => kv.Value).ToList(), Path.Combine(dataFolderPath, FileName), true);
        Modified = false;
    }

    public void Save()
    {
        Save(Source, DataFolderPath);
    }
}