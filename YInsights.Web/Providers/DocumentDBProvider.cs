
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YInsights.Web.Providers
{
    public class DocumentDBProvider : IDocumentDBProvider
    {
     
        public DocumentClient Client { get; set; }
        public DocumentDBProvider(string endpointUri,string primaryKey)
        {
            var docclient = new DocumentClient(new Uri(endpointUri), primaryKey, new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Https
            });
     
            Client = docclient;

        }

        
    }
}
