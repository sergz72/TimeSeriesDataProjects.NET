namespace TimeSeriesData;

public interface ICryptoProcessor
{
    byte[] Encrypt(byte[] data);
    byte[] Decrypt(byte[] data);
}

public interface IBinaryData<out T>
{
    static abstract T Create(BinaryReader stream);
    void Save(BinaryWriter stream);
}

public class BinaryListLoader<T>(ICryptoProcessor? processor) : BinaryLoader<List<T>>(processor)
    where T : IBinaryData<T>
{
    protected override List<T> Create(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        return Enumerable.Range(0, count).Select(_ => T.Create(reader)).ToList();
    }

    protected override void Save(List<T> value, BinaryWriter writer)
    {
        writer.Write(value.Count);
        foreach (var item in value)
            item.Save(writer);
    }
}

public class BinaryClassLoader<T>(ICryptoProcessor? processor) : BinaryLoader<T>(processor)
    where T : IBinaryData<T>
{
    protected override T Create(BinaryReader reader)
    {
        return T.Create(reader);
    }

    protected override void Save(T value, BinaryWriter writer)
    {
        value.Save(writer);
    }
}

public abstract class BinaryLoader<T>(ICryptoProcessor? processor) : IDataSource<T>
{
    private readonly ICryptoProcessor? _processor = processor;

    public T Load(string fileName, bool addExtension)
    {
        if (addExtension)
            fileName += ".bin";
        var bytes = File.ReadAllBytes(fileName);
        if (_processor != null)
            bytes = _processor.Decrypt(bytes);
        var reader = new BinaryReader(new MemoryStream(bytes));
        return Create(reader);
    }

    protected abstract T Create(BinaryReader reader);

    protected abstract void Save(T value, BinaryWriter writer);
    
    public void Save(T value, string fileName, bool addExtension)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        Save(value, writer);
        writer.Flush();
        stream.Flush();
        var bytes = stream.GetBuffer();
        if (_processor != null)
            bytes = _processor.Encrypt(bytes);
        if (addExtension)
            fileName += ".bin";
        File.WriteAllBytes(fileName, bytes);
    }
}