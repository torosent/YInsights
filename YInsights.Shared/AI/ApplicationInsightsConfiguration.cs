using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YInsights.Shared.AI
{
    public static class ApplicationInsightsConfiguration
    {
        public static void Initialize()
        {
            TelemetryConfiguration config = new TelemetryConfiguration();
            config.InstrumentationKey = CloudConfigurationManager.GetSetting("ApplicationInsights");
            TelemetryConfiguration.Active.InstrumentationKey = config.InstrumentationKey;
            
        }

    }

}
