﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDevelopment.AzureStorage.Blobs
{
    public sealed class BlobsInitializator
    {
        private IEnumerable<IFileStorage> _storages { get; }

        private readonly ILogger<BlobsInitializator> _logger;

        public BlobsInitializator(IEnumerable<IFileStorage> storages, ILogger<BlobsInitializator> logger)
        {
            _storages = storages;
            _logger = logger;
        }

        public async Task Init()
        {
            foreach (var store in _storages)
            {
                try
                {
                    await store.Init();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"failed to init blob {store.GetType().FullName}");
                    throw;
                }
            }
        }
    }
}
