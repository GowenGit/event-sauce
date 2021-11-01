using System;
using System.Runtime.Serialization;

namespace EventSauce.MongoDB
{
    [Serializable]
    public class EventSauceMongoDBException : Exception
    {
        public EventSauceMongoDBException() { }

        public EventSauceMongoDBException(string message) : base(message) { }

        public EventSauceMongoDBException(string message, Exception inner) : base(message, inner) { }

        protected EventSauceMongoDBException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
