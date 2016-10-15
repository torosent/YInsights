﻿using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YInsights.Shared.AI
{
    public class ServiceFabricTelemetryInitializer : ITelemetryInitializer
    {
        private string roleInstanceName;
        private string roleName;

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Operation.Id = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
            {
                var name = LazyInitializer.EnsureInitialized(ref this.roleName, () => ServiceFabricEnvironmentContextReader.Instance.NodeType);
                telemetry.Context.Cloud.RoleName = name;

            }
            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleInstance))
            {
                var name = LazyInitializer.EnsureInitialized(ref this.roleInstanceName, () => ServiceFabricEnvironmentContextReader.Instance.NodeName);
                telemetry.Context.Cloud.RoleInstance = name;



            }



        }

    }

}