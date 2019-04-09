using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace SmartDevelopment.AzureStorage.Blobs
{
    public interface IFileStorage
    {
        string ContainerName { get; }
        Task<StorageItem> Save(Stream stream, string fileExtension = null, string contentType = null, IDictionary<string, string> metadata = null);
        Task<StorageItem> Get(Guid id);
        Task Remove(Guid id);
        Task Init();
    }

    public abstract class BaseFileStorage : IFileStorage
    {
        protected readonly CloudBlobContainer Container;

        private readonly IEnumerable<IContentTypeResolver> _contentTypeMappers;

        public string ContainerName { get; }

        protected BaseFileStorage(ConnectionSettings connectionSettings, string containerName, IEnumerable<IContentTypeResolver> contentTypeMappers)
        {
            ContainerName = containerName.ToLower();
            var account = CloudStorageAccount.Parse(connectionSettings.ConnectionString);
            var client = account.CreateCloudBlobClient();
            Container = client.GetContainerReference(ContainerName);
            _contentTypeMappers = contentTypeMappers;
        }

        public virtual Task Init()
        {
            return Container.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, new BlobRequestOptions(), new   OperationContext());
        }

        public Task Remove(Guid id)
        {
            var blob = Container.GetBlockBlobReference(id.ToString());
            return blob.DeleteAsync();
        }

        public async Task<StorageItem> Save(Stream stream, string fileExtension = null, string contentType = null,
            IDictionary<string, string> metadata = null)
        {
            var id = Guid.NewGuid();

            var blob = Container.GetBlockBlobReference(id.ToString());

            await blob.UploadFromStreamAsync(stream).ConfigureAwait(false);

            blob.Properties.ContentType = contentType ??
                _contentTypeMappers?.Select(v => v.GetContentType(fileExtension)).FirstOrDefault(v => !string.IsNullOrEmpty(v));
            blob.Properties.ContentDisposition = $"attachment; filename=\"{ id}{fileExtension}\"";
            await blob.SetPropertiesAsync().ConfigureAwait(false);

            metadata = metadata ?? new Dictionary<string, string>();
            metadata["CreatedAt"] = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            if(!string.IsNullOrWhiteSpace(fileExtension))
                metadata["Extension"] = fileExtension;

            foreach (var data in metadata)
            {
                blob.Metadata.Add(data.Key, data.Value);
            }
            await blob.SetMetadataAsync().ConfigureAwait(false);

            return new StorageItem(id, blob.Uri.ToString(), stream, contentType, metadata);
        }

        public async Task<StorageItem> Get(Guid id)
        {
            var blob = Container.GetBlockBlobReference(id.ToString());
            if (!await blob.ExistsAsync().ConfigureAwait(false))
            {
                return null;
            }

            var stream = new MemoryStream();
            await blob.DownloadToStreamAsync(stream).ConfigureAwait(false);
            stream.Position = 0;

            return new StorageItem(id, blob.Uri.ToString(), stream, blob.Properties.ContentType, blob.Metadata);
        }
    }
}
