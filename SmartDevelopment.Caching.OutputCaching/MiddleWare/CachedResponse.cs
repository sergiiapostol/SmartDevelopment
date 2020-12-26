using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDevelopment.Caching.OutputCaching
{
    public partial class CachingMiddleware
    {
        internal class CachedResponse
        {
            public byte[] Content { get; private set; }
            public Dictionary<string, StringValues> Headers { get; private set; }

            public bool ForAnonymusUsers { get; set; }

            public CachedResponse(byte[] content, IHeaderDictionary headers)
            {
                Content = content;
                Headers = new Dictionary<string, StringValues>();
                foreach (var header in headers)
                {
                    Headers[header.Key] = header.Value;
                }
            }

            public async Task Apply(HttpContext context)
            {
                foreach (var header in Headers)
                {
                    context.Response.Headers[header.Key] = header.Value;
                }

                context.Items.TryAdd("OutputCache", true);

                await context.Response.Body.WriteAsync(Content, 0, Content.Length).ConfigureAwait(false);
            }
        }
    }
}
