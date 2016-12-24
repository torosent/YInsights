using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YInsights.Web.Services
{
    public class AIService
    {
        private TelemetryClient telemetry = new TelemetryClient();

        public AIService()
        {

        }

        public void TrackUser(string method,string username,params string[] parameters)
        {
            var dic = new Dictionary<string, string>();
            dic.Add("username", username);

            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    dic.Add($"Param{i}", parameters[i]);
                }
            }
            
            telemetry.TrackEvent(method, dic);

        }
        public void TrackException(Exception ex)
        {
            telemetry.TrackException(ex);
        }
    }
}
