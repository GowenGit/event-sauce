using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;

[assembly: InternalsVisibleTo("EventSauce.Tests")]

#pragma warning disable CA2100

namespace EventSauce.Postgre
{
    internal static class PostgreSqlCommands
    {
        public const string SetupTablesCommand = @"
CREATE TABLE IF NOT EXISTS `{0}` (
    `AggregateId` uuid NOT NULL,
    `AggregateIdType` varchar(256) NOT NULL,
    `AggregateVersion` int8 NOT NULL,
    `Created` timestamp(0) NOT NULL,
    `EventId` uuid NOT NULL UNIQUE,
    `EventType` varchar(1024) NOT NULL,
    `EventData` jsonb NOT NULL,
    UNIQUE (`AggregateIdType`,`AggregateId`,`AggregateVersion`)
);

CREATE INDEX IF NOT EXISTS {0}_aggregate_idx ON {0}(`AggregateIdType`,`AggregateId`);
";

        public const string InsertEvent = @"
INSERT INTO `{0}`
(
    `AggregateId`, `AggregateIdType`, `AggregateVersion`, `Created`, `EventId`, `EventType`, `EventData`
)
VALUES
(
    @aggregate_id, @aggregate_id_type, @aggregate_version, @created, @event_id, @event_type, @event_data
)
";
    }

    internal class PostgreSauceStore : ISauceStore, IDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _tableName;
        private readonly IReadOnlyList<Type> _eventTypes;

        public PostgreSauceStore(
            NpgsqlConnection connection,
            string tableName,
            IReadOnlyList<Type> eventTypes)
        {
            _connection = connection;
            _tableName = tableName;
            _eventTypes = eventTypes;
        }

        public Task<IEnumerable<SaucyEvent>> ReadEvents(SaucyAggregateId id)
        {
            throw new System.NotImplementedException();
        }

        public async Task AppendEvent(SaucyEvent sourceEvent)
        {
            var eventData = JsonSerializer.Serialize(sourceEvent, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var eventType = sourceEvent.GetType().Name;

            var sql = GetCommand(PostgreSqlCommands.InsertEvent);

            await using var command = new NpgsqlCommand(sql, _connection);

            command.Parameters.AddWithValue("aggregate_id", sourceEvent.AggregateId?.Id ?? throw new ArgumentNullException(nameof(sourceEvent.AggregateId)));
            command.Parameters.AddWithValue("aggregate_id_type", sourceEvent.AggregateId?.IdType ?? throw new ArgumentNullException(nameof(sourceEvent.AggregateId)));
            command.Parameters.AddWithValue("aggregate_version", sourceEvent.AggregateVersion);
            command.Parameters.AddWithValue("created", sourceEvent.Created);
            command.Parameters.AddWithValue("event_id", sourceEvent.EventId);
            command.Parameters.AddWithValue("event_type", eventType);
            command.Parameters.AddWithValue("event_data", eventData);

            await command.ExecuteNonQueryAsync();
        }

        private string GetCommand(string text)
        {
            return string.Format(text, _tableName);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
