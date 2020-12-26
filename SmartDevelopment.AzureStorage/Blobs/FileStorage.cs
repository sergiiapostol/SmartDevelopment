using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace SmartDevelopment.AzureStorage.Blobs
{
    public interface IFileStorage
    {
        string ContainerName { get; }
        Task<StorageItem> Save(Stream stream, string fileExtension = null, string contentType = null, 
            IDictionary<string, string> metadata = null, int? cacheDuration = null);
        Task<StorageItem> Get(Guid id);
        Task Remove(Guid id);
        Task Init();
    }

    public abstract class BaseFileStorage : IFileStorage
    {
        protected readonly BlobContainerClient Container;

        private readonly IEnumerable<IContentTypeResolver> _contentTypeMappers;

        public string ContainerName { get; }

        public int? CacheDuration { get; }

        protected BaseFileStorage(ConnectionSettings connectionSettings, string containerName, IEnumerable<IContentTypeResolver> contentTypeMappers, int? cacheDuration = null)
        {
            ContainerName = containerName.ToLower();
            CacheDuration = cacheDuration;
            Container = new BlobContainerClient(connectionSettings.ConnectionString, ContainerName);
            _contentTypeMappers = contentTypeMappers;
        }

        public virtual Task Init()
        {
            return Container.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
        }

        public Task Remove(Guid id)
        {
            var blob = Container.GetBlobClient(id.ToString());
            return blob.DeleteIfExistsAsync();
        }

        public async Task<StorageItem> Save(Stream stream, string fileExtension = null, string contentType = null,
            IDictionary<string, string> metadata = null, int? cacheDuration = null)
        {
            var id = Guid.NewGuid();

            var blob = Container.GetBlobClient(id.ToString());
            await blob.UploadAsync(stream);

            var headers = new Azure.Storage.Blobs.Models.BlobHttpHeaders { };
            contentType ??= _contentTypeMappers?.Select(v => v.GetContentType(fileExtension)).FirstOrDefault(v => !string.IsNullOrEmpty(v));
            if (!string.IsNullOrEmpty(contentType))
                headers.ContentType = contentType;
            if(!string.IsNullOrEmpty(fileExtension))
                headers.ContentDisposition = $"attachment; filename=\"{ id}{fileExtension}\"";
            if(cacheDuration.HasValue)
                headers.CacheControl = $"public, max-age={cacheDuration ?? CacheDuration}";
            var headersTask = blob.SetHttpHeadersAsync(headers);            
            
            metadata ??= new Dictionary<string, string>();
            metadata["CreatedAt"] = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            if(!string.IsNullOrWhiteSpace(fileExtension))
                metadata["Extension"] = fileExtension;
            var metadataTask = blob.SetMetadataAsync(metadata);

            await Task.WhenAll(headersTask, metadataTask);

            return new StorageItem(id, blob.Uri.ToString(), stream, contentType, metadata);
        }

        public async Task<StorageItem> Get(Guid id)
        {
            var blobReference = Container.GetBlobClient(id.ToString());            
            var blob = await blobReference.DownloadAsync();
            if (blob?.Value == null)
                return null;

            blob.Value.Content.Position = 0;

            return new StorageItem(id, blobReference.Uri.ToString(), blob.Value.Content, blob.Value.ContentType, blob.Value.Details.Metadata);
        }
    }
}
