using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Azure.Management.ContainerRegistry.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Configuration;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Writers;
using System;
using System.Collections.Generic;
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
            Console.WriteLine($"  AzureEnvironment: {options.AzureEnvironment.Name}");
            Console.WriteLine($"  SubscriptionId: {options.SubscriptionId}");
            Console.WriteLine($"  ResourceGroupName: {options.ResourceGroupName}");
            Console.WriteLine($"  RegistryName: {options.RegistryName}");
            Console.WriteLine($"======================================================================");
            Console.WriteLine();

            var registryClient = new AzureUtility(
                options.AzureEnvironment,
                options.TenantId,
                options.SubscriptionId,
                options.MIClientId,
                options.SPClientId,
                options.SPClientSecret).RegistryClient;

            // Pack and upload the local weather service source
            var sourceDirecotry = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "WeatherService");

            Console.WriteLine($"{DateTimeOffset.Now}: Creating tarball from '{sourceDirecotry}'");

            var tarball = CreateTarballFromDirectory(sourceDirecotry);

            Console.WriteLine($"{DateTimeOffset.Now}: Created tarball '{tarball}'");

            Console.WriteLine($"{DateTimeOffset.Now}: Uploading tarball");

            var sourceUpload = await registryClient.Registries.GetBuildSourceUploadUrlAsync(
                options.ResourceGroupName,
                options.RegistryName).ConfigureAwait(false);

            using(var stream = new FileStream(tarball, FileMode.Open, FileAccess.Read))
            using (var content = new StreamContent(stream))
            using (var httpClient = new HttpClient())
            {
                // NOTE: You can also use azure storage sdk to upload the file
                content.Headers.Add("x-ms-blob-type", "BlockBlob");
                var response = await httpClient.PutAsync(sourceUpload.UploadUrl, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }

            File.Delete(tarball);

            Console.WriteLine($"{DateTimeOffset.Now}: Uploaded tarball to '{sourceUpload.RelativePath}'");

            Console.WriteLine($"{DateTimeOffset.Now}: Starting new run");

            var run = await registryClient.Registries.ScheduleRunAsync(
                options.ResourceGroupName,
                options.RegistryName,
                new DockerBuildRequest
                {
                    SourceLocation = sourceUpload.RelativePath,
                    DockerFilePath = "Dockerfile",
                    ImageNames = new List<string> { "weatherservice:{{.Run.ID}}" },
                    IsPushEnabled = true,
                    Platform = new PlatformProperties(OS.Linux),
                    Timeout = 60 * 10, // 10 minutes
                    AgentConfiguration = new AgentProperties(cpu: 2)
                }).ConfigureAwait(false);

            Console.WriteLine($"{DateTimeOffset.Now}: Started run: '{run.RunId}'");

            Console.WriteLine($"{DateTimeOffset.Now}: Starting new run with encoded task");

            string imageName = $"{options.RegistryName}.azurecr.io/weatherservice:latest";
            string taskString =
$@"
version: v1.1.0
steps:
  - build: . -t {imageName}
  - push: 
    timeout: 1800
     - {imageName}";

            run = await registryClient.Registries.ScheduleRunAsync(
                options.ResourceGroupName,
                options.RegistryName,
                new EncodedTaskRunRequest
                {
                    EncodedTaskContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(taskString)),
                    SourceLocation = sourceUpload.RelativePath,
                    Platform = new PlatformProperties(OS.Linux),
                    Timeout = 60 * 10, // 10 minutes
                    AgentConfiguration = new AgentProperties(cpu: 2)
                }).ConfigureAwait(false);

            Console.WriteLine($"{DateTimeOffset.Now}: Started run: '{run.RunId}'");

            // Poll the run status and wait for completion
            DateTimeOffset deadline = DateTimeOffset.Now.AddMinutes(10);
            while (RunInProgress(run.Status)
                && deadline >= DateTimeOffset.Now)
            {
                Console.WriteLine($"{DateTimeOffset.Now}: In progress: '{run.Status}'. Wait 10 seconds");
                await Task.Delay(10000).ConfigureAwait(false);
                run = await registryClient.Runs.GetAsync(
                    options.ResourceGroupName, 
                    options.RegistryName, 
                    run.RunId).ConfigureAwait(false);
            }

            Console.WriteLine($"{DateTimeOffset.Now}: Run status: '{run.Status}'");

            // Download the run log
            var logResult = await registryClient.Runs.GetLogSasUrlAsync(
                options.ResourceGroupName,
                options.RegistryName,
                run.RunId).ConfigureAwait(false);

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

            public AzureEnvironment AzureEnvironment
            {
                get
                {
                    return string.IsNullOrWhiteSpace(Environment)
                        ? AzureEnvironment.AzureGlobalCloud
                        : AzureEnvironment.FromName(Environment);
                }
            }

            public void Validate()
            {
                if (string.IsNullOrWhiteSpace(TenantId))
                {
                    throw new ArgumentNullException(nameof(TenantId));
                }

                if (string.IsNullOrWhiteSpace(MIClientId)
                    && (string.IsNullOrWhiteSpace(SPClientId) || string.IsNullOrWhiteSpace(SPClientSecret)))
                {
                    throw new ArgumentNullException($"Missing {nameof(MIClientId)} or {nameof(SPClientId)}/{nameof(SPClientSecret)}");
                }

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

        private static bool RunInProgress(string runStatus)
        {
            return runStatus == RunStatus.Queued 
                || runStatus == RunStatus.Started 
                || runStatus == RunStatus.Running;
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
