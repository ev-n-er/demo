using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfService.Controllers.Models;
using PdfService.Services;

namespace PdfService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PdfController : ControllerBase
    {
        private readonly IContentService _content;
        private readonly ILogger<PdfController> _logger;

        public PdfController(IContentService content, ILogger<PdfController> logger)
        {
            _logger = logger;
            _content = content;
        }

        [HttpGet()]
        public async Task<ActionResult<IEnumerable<PdfItem>>> Get()
        {
            try
            {
                var body = (await _content.GetAll()).Select(x => new PdfItem
                {
                    Id = x.Id.ToString(),
                    Status = x.Status.ToString(),
                });

                return Ok(body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return Problem(statusCode: 500);
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<string>> Details(Guid id)
        {
            try
            {
                using (var reader = await _content.StreamText(id))
                {
                    Response.ContentType = "text/plain";
                    if(await reader.ReadAsync())
                    {
                        Stream stream = reader.GetStream(0);

                        await stream.CopyToAsync(Response.Body);
                        await Response.Body.FlushAsync();
                        return Ok();
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return Problem(statusCode: 500);
            }
        }

    }
}
