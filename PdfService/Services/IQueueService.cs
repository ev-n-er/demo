using Common.Contracts;
using static Common.RabbitClient;

namespace PdfService.Services
{
    public interface IQueueService : IHostedService
    {
        Task ListenFileUpload(HandleMessage<FileUploadedMessage?> handler, CancellationToken cancellationToken);
        Task SignalProcessingReady(Guid id);
    }
}
