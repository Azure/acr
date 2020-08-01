using DotNetTransfer.Clients;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace DotNetTransfer
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

                // Use ACR Transfer to move artifacts between two registries
                await TransferRegistryArtifacts(options).ConfigureAwait(false);

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

        private static async Task TransferRegistryArtifacts(Options options)
        {
            var exportOptionsDisplay = options.ExportPipeline.Options != null ? string.Join(", ", options.ExportPipeline.Options) : "";
            var importOptionsDisplay = options.ImportPipeline.Options != null ? string.Join(", ", options.ImportPipeline.Options) : "";

            Console.WriteLine($"TransferRegistryArtifacts");
            Console.WriteLine($"  MIClientId: {options.MIClientId}");
            Console.WriteLine($"  SPClientId: {options.SPClientId}");
            Console.WriteLine($"  AzureEnvironment: {options.AzureEnvironment.Name}");
            Console.WriteLine($"  SubscriptionId: {options.SubscriptionId}");
            Console.WriteLine($"======================================================================");
            Console.WriteLine($"ExportPipeline properties:");
            Console.WriteLine($"  ResourceGroupName: {options.ExportPipeline.ResourceGroupName}");
            Console.WriteLine($"  RegistryName: {options.ExportPipeline.RegistryName}");
            Console.WriteLine($"  ExportPipelineName: {options.ExportPipeline.PipelineName}");
            Console.WriteLine($"  UserAssignedIdentity: {options.ExportPipeline.UserAssignedIdentity}");
            Console.WriteLine($"  StorageUri: {options.ExportPipeline.ContainerUri}");
            Console.WriteLine($"  KeyVaultSecretUri: {options.ExportPipeline.KeyVaultUri}");
            Console.WriteLine($"  Options: {exportOptionsDisplay}");
            Console.WriteLine($"======================================================================");
            Console.WriteLine($"ImportPipeline properties:");
            Console.WriteLine($"  ResourceGroupName: {options.ImportPipeline.ResourceGroupName}");
            Console.WriteLine($"  RegistryName: {options.ImportPipeline.RegistryName}");
            Console.WriteLine($"  ImportPipelineName: {options.ImportPipeline.PipelineName}");
            Console.WriteLine($"  UserAssignedIdentity: {options.ImportPipeline.UserAssignedIdentity}");
            Console.WriteLine($"  StorageUri: {options.ImportPipeline.ContainerUri}");
            Console.WriteLine($"  KeyVaultSecretUri: {options.ImportPipeline.KeyVaultUri}");
            Console.WriteLine($"  Options: {importOptionsDisplay}");
            Console.WriteLine($"======================================================================");
            Console.WriteLine();

            var azure = new AzureUtility(
                options.AzureEnvironment,
                options.TenantId,
                options.SubscriptionId,
                options.MIClientId,
                options.SPClientId,
                options.SPClientSecret);

            var registryClient = azure.RegistryClient;
            var keyVaultClient = azure.KeyVaultClient;

            ExportClient exportClient = new ExportClient(registryClient, keyVaultClient, options);
            var exportPipeline = exportClient.CreateExportPipeline();

            ImportClient importClient = new ImportClient(registryClient, keyVaultClient, options);
            var importPipeline = importClient.CreateImportPipeline();

            Console.WriteLine($"Your importPipeline '{options.ImportPipeline.PipelineName}' will be run automatically.");
            Console.WriteLine($"Would you like to run your exportPipeline '{options.ExportPipeline.PipelineName}'? [Y/N]");
            var response = Console.ReadLine();
            if (string.Equals("Y", response, StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("Validating pipelineRun configurations for export.");
                options.ExportPipelineRun.Validate();
                await exportClient.ExportImagesAsync(exportPipeline).ConfigureAwait(false);
            }
            else
            {
                Console.WriteLine("Goodbye!");
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
    }
}
