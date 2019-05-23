using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SmartDevelopment.HttpClientExtensions
{
    public static class Extensions
    {
        private static async Task<TResult> SendAsync<TModel, TResult>(HttpClient client, string url, TModel model, HttpMethod method, string authToken, JsonSerializer jsonSerializer = null)
             where TModel : class
            where TResult : class
        {
            var request = new HttpRequestMessage
            {
                Method = method,
                RequestUri = new Uri(client.BaseAddress, url),
            };

            if (model != null)
            {
                request.Content = JsonContent.Create(model);
            }

            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }

            using (var response = await client.SendAsync(request).ConfigureAwait(false))
            {
                return await Deserialize<TResult>(response, jsonSerializer).ConfigureAwait(false);
            }
        }

        public static Task<TResult> PatchAsync<TModel, TResult>(this HttpClient client, string url, TModel model, string authToken = null, JsonSerializer jsonSerializer = null)
            where TModel : class
            where TResult : class
        {
            return SendAsync<TModel, TResult>(client, url, model, new HttpMethod("PATCH"), authToken, jsonSerializer);
        }

        public static Task<TResult> PutAsync<TModel, TResult>(this HttpClient client, string url, TModel model, string authToken = null, JsonSerializer jsonSerializer = null)
            where TModel : class
            where TResult : class
        {
            return SendAsync<TModel, TResult>(client, url, model, HttpMethod.Put, authToken, jsonSerializer);
        }

        public static Task<TResult> PostAsync<TModel, TResult>(this HttpClient client, string url, TModel model, string authToken = null, JsonSerializer jsonSerializer = null)
            where TModel : class
            where TResult : class
        {
            return SendAsync<TModel, TResult>(client, url, model, HttpMethod.Post, authToken, jsonSerializer);
        }

        public static Task<TResult> GetAsync<TResult>(this HttpClient client, string url, string authToken = null, JsonSerializer jsonSerializer = null)
            where TResult : class
        {
            return SendAsync<object, TResult>(client, url, null, HttpMethod.Get, authToken, jsonSerializer);
        }
            
        public static Task DeleteAsync(this HttpClient client, string url, string authToken = null, JsonSerializer jsonSerializer = null)
        {
            return SendAsync<object, object>(client, url, null, HttpMethod.Delete, authToken, jsonSerializer);
        }

        private static readonly JsonSerializer _serializer = new JsonSerializer();

        private static async Task<TObject> Deserialize<TObject>(HttpResponseMessage response, JsonSerializer jsonSerializer = null) where TObject : class
        {
            jsonSerializer = jsonSerializer ?? _serializer;

            await ThrowIfNotSuccess(response).ConfigureAwait(false);

            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                try
                {
                    return jsonSerializer.Deserialize<TObject>(jsonTextReader);
                }
                catch (Exception ex)
                {
                    var exception = new InvalidDataException("failed to deserialize response", ex);

                    string rawResponse = null;
                    try
                    {
                        stream.Position = 0;
                        rawResponse = await sr.ReadLineAsync().ConfigureAwait(false);
                        exception.Data.Add("rawResponse", rawResponse);
                    }
                    catch { }

                    throw exception;
                }
            }
        }

        private static async Task ThrowIfNotSuccess(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var exception = new HttpRequestException
                {
                    StatusCode = response.StatusCode.ToString(),
                    ReasonPhrase = response.ReasonPhrase,
                    Uri = response.RequestMessage.RequestUri.ToString()
                };

                try
                {
                    exception.RawResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch { }

                throw exception;
            }
        }
    }
}
