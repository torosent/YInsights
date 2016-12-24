using Microsoft.Azure.Documents.Client;
using Microsoft.WindowsAzure.Storage.Table;
using SharedCode.Extentions;
using SharedCode.Poco;
using SharedCode.Providers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateUserArticles.Services
{
    public class GenerateArticles
    {
        private DocumentDBProvider documentDBProvider;
        private string connectionString;
        CloudTableClient tableClient;
        public GenerateArticles(string _connectionString, DocumentDBProvider _documentDBProvider, CloudTableClient _tableClient)
        {
            tableClient = _tableClient;
            connectionString = _connectionString;
            documentDBProvider = _documentDBProvider;
        }

        public async void GenerateUsersArticles()
        {
            CloudTable table = tableClient.GetTableReference("userstable");
            await table.CreateIfNotExistsAsync();

            while (true)
            {
                try
                {
                    TableQuery<UserEntity> tableQuery = new TableQuery<UserEntity>();
                    TableContinuationToken continuationToken = null;
                    do
                    {  
                        TableQuerySegment<UserEntity> tableQueryResult =
                            await table.ExecuteQuerySegmentedAsync(tableQuery, continuationToken);
                        continuationToken = tableQueryResult.ContinuationToken;

                        foreach (var item in tableQueryResult.Results)
                        {
                            var id = item.RowKey;
                            var topics = (List<string>)Newtonsoft.Json.JsonConvert.DeserializeObject(item.Topics, typeof(List<string>));
                            SearchForArticles(id, topics);

                            TableOperation deleteOperation = TableOperation.Delete(item);
                            await table.ExecuteAsync(deleteOperation);
                        }


                      
                    } while (continuationToken != null);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.Message);

                }
            }

        }

        /// <summary>
        /// Must change this method!!! It will fail with when the database will become larger.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="topics"></param>

        private async void SearchForArticles(string id, List<string> topics)
        {

            int topicIdx = 1;
            var queryBuilder = new StringBuilder("SELECT * FROM articles c WHERE (");

            var paramCollection = new Microsoft.Azure.Documents.SqlParameterCollection();
            foreach (var topic in topics)
            {

                queryBuilder.Append($" ARRAY_CONTAINS(c.lctags,@topic{topicIdx}) ");
                if (topicIdx < topics.Count)
                {
                    queryBuilder.Append("OR ");
                }
                else
                {
                    queryBuilder.Append(")");
                }
                paramCollection.Add(new Microsoft.Azure.Documents.SqlParameter { Name = $"@topic{topicIdx}", Value = topic });

                topicIdx++;
            }

            var queryString = queryBuilder.ToString();
            var query = new Microsoft.Azure.Documents.SqlQuerySpec(queryString, paramCollection);

            var articleQuery = documentDBProvider.Client.CreateDocumentQuery<Article>(
             UriFactory.CreateDocumentCollectionUri("articles", "article"), query).AsEnumerable();

            var articles = articleQuery.ToList().DistinctBy(x => x.title);

            var existingArticles = await GetUserExistingArticles(id);

            var finalList = articles.AsParallel().Where(x => !existingArticles.Any(y => y == x.Id)).Select(x => x.Id);
         

            InsertNewArticles(id, finalList);


        }
        private async void InsertNewArticles(string username, IEnumerable<string> articles)
        {
            if (articles.Any())
            {
                using (var sqlConnection =
                           new SqlConnection(connectionString))
                {
                    sqlConnection.Open();
                    var trans = sqlConnection.BeginTransaction();

                    foreach (var article in articles)
                    {
                        var insertCmd = new SqlCommand("INSERT INTO UserArticles(username,articleid,isviewed,addeddate) VALUES(@param1,@param2,@param3,@param4)", sqlConnection);
                        insertCmd.Parameters.Add(new SqlParameter("@param1", username));
                        insertCmd.Parameters.Add(new SqlParameter("@param2", article));
                        insertCmd.Parameters.Add(new SqlParameter("@param3", false));
                        insertCmd.Parameters.Add(new SqlParameter("@param4", DateTime.Now));
                        insertCmd.Transaction = trans;
                        await insertCmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"Commited document {article} for {username}");

                    }

                    trans.Commit();


                }
            }
        }

        private async Task<List<string>> GetUserExistingArticles(string id)
        {
            var articles = new List<string>();
            using (var sqlConnection =
                    new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                var sql = $"SELECT articleid FROM UserArticles where username = '{id}'";
                var cmd = new SqlCommand(sql, sqlConnection);
                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    var articleid = reader[0].ToString();
                    articles.Add(articleid);
                }
            }
            return articles;
        }
    }
}
