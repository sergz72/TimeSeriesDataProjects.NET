using System.Collections;

namespace TimeSeriesData;

public sealed class ListDictionary<T>: IEnumerable<KeyValuePair<int, T>> where T: class
{
    private readonly T?[] _items;

    public ListDictionary(int capacity)
    {
        _items = new T?[capacity];
    }
    
    public ListDictionary(IEnumerable<(int key, T value)> source, int capacity)
    {
        _items = new T?[capacity];
        foreach (var kv in source)
        {
            this[kv.key] = kv.value;
        }
    }

    public int MaxIndex { get; private set; } = -1;
    
    public T this[int key]
    {
        get => _items[key] ?? throw new NullReferenceException("null ListDictionary item");
        set
        {
            _items[key] = value;
            if (key > MaxIndex)
                MaxIndex = key;
        }
    }

    public void Remove(int key)
    {
        _items[key] = null;
    }

    public IEnumerator<KeyValuePair<int, T>> GetEnumerator()
    {
        for (var i = 0; i <= MaxIndex; i++)
        {
            var v = _items[i];
            if (v != null)
                yield return new KeyValuePair<int, T>(i, v);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public static class ListDictionaryExtensions
{
    public static ListDictionary<T> ToListDictionary<T>(this IEnumerable<(int key, T value)> source, int capacity) where T: class
    {
        return new ListDictionary<T>(source, capacity);
    } 
}