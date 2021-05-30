using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

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

        public const string SelectEvent = @"
SELECT * FROM `{0}`
WHERE `AggregateId` = @aggregate_id AND `AggregateIdType` = @aggregate_id_type;
";
    }

    internal static class SauceDbExtensions
    {
        public static string GetSaucyString(this NpgsqlDataReader reader, string name)
        {
            return reader.GetString(reader.GetOrdinal(name));
        }

        public static long GetSaucyLong(this NpgsqlDataReader reader, string name)
        {
            return reader.GetInt64(reader.GetOrdinal(name));
        }

        public static DateTime GetSaucyDate(this NpgsqlDataReader reader, string name)
        {
            return reader.GetDateTime(reader.GetOrdinal(name));
        }

        public static Guid GetSaucyGuid(this NpgsqlDataReader reader, string name)
        {
            return (Guid)reader.GetValue(reader.GetOrdinal(name));
        }
    }

    internal class PostgreSauceStore : ISauceStore, IDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _tableName;

        private readonly IReadOnlyList<Type> _eventTypes;
        private readonly IReadOnlyList<Type> _aggregateTypes;

        public PostgreSauceStore(
            NpgsqlConnection connection,
            string tableName,
            IReadOnlyList<Type> eventTypes,
            IReadOnlyList<Type> aggregateTypes)
        {
            _connection = connection;
            _tableName = tableName;
            _eventTypes = eventTypes;
            _aggregateTypes = aggregateTypes;
        }

        public async Task<IEnumerable<SaucyEvent>> ReadEvents(SaucyAggregateId id)
        {
            try
            {
                var sql = GetCommand(PostgreSqlCommands.SelectEvent);

                await using var command = new NpgsqlCommand(sql, _connection);

                command.Parameters.AddWithValue("aggregate_id", id.Id);
                command.Parameters.AddWithValue("aggregate_id_type", id.IdType);

                await using var reader = await command.ExecuteReaderAsync();

                var result = new List<SaucyEvent>();

                SaucyAggregateId? aggregate = null;

                while (reader.Read())
                {
                    var eventTypeName = reader.GetSaucyString("EventType");
                    var eventData = reader.GetSaucyString("EventData");
                    var eventId = reader.GetSaucyGuid("EventId");
                    var created = reader.GetSaucyDate("Created");
                    var aggregateVersion = reader.GetSaucyLong("AggregateVersion");

                    var eventType = _eventTypes.First(x =>
                        x.Name.Equals(eventTypeName, StringComparison.InvariantCultureIgnoreCase));

                    if (aggregate == null)
                    {
                        var aggregateId = reader.GetSaucyGuid("AggregateId");
                        var aggregateIdTypeName = reader.GetSaucyString("AggregateIdType");
                        var aggregateIdType = _aggregateTypes.First(x =>
                            x.Name.Equals(aggregateIdTypeName, StringComparison.InvariantCultureIgnoreCase));
                        aggregate = (SaucyAggregateId)Activator.CreateInstance(aggregateIdType, aggregateId)!;
                    }

                    var sourceEvent = (SaucyEvent)JsonSerializer.Deserialize(eventData, eventType)!;

                    result.Add(sourceEvent with
                    {
                        AggregateVersion = aggregateVersion,
                        AggregateId = aggregate,
                        EventId = eventId,
                        Created = created
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new EventSaucePostgreException($"Failed to fetch events for {id}", ex);
            }
        }

        public async Task AppendEvent(SaucyEvent sourceEvent)
        {
            try
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
            catch (Exception ex)
            {
                throw new EventSaucePostgreException($"Failed to insert event {sourceEvent}", ex);
            }
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
