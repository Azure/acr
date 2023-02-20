using Azure;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.ContainerRegistry.Models;
using Microsoft.Extensions.Configuration;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Writers;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Task=System.Threading.Tasks.Task;

namespace ManageTask
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                string appSettingsFile = args.Length > 0
                    ? args[0]
                    : Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "appsettings.json");
                var options = LoadOptions(appSettingsFile);
                options.Validate();

                // Build a container image using local source (WeatherService) and push the image to the registry

                await BuildImageUsingLocalSourceAsync(options).ConfigureAwait(false);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"Failed with the following error:");
                Console.WriteLine(ex);
                return -1;
            }
        }

        private static async Task BuildImageUsingLocalSourceAsync(Options options)
        {
            Console.WriteLine($"BuildImageUsingLocalSource");
            Console.WriteLine($"  MIClientId: {options.MIClientId}");
            Console.WriteLine($"  SPClientId: {options.SPClientId}");
            Console.WriteLine($"  AzureAuthorityHosts: {options.AzureAuthorityHosts}");
            Console.WriteLine($"  SubscriptionId: {options.SubscriptionId}");
            Console.WriteLine($"  ResourceGroupName: {options.ResourceGroupName}");
            Console.WriteLine($"  RegistryName: {options.RegistryName}");
            Console.WriteLine($"======================================================================");
            Console.WriteLine();

            var armClient = new AzureUtility(
                azureAuthorityHosts: options.AzureAuthorityHosts,
                subscriptionId: options.SubscriptionId,
                tenantId: options.TenantId,
                miClientId: options.MIClientId,
                spClientId: options.SPClientId,
                spClientSecret: options.SPClientSecret).ArmClient;

            // Pack and upload the local weather service source
            var sourceDirecotry = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "WeatherService");

            Console.WriteLine($"{DateTimeOffset.Now}: Creating tarball from '{sourceDirecotry}'");

            var tarball = CreateTarballFromDirectory(sourceDirecotry);

            Console.WriteLine($"{DateTimeOffset.Now}: Created tarball '{tarball}'");
            Console.WriteLine($"{DateTimeOffset.Now}: Uploading tarball");

            var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
            var resourceGroup = (await subscription.GetResourceGroupAsync (options.ResourceGroupName).ConfigureAwait(false)).Value;
            var registry = (await resourceGroup.GetContainerRegistryAsync(options.RegistryName).ConfigureAwait(false)).Value;

            var sourceUpload = (await registry.GetBuildSourceUploadUrlAsync().ConfigureAwait(false)).Value;

            using(var stream = new FileStream(tarball, FileMode.Open, FileAccess.Read))
            using (var content = new StreamContent(stream))
            using (var httpClient = new HttpClient())
            {
                // NOTE: You can also use azure storage sdk to upload the file
                content.Headers.Add("x-ms-blob-type", "BlockBlob");
                var response = await httpClient.PutAsync(sourceUpload.UploadUri, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }

            File.Delete(tarball);

            Console.WriteLine($"{DateTimeOffset.Now}: Uploaded tarball to '{sourceUpload.RelativePath}'");

            Console.WriteLine($"{DateTimeOffset.Now}: Starting new run");

            var dockerBuildContent = new ContainerRegistryDockerBuildContent(
                dockerFilePath: "Dockerfile",
                platform: new ContainerRegistryPlatformProperties(ContainerRegistryOS.Linux));
            dockerBuildContent.SourceLocation = sourceUpload.RelativePath;
            dockerBuildContent.ImageNames.Add("weatherservice:{{.Run.ID}}");
            dockerBuildContent.IsPushEnabled = true;
            dockerBuildContent.TimeoutInSeconds = 60 * 10; // 10 minutes
            dockerBuildContent.AgentCpu = 2;

            var run = (await registry.ScheduleRunAsync(
                waitUntil: WaitUntil.Completed,
                content: dockerBuildContent).ConfigureAwait(false)).Value;

            Console.WriteLine($"{DateTimeOffset.Now}: Started run: '{run.Id}'");

            Console.WriteLine($"{DateTimeOffset.Now}: Starting new run with encoded task");

            string imageName = $"{options.RegistryName}.azurecr.io/weatherservice:latest";
            string taskString =
$@"
version: v1.1.0
steps:
  - build: . -t {imageName}
  - push:
    - {imageName}
    timeout: 1800";

            var encodedTaskRunContent = new ContainerRegistryEncodedTaskRunContent(
                encodedTaskContent: Convert.ToBase64String(Encoding.UTF8.GetBytes(taskString)),
                platform: new ContainerRegistryPlatformProperties(ContainerRegistryOS.Linux));
            encodedTaskRunContent.SourceLocation = sourceUpload.RelativePath;
            encodedTaskRunContent.TimeoutInSeconds = 60 * 10; // 10 minutes
            encodedTaskRunContent.AgentCpu = 2;

            run = (await registry.ScheduleRunAsync(
                waitUntil: WaitUntil.Completed,
                content: encodedTaskRunContent).ConfigureAwait(false)).Value;

            Console.WriteLine($"{DateTimeOffset.Now}: Started run: '{run.Data.RunId}'");

            // Poll the run status and wait for completion
            DateTimeOffset deadline = DateTimeOffset.Now.AddMinutes(10);
            while (RunInProgress(run.Data.Status)
                && deadline >= DateTimeOffset.Now)
            {
                Console.WriteLine($"{DateTimeOffset.Now}: In progress: '{run.Data.Status}'. Wait 10 seconds");
                await Task.Delay(10000).ConfigureAwait(false);
                run = (await registry.GetContainerRegistryRunAsync(run.Data.RunId).ConfigureAwait(false)).Value;
            }

            Console.WriteLine($"{DateTimeOffset.Now}: Run status: '{run.Data.RunId}'");

            // Download the run log
            var logResult = (await run.GetLogSasUrlAsync().ConfigureAwait(false)).Value;

            using (var httpClient = new HttpClient())
            {
                // NOTE: You can also use azure storage sdk to download the log
                Console.WriteLine($"{DateTimeOffset.Now}: Run log: ");
                var log = await httpClient.GetStringAsync(logResult.LogLink).ConfigureAwait(false);
                Console.WriteLine(log);
            }
        }

        #region Private
        private class Options
        {
            public string Environment { get; set; }
            public string TenantId { get; set; }
            public string MIClientId { get; set; }
            public string SPClientId { get; set; }
            public string SPClientSecret { get; set; }
            public string SubscriptionId { get; set; }
            public string ResourceGroupName { get; set; }
            public string RegistryName { get; set; }

            public Uri AzureAuthorityHosts
            {
                get
                {
                    switch (Environment?.ToLowerInvariant())
                    {
                        case "azurechina":
                            return Azure.Identity.AzureAuthorityHosts.AzureChina;
                        case "azuregovernment":
                            return Azure.Identity.AzureAuthorityHosts.AzureGovernment;
                        default:
                            return Azure.Identity.AzureAuthorityHosts.AzurePublicCloud;
                    }
                }
            }

            public void Validate()
            {
                if (string.IsNullOrWhiteSpace(SubscriptionId))
                {
                    throw new ArgumentNullException(nameof(SubscriptionId));
                }

                if (string.IsNullOrWhiteSpace(ResourceGroupName))
                {
                    throw new ArgumentNullException(nameof(ResourceGroupName));
                }

                if (string.IsNullOrWhiteSpace(RegistryName))
                {
                    throw new ArgumentNullException(nameof(RegistryName));
                }
            }
        }

        private static Options LoadOptions(string appSettingsFile)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile(appSettingsFile, optional: true)
                .AddEnvironmentVariables();

            var options = new Options();

            builder.Build().Bind(options);

            return options;
        }

        private static bool RunInProgress(ContainerRegistryRunStatus? runStatus)
        {
            return runStatus == ContainerRegistryRunStatus.Queued 
                || runStatus == ContainerRegistryRunStatus.Started 
                || runStatus == ContainerRegistryRunStatus.Running;
        }

        private static string CreateTarballFromDirectory(string direcotryPath)
        {
            var outputFile = Path.GetTempFileName();

            using (var archive = TarArchive.Create())
            {
                archive.AddAllFromDirectory(direcotryPath);
                archive.SaveTo(outputFile, new WriterOptions(CompressionType.GZip));
            }

            return outputFile;
        }
        #endregion
    }
}
