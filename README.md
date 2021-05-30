![Sauce](assets/icon.png)

# Opinionated Event Sourcing

![Master Build](https://github.com/GowenGit/event-sauce/workflows/Master%20Build/badge.svg) ![Deploy](https://github.com/GowenGit/event-sauce/workflows/Deploy/badge.svg) [![NuGet](https://img.shields.io/nuget/v/EventSauce.svg)](https://www.nuget.org/packages/EventSauce)

A simple NuGet package to add event sourcing support to any project.

# Example

1) Register dependencies:

```csharp
services.AddEventSauce(options =>
{
    var factory = new PostgreSauceStoreFactory(
        new[]
        {
            Assembly.GetExecutingAssembly(), // Assemblies where to look for events and aggregate IDs
        },
        connectionString);

    options.Store = _ => factory.Create();
});
```

2) Define events:

```csharp
public record UserCreatedEvent : SaucyEvent
{
    public string Email { get; init; } = string.Empty;

    public string AuthId { get; init; } = string.Empty;
}

public record UserChangeEmailEvent : SaucyEvent
{
    public string Email { get; init; } = string.Empty;
}
```

3) Define aggregate IDs:

```csharp
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
```

4) Build your aggregate:

```csharp
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

    /// <summary>
    /// Apply events need to be defined
    /// for each applicable event since this is
    /// how data is loaded back to the aggregate when
    /// reading from the database.
    /// </summary>
    public void Apply(UserCreatedEvent domainEvent)
    {
        Email = domainEvent.Email;
    }

    public void Apply(UserChangeEmailEvent domainEvent)
    {
        Email = domainEvent.Email;
    }
}
```

At this point you have working aggregates that you can easily interact
and build all desired business logic with. However it would be useful 
to be able to persist and retrieve aggregates from the database.

5) Persist/retrieve aggregates from the database:

```csharp
var userId = User.NewUser();

const string? email = "origina@gmail.com";
const string? auth = "auth_id";

var user = new UserAggregate(userId, email, auth);

const string newEmail = "changed@gmail.com";

user.ChangeEmail(newEmail);

await _saucyRepository.Save(user); // _saucyRepository is injected instance of ISaucyRepository

var repoUser = await _saucyRepository.GetById<UserAggregate>(userId);
```