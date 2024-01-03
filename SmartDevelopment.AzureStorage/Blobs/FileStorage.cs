using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            var client = new BlobServiceClient(connectionSettings.ConnectionString);
            Container = client.GetBlobContainerClient(ContainerName);
            _contentTypeMappers = contentTypeMappers;
        }

        public virtual Task Init()
        {
            return Container.CreateIfNotExistsAsync(PublicAccessType.Blob);
        }

        public Task Remove(Guid id)
        {
            var blob = Container.GetBlobClient(id.ToString());
            return blob.DeleteAsync();
        }

        public async Task<StorageItem> Save(Stream stream, string fileExtension = null, string contentType = null,
            IDictionary<string, string> metadata = null, int? cacheDuration = null)
        {
            var id = Guid.NewGuid();

            var blob = Container.GetBlobClient(id.ToString());

            await blob.UploadAsync(stream);

            var headers = new BlobHttpHeaders() 
            { 
                ContentType = contentType ??
                _contentTypeMappers?.Select(v => v.GetContentType(fileExtension)).FirstOrDefault(v => !string.IsNullOrEmpty(v)),
                ContentDisposition = $"attachment; filename=\"{id}{fileExtension}\""
            };        

            if ((cacheDuration ?? CacheDuration).HasValue)
                headers.CacheControl = $"public, max-age={(cacheDuration ?? CacheDuration)}";            

            metadata ??= new Dictionary<string, string>();
            metadata["CreatedAt"] = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            if(!string.IsNullOrWhiteSpace(fileExtension))
                metadata["Extension"] = fileExtension;
            
            await Task.WhenAll(
                blob.SetHttpHeadersAsync(headers), 
                blob.SetMetadataAsync(metadata));

            return new StorageItem(id, blob.Uri.ToString(), stream, contentType, metadata);
        }

        public async Task<StorageItem> Get(Guid id)
        {
            var blob = Container.GetBlobClient(id.ToString());
            if (!await blob.ExistsAsync())
            {
                return null;
            }

            var stream = new MemoryStream();
            await blob.DownloadToAsync(stream);
            stream.Position = 0;
            
            var properties = await blob.GetPropertiesAsync();
            return new StorageItem(id, blob.Uri.ToString(), stream, properties.Value.ContentType, properties.Value.Metadata);
        }
    }
}
