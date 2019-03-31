using System;
using System.Collections.Generic;
using System.IO;

namespace SmartDevelopment.AzureStorage.Blobs
{
    public class StorageItem : IDisposable
    {
        public StorageItem(Guid id, string uri, Stream stream, string contentType, IDictionary<string, string> metadata)
        {
            Id = id;
            Uri = uri;
            Stream = stream;
            Metadata = metadata;
            ContentType = contentType;
        }

        public Guid Id { get; }

        public string Uri { get; }

        public string ContentType { get; }

        public Stream Stream { get; }

        public IDictionary<string, string> Metadata { get; }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}
