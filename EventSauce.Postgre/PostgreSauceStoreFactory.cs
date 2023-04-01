using Npgsql;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace EventSauce.Postgre
{
    public class PostgreSauceStoreFactory
    {
        private readonly Assembly[] _assemblies;
        private readonly JsonSerializerOptions _options;
        private readonly string _connectionString;
        private readonly string _tableName;

        private readonly Dictionary<string, Type> _eventTypes = new();

        public PostgreSauceStoreFactory(
            Assembly[] assemblies,
            JsonSerializerOptions options,
            string connectionString) : this(assemblies, options, connectionString, "saucy_events")
        {
        }

        public PostgreSauceStoreFactory(
            Assembly[] assemblies,
            JsonSerializerOptions options,
            string connectionString,
            string tableName)
        {
            _assemblies = assemblies;
            _options = options;
            _connectionString = connectionString;
            _tableName = tableName;

            SetupTables();
            FindEvents();
        }

        private void SetupTables()
        {
            try
            {
                using var connection = CreateConnection();

                var sql = GetCommand(PostgreSqlCommands.SetupTablesCommand);

                using var command = new NpgsqlCommand(sql, connection);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new EventSaucePostgreException("Failed to setup necessary tables", ex);
            }
        }

        private void FindEvents()
        {
            try
            {
                var genericEventType = typeof(SaucyEvent<>);

                foreach (var assembly in _assemblies)
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (IsSubclassOfRawGeneric(genericEventType, type))
                        {
                            _eventTypes.Add(type.Name, type);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new EventSaucePostgreException("Failed to register events and saucy aggregates", ex);
            }
        }

        public ISauceStore Create()
        {
            try
            {
                var connection = CreateConnection();

                return new PostgreSauceStore(connection, _tableName, _eventTypes, _options);
            }
            catch (Exception ex)
            {
                throw new EventSaucePostgreException("Failed to create PostgreSauceStore", ex);
            }
        }

        private NpgsqlConnection CreateConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);

            connection.Open();

            return connection;
        }

        private string GetCommand(string text)
        {
            return string.Format(text, _tableName);
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
