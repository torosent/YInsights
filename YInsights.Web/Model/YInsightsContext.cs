using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace YInsights.Web.Model
{
    public class YInsightsContext : DbContext
    {
        public DbSet<User> User { get; set; }
        public DbSet<Topics> Topics { get; set; }
        public DbSet<UserArticles> UserArticles { get; set; }

        public YInsightsContext(DbContextOptions<YInsightsContext> options)
      :base(options)
    { }
    }

    

  
    
}
