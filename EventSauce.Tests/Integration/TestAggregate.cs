using System;

namespace EventSauce.Tests.Integration
{
    public class UserAggregate : SaucyAggregate
    {
        private UserAggregate() { }

        public string Email { get; private set; } = string.Empty;

        public UserAggregate(User id, string email, string authId)
        {
            IssueEvent(new UserCreatedEvent
            {
                Email = email,
                AuthId = authId,
                AggregateId = id
            });
        }

        public void ChangeEmail(string email)
        {
            IssueEvent(new UserChangeEmailEvent
            {
                Email = email
            });
        }

        public void Apply(UserCreatedEvent domainEvent)
        {
            Email = domainEvent.Email;
        }

        public void Apply(UserChangeEmailEvent domainEvent)
        {
            Email = domainEvent.Email;
        }
    }

    public record UserCreatedEvent : SaucyEvent
    {
        public string Email { get; init; } = string.Empty;

        public string AuthId { get; init; } = string.Empty;
    }

    public record UserChangeEmailEvent : SaucyEvent
    {
        public string Email { get; init; } = string.Empty;
    }

    public record User : SaucyAggregateId
    {
        public User(Guid id)
        {
            Id = id;
        }

        public static User NewUser()
        {
            return new (Guid.NewGuid());
        }
    }
}
