using Microsoft.Azure;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCode.Providers
{
    public class DocumentDBProvider:IDocumentDBProvider
    {
       
        public DocumentClient Client { get; set; }
        public string ConnectionString { get; set; }
        public DocumentDBProvider(string EndpointUri,string PrimaryKey)
        {
            var docclient = new DocumentClient(new Uri(EndpointUri), PrimaryKey, new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            });
            ConnectionString = docclient.ServiceEndpoint.AbsoluteUri;
            // await docclient.OpenAsync();
            Client = docclient;

        }
    }
}
