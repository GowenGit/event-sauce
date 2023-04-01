using MongoDB.Bson;
using MongoDB.Driver;
using System;
using MongoDB.Bson.Serialization.Conventions;

namespace EventSauce.MongoDB
{
    public class MongoDBSauceStoreFactory
    {
        private readonly MongoClientSettings _clientSettings;
        private readonly string _database;
        private readonly string _collection;

        public MongoDBSauceStoreFactory(
            string connectionString,
            string database) : this(MongoClientSettings.FromConnectionString(connectionString), database, "events")
        {
        }

        public MongoDBSauceStoreFactory(
            MongoClientSettings clientSettings,
            string database) : this(clientSettings, database, "events")
        {
        }

        public MongoDBSauceStoreFactory(
            MongoClientSettings clientSettings,
            string database,
            string collection)
        {
            _clientSettings = clientSettings;
            _database = database;
            _collection = collection;

            FindEvents();
        }

        private void FindEvents()
        {
            try
            {
                var pack = new ConventionPack
                {
                    new IgnoreExtraElementsConvention(true)
                };

                var genericEventType = typeof(SaucyEvent<>);

                ConventionRegistry.Register("Sauce Conventions", pack, type => IsSubclassOfRawGeneric(genericEventType, type));
            }
            catch (Exception ex)
            {
                throw new EventSauceMongoDBException("Failed to register events and saucy aggregates", ex);
            }
        }

        public ISauceStore Create()
        {
            try
            {
                var collection = CreateCollection();

                return new MongoDBSauceStore(collection);
            }
            catch (Exception ex)
            {
                throw new EventSauceMongoDBException("Failed to create MongoDBSauceStore", ex);
            }
        }

        private IMongoCollection<BsonDocument> CreateCollection()
        {
            var client = new MongoClient(_clientSettings);

            var database = client.GetDatabase(_database);

            var settings = new MongoCollectionSettings
            {
                AssignIdOnInsert = false
            };

            var collection = database.GetCollection<BsonDocument>(_collection, settings);

            CreateIndex(collection, nameof(SaucyEvent<object>.AggregateId), false);

            return collection;
        }

        private static void CreateIndex(IMongoCollection<BsonDocument> collection, string field, bool isUnique)
        {
            var indexBuilder = Builders<BsonDocument>.IndexKeys;

            var keys = indexBuilder.Ascending(field);

            var options = new CreateIndexOptions { Unique = isUnique };

            var indexModel = new CreateIndexModel<BsonDocument>(keys, options);

            collection.Indexes.CreateOne(indexModel);
        }

        private static bool IsSubclassOfRawGeneric(Type generic, Type? toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var current = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;

                if (generic == current)
                {
                    return true;
                }

                toCheck = toCheck.BaseType!;
            }

            return false;
        }
    }
}
