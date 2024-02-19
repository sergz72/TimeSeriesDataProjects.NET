using System.Text.Json;
using HomeAccountingDB.entities;
using TimeSeriesData;

namespace HomeAccountingDB;

public class JsonDbConfiguration: IDbConfiguration
{
    public IDatedSource<FinanceRecord> GetMainDataSource()
    {
        return new JsonDatedSource();
    }

    public IDataSource<List<Account>> GetAccountsSource()
    {
        return new JsonListLoader<Account>(null);
    }

    public IDataSource<List<Category>> GetCategoriesSource()
    {
        return new JsonListLoader<Category>(null);
    }

    public IDataSource<List<Subcategory>> GetSubcategoriesSource()
    {
        return new JsonListLoader<Subcategory>(null);
    }
}

public class JsonDatedSource : IDatedSource<FinanceRecord>
{
    private readonly JsonListLoader<FinanceOperation> _loader = new(new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true
    });

    public void Save(FinanceRecord value, int date)
    {
        throw new NotImplementedException();
    }

    public FinanceRecord Load(IEnumerable<DbFileWithDate> files)
    {
        var allData = new List<FinanceOperation>();
        foreach (var file in files)
        {
            var data = _loader.Load(file.FileName, false);
            foreach (var item in data)
                item.Date = file.Date;
            allData.AddRange(data);
        }

        return new FinanceRecord(allData);
    }

    public void Save(FinanceRecord value, string dataFolderPath, int key)
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