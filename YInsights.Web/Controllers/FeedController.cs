using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using YInsights.Web.Providers;
using cloudscribe.Syndication.Models.Rss;
using YInsights.Web.Extentions;


namespace YInsights.Web.Controllers
{
    [Route("api/[controller]")]
    public class FeedController : Controller
    {
        RssChannelProvider feed;
        public FeedController(RssChannelProvider feed)
        {
            this.feed = feed;
        }
      
        [HttpGet]
        public async Task<ActionResult> Get(string id)
        {
            var currentChannel= await feed.GetChannel();
            var xmlFormatter = new DefaultXmlFormatter();
            var xml = xmlFormatter.BuildXml(currentChannel);

            return new XmlResult(xml);
            
        }

      
    }
}
