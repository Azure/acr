using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace RegistryArtifactTransfer
{
    public class RepositoryProviderV2
    {
        #region Route constants
        private const string GetCatalogRoute = "https://{0}/v2/_catalog?n={1}";
        private const string GetTagsRoute = "https://{0}/v2/{1}/tags/list?n={2}";
        #endregion

        private const int pageSize = 100;


        #region Exponential backoff retry with jitter
        private readonly TimeSpan initWaitTime = TimeSpan.FromSeconds(2);
        private readonly int maxRetryCount = 4;
        private readonly IAsyncPolicy<HttpResponseMessage> retryPolicy;
        #endregion

        private readonly HttpClient _httpClient;

        public RepositoryProviderV2()
        {
            _httpClient = new HttpClient();

            retryPolicy = HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .WaitAndRetryAsync(
                            maxRetryCount,
                            retryAttempt => TimeSpan.FromSeconds(Math.Pow(initWaitTime.TotalSeconds, retryAttempt))  
                            + TimeSpan.FromMilliseconds(new Random().Next(0, 100)));
        }

        public async Task<IEnumerable<string>> GetRepositoriesAsync(
            string registry,
            string userName,
            string password)
        {
            if (string.IsNullOrWhiteSpace(registry))
            {
                throw new ArgumentNullException(nameof(registry));
            }

            var repositories = new List<string>();

            var nextPageUri = new Uri(string.Format(GetCatalogRoute, registry, pageSize));
            while (nextPageUri != null)
            {

                var response = await retryPolicy.ExecuteAsync(
                    async () =>
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Get, nextPageUri))
                        {
                            request.AddBasicAuth(userName, password);
                            return await _httpClient.SendAsync(request);
                        }
                    });

                if (response.IsSuccessStatusCode)
                {
                    var repoPage = JsonConvert.DeserializeObject<CatalogApiResponse>(await response.Content.ReadAsStringAsync())?.Repositories;
                    if (repoPage != null)
                    {
                        repositories.AddRange(repoPage);
                    }

                    nextPageUri = response.GetNextPageUri();
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Registry:{registry} failed to list repositories. StatusCode:{response.StatusCode}, Reason:{response.ReasonPhrase}, Content:{content}");
                }
            }

            return repositories;
        }

        public async Task<IEnumerable<string>> GetTagsAsync(
            string registry,
            string repository,
            string userName,
            string password)
        {
            if (string.IsNullOrWhiteSpace(registry))
            {
                throw new ArgumentNullException(nameof(registry));
            }

            if (string.IsNullOrWhiteSpace(repository))
            {
                throw new ArgumentNullException(nameof(repository));
            }

            var tags = new List<string>();

            var nextPageUri = new Uri(string.Format(GetTagsRoute, registry, repository, pageSize));
            while (nextPageUri != null)
            {
                var response = await retryPolicy.ExecuteAsync(
                    async () =>
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Get, nextPageUri))
                        {
                            request.AddBasicAuth(userName, password);
                            return await _httpClient.SendAsync(request);
                        }
                    });

                if (response.IsSuccessStatusCode)
                {
                    var tagPage = JsonConvert.DeserializeObject<TagListApiResponse>(await response.Content.ReadAsStringAsync())?.Tags;

                    if (tagPage != null)
                    {
                        tags.AddRange(tagPage);
                    }

                    nextPageUri = response.GetNextPageUri();
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Registry:{registry} Repository:{repository} failed to list tags. StatusCode:{response.StatusCode}, Reason:{response.ReasonPhrase}, Content:{content}");
                }
            }

            return tags;
        }
    }
}
