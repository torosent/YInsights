using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SharedCode.Providers;
using Microsoft.Azure.Documents.Client;
using SharedCode.Poco;
using System.Net.Http;
using System.Data.SqlClient;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using GenerateUserArticles.Services;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace GenerateUserArticles.Controllers
{
    [Route("api/[controller]")]
    public class CalculateController : Controller
    {
        private string connectionString;
        CloudTableClient tableClient;
        DocumentDBProvider documentDBProvider;
        public CalculateController(DocumentDBProvider _documentDBProvider, SqlProvider _connectionString, CloudTableClient _tableClient)
        {
            tableClient = _tableClient;
            connectionString = _connectionString.ConnectionString;
            documentDBProvider = _documentDBProvider;
        }
        // GET: api/values
        [HttpGet]
        public void Get()
        {
            GetUsersAndTopics();
        }

        private async void GetUsersAndTopics()
        {

            CloudTable table = tableClient.GetTableReference("userstable");
            await table.CreateIfNotExistsAsync();


            using (var sqlConnection =
                     new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                var sql = $"SELECT Id,topics FROM [User] where topics is not null";
                var cmd = new SqlCommand(sql, sqlConnection);
                var reader = await cmd.ExecuteReaderAsync();


                while (reader.Read())
                {
                    var id = reader[0].ToString();
                    var topics = reader[1].ToString().ToLower();

                    var userEntity = new UserEntity(id);
                    userEntity.Topics = topics;
                    TableOperation retrieveOperation = TableOperation.Retrieve<UserEntity>(userEntity.PKey,id);
                    TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

                 
                    if (retrievedResult.Result == null)
                    {
                        TableOperation insertOperation = TableOperation.Insert(userEntity);
                        await table.ExecuteAsync(insertOperation);
                    }
                }
            }
        }

    }
}
