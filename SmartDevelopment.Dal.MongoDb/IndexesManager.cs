using Microsoft.Extensions.Logging;
using SmartDevelopment.Dal.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDevelopment.Dal.MongoDb
{
    public class IndexesManager
    {
        private IEnumerable<IIndexedSource> IndexedSources { get; }

        private readonly ILogger<IndexesManager> _logger;

        public IndexesManager(IEnumerable<IIndexedSource> indexedSources, ILogger<IndexesManager> logger)
        {
            IndexedSources = indexedSources;
            _logger = logger;
        }

        public async Task UpdateIndexes()
        {
            foreach (var indexedSource in IndexedSources)
            {
                try
                {
                    await indexedSource.EnsureIndex();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"failed to create index for {indexedSource.GetType().FullName}");
                    throw;
                }
            }
        }
    }
}
