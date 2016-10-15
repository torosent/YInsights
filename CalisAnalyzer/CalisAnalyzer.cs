using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Net.Http;
using YInsights.Shared;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json.Linq;

namespace CalisAnalyzer
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class CalisAnalyzer : StatelessService
    {
        private const string URL = "https://api.thomsonreuters.com/permid/calais";
        string EndpointUri = CloudConfigurationManager.GetSetting("DocumentDBUri");
        string PrimaryKey = CloudConfigurationManager.GetSetting("DocumentDBKey");

        public CalisAnalyzer(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
           
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                GetPreProcessedArticles();


                await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
            }
        }

        private async void GetPreProcessedArticles()
        {
            var docclient = new DocumentClient(new Uri(EndpointUri), PrimaryKey, new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp  
            });

            await docclient.OpenAsync();
            var articleExistQuery = docclient.CreateDocumentQuery<Article>(
                UriFactory.CreateDocumentCollectionUri("articles", "article")).Where(f => f.processed == false).Take(1000);
            
            foreach (var item in articleExistQuery)
            {

                var result = await ProcessArticle(item);
                if (result)
                {
                    var upsertResult = await docclient.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri("articles", "article"), item);
                    
                }

            }

        }
        private async Task<bool> ProcessArticle(Article article)
        {
            string apitoken = "MxkbV1PxWcsQz7q8YLFxMaGWyRSNhW5Q";
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.TryAddWithoutValidation("ContentType", "text/raw");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-AG-Access-Token", apitoken);
            client.DefaultRequestHeaders.TryAddWithoutValidation("outputformat", "application/json");

            var response =  client.PostAsync(URL, new StringContent(article.title)).Result;
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                ParseTopics(article, responseBody);
                return true;
            }
            return false;

        }

        private static void ParseTopics(Article article, string responseBody)
        {
            JObject dynamicResult = (JObject)Newtonsoft.Json.Linq.JObject.Parse(responseBody);

            var topics = new List<string>();
            var tags = new List<string>();
            foreach (JToken token in dynamicResult.Children())
            {
                if (token is JProperty)
                {
                    var prop = token as JProperty;
                    if (prop != null)
                    {
                        var values = prop.Children()["_typeGroup"].Values();
                        if (values.Count() > 0)
                        {
                            var value = values.FirstOrDefault();
                            if (value != null && value.Value<string>() == "topics")
                            {
                                var name = prop.Children()["name"].Values().FirstOrDefault().Value<string>();
                                topics.Add(name);
                            }
                            if (value != null && value.Value<string>() == "socialTag")
                            {
                                var name = prop.Children()["name"].Values().FirstOrDefault().Value<string>();
                                tags.Add(name);
                            }
                        }

                    }
                }
            }
            article.tags = tags;
            article.topics = topics;
        }
    }
}
