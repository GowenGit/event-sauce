using System;
using Npgsql;

namespace EventSauce.Postgre
{
    internal static class SauceDbExtensions
    {
        public static string GetSaucyString(this NpgsqlDataReader reader, int index)
        {
            return reader.GetString(index);
        }

        public static long GetSaucyLong(this NpgsqlDataReader reader, int index)
        {
            return reader.GetInt64(index);
        }

        public static DateTime GetSaucyDate(this NpgsqlDataReader reader, int index)
        {
            return reader.GetDateTime(index);
        }

        public static Guid GetSaucyGuid(this NpgsqlDataReader reader, int index)
        {
            return (Guid)reader.GetValue(index);
        }
    }
}