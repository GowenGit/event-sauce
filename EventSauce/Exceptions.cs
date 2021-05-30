using System;
using System.Runtime.Serialization;

namespace EventSauce
{
    [Serializable]
    public class EventSauceRepositoryException : Exception
    {
        public EventSauceRepositoryException() { }

        public EventSauceRepositoryException(string message) : base(message) { }

        public EventSauceRepositoryException(string message, Exception inner) : base(message, inner) { }

        protected EventSauceRepositoryException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class EventSauceException : Exception
    {
        public EventSauceException() { }

        public EventSauceException(string message) : base(message) { }

        public EventSauceException(string message, Exception inner) : base(message, inner) { }

        protected EventSauceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class EventSauceAggregateNotFoundException : EventSauceException
    {
        public EventSauceAggregateNotFoundException() { }

        public EventSauceAggregateNotFoundException(string message) : base(message) { }

        public EventSauceAggregateNotFoundException(string message, Exception inner) : base(message, inner) { }

        protected EventSauceAggregateNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class EventSauceCommunicationException : EventSauceException
    {
        public EventSauceCommunicationException() { }

        public EventSauceCommunicationException(string message) : base(message) { }

        public EventSauceCommunicationException(string message, Exception inner) : base(message, inner) { }

        protected EventSauceCommunicationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
