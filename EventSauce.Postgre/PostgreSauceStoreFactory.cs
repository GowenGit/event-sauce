using System;
using System.Collections.Generic;
using System.Reflection;
using Npgsql;

#pragma warning disable CA2100

namespace EventSauce.Postgre
{
    public class PostgreSauceStoreFactory
    {
        private readonly Assembly[] _assemblies;
        private readonly string _connectionString;
        private readonly string _tableName;

        private readonly List<Type> _eventTypes = new ();

        public PostgreSauceStoreFactory(
            Assembly[] assemblies,
            string connectionString,
            string tableName)
        {
            _assemblies = assemblies;
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
            catch (Exception e)
            {
                throw new EventSaucePostgreException("Failed to setup necessary tables", e);
            }
        }

        private void FindEvents()
        {
            try
            {
                foreach (var assembly in _assemblies)
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsSubclassOf(typeof(SaucyEvent)))
                        {
                            _eventTypes.Add(type);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new EventSaucePostgreException("Failed to setup necessary tables", e);
            }
        }

        public ISauceStore Create()
        {
            try
            {
                var connection = CreateConnection();

                return new PostgreSauceStore(connection, _tableName, _eventTypes);
            }
            catch (Exception e)
            {
                throw new EventSaucePostgreException("Failed to create PostgreSauceStore", e);
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
    }
}