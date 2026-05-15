using Common;
using Common.Contracts;

namespace PdfService.Services
{
    public class RabbitService : RabbitClient, IQueueService
    {
        protected override string QueueName => "pdfservice_queue";
        protected override string[] ConsumeKeys => [Keys.FileUploaded.ToString()];

        public RabbitService(IConfiguration configuration): base(configuration["RabbitMQ:Host"] ?? "")
        {

        }

        public Task SignalProcessingReady(Guid id) => base.SendDirect(Keys.FileProcessed.ToString(), new FileProcessedMessage { TempId = id });

        public Task ListenFileUpload(HandleMessage<FileUploadedMessage?> handler, CancellationToken cancellationToken) => base.Listen(handler, cancellationToken);
    }
}
