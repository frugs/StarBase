using System.Threading.Channels;

namespace PlayerDB.Utilities;

public class SerialJobQueue(string queueName = "")
{
    private readonly SerialTaskQueue _taskQueue = new(queueName);

    public IAsyncEnumerable<T> Enqueue<T>(Func<ChannelWriter<T>, Task> job, CancellationToken cancellation = default)
    {
        var channel = Channel.CreateBounded<T>(
            new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

        _taskQueue.Enqueue(async () => 
        {
            try
            {
                await job(channel.Writer);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                channel.Writer.TryComplete(ex);
                return Task.CompletedTask;
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, cancellation);


        return channel.Reader.ReadAllAsync(cancellation);
    }
}