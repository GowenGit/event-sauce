using EventSauce.Extensions.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using EventSauce.Postgre;
using Xunit;

namespace EventSauce.Tests.Integration
{
    public class PostgreTests
    {
        public static ISaucyRepository GetSutObject()
        {
            var services = new ServiceCollection();

            services.AddEventSauce(options =>
            {
                var factory = new PostgreSauceStoreFactory(
                    new[] {typeof(PostgreTests).Assembly},
                    "Server=localhost;Port=5433;Database=sauce;User Id=postgres;Password=password;",
                    "sauce_jar");

                options.Store = x => factory.Create();
            });

            var provider = services.BuildServiceProvider();

            return provider.GetService<ISaucyRepository>() ?? throw new ArgumentNullException(nameof(ISaucyRepository));
        }

        [Fact]
        public async Task Aggregate_WhenCreatedAndSaved_ShouldHaveNoUncommittedEvents()
        {
            var userId = User.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate(userId, email, auth);

            const string newEmail = "changed@gmail.com";

            user.ChangeEmail(newEmail);

            var sut = GetSutObject();

            await sut.Save(user);

            var events = user.GetUncommittedEvents().ToList();

            Assert.Empty(events);

            Assert.Equal(userId, user.Id);
            Assert.Equal(newEmail, user.Email);
            Assert.Equal(1, user.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedAndSaved_ShouldRetrieve()
        {
            var userId = User.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate(userId, email, auth);

            var sut = GetSutObject();

            var repoUser = await sut.GetById<UserAggregate>(userId);

            Assert.Null(repoUser);

            await sut.Save(user);

            repoUser = await sut.GetById<UserAggregate>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser?.Id);
            Assert.Equal(email, repoUser?.Email);
            Assert.Equal(0, repoUser?.Version);
        }

        [Fact]
        public async Task Aggregate_WhenCreatedChangedAndSaved_ShouldRetrieve()
        {
            var userId = User.NewUser();

            const string? email = "origina@gmail.com";
            const string? auth = "auth_id";

            var user = new UserAggregate(userId, email, auth);

            const string newEmail = "changed@gmail.com";

            user.ChangeEmail(newEmail);

            var sut = GetSutObject();

            var repoUser = await sut.GetById<UserAggregate>(userId);

            Assert.Null(repoUser);

            await sut.Save(user);

            repoUser = await sut.GetById<UserAggregate>(userId);

            Assert.NotNull(repoUser);

            Assert.Equal(userId, repoUser?.Id);
            Assert.Equal(newEmail, repoUser?.Email);
            Assert.Equal(1, repoUser?.Version);
        }
    }
}
