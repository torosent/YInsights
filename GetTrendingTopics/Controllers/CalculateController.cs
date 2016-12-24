using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SharedCode.Providers;
using Microsoft.Azure.Documents.Client;
using SharedCode.Poco;
using SharedCode.Extentions;
using SharedCode.Common;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace GetTrendingTopics.Controllers
{
    [Route("api/[controller]")]
    public class CalculateController : Controller
    {
        DocumentDBProvider documentDBProvider;
        public CalculateController(DocumentDBProvider _documentDBProvider)
        {
            documentDBProvider = _documentDBProvider;
        }
        // GET: api/values
        [HttpGet]
        public void Get()
        {
            CalculateTrendingTopics();
        }

        private async void CalculateTrendingTopics()
        {

            var currentDate = DateTime.Now.AddDays(-2).DateTimeToUnixTimestamp();


            var articleExistQuery = documentDBProvider.Client.CreateDocumentQuery<Article>(
                UriFactory.CreateDocumentCollectionUri("articles", "article")).Where(f => f.processed == true && f.time > currentDate);


            var tags = new Dictionary<string, int>();
            foreach (var article in articleExistQuery)
            {
                Tags.CalculateTags(tags, article, false);
            }


            var listTags = tags.OrderByDescending(x => x.Value);

            var list = new List<dynamic>();
            foreach (var tag in listTags)
            {
                list.Add(new { topic = tag.Key, count = tag.Value });
            }

            var topics = new { id = "TrendingTopics", value = Newtonsoft.Json.JsonConvert.SerializeObject(list.Take(5)) };

            var upsertResult = await documentDBProvider.Client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri("articles", "article"), topics);

          

        }

    }
}
