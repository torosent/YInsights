using Microsoft.Azure;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using YInsights.Shared;
using YInsights.Shared.Poco;

namespace DocumentDbMiddlewareWebApi.Controllers
{
    /// <summary>
    /// This will be removed
    /// </summary>
    [ServiceRequestActionFilter]
    public class ArticlesController : ApiController
    {
        string EndpointUri = CloudConfigurationManager.GetSetting("DocumentDBUri");
        string PrimaryKey = CloudConfigurationManager.GetSetting("DocumentDBKey");


        public async Task<List<Article>> Post([FromBody]string [] ids)
        {
            var list =new List<Article>();

            var docclient = new DocumentClient(new Uri(EndpointUri), PrimaryKey, new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            });

            await docclient.OpenAsync();
           
            var queryBuilder = new StringBuilder("SELECT * FROM articles c WHERE c.id IN (");

            var paramCollection = new Microsoft.Azure.Documents.SqlParameterCollection();

            int idIdx = 1;
            foreach (var item in ids)
            {
                queryBuilder.Append('"');
                queryBuilder.Append(item);
                queryBuilder.Append('"');
                if (idIdx < ids.Length)
                {
                    queryBuilder.Append(",");
                }

                idIdx++;
            }
            queryBuilder.Append(')');

            var queryString = queryBuilder.ToString();
            var query = new Microsoft.Azure.Documents.SqlQuerySpec(queryString, paramCollection);

            var articleQuery = docclient.CreateDocumentQuery<Article>(
                UriFactory.CreateDocumentCollectionUri("articles", "article"), query);

            var lst = articleQuery.ToList();
            return lst;
        }

      
    }
}
