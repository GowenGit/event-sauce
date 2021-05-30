using System;
using System.Runtime.Serialization;

namespace EventSauce.Postgre
{
    [Serializable]
    public class EventSaucePostgreException : Exception
    {
        public EventSaucePostgreException() { }

        public EventSaucePostgreException(string message) : base(message) { }

        public EventSaucePostgreException(string message, Exception inner) : base(message, inner) { }

        protected EventSaucePostgreException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
