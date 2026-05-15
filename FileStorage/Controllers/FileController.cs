using FileStorage.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileStorage.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : ControllerBase
    {

        private readonly ILogger<FileController> _logger;
        private readonly IStorageService _storage;
        private readonly IQueueService _queue;

        public FileController(IStorageService storage, IQueueService queue, ILogger<FileController> logger)
        {
            _logger = logger;
            _storage = storage;
            _queue = queue;
        }

        [HttpPost]
        [Route("pdf")]
        [RequestSizeLimit(1024*1024)]
        public async Task<IActionResult> Upload()
        {
            if (Request.ContentType != "application/pdf")
            {
                return BadRequest("PDF expected.");
            }
            
            var tempFileId = await _storage.Save(Request.BodyReader, HttpContext.RequestAborted);
            await _queue.SignalUploadReady(tempFileId);

            return Accepted();
        }
    }
}
