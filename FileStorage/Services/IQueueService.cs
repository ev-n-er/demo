using Common.Contracts;
using static Common.RabbitClient;

namespace FileStorage.Services
{
    public interface IQueueService: IHostedService
    {
        Task SignalUploadReady(Guid id);

        Task ListenFileProcessed(HandleMessage<FileProcessedMessage?> handler, CancellationToken cancellationToken);
    }
}
