using System;
using System.IO;
using System.Threading.Tasks;
using SmartDevelopment.Logging;

namespace SmartDevelopment.AzureStorage.Blobs
{
    public class BlobTransfer
    {
        private readonly ILogger _logger;

        private readonly IFileStorage _from;

        private readonly IFileStorage _to;

        public BlobTransfer(ILogger<BlobTransfer> logger, IFileStorage from, IFileStorage to)
        {
            _logger = logger;
            _from = from;
            _to = to;
        }

        public async Task<StorageItem> Move(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
                throw new ArgumentNullException(nameof(fileId));

            var file = await _from.Get(fileId).ConfigureAwait(false);
            if (file == null)
            {
                throw new FileNotFoundException($"File {fileId} not found in {_from.ContainerName}");
            }
            var newFile = await _to.Save(file.Stream, null, file.ContentType, file.Metadata).ConfigureAwait(false);

            try
            {
                await _from.Remove(fileId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }

            return newFile;
        }
    }
}