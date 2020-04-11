using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

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

                await RunAsync(options).ConfigureAwait(false);

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

        private static async Task RunAsync(Options options)
        {
            var subscription = new SubscriptionUtility(
                options.AzureEnvironment,
                options.TenantId,
                options.SubscriptionId,
                options.MIClientId,
                options.SPClientId,
                options.SPClientSecret);

            // 
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
        #endregion
    }
}
