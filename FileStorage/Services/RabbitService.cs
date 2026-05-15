using Common;
using Common.Contracts;

namespace FileStorage.Services
{
    public class RabbitService:RabbitClient, IQueueService
    {
        protected override string QueueName => "filestorage_queue";
        protected override string[] ConsumeKeys => [Keys.FileProcessed.ToString()];

        public RabbitService(IConfiguration configuration) : base(configuration["RabbitMQ:Host"] ?? "")
        {

        }

        public Task SignalUploadReady(Guid id) => base.SendDirect(Keys.FileUploaded.ToString(), new FileUploadedMessage { TempId = id });

        public Task ListenFileProcessed(HandleMessage<FileProcessedMessage?> handler, CancellationToken cancellationToken) => base.Listen(handler, cancellationToken);

    }
}
