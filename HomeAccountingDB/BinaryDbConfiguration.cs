using HomeAccountingDB.entities;
using NetworkAndCrypto;
using TimeSeriesData;

namespace HomeAccountingDB;

public sealed class BinaryDbConfiguration(byte[] aesKey): IDbConfiguration
{
    private readonly byte[] _aesKey = aesKey;
    
    public IDatedSource<FinanceRecord> GetMainDataSource()
    {
        return new BinaryDatedSource(new AesProcessor(_aesKey));
    }

    public IDataSource<List<Account>> GetAccountsSource()
    {
        return new BinaryListLoader<Account>(new AesProcessor(_aesKey));
    }

    public IDataSource<List<Category>> GetCategoriesSource()
    {
        return new BinaryListLoader<Category>(new AesProcessor(_aesKey));
    }

    public IDataSource<List<Subcategory>> GetSubcategoriesSource()
    {
        return new BinaryListLoader<Subcategory>(new AesProcessor(_aesKey));
    }
}

public sealed class BinaryDatedSource(ICryptoProcessor processor) : IDatedSource<FinanceRecord>
{
    private readonly BinaryClassLoader<FinanceRecord> _loader = new(processor);

    private static string GetFileName(string dataFolderPath, int key) => dataFolderPath + key + ".bin";

    public FinanceRecord Load(IEnumerable<DbFileWithDate> files)
    {
        return _loader.Load(files.First().FileName, false);
    }

    public void Save(FinanceRecord value, string dataFolderPath, int key)
    {
        var fileName = GetFileNames(dataFolderPath, key).First().FileName;
        _loader.Save(value, fileName, false);
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