using Newtonsoft.Json.Serialization;

namespace Serilog.Sinks.Datadog.Logs.Sinks.Datadog
{
    public class OriginalCaseNamingResolver : NamingStrategy
    {
        public OriginalCaseNamingResolver()
        {
            this.ProcessDictionaryKeys = true;
            this.OverrideSpecifiedNames = true;
        }

        protected override string ResolvePropertyName(string name)
        {
            return name;
        }

        public class OriginalCasePropertyNamesContractResolver : DefaultContractResolver
        {
            public OriginalCasePropertyNamesContractResolver()
            {
                this.NamingStrategy = new OriginalCaseNamingResolver
                {
                    ProcessDictionaryKeys = true,
                    OverrideSpecifiedNames = true
                };
            }
        }
    }
}
