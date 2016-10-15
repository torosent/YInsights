using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Azure;
using YInsights.Shared;
using Microsoft.Azure.Documents.Client;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using ServiceFabric.ServiceBus.Services.CommunicationListeners;
using ServiceFabric.ServiceBus.Services;
using Microsoft.ServiceBus.Messaging;

namespace CalaisTopicAndTagsAnalyze
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class CalaisTopicAndTagsAnalyze : StatefulService
    {
       
        public CalaisTopicAndTagsAnalyze(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            string serviceBusQueueName = null;

            yield return new ServiceReplicaListener(context => new ServiceBusQueueCommunicationListener(
             new Handler(this)
             , context
             , serviceBusQueueName
             , requireSessions: true)
            {
                AutoRenewTimeout = TimeSpan.FromSeconds(70),  //auto renew up until 70s, so processing can take no longer than 60s (default lock duration).
                MessagePrefetchCount = 1
            }, "StatefullService-ServiceBusQueueListener");

        }

        
    }
    internal sealed class Handler : AutoCompleteServiceBusMessageReceiver
    {
        private const string URL = "https://api.thomsonreuters.com/permid/calais";
        string EndpointUri = CloudConfigurationManager.GetSetting("DocumentDBUri");
        string PrimaryKey = CloudConfigurationManager.GetSetting("DocumentDBKey");
        DocumentClient docclient;
        StatefulService _service;
        public Handler(StatefulService service)
        {
            _service = service;
             docclient = new DocumentClient(new Uri(EndpointUri), PrimaryKey, new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            });
             docclient.OpenAsync().Wait();
        }
        protected override  Task ReceiveMessageImplAsync(BrokeredMessage message, MessageSession messageSession, CancellationToken cancellationToken)
        {
            try
            {
                var article = message.GetBody<Article>();
                var result =  ProcessArticle(article).Result;
                if (result)
                {
                    article.processed = true;
                    var upsertResult =  docclient.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri("articles", "article"), article).Result;
                    ServiceEventSource.Current.ServiceMessage(_service, $"Updated article {message.MessageId}");
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(_service, ex.Message, ex);
                return Task.FromResult(false);
            }
            
            return Task.FromResult(true);

        }

        private async Task<bool> ProcessArticle(Article article)
        {
            string apitoken = "MxkbV1PxWcsQz7q8YLFxMaGWyRSNhW5Q";
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.TryAddWithoutValidation("ContentType", "text/raw");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-AG-Access-Token", apitoken);
            client.DefaultRequestHeaders.TryAddWithoutValidation("outputformat", "application/json");

            var response = client.PostAsync(URL, new StringContent(article.title)).Result;
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                ParseTopics(article, responseBody);
                ServiceEventSource.Current.ServiceMessage(_service, $"Parsed article {article.Id}");
                return true;
            }
            ServiceEventSource.Current.ServiceMessage(_service, $"Error with status code {article.Id} {response.StatusCode.ToString()} -  {response.Content.ReadAsStringAsync().Result}");
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
