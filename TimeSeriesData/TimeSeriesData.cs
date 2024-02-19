namespace TimeSeriesData;

public struct DbFileInfo
{
    public readonly string Folder;
    public readonly string FileName;

    internal DbFileInfo(string folder, string fileName)
    {
        Folder = folder;
        FileName = fileName;
    }
}

public struct DbFileWithDate(string fileName, int date)
{
    public readonly string FileName = fileName;
    public readonly int Date = date;
}

public interface IDatedSource<T>
{
    T Load(IEnumerable<DbFileWithDate> files);
    void Save(T value, string dataFolderPath, int key);
    int GetDate(DbFileInfo fi);
    IEnumerable<DbFileWithDate> GetFileNames(string dataFolderPath, int key);
}

internal class LruItem<T> where T: class
{
    internal T? Value;
    internal long LastAccessTime;

    internal LruItem()
    {
        Value = null;
        LastAccessTime = 0;
    }
    
    internal LruItem(T? value)
    {
        Set(value);
    }

    internal void Update()
    {
        LastAccessTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
    
    internal void Set(T? value)
    {
        Value = value;
        if (value != null)
            Update();
    }
}

public abstract class TimeSeriesData<T>(string dataFolderPath, IDatedSource<T> source, int maxItems)
    where T : class, new()
{
    private SortedDictionary<int, LruItem<T>> _data = new();
    private readonly IDatedSource<T> _source = source;
    private readonly string _dataFolderPath = dataFolderPath;
    private readonly SortedDictionary<long, HashSet<int>> _lastAccessTimeMap = new();
    private readonly HashSet<int> _modified = [];
    private readonly int _maxItems = maxItems;
    public int ActiveItems { get; private set; }

    public void LoadAll()
    {
        foreach (var (key, t) in GetFileList().Aggregate(
                new Dictionary<int, List<DbFileWithDate>>(), (acc, fileInfo) =>
                {
                    var date = _source.GetDate(fileInfo);
                    var key = CalculateKey(date);
                    var dbFile = new DbFileWithDate(fileInfo.FileName, date);
                    if (!acc.TryGetValue(key, out var value))
                    {
                        value = new List<DbFileWithDate>();
                        acc[key] = value;
                    }
                    value.Add(dbFile);
                    return acc;
                }).Select(kv => (kv.Key, _source.Load(kv.Value))))
        {
            Add(key, t);
        }
    }

    public void Init() // create all indexes
    {
        foreach (var fileInfo in GetFileList())
        {
            var date = _source.GetDate(fileInfo);
            var key = CalculateKey(date);
            Add(key, null);
        }
    }

    public T LoadOrAdd(int date)
    {
        var key = CalculateKey(date);
        if (_data.TryGetValue(key, out var value))
            return GetT(value, key);
        var v = new T(); 
        Add(key, v);
        return v;
    }

    public T? Load(int date)
    {
        var key = CalculateKey(date);
        try
        {
            var t = _data.Last(kv => kv.Key <= key);
            return GetT(t.Value, t.Key);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private T GetT(LruItem<T> t, int key)
    {
        if (t.Value != null)
        {
            var prevAccessTime = t.LastAccessTime;
            t.Update();
            LruUpdate(key, prevAccessTime, t.LastAccessTime);
            return t.Value;
        }
        
        Cleanup();
        var v = _source.Load(_source.GetFileNames(_dataFolderPath, key));
        t.Set(v);
        LruAdd(key, t.LastAccessTime);
        ActiveItems++;

        return v;
    }

    private void LruAdd(int key, long newLastAccessTime)
    {
        if (_lastAccessTimeMap.TryGetValue(newLastAccessTime, out var items))
        {
            items.Add(key);
        }
        else
        {
            _lastAccessTimeMap[newLastAccessTime] = [key];
        }
    }
    
    private void LruUpdate(int key, long prevLastAccessTime, long newLastAccessTime)
    {
        if (prevLastAccessTime == newLastAccessTime)
            return;
        _lastAccessTimeMap[prevLastAccessTime].Remove(key);
        LruAdd(key, newLastAccessTime);
    }

    public void Add(int idx, T? value)
    {
        Cleanup();
        var v = new LruItem<T>(value);
        _data[idx] = v;
        LruAdd(idx, v.LastAccessTime);
        if (value == null) return;
        ActiveItems++;
        _modified.Add(idx);
    }

    public void MarkAsModified(int idx)
    {
        _modified.Add(idx);
    }
    
    private void Cleanup()
    {
        while (ActiveItems >= _maxItems)
            Cleanup(_lastAccessTimeMap.First());
    }

    private void Cleanup(KeyValuePair<long, HashSet<int>> kv)
    {
        foreach (var key in kv.Value)
            Cleanup(key);

        _lastAccessTimeMap.Remove(kv.Key);
    }

    private void Cleanup(int key)
    {
        var v = _data[key];
        _lastAccessTimeMap[v.LastAccessTime].Remove(key);
        Save(key, v.Value!);
        v.Set(null);
        ActiveItems--;
    }

    private void Save(int key, T value)
    {
        if (!_modified.Contains(key)) return;
        _source.Save(value, _dataFolderPath, key);
        _modified.Remove(key);
    }

    public void Save()
    {
        foreach (var key in _modified)
            _source.Save(_data[key].Value!, _dataFolderPath, key);
        _modified.Clear();
    }

    public void SaveAll(IDatedSource<T> source, string dataFolderPath)
    {
        foreach (var kv in _data)
        {
            if (kv.Value.Value != null)
                source.Save(kv.Value.Value, dataFolderPath, kv.Key);
        }
        _modified.Clear();
    }
    
    public IEnumerable<(int key, T)> Load(int from, int to)
    {
        var key1 = CalculateKey(from);
        var key2 = CalculateKey(to);
        return _data.Where(kv => kv.Key >= key1 && kv.Key <= key2)
            .Select(kv => (kv.Key, GetT(kv.Value, kv.Key)));
    }
    
    private IEnumerable<DbFileInfo> GetFileList(string directory = "")
    {
        var path = Path.Combine(_dataFolderPath, directory);
        return Directory.EnumerateFiles(path)
            .Select(file => new DbFileInfo(directory, file))
            .Concat(Directory.EnumerateDirectories(path)
                .SelectMany(dir => GetFileList(Path.GetFileName(dir))));
    }
    
    protected abstract int CalculateKey(int date);
}