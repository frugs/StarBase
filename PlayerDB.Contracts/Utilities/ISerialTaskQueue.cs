namespace PlayerDB.Utilities;

public interface ISerialTaskQueue
{
    Task<T> Enqueue<T>(Func<Task<T>> taskProvider, CancellationToken cancellation = default);

    Task Enqueue(Func<Task> taskProvider, CancellationToken cancellation = default);
}