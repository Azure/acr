using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace RegistryArtifactTransfer
{
    public static class HttpMessageExtensions
    {
        private const string LinkHeaderName = "Link";
        private const string NextRelationType = "rel=\"next\"";

        public static Uri GetNextPageUri(this HttpResponseMessage response)
        {
            if (!response.Headers.Contains(LinkHeaderName))
            {
                return null;
            }

            var headerLink = response.Headers.GetValues(LinkHeaderName);
            var nextPage = headerLink.FirstOrDefault();

            if (!nextPage.Contains(NextRelationType))
            {
                return null;
            }

            int backwardsPointer = nextPage.IndexOf(NextRelationType);
            nextPage = nextPage.Substring(nextPage.LastIndexOf('<', backwardsPointer) + 1, nextPage.LastIndexOf('>', backwardsPointer) - 1);

            if (nextPage.StartsWith('/'))
            {
                nextPage = nextPage.Substring(1);
                if (nextPage.StartsWith('/'))
                {
                    return null;
                }
            }

            if (Uri.TryCreate(nextPage, UriKind.Absolute, out Uri nextPageUri))
            {
                if (!string.Equals(nextPageUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(nextPageUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                //
                // Prevent SSRF (CWE-918): the Link header is supplied by the remote registry, and
                // callers attach registry credentials to every paged request. Only follow an absolute
                // next-page link when it targets the same registry that served this response; otherwise
                // a malicious or compromised registry could redirect the authenticated request to an
                // attacker-chosen host and exfiltrate the credentials.
                if (!IsSameRegistry(response.RequestMessage?.RequestUri, nextPageUri))
                {
                    return null;
                }

                return nextPageUri;
            }

            if (Uri.IsWellFormedUriString(nextPage, UriKind.Relative))
            {
                var requestUri = response.RequestMessage.RequestUri;
                var baseUriBuilder = new UriBuilder(requestUri.Scheme, requestUri.Host, requestUri.Port);
                return new Uri(baseUriBuilder.Uri, nextPage);
            }

            return null;
        }

        private static bool IsSameRegistry(Uri requestUri, Uri nextPageUri)
        {
            return requestUri != null &&
                string.Equals(requestUri.Scheme, nextPageUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(requestUri.Host, nextPageUri.Host, StringComparison.OrdinalIgnoreCase) &&
                requestUri.Port == nextPageUri.Port;
        }

        public static void AddBasicAuth(this HttpRequestMessage request, string userName, string password)
        {
            if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
            {
                var svcCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + password));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", svcCredentials);
            }
        }
    }
}
