namespace TimeSeriesData;

public interface IDataSource<T>
{
    T Load(string fileName, bool addExtension);
    void Save(T value, string fileName, bool addExtension);
}
