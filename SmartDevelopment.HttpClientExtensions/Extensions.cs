using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SmartDevelopment.HttpClientExtensions
{
    public static class Extensions
    {
        private static async Task<TResult> SendAsync<TResult>(HttpClient client, string url, HttpMethod method, 
            HttpContent content, string authToken, JsonSerializer jsonSerializer = null, Dictionary<string, string> headers = null)
            where TResult : class
        {
            var request = new HttpRequestMessage
            {
                Method = method,
                RequestUri = new Uri(client.BaseAddress, url)
            };

            if (content != null)
                request.Content = content;

            if(headers?.Count > 0)
            {
                foreach (var header in headers)
                {
                    if (request.Headers.Contains(header.Key))
                        request.Headers.Remove(header.Key);

                    request.Headers.Add(header.Key, header.Value);
                }
            }

            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }

            using var response = await client.SendAsync(request).ConfigureAwait(false);
            return await Deserialize<TResult>(response, jsonSerializer).ConfigureAwait(false);
        }

        private static Task<TResult> SendAsync<TModel, TResult>(HttpClient client, string url, TModel model, 
            HttpMethod method, string authToken, JsonSerializer jsonSerializer = null, Dictionary<string, string> headers = null)
             where TModel : class
            where TResult : class
        {
            var content = model == null ? null : JsonContent.Create(model);
            return SendAsync<TResult>(client, url, method, content, authToken, jsonSerializer, headers);
        }

        public static Task<TResult> PatchAsync<TModel, TResult>(this HttpClient client, string url, TModel model, 
            string authToken = null, JsonSerializer jsonSerializer = null, Dictionary<string, string> headers = null)
            where TModel : class
            where TResult : class
        {
            return SendAsync<TModel, TResult>(client, url, model, new HttpMethod("PATCH"), authToken, jsonSerializer, headers);
        }

        public static Task<TResult> PutAsync<TModel, TResult>(this HttpClient client, string url, TModel model, 
            string authToken = null, JsonSerializer jsonSerializer = null, Dictionary<string, string> headers = null)
            where TModel : class
            where TResult : class
        {
            return SendAsync<TModel, TResult>(client, url, model, HttpMethod.Put, authToken, jsonSerializer, headers);
        }

        public static Task<TResult> PostAsync<TModel, TResult>(this HttpClient client, string url, TModel model, 
            string authToken = null, JsonSerializer jsonSerializer = null, Dictionary<string, string> headers = null)
            where TModel : class
            where TResult : class
        {
            return SendAsync<TModel, TResult>(client, url, model, HttpMethod.Post, authToken, jsonSerializer, headers);
        }

        public static Task<TResult> GetAsync<TResult>(this HttpClient client, string url, 
            string authToken = null, JsonSerializer jsonSerializer = null, Dictionary<string, string> headers = null)
            where TResult : class
        {
            return SendAsync<object, TResult>(client, url, null, HttpMethod.Get, authToken, jsonSerializer, headers);
        }
            
        public static Task DeleteAsync(this HttpClient client, string url, 
            string authToken = null, JsonSerializer jsonSerializer = null, Dictionary<string, string> headers = null)
        {
            return SendAsync<object, object>(client, url, null, HttpMethod.Delete, authToken, jsonSerializer, headers);
        }

        public class FormFileModel
        {
            public Stream Stream { get; set; }

            public string FileName { get; set; }

            public string ContentType { get; set; }

            public string ModelName { get; set; }
        }

        private static MultipartFormDataContent CreateForm(List<FormFileModel> files)
        {
            var form = new MultipartFormDataContent();

            foreach (var file in files)
            {
                var streamContent = new StreamContent(file.Stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                form.Add(streamContent, file.ModelName, file.FileName);
            }
            return form;
        }

        public static Task<TResult> PostFormFiles<TResult>(this HttpClient client, string url, List<FormFileModel> files, 
            string authToken = null, JsonSerializer jsonSerializer = null, Dictionary<string, string> headers = null)
            where TResult : class
        {
            return SendAsync<TResult>(client, url, HttpMethod.Post, CreateForm(files), authToken, jsonSerializer, headers);
        }

        public static Task<TResult> PutFormFiles<TResult>(this HttpClient client, string url, List<FormFileModel> files,
            string authToken = null, JsonSerializer jsonSerializer = null, Dictionary<string, string> headers = null)
            where TResult : class
        {
            return SendAsync<TResult>(client, url, HttpMethod.Put, CreateForm(files), authToken, jsonSerializer, headers);
        }

        private static readonly JsonSerializer _serializer = new JsonSerializer();

        private static async Task<TObject> Deserialize<TObject>(HttpResponseMessage response, JsonSerializer jsonSerializer = null) where TObject : class
        {
            jsonSerializer ??= _serializer;

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
                    Uri = response.RequestMessage?.RequestUri?.ToString()
                };

                try
                {
                    if(response.Content != null)
                        exception.RawResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch { }

                throw exception;
            }
        }
    }
}
