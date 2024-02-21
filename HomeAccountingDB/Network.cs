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

internal readonly struct NetworkCommand(DbCommand command, int date1 = 0, int operationId = 0, FinanceOperation? operation = null) : INetworkCommand
{
    public byte[]? ExecuteCommand(ICommandDecoder decoder)
    {
        var db = ((CommandDecoder)decoder).Database!;
        var d1 = date1;
        var opId = operationId;
        return command switch
        {
            DbCommand.Dicts => PacketHandler.BuildResponse(() => db.GetDicts()),
            DbCommand.GetOps => PacketHandler.BuildResponse(() => db.GetOps(d1)),
            DbCommand.ModifyOp => PacketHandler.BuildResponse(() => db.GetDicts()),
            DbCommand.AddOp => PacketHandler.BuildResponse(() => db.GetDicts()),
            DbCommand.DeleteOp => PacketHandler.BuildResponse(() => db.DeleteOperation(d1, opId)),
            _ => throw new NetworkException("unknown database command")
        };
    }
}

internal class CommandDecoder(string dataFolderPath, int maxTimeEntries): ICommandDecoder
{
    internal Db? Database;
    
    public INetworkCommand Build(BinaryReader command)
    {
        var cmd = command.ReadByte();
        if (!Enum.IsDefined(typeof(DbCommand), cmd))
            throw new NetworkException("wrong database command");
        int date;
        int opId;
        FinanceOperation? op;
        switch ((DbCommand)cmd)
        {
            case DbCommand.Dicts:
                return new NetworkCommand(DbCommand.Dicts);
            case DbCommand.GetOps:
                date = command.ReadInt32();
                return new NetworkCommand(DbCommand.GetOps, date);
            case DbCommand.ModifyOp:
                date = command.ReadInt32();
                opId = command.ReadInt32();
                op = FinanceOperation.Create(command);
                return new NetworkCommand(DbCommand.ModifyOp, date, opId, op);
            case DbCommand.AddOp:
                date = command.ReadInt32();
                op = FinanceOperation.Create(command);
                return new NetworkCommand(DbCommand.AddOp, date, 0, op);
            case DbCommand.DeleteOp:
                date = command.ReadInt32();
                opId = command.ReadInt32();
                return new NetworkCommand(DbCommand.AddOp, date, opId);
            default:
                throw new NetworkException("unknown database command");
        }
    }

    public void DatabaseInit(byte[] aesKey)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Database = new Db(dataFolderPath, new BinaryDbConfiguration(aesKey), maxTimeEntries);
        Database.Init();
        stopwatch.Stop();
        Console.WriteLine("Database initialized in {0} ms", stopwatch.ElapsedMilliseconds);
    }
}
