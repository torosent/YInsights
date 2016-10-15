using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YInsights.Shared.AI
{
    public static class ApplicationInsightsClient
    {
        static ApplicationInsightsClient()
        {
           
            ApplicationInsightsConfiguration.Initialize();
        }
        public static void LogException(Exception ex)
        {
            Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry telemetry = new Microsoft.ApplicationInsights.DataContracts.ExceptionTelemetry(ex);
            ServiceFabricTelemetryInitializer context = new ServiceFabricTelemetryInitializer();
            telemetry.Context.Operation.Name = "Exception";
            context.Initialize(telemetry);
            var client = new TelemetryClient();
            client.TrackException(telemetry);
        }

        public static void LogEvent(string name, params string[] parameters)
        {
            Microsoft.ApplicationInsights.DataContracts.EventTelemetry telemetry = new Microsoft.ApplicationInsights.DataContracts.EventTelemetry(name);
            ServiceFabricTelemetryInitializer context = new ServiceFabricTelemetryInitializer();
            telemetry.Context.Operation.Name = "Event";
            context.Initialize(telemetry);

            int i = 0;
            foreach (var item in parameters)
            {
                telemetry.Properties.Add($"Param{i}", item);
                i++;
            }
            var client = new TelemetryClient();
            client.TrackEvent(telemetry);

        }
    }
}
