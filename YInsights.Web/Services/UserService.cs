using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YInsights.Web.Model;

namespace YInsights.Web.Services
{
    public class UserService
    {
        private readonly YInsightsContext db;
        public UserService(YInsightsContext _db)
        {
            db = _db;
        }

        public User FindUserById(string username)
        {
            return db.User.FirstOrDefault(x => x.Id == username);
        }

        public void InsertUser(User user)
        {
            db.User.Add(user);
            db.SaveChanges();
        }
        public void UpdateUser(User user)
        {
            db.User.Update(user);
            db.SaveChanges();
        }


    }
}
