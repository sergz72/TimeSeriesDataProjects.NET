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
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    switch (args[1]) {
        case "test_json":
            var db = new Db(args[0], new JsonDbConfiguration(), 1000000);
            db.LoadAll();
            stopwatch.Stop();
            Console.WriteLine("Database loaded in {0} ms", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
            stopwatch.Start();
            //db.Test(1000);
            db.CalculateTotals(0);
            stopwatch.Stop();
            Console.WriteLine("Totals calculation finished in {0} us", stopwatch.Elapsed.TotalMicroseconds);
            db.PrintChanges(int.Parse(args[2]));
            Console.WriteLine("Alive items count = {0}", db.ActiveItems);
            break;
        default:
            Usage();
            break;
    }
}
catch (Exception e)
{
    Console.WriteLine(e);
}

return;

void Usage()
{
    Console.WriteLine("Usage: home_accounting_db data_folder_path\n  test_json date\n  test date aes_key_file");
    Console.WriteLine("  migrate source_folder_path aes_key\n  server port rsa_key_file");
}
