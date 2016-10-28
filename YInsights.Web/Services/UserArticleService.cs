using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using YInsights.Web.Extentions;
using YInsights.Web.Model;
using YInsights.Web.Providers;


namespace YInsights.Web.Services
{
    public class UserArticleService : IUserArticleService
    {
        private readonly YInsightsContext db;
        private readonly RedisProvider redisdb;
        public UserArticleService(YInsightsContext _db, RedisProvider _redisdb)
        {
            db = _db;
            redisdb = _redisdb;

        }

        public async Task<Tuple<IEnumerable<UserArticles>, int>> GetUserUnviewedArticles(string username, string title = null, string tags = null, int pageIndex = -1, int pageSize = -1,bool star = false)
        {
            var articlesList = new List<UserArticles>();
            var query = db.UserArticles.Where(x => x.username.Contains(username) && x.isviewed != true).OrderByDescending(x => x.articleid);
            if (star)
            {
                query = query.Where(x => x.star == true).OrderByDescending(x => x.articleid);
            }
            var count = db.UserArticles.Count(x => x.username == username && x.isviewed != true);

            if (pageIndex > -1 && string.IsNullOrEmpty(title) && string.IsNullOrEmpty(tags))
            {
                query = query.Skip(pageIndex).Take(pageSize).OrderByDescending(x => x.articleid); ;
            }

            foreach (var id in query)
            {
                var val = await redisdb.GetValue(id.articleid.ToString());
                if (!string.IsNullOrEmpty(val))
                {
                    var article = Newtonsoft.Json.JsonConvert.DeserializeObject<UserArticles>(val);
                    article.star = id.star;
                    if (article.articleid == 0)
                    {
                        article.articleid = article.id;
                        redisdb.SetValue(article.articleid.ToString(), Newtonsoft.Json.JsonConvert.SerializeObject(article));
                    }
                    articlesList.Add(article);
                }   
            }

            if (!string.IsNullOrEmpty(title))
            {
                articlesList.RemoveAll(x => !x.title.Contains(title, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(tags))
            {
                var tempList = new List<UserArticles>();
                foreach (var item in articlesList)
                {
                    if (item.tags.Contains(tags, StringComparison.OrdinalIgnoreCase))
                    {
                        tempList.Add(item);
                    }

                }

                articlesList.Clear();
                articlesList.AddRange(tempList);
            }
            if (pageIndex > -1 && (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(tags)))
            {
                count = articlesList.Count;
                articlesList = articlesList.Skip(pageIndex).Take(pageSize).OrderByDescending(x => x.articleid).ToList();
            }
            return new Tuple<IEnumerable<UserArticles>, int>(articlesList.OrderByDescending(x => x.time), count);
        }


        public void DeleteUserArticle(string username, int id)
        {
            var article = db.UserArticles.FirstOrDefault(x => x.username == username && x.articleid == id);
            article.isviewed = true;
            db.SaveChanges();
        }
        public void StarUserArticle(string username, int id,bool star)
        {
            var article = db.UserArticles.FirstOrDefault(x => x.username == username && x.articleid == id);
            article.star = star;
            db.SaveChanges();
        }
    }
}
