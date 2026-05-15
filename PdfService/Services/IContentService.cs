using Microsoft.EntityFrameworkCore;
using PdfService.DAL;
using System.Data;

namespace PdfService.Services
{
    public interface IContentService
    {
        Task<Pdf[]> GetAll();

        Task<Pdf?> Get(Guid id);

        Task<Guid> Create();

        Task AppendText(Guid id, string text);
        Task AppendTextAndComplete(Guid id, string text);

        Task SetFailed(Guid id);

        Task<IPdfTextReader> StreamText(Guid id);
    }

    public interface IPdfTextReader : IAsyncDisposable, IDisposable
    {
        Task<bool> ReadAsync();
        Stream GetStream(int ordinal);
    }

}
