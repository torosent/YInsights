using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SharedCode.Providers;
using Microsoft.Azure.Documents.Client;
using SharedCode.Poco;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using SharedCode.Common;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace GetGeneralTopics.Controllers
{
    [Route("api/[controller]")]
    public class CalculateController : Controller
    {
        private const string URL = "https://api.thomsonreuters.com/permid/calais";

        private string apitoken;

        DocumentDBProvider documentDBProvider;

        public CalculateController(DocumentDBProvider _documentDBProvider)
        {
            documentDBProvider = _documentDBProvider;
           
        }
        // GET: api/values
        [HttpGet]
        public void Get()
        {
            CalculateTopics();
        }
        private async void CalculateTopics()
        {
            var uri = UriFactory.CreateDocumentCollectionUri("articles", "article");
            var articleExistQuery = documentDBProvider.Client.CreateDocumentQuery<Article>(uri).Where(f => f.processed == true);


            var tags = new Dictionary<string, int>();
            foreach (var article in articleExistQuery)
            {
                Tags.CalculateTags(tags, article);
            }


            var listTags = tags.OrderByDescending(x => x.Value);

            var list = new List<dynamic>();
            foreach (var tag in listTags)
            {
                list.Add(new { topic = tag.Key, count = tag.Value });
            }

            var topics = new { id = "Topics", value = Newtonsoft.Json.JsonConvert.SerializeObject(list.Take(1000)) };
            var cloud = new { id = "WordCloudTopics", value = Newtonsoft.Json.JsonConvert.SerializeObject(list.Take(1000)) };

            var upsertResult = await documentDBProvider.Client.UpsertDocumentAsync(uri, topics);
            upsertResult = await documentDBProvider.Client.UpsertDocumentAsync(uri, cloud);

           


        }


    }
}
