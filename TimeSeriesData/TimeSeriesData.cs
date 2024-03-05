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
    internal int Idx;
    internal T? Value;
    internal LruItem<T>? Prev;
    internal LruItem<T>? Next;

    internal LruItem(int idx)
    {
        Idx = idx;
        Value = null;
        Prev = Next = null;
    }
    
    internal LruItem(int idx, T? value)
    {
        Idx = idx;
        Value = value;
    }
}

internal class Lru<T> where T: class
{
    private LruItem<T>? _head;
    private LruItem<T>? _tail;
    internal LruItem<T> Tail => _tail ?? throw new NullReferenceException("_tail is null");
    internal int ActiveItems { get; private set; }

    internal Lru()
    {
        _head = _tail = null;
    }

    internal void MoveToFront(LruItem<T> item)
    {
        if (_head == item)
            return;
        Detach(item);
        Add(item);
    }

    internal void Add(LruItem<T> item)
    {
        ActiveItems++;
        if (_head == null)
        {
            _head = _tail = item;
            item.Prev = item.Next = null;
            return;
        }
        var oldHead = _head;
        _head = item;
        oldHead.Prev = item;
        item.Prev = null;
        item.Next = oldHead;
    }

    private void Detach(LruItem<T> item)
    {
        if (item.Prev != null)
            item.Prev.Next = item.Next;
        else
            _head = item.Next;
        if (item.Next != null)
            item.Next.Prev = item.Prev;
        else
            _tail = item.Prev;
        ActiveItems--;
    }

    internal void DetachTail()
    {
        Detach(Tail);
    }
}

public abstract class TimeSeriesData<T>(string dataFolderPath, IDatedSource<T> source, int maxItems)
    where T : class, new()
{
    private readonly SortedDictionary<int, LruItem<T>> _data = new();
    private readonly Lru<T> _lru = new();
    private readonly HashSet<int> _modified = [];
    public int ActiveItems => _lru.ActiveItems;

    public void LoadAll()
    {
        foreach (var (key, t) in GetFileList().Aggregate(
                new Dictionary<int, List<DbFileWithDate>>(), (acc, fileInfo) =>
                {
                    var date = source.GetDate(fileInfo);
                    var key = CalculateKey(date);
                    var dbFile = new DbFileWithDate(fileInfo.FileName, date);
                    if (!acc.TryGetValue(key, out var value))
                    {
                        value = new List<DbFileWithDate>();
                        acc[key] = value;
                    }
                    value.Add(dbFile);
                    return acc;
                }).Select(kv => (kv.Key, source.Load(kv.Value))))
        {
            Add(key, t, false);
        }
    }

    public void Init() // create all indexes
    {
        foreach (var fileInfo in GetFileList())
        {
            var date = source.GetDate(fileInfo);
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
            _lru.MoveToFront(t);
            return t.Value;
        }
        
        Cleanup();
        var v = source.Load(source.GetFileNames(dataFolderPath, key));
        t.Value = v;
        _lru.Add(t);

        return v;
    }
    
    public void Add(int idx, T? value, bool modified = true)
    {
        Cleanup();
        var v = new LruItem<T>(idx, value);
        _data[idx] = v;
        if (value == null) return;
        _lru.Add(v);
        if (modified)
            _modified.Add(idx);
    }

    public void MarkAsModified(int idx)
    {
        _modified.Add(idx);
    }
    
    private void Cleanup()
    {
        while (ActiveItems >= maxItems)
        {
            var item = _lru.Tail;
            Save(item.Idx, item.Value!);
            _lru.DetachTail();
        }
    }
    
    private void Save(int key, T value)
    {
        if (!_modified.Contains(key)) return;
        source.Save(value, dataFolderPath, key);
        _modified.Remove(key);
    }

    public void Save()
    {
        foreach (var key in _modified)
            source.Save(_data[key].Value!, dataFolderPath, key);
        _modified.Clear();
    }

    public void SaveAll(IDatedSource<T> target, string targetDataFolder)
    {
        foreach (var kv in _data)
        {
            if (kv.Value.Value != null)
                target.Save(kv.Value.Value, targetDataFolder, kv.Key);
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
        var path = Path.Combine(dataFolderPath, directory);
        return Directory.EnumerateFiles(path)
            .Select(file => new DbFileInfo(directory, file))
            .Concat(Directory.EnumerateDirectories(path)
                .SelectMany(dir => GetFileList(Path.GetFileName(dir))));
    }
    
    protected abstract int CalculateKey(int date);
}