
using Common.Contracts;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;

namespace FileStorage.Services
{
    public class LocalStorageService : IStorageService
    {
        public void Delete(Guid fileUID)
        {
            var fileName = $"{fileUID}.pdf";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), Constants.UPLOAD_DIR_NAME, fileName);
            File.Delete(filePath);
        }
        public async Task<Guid> Save(PipeReader reader, CancellationToken cancellationToken)
        {
            var fileUID = Guid.NewGuid();
            var fileName = $"{fileUID}.pdf";
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), Constants.UPLOAD_DIR_NAME, fileName);
            using (var fs = new FileStream(savePath, 
                FileMode.CreateNew, 
                FileAccess.Write, 
                FileShare.None, 
                4096)) 
            {
                ReadResult result;
                do
                {
                    result = await reader.ReadAsync(cancellationToken);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    try
                    {
                        foreach (var bufferSegment in buffer)
                        {
                            await fs.WriteAsync(bufferSegment, cancellationToken);
                        }
                    }
                    finally
                    {
                        reader.AdvanceTo(buffer.End);
                    }

                } while (!result.IsCompleted);
            }

            return fileUID;
        }
    }
}
