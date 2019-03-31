﻿using System;
using System.IO;
using System.Threading.Tasks;
using SmartDevelopment.Logging;

namespace SmartDevelopment.AzureStorage.Blobs
{
    public class BlobTransfer
    {
        private readonly ILogger _logger;

        public BlobTransfer(ILogger<BlobTransfer> logger)
        {
            _logger = logger;
        }

        public async Task<StorageItem> Move(IFileStorage from, IFileStorage to, Guid fileId)
        {
            var file = await from.Get(fileId).ConfigureAwait(false);
            if (file == null)
            {
                throw new FileNotFoundException($"File {fileId} not found in {from.ContainerName}");
            }
            var newFile = await to.Save(file.Stream, null, file.ContentType, file.Metadata).ConfigureAwait(false);

            try
            {
                await from.Remove(fileId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }

            return newFile;
        }
    }
}