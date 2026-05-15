using Common.Contracts;
using PdfService.Services;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PdfService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IQueueService _queueService;

        private readonly IServiceScopeFactory _scopeFactory;

        public Worker(IServiceScopeFactory scopeFactory, IQueueService queueService, ILogger<Worker> logger)
        {
            _scopeFactory = scopeFactory;
            _queueService = queueService;
            _logger = logger;
        }

        private async ValueTask<bool> HandleUpload(string key, FileUploadedMessage? message)
        {
            if (message == null)
            {
                _logger.LogWarning("Unexpected empty message");
                return true;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var contentService = scope.ServiceProvider.GetRequiredService<IContentService>();
                var itemId = await contentService.Create();
                try
                {
                    var fileName = $"{message.TempId}.pdf";
                    var pdfPath = Path.Combine(Directory.GetCurrentDirectory(), Constants.UPLOAD_DIR_NAME, fileName);

                    using (PdfDocument document = PdfDocument.Open(pdfPath))
                    {
                        const int lohThresholdBytes = 85000;
                        var chunk = string.Empty;
                        foreach(var page in document.GetPages())
                        {
                            if(page.Text.Length + chunk.Length >= lohThresholdBytes)
                            {
                                await contentService.AppendText(itemId, chunk);

                                chunk = $"{page.Text}{Environment.NewLine}";
                            }
                            else
                            {
                                chunk = $"{chunk}{page.Text}{Environment.NewLine}";
                            }
                        }
                        await contentService.AppendTextAndComplete(itemId, chunk);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                    await contentService.SetFailed(itemId);
                }

                await _queueService.SignalProcessingReady(message.TempId);
            } 

            return true;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _queueService.ListenFileUpload(this.HandleUpload, stoppingToken);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
