using HomeAccountingDB.entities;
using TimeSeriesData;

namespace HomeAccountingDB;

internal interface IDbConfiguration
{
    IDatedSource<FinanceRecord> GetMainDataSource();
    IDataSource<List<Account>> GetAccountsSource();
    IDataSource<List<Category>> GetCategoriesSource();
    IDataSource<List<Subcategory>> GetSubcategoriesSource();
}

internal sealed class Db: TimeSeriesData<FinanceRecord>
{
    private readonly Accounts _accounts;
    private readonly Categories _categories;
    private readonly Subcategories _subcategories;
    
    internal Db(string dataFolderPath, IDbConfiguration configuration, int maxItems):
        base(Path.Combine(dataFolderPath, "dates"), configuration.GetMainDataSource(), maxItems)
    {
        _accounts = new Accounts(dataFolderPath, configuration.GetAccountsSource());
        _categories = new Categories(dataFolderPath, configuration.GetCategoriesSource());
        _subcategories = new Subcategories(dataFolderPath, configuration.GetSubcategoriesSource());
    }

    internal void CalculateTotals(int from)
    {
        FinanceChanges? changes = null;
        foreach (var kv in Load(from, int.MaxValue))
        {
            if (changes == null)
                changes = new FinanceChanges(kv.Item2.Totals);
            else
                kv.Item2.Totals = changes.BuildTotals();
            kv.Item2.UpdateChanges(changes, _accounts, _subcategories);
            MarkAsModified(kv.key);
        }
    }
    
    internal void PrintChanges(int date)
    {
        if (date == 0)
            date = int.MaxValue;
        var record = Load(date);
        var changes = record!.BuildChanges(_accounts, _subcategories);
        foreach (var change in changes.Changes)
        {
            Console.WriteLine("{0} {1} {2} {3} {4}",
                _accounts.Get(change.Key).Name,
                change.Value.Summa,
                change.Value.Income,
                change.Value.Expenditure,
                change.Value.GetEndSumma());
        }
    }

    internal void Test(int keys)
    {
        for (var i = 0; i < keys; i++)
            Add(i, new FinanceRecord(200));
    }

    protected override int CalculateKey(int date)
    {
        return date / 100;
    }

    internal void Save(IDbConfiguration configuration, string dataFolderPath)
    {
        _accounts.SaveAll(configuration.GetAccountsSource(), dataFolderPath);
        _categories.SaveAll(configuration.GetCategoriesSource(), dataFolderPath);
        _subcategories.SaveAll(configuration.GetSubcategoriesSource(), dataFolderPath);
        SaveAll(configuration.GetMainDataSource(), Path.Combine(dataFolderPath, "dates"));
    }
}