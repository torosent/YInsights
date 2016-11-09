using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using cloudscribe.Syndication.Models.Rss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using YInsights.Web.Services;
using YInsights.Web.Extentions;
using System.Linq;
namespace YInsights.Web.Providers
{
    public class RssChannelProvider : IChannelProvider
    {
        public RssChannelProvider(

            IHttpContextAccessor contextAccessor,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccesor,
            UserArticleService userArticleService
        )
        {

            this.contextAccessor = contextAccessor;
            this.urlHelperFactory = urlHelperFactory;
            this.actionContextAccesor = actionContextAccesor;
            this.userArticleService = userArticleService;
        }

        private IUrlHelperFactory urlHelperFactory;
        private IActionContextAccessor actionContextAccesor;
        private IHttpContextAccessor contextAccessor;
        private UserArticleService userArticleService;
        private int maxFeedItems = 100;

        public string Name { get; } = "YInsights.Web.Providers.RssChannelProvider";

        public async Task<RssChannel> GetChannel(CancellationToken cancellationToken = default(CancellationToken))
        {

            string username = contextAccessor.HttpContext.Request.Query["id"];

            var channel = new RssChannel();
            channel.Generator = "YInsights";
            channel.Description = $"Feed for {username}";
            channel.Title = "YInsights perosonalized feed";
            channel.Language = new CultureInfo("en-US");
       
            channel.PublicationDate = DateTime.Now;
            channel.TimeToLive = 5;
            var category = new RssCategory("articles");
            channel.Categories.Add(category);
            channel.Image = new RssImage(new Uri("https://yinsights.torosent.com"), "YInsights", new Uri("https://yinsights.torosent.com/images/Logomakr_5r1SVn.png"));


            var baseUrl = string.Concat(
                        contextAccessor.HttpContext.Request.Scheme,
                        "://",
                        contextAccessor.HttpContext.Request.Host.ToUriComponent()
                        );



            var feedUrl = string.Concat(
                          contextAccessor.HttpContext.Request.Scheme,
                          "://",
                          contextAccessor.HttpContext.Request.Host.ToUriComponent(),
                          contextAccessor.HttpContext.Request.PathBase.ToUriComponent(),
                          contextAccessor.HttpContext.Request.Path.ToUriComponent(),
                          contextAccessor.HttpContext.Request.QueryString.ToUriComponent());
            channel.SelfLink = new Uri(feedUrl);
            channel.Link = channel.SelfLink;

            var items = new List<RssItem>();

            var tuple =  userArticleService.GetUserUnviewedArticles(username,null,null,-1, maxFeedItems);
            var articles = tuple.Item1.Take(maxFeedItems);
            foreach (var item in articles)
            {
                var rssItem = new RssItem();
                rssItem.Author = "YInsights";
                rssItem.Categories.Add(category);

                rssItem.Guid = new RssGuid(item.url, true);
                rssItem.Link = new Uri(item.url);
                rssItem.PublicationDate = ConvertExtentions.UnixTimeStampToDateTime(item.time);
                rssItem.Title = item.title;
                items.Add(rssItem);
            }



            channel.Items = items;
            return channel;
        }


    }
}
