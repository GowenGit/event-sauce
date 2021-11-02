using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;

#pragma warning disable CA2100

namespace EventSauce.MongoDB
{
    public class MongoDBSauceStoreFactory
    {
        private readonly Assembly[] _assemblies;
        private readonly string _connectionString;
        private readonly string _database;
        private readonly string _collection;

        public MongoDBSauceStoreFactory(
            Assembly[] assemblies,
            string connectionString,
            string database) : this(assemblies, connectionString, database, "events") { }

        public MongoDBSauceStoreFactory(
            Assembly[] assemblies,
            string connectionString,
            string database,
            string collection)
        {
            _assemblies = assemblies;
            _connectionString = connectionString;
            _database = database;
            _collection = collection;

            FindEvents();
        }

        private void FindEvents()
        {
            try
            {
                var saucyEventTypes = new List<Type>();

                foreach (var assembly in _assemblies)
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsSubclassOf(typeof(SaucyEvent)))
                        {
                            RegisterType(type);

                            saucyEventTypes.Add(type);
                        }

                        if (type.IsSubclassOf(typeof(SaucyAggregateId)))
                        {
                            RegisterType(type);
                        }
                    }
                }

                // Setup base class deserialization
                BsonClassMap.RegisterClassMap<SaucyEvent>(map =>
                {
                    map.AutoMap();
                    map.SetIsRootClass(true);

                    foreach (var type in saucyEventTypes)
                    {
                        map.AddKnownType(type);
                    }
                });
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
            var client = new MongoClient(_connectionString);

            var database = client.GetDatabase(_database);

            var settings = new MongoCollectionSettings
            {
                AssignIdOnInsert = false
            };

            var collection = database.GetCollection<BsonDocument>(_collection, settings);

            CreateIndex(collection, nameof(SaucyEvent.AggregateId), false);

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

        private static void RegisterType(Type type)
        {
            var map = new BsonClassMap(type);

            map.AutoMap();
            map.SetIgnoreExtraElements(true);

            BsonClassMap.RegisterClassMap(map);
        }
    }
}