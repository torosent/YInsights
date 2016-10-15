using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using YInsights.Web.Services;

namespace YInsights.Web.Controllers
{
    public class HomeController : Controller
    {
     

        
        private UserService userService;
        private TopicService topicService;
        private AIService aiService;
        public HomeController(UserService _userService,TopicService _topicService,AIService _aiService)
        {
            userService = _userService;
            topicService = _topicService;
            aiService = _aiService;
        }
        [Authorize]
        public IActionResult Index()
        {
            try
            {
                string username = User.Claims.FirstOrDefault(y => y.Type == "name").Value;
                var user = userService.FindUserByUsername(username);     
                aiService.TrackUser("Homepage", username);
                if (user == null || string.IsNullOrEmpty(user.topics))
                {
                    ViewBag.user = string.Empty;
                }
                else
                {
                    ViewBag.user = user.Id;
                   
                }

            ViewBag.title = "Home";
            }
            catch (Exception ex)
            {
                aiService.TrackException(ex);
            }
            return View();
        }

        [Authorize]
        public async Task<IActionResult> WordCloud()
        {
            try
            {
                var topicsList = await topicService.GetTopics(200);
                return Json(topicsList);
            }
            catch(Exception ex)
            {
                aiService.TrackException(ex);
            }
            return null;
        }

        

        public IActionResult Error()
        {
            return View();
        }

       
    }
}
