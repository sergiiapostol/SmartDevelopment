using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace SmartDevelopment.Caching.OutputCaching
{
    public class OutputCacheManager
    {
        public void TagCache(HttpContext context, Dictionary<string, string> tags)
        {
            context.Items.TryAdd(Consts.CachedObjectTags, tags);
        }
    }
}
