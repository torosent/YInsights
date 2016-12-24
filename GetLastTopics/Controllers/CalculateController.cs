using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SharedCode.Providers;
using Microsoft.Azure.Documents.Client;
using SharedCode.Poco;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace GetLastTopics.Controllers
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
            CalculateLastTopics();
        }

        private async void CalculateLastTopics()
        {
            var uri = UriFactory.CreateDocumentCollectionUri("articles", "article");

          var articleExistQuery = documentDBProvider.Client.CreateDocumentQuery<Article>(
              uri).Where(f => f.processed == true).OrderByDescending(x => x.time).Take(30);


            var tags = new Dictionary<string, int>();

            foreach (var article in articleExistQuery)
            {
                var random = new Random((int)DateTime.Now.Ticks);
                if (article.tags.Count == 0)
                    continue;
                var tag = article.tags[random.Next(0, article.tags.Count)];
                if (tags.ContainsKey(tag))
                    continue;
                tags.Add(tag, 1);


            }


            var listTags = tags.OrderByDescending(x => x.Value);

            var list = new List<dynamic>();
            foreach (var tag in listTags)
            {
                list.Add(new { topic = tag.Key, count = tag.Value });
            }
            var topics = new { id = "LastTopics", value = Newtonsoft.Json.JsonConvert.SerializeObject(list.Take(5)) };

            var upsertResult = await documentDBProvider.Client.UpsertDocumentAsync(uri, topics);


         


        }

    }
}
