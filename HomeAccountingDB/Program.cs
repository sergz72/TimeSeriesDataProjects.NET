using System.Diagnostics;
using HomeAccountingDB;
using NetworkAndCrypto;

const int MaxTimeEntries = 1000000;
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
                    Test(new JsonDbConfiguration(), args[2], true, MaxTimeEntries);
                break;
            case "test":
                if (args.Length != 4)
                    Usage();
                else
                    Test(new BinaryDbConfiguration(AesProcessor.LoadKeyFile(args[2])), args[3], false, MaxTimeEntries);
                break;
            case "migrate":
                if (args.Length != 4)
                    Usage();
                else
                    Migrate(args[2], args[0], args[3], MaxTimeEntries);
                break;
            case "server":
                if (args.Length != 4)
                    Usage();
                else
                    StartServer(args[0], int.Parse(args[2]), args[3], MaxTimeEntries);
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

void StartServer(string dataFolderPath, int port, string rsaKeyFile, int maxTimeEntries)
{
    var server = new TcpServer(port, rsaKeyFile, new AesKeyPacketHandler(new CommandDecoder(dataFolderPath, maxTimeEntries)));
    Console.WriteLine("Starting server on port {0}", port);
    server.Start();
}

void Migrate(string from, string to, string aesKeyFile, int maxTimeEntries)
{
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    var db = new Db(from, new JsonDbConfiguration(), maxTimeEntries);
    db.LoadAll();
    stopwatch.Stop();
    Console.WriteLine("Database loaded in {0} ms", stopwatch.ElapsedMilliseconds);
    stopwatch.Reset();
    stopwatch.Start();
    db.CalculateTotals(0);
    stopwatch.Stop();
    Console.WriteLine("Totals calculation finished in {0} us", stopwatch.Elapsed.TotalMicroseconds);
    stopwatch.Reset();
    stopwatch.Start();
    db.Save(new BinaryDbConfiguration(AesProcessor.LoadKeyFile(aesKeyFile)), to);
    stopwatch.Stop();
    Console.WriteLine("Database saved in {0} ms", stopwatch.ElapsedMilliseconds);
}

void Test(IDbConfiguration configuration, string date, bool calculateTotals, int maxTimeEntries)
{
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    var db = new Db(args[0], configuration, maxTimeEntries);
    stopwatch.Stop();
    Console.WriteLine("Database loaded in {0} ms", stopwatch.ElapsedMilliseconds);
    if (calculateTotals)
    {
        db.LoadAll();
        stopwatch.Reset();
        stopwatch.Start();
        db.CalculateTotals(0);
        stopwatch.Stop();
        Console.WriteLine("Totals calculation finished in {0} us", stopwatch.Elapsed.TotalMicroseconds);
    }
    else
        db.Init();
    db.PrintChanges(int.Parse(date));
    Console.WriteLine("Alive items count = {0}", db.ActiveItems);
}

void Usage()
{
    Console.WriteLine("Usage: HomeAccountingDB data_folder_path\n  test_json date\n  test date aes_key_file");
    Console.WriteLine("  migrate source_folder_path aes_key\n  server port rsa_key_file");
}
