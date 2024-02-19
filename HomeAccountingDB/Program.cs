using System.Diagnostics;
using HomeAccountingDB;

var l = args.Length;

if (l is < 3 or > 4)
{
    Usage();
    return;
}

try
{
    Console.WriteLine("Loading database...");
    try
    {
        switch (args[1])
        {
            case "test_json":
                if (args.Length != 3)
                    Usage();
                else
                    Test(new JsonDbConfiguration(), args[2]);
                break;
            case "test":
                if (args.Length != 4)
                    Usage();
                else
                    Test(new BinaryDbConfiguration(args[2]), args[3]);
                break;
            case "migrate":
                if (args.Length != 4)
                    Usage();
                else
                    Migrate(args[2], args[0], args[3]);
                break;
            default:
                Usage();
                break;
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}
catch (Exception e)
{
    Console.WriteLine(e);
}

return;

void Migrate(string from, string to, string aesKeyFile)
{
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    var db = new Db(from, new JsonDbConfiguration(), 1000000);
    db.LoadAll();
    stopwatch.Stop();
    Console.WriteLine("Database loaded in {0} ms", stopwatch.ElapsedMilliseconds);
    stopwatch.Reset();
    stopwatch.Start();
    db.Save(new BinaryDbConfiguration(aesKeyFile), to);
    stopwatch.Stop();
    Console.WriteLine("Database saved in {0} ms", stopwatch.ElapsedMilliseconds);
}

void Test(IDbConfiguration configuration, string date)
{
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    var db = new Db(args[0], configuration, 1000000);
    db.LoadAll();
    stopwatch.Stop();
    Console.WriteLine("Database loaded in {0} ms", stopwatch.ElapsedMilliseconds);
    stopwatch.Reset();
    stopwatch.Start();
    db.CalculateTotals(0);
    stopwatch.Stop();
    Console.WriteLine("Totals calculation finished in {0} us", stopwatch.Elapsed.TotalMicroseconds);
    db.PrintChanges(int.Parse(date));
    Console.WriteLine("Alive items count = {0}", db.ActiveItems);
}

void Usage()
{
    Console.WriteLine("Usage: HomeAccountingDB data_folder_path\n  test_json date\n  test date aes_key_file");
    Console.WriteLine("  migrate source_folder_path aes_key\n  server port rsa_key_file");
}
