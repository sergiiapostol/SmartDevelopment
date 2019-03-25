using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartDevelopment.AzureStorage.Blobs
{
    public interface IFileStorage
    {
        Task<StorageItem> Save(Stream stream, string fileExtension, Dictionary<string, string> metadata = null);
        Task<Stream> Get(string id);
        Task Remove(string id);
        Task Init();
    }

    public class StorageItem
    {
        public string Id { get; set; }

        public string Uri { get; set; }
    }
}
