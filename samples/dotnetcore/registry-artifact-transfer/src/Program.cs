using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace RegistryArtifactTransfer
{
    public static class Program
    {
        private const string ReportFileNamePrefix = "report";
        private const string LogFileNamePrefix = "log";

        public static async Task Main(string[] args)
        {
            #region Global HTTP settings

            // https://github.com/Azure/azure-storage-net-data-movement#best-practice
            ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 8;
            ServicePointManager.Expect100Continue = false;

            #endregion

            #region Logger

            var logFileName = LogFileNamePrefix + "_" + string.Format("{0:yyyy-MM-dd_HH-mm-ss-fff}", DateTimeOffset.Now) + ".txt";
            var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(new LoggerConfiguration()
                .WriteTo.File(logFileName)
                .WriteTo.ColoredConsole()
                .CreateLogger()));
            var logger = loggerFactory.CreateLogger("RegistryArtifactTransfer");

            #endregion

            #region Configurations

            string transferDefinitionFile = args.Length > 0
                ? args[0]
                : Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "transferdefinition.json");
            var transferDefinition = GetConfig(transferDefinitionFile);
            transferDefinition.Validate();

            var azureEnvironmentConfiguration = transferDefinition.AzureEnvironment;
            var identityConfiguration = transferDefinition.Identity;
            var registryConfiguration = transferDefinition.Registry;

            #endregion

            #region TransferClient

            var transferClient = new TransferClient(
                azureEnvironmentConfiguration,
                identityConfiguration,
                registryConfiguration);

            #endregion

            #region Process transfer

            var report = new TransferReport();

            var importWorker = new ImportWorker(
                transferDefinition.Import,
                registryConfiguration,
                transferClient,
                loggerFactory.CreateLogger<ImportWorker>());

            var exportWorker = new ExportWorker(
                transferDefinition.Export,
                registryConfiguration,
                identityConfiguration,
                transferClient,
                loggerFactory.CreateLogger<ExportWorker>());

            await importWorker.RunAsync(report);
            await exportWorker.RunAsync(report);

            #endregion

            #region Report results

            logger.LogInformation($"Total artifacts successfully imported: {report.ImportArtifacts.Succeeded.Count}.");
            logger.LogInformation($"Total artifacts failed to import: {report.ImportArtifacts.Failed.Count}.");
            logger.LogInformation($"Total blobs successfully exported: {report.ImportBlobs.Succeeded.Count}.");
            logger.LogInformation($"Total blobs failed to export: {report.ImportBlobs.Failed.Count}.");
            logger.LogInformation($"Total artifacts successfully exported: {report.ExportArtifacts.Succeeded.Count}.");
            logger.LogInformation($"Total artifacts failed in exporting: {report.ExportArtifacts.Failed.Count}.");
            logger.LogInformation($"Total blobs successfully exported: {report.ExportBlobs.Succeeded.Count}.");
            logger.LogInformation($"Total blobs failed in exporting: {report.ExportBlobs.Failed.Count}.");
            logger.LogInformation($"Total blobs successfully copied: {report.CopyBlobs.Succeeded.Count}.");
            logger.LogInformation($"Total blobs failed in copying: {report.CopyBlobs.Failed.Count}.");

            var reportFileName = ReportFileNamePrefix + "_" + string.Format("{0:yyyy-MM-dd_HH-mm-ss-fff}", DateTimeOffset.Now) + ".json";
            await File.WriteAllTextAsync(reportFileName, JsonConvert.SerializeObject(report, Formatting.Indented));

            #endregion

            logger.LogInformation("Done!");
        }

        private static TransferDefinition GetConfig(string transferDefinitionFile)
        {

            var builder = new ConfigurationBuilder()
                .AddJsonFile(transferDefinitionFile)
                .AddEnvironmentVariables();

            var transferDefinition = new TransferDefinition();

            builder.Build().Bind(transferDefinition);

            return transferDefinition;
        }
    }
}
