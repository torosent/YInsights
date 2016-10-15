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

        public User FindUserByUsername(string username)
        {
            return db.User.FirstOrDefault(x => x.Id == username);
        }

        public async void InsertUser(User user)
        {
            db.User.Add(user);
            await db.SaveChangesAsync();
        }
        public async void UpdateUser(User user)
        {
            db.User.Update(user);
            await db.SaveChangesAsync();
        }
    }
}
