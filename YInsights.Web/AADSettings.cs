using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YInsights
{
    public class AADSettings
    {
        public string SignUpPolicyId { get; set; }

        public string SignInPolicyId { get; set; }

        public string ProfilePolicyId { get; set; }

        public string ClientId { get; set; }
        public string RedirectUri { get; set; }

        public string AadInstance { get; set; }

        public string Tenant { get; set; }

    }
}
