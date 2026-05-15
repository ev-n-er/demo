using Common.Contracts;
using FileStorage.Services;

namespace FileStorage
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IQueueService _queueService;
        private readonly IStorageService _storageService;

        public Worker( IQueueService queueService, IStorageService storageService, ILogger<Worker> logger)
        {
            _queueService = queueService;
            _storageService = storageService;
            _logger = logger;
        }

        private ValueTask<bool> HandleProcessed(string key, FileProcessedMessage? message)
        {
            if (message == null)
            {
                _logger.LogWarning("Unexpected empty message");
                return new ValueTask<bool>(true);
            }

            try
            {
                _storageService.Delete(message.TempId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            return new ValueTask<bool>(true);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _queueService.ListenFileProcessed(this.HandleProcessed, stoppingToken);
        }

    }
}
