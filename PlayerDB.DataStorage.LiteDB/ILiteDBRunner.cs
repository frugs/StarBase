using LiteDB;

namespace PlayerDB.DataStorage.LiteDB;

public interface ILiteDBRunner
{
    Task<T> Perform<T>(Func<ILiteDatabase, T> dbOperation, CancellationToken cancellation = default);

    Task Perform(Action<ILiteDatabase> dbOperation, CancellationToken cancellation = default);
}