using Microsoft.Extensions.Configuration;
using System.Linq;

namespace Serilog.Sinks.Datadog.Logs
{
    /// <summary>
    /// Configures the sink's DatadogConfiguration object.
    /// </summary>
    internal static class ApplyMicrosoftExtensionsConfiguration
    {
        /// <summary>
        /// Create the DatadogConfiguration object or apply any configuration changes to it.
        /// </summary>
        /// <param name="datadogConfiguration">An optional externally-created DatadogConfiguration object to be updated with additional configuration values.</param>
        /// <param name="config">A configuration section typically named "configurationSection".</param>
        /// <returns>The "merged" DatadogConfiguration object.</returns>
        internal static DatadogConfiguration ConfigureDatadogConfiguration(DatadogConfiguration datadogConfiguration, IConfigurationSection configurationOption)
        {
            if (configurationOption == null || !configurationOption.GetChildren().Any()) return datadogConfiguration ?? new DatadogConfiguration();

            var section = configurationOption.Get<DatadogConfiguration>();

            return new DatadogConfiguration(
                url: datadogConfiguration?.Url ?? section.Url,
                port: datadogConfiguration?.Port ?? section.Port,
                useSSL: datadogConfiguration?.UseSSL ?? section.UseSSL
            );
        }
    }
}
