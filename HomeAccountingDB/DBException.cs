namespace HomeAccountingDB;

internal class DbException: Exception
{
    internal DbException(string message) : base(message)
    {
    }
}