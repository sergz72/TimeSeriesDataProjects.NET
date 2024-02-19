using System.Diagnostics;
using SmartHome;

var l = args.Length;

if (l is < 2 or > 4)
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
    Console.WriteLine("Usage: SmartHome data_folder_path\n  test_json\n  test aes_key_file");
    Console.WriteLine("  migrate source_folder_path aes_key\n  server port rsa_key_file");
}