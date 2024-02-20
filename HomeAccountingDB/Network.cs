using System.Diagnostics;
using HomeAccountingDB.entities;
using NetworkAndCrypto;

namespace HomeAccountingDB;

internal enum DbCommand
{
    Dicts,
    GetOps,
    ModifyOp,
    AddOp,
    DeleteOp
}

internal class CommandDecoder(string dataFolderPath, int maxTimeEntries): ICommandDecoder
{
    private Db? _database;
    private DbCommand _command = DbCommand.Dicts;
    private int _operationId;
    private int _date1;
    private FinanceOperation? _operation;
    
    public ICommandDecoder Build(byte[] command)
    {
        var cmd = command[0];
        if (!Enum.IsDefined(typeof(DbCommand), cmd))
            throw new NetworkException("wrong database command");
        int date;
        int opId;
        FinanceOperation? op;
        switch ((DbCommand)cmd)
        {
            case DbCommand.Dicts:
                CheckLength(command.Length, 1, 1);
                return new CommandDecoder(dataFolderPath, maxTimeEntries)
                    { _database = this._database, _command = DbCommand.Dicts };
            case DbCommand.GetOps:
                CheckLength(command.Length, 5, 5); // 1 - command id + 4 - date length
                date = BitConverter.ToInt32(command, 1);
                return new CommandDecoder(dataFolderPath, maxTimeEntries)
                    { _database = this._database, _command = DbCommand.GetOps, _date1 = date };
            case DbCommand.ModifyOp:
                CheckLength(command.Length, 10, int.MaxValue);
                date = BitConverter.ToInt32(command, 1);
                opId = BitConverter.ToInt32(command, 5);
                op = FinanceOperation.Create(command, 9);
                return new CommandDecoder(dataFolderPath, maxTimeEntries)
                    { _database = this._database, _command = DbCommand.ModifyOp, _date1 = date, _operationId = opId, _operation = op };
            case DbCommand.AddOp:
                CheckLength(command.Length, 6, int.MaxValue);
                date = BitConverter.ToInt32(command, 1);
                op = FinanceOperation.Create(command, 5);
                return new CommandDecoder(dataFolderPath, maxTimeEntries)
                    { _database = this._database, _command = DbCommand.AddOp, _date1 = date, _operation = op };
            case DbCommand.DeleteOp:
                CheckLength(command.Length, 9, 9); // 1 - command id + 4 - date length + 4 - operation id length
                date = BitConverter.ToInt32(command, 1);
                opId = BitConverter.ToInt32(command, 5);
                return new CommandDecoder(dataFolderPath, maxTimeEntries)
                    { _database = this._database, _command = DbCommand.DeleteOp, _date1 = date, _operationId = opId};
            default:
                throw new NetworkException("unknown database command");
        }
    }

    private static void CheckLength(int l, int from, int to)
    {
        if (l < from || l > to)
            throw new NetworkException("wrong database command length");
    }

    public byte[]? ExecuteCommand()
    {
        return _command switch
        {
            DbCommand.Dicts => PacketHandler.BuildResponse(() => _database!.GetDicts()),
            DbCommand.GetOps => PacketHandler.BuildResponse(() => _database!.GetOps(_date1)),
            DbCommand.ModifyOp => PacketHandler.BuildResponse(() => _database!.GetDicts()),
            DbCommand.AddOp => PacketHandler.BuildResponse(() => _database!.GetDicts()),
            DbCommand.DeleteOp => PacketHandler.BuildResponse(() => _database!.DeleteOperation(_date1, _operationId)),
            _ => throw new NetworkException("unknown database command")
        };
    }

    public void DatabaseInit(byte[] aesKey)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        _database = new Db(dataFolderPath, new BinaryDbConfiguration(aesKey), maxTimeEntries);
        _database.Init();
        stopwatch.Stop();
        Console.WriteLine("Database initialized in {0} ms", stopwatch.ElapsedMilliseconds);
    }
}
