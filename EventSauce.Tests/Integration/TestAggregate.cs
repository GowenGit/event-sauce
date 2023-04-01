using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace EventSauce.Tests.Integration
{
    public class UserAggregate<T> : SaucyAggregate<T>
    {
        private UserAggregate() { }

        public string Email { get; private set; } = string.Empty;

        public UserAggregate(T id, string email, string authId)
        {
            Id = id;

            IssueEvent(new UserCreatedEvent<T>
            {
                Email = email,
                AuthId = authId,
                AggregateId = id
            });
        }

        public void ChangeEmail(string email)
        {
            IssueEvent(new UserChangeEmailEvent<T>
            {
                Email = email
            });
        }

        public void ChangeEmailToInvalid(string email)
        {
            IssueEvent(new UserChangeEmailToInvalidEvent<T>
            {
                Email = email,
                IsValid = true
            });
        }

        /// <summary>
        /// Apply events need to be defined
        /// for each applicable event since this is
        /// how data is loaded back to the aggregate when
        /// reading from the database.
        /// </summary>
        public void Apply(UserCreatedEvent<T> domainEvent)
        {
            Email = domainEvent.Email;
        }

        public void Apply(UserChangeEmailEvent<T> domainEvent)
        {
            Email = domainEvent.Email;
        }
    }

    public record UserCreatedEvent<T> : SaucyEvent<T>
    {
        public string Email { get; init; } = string.Empty;

        public string AuthId { get; init; } = string.Empty;
    }

    public record UserChangeEmailEvent<T> : SaucyEvent<T>
    {
        public string Email { get; init; } = string.Empty;
    }

    public record UserChangeEmailToInvalidEvent<T> : UserChangeEmailEvent<T>
    {
        public bool IsValid { get; init; } = false;
    }

    public record User(ObjectId Value)
    {
        public static User New() => new(ObjectId.GenerateNewId());
    }

    public class UserSerializer : SerializerBase<User>
    {
        private readonly IBsonSerializer<ObjectId> _serializer;
        public UserSerializer(IBsonSerializer<ObjectId> serializer) => _serializer = serializer;

        public override User Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            => new(_serializer.Deserialize(context, args));

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, User value)
            => _serializer.Serialize(context, args, value.Value);
    }
}
