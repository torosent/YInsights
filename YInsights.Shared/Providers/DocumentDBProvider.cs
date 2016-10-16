using Microsoft.Azure;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YInsights.Shared.Providers
{
    public class DocumentDBProvider:IDocumentDBProvider
    {
        string EndpointUri = CloudConfigurationManager.GetSetting("DocumentDBUri");
        string PrimaryKey = CloudConfigurationManager.GetSetting("DocumentDBKey");

        public DocumentClient Client { get; set; }
        public DocumentDBProvider()
        {
            var docclient = new DocumentClient(new Uri(EndpointUri), PrimaryKey, new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            });
        
            // await docclient.OpenAsync();
            Client = docclient;

        }
    }
}
