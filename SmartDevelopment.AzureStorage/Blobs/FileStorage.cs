using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace SmartDevelopment.AzureStorage.Blobs
{
    public abstract class BaseFileStorage : IFileStorage
    {
        protected readonly CloudBlobContainer Container;

        private readonly IContentTypeResolver _contentTypeMapper;

        protected BaseFileStorage(IOptions<ConnectionSettings> connectionSettings, string containerName, IContentTypeResolver contentTypeMapper)
        {
             var account = CloudStorageAccount.Parse(connectionSettings.Value.ConnectionString);
            var client = account.CreateCloudBlobClient();
            Container = client.GetContainerReference(containerName);
            _contentTypeMapper = contentTypeMapper;
        }

        public abstract Task Init();

        public async Task<Stream> Get(string id)
        {
            var blob = Container.GetBlockBlobReference(id);
            if (!await blob.ExistsAsync().ConfigureAwait(false))
            {
                return null;
            }

            var stream = new MemoryStream();
            await blob.DownloadToStreamAsync(stream).ConfigureAwait(false);
            stream.Position = 0;

            return stream;
        }

        public Task Remove(string id)
        {
            var blob = Container.GetBlockBlobReference(id);
            return blob.DeleteAsync();
        }

        public async Task<StorageItem> Save(Stream stream, string fileExtension,
            Dictionary<string, string> metadata = null)
        {
            var id = Guid.NewGuid().ToString();

            stream.Position = 0;
            var blob = Container.GetBlockBlobReference(id);

            await blob.UploadFromStreamAsync(stream).ConfigureAwait(false);

            blob.Properties.ContentType = _contentTypeMapper.GetContentType(fileExtension);
            await blob.SetPropertiesAsync().ConfigureAwait(false);

            metadata = metadata ?? new Dictionary<string, string>();
            metadata.Add("CreatedAt", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

            foreach (var data in metadata)
            {
                blob.Metadata.Add(data.Key, data.Value);
            }
            await blob.SetMetadataAsync().ConfigureAwait(false);

            return new StorageItem {Id = id, Uri = blob.Uri.ToString() };
        }
    }
}
