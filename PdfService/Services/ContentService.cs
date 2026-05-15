using Microsoft.EntityFrameworkCore;
using PdfService.DAL;
using System.Data;
using System.Data.Common;

namespace PdfService.Services
{
    public class ContentService : IContentService
    {
        private readonly AppDBContext _dbContext;

        public ContentService(AppDBContext dbContext)
        {
            _dbContext = dbContext;
        }
        public Task<Pdf[]> GetAll() => _dbContext.Pdfs
            .Select(x => new Pdf { Id = x.Id, Text = null, Status = x.Status})
            .ToArrayAsync();

        public Task<Pdf?> Get(Guid id) => _dbContext.Pdfs.SingleOrDefaultAsync(x => x.Id == id);

        public async Task<IPdfTextReader> StreamText(Guid id)
        {
            var reader = await _dbContext.GetPdfTextReader(id);
            return new PdfTextReader(reader);
        }

        public async Task<Guid> Create()
        {
            var newItem = new Pdf
            {
                Text = string.Empty,
                Status = StatusEnum.Queued
            };

            await _dbContext.Pdfs.AddAsync(newItem);
            await _dbContext.SaveChangesAsync();

            return newItem.Id;
        }

        public async Task AppendText(Guid id, string text)
        {
            await _dbContext.Pdfs.Where(x => x.Id == id)
                .ExecuteUpdateAsync(setter => setter
                .SetProperty(x => x.Text, x=> x.Text + text));
        }

        public async Task AppendTextAndComplete(Guid id, string text)
        {
            await _dbContext.Pdfs.Where(x => x.Id == id)
                .ExecuteUpdateAsync(setter => setter
                .SetProperty(x => x.Text, x => x.Text + text)
                .SetProperty(x => x.Status, StatusEnum.Ready));

        }

        public async Task SetFailed(Guid id)
        {
            await _dbContext.Pdfs.Where(x => x.Id == id)
                .ExecuteUpdateAsync(setter => setter
                .SetProperty(x => x.Status, StatusEnum.Failed));
        }
    }

    public class PdfTextReader : IPdfTextReader
    {
        private readonly DbDataReader _reader;

        public PdfTextReader(DbDataReader reader)
        {
            _reader = reader;

        }

        public Task<bool> ReadAsync() => _reader.ReadAsync();

        public Stream GetStream(int ordinal) => _reader.GetStream(ordinal);

        public ValueTask DisposeAsync() => _reader.DisposeAsync();

        public void Dispose() => _reader.Dispose();
    }

}

