using System.IO.Pipelines;

namespace FileStorage.Services
{
    public interface IStorageService
    {
        Task<Guid> Save(PipeReader reader, CancellationToken cancellationToken);
        void Delete(Guid fileUID);
    }
}
