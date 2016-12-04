using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenerateUserArticles.Services
{

    public class UserEntity : TableEntity
    {
      
        public UserEntity(string id)
        {
            this.PartitionKey = PKey;
            this.RowKey = id;
        }

        public UserEntity() { }

        public string Topics { get; set; }
        public string PKey { get { return "users"; } }

    }
}