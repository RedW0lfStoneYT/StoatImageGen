using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using Newtonsoft.Json.Serialization;
using RevoltImageGenApi.Controllers.ImageGen;
using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace RevoltImageGenApi.Controllers
{
    public class Utils
    {
        private static readonly MongoClient client = new MongoClient(ConfigAndDefaults.mongoConnectionString);
        public static readonly IMongoDatabase database = client.GetDatabase(ConfigAndDefaults.mongoDatabaseName);

        public static async Task<(T data, bool isNew)> getDataAsync<T>(string serverId) where T : IServerData, new()
        {
            IMongoCollection<T> db = getCollection<T>(ConfigAndDefaults.mongoCollectionName);
            T result = await db.Find<T>(getFilter<T>(serverId)).FirstOrDefaultAsync();
            bool isNew = result == null;
            if (isNew)
            {
                result = new T();
                result.ServerID = serverId;
            }
            
            return (result, isNew);
        }

        public static (T data, bool isNew) getData<T>(string serverId) where T : IServerData, new()
        {
            IMongoCollection<T> db = getCollection<T>(ConfigAndDefaults.mongoCollectionName);
            T result = db.Find(getFilter<T>(serverId)).FirstOrDefault();
            bool isNew = result == null;
            if (isNew)
            {
                result = new T();
                result.ServerID = serverId;
            }
            return (result, isNew);
        }

        public static bool authenticate(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;
            string tokenHash = Sha256(token);
            var filter = Builders<BsonDocument>.Filter.Eq("token", tokenHash);
            return database
                .GetCollection<BsonDocument>(ConfigAndDefaults.mongoAuthCollectionName)
                .Find(filter).CountDocuments() > 0;
        }

        public static string Sha256(string text)
        {
            var sb = new StringBuilder();
            using (var hash = SHA256.Create())
            {
                var result = hash.ComputeHash(Encoding.UTF8.GetBytes(text));
                for (int i = 0; i < result.Length; i++)
                    sb.Append(result[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public static IMongoCollection<T> getCollection<T>(string collectionName)
        {
            return database.GetCollection<T>(collectionName);
        }

        public static FilterDefinition<T> getFilter<T>(string serverId)
        {
            return Builders<T>.Filter.Eq("SERVERID", serverId);
        }

        public static bool getDatabaseStatus()
        {
            try
            {
                database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
