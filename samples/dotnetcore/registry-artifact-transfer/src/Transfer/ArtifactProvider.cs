using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RegistryArtifactTransfer
{
    public class ArtifactProvider
    {
        private readonly ILogger _logger;
        private readonly RepositoryProviderV2 _repositoryProvider;

        public ArtifactProvider(ILogger logger) : base()
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repositoryProvider = new RepositoryProviderV2();
        }

        public async Task<List<string>> GetArtifactsAsync(
            string registryUri,
            string userName,
            string password,
            List<string> repoFilters)
        {
            var artifacts = new List<string>();
            IEnumerable<string> repositories = null;

            repositories = await _repositoryProvider.GetRepositoriesAsync(
                registryUri,
                userName,
                password).ConfigureAwait(false);

            if (repositories != null)
            {
                foreach (var repo in repositories)
                {
                    if (Match(repo, repoFilters))
                    {
                        _logger.LogInformation($"Repository matched: {repo}");
                        IEnumerable<string> tags;

                        tags = await _repositoryProvider.GetTagsAsync(
                            registryUri,
                            repo,
                            userName,
                            password).ConfigureAwait(false);

                        if (tags != null)
                        {
                            foreach (var tag in tags)
                            {
                                artifacts.Add($"{repo.ToLowerInvariant()}:{tag}");
                            }
                        }
                    }
                }
            }

            return artifacts;
        }

        private static bool Match(
            string repo,
            List<string> repoFilters)
        {
            foreach (var filter in repoFilters)
            {
                if (filter.EndsWith('*'))
                {
                    var prefix = filter.Substring(0, filter.Length - 1);
                    if (repo.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                else if (string.Equals(repo, filter, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
