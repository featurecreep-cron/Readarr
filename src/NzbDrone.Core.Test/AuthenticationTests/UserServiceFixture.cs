using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.AuthenticationTests
{
    [TestFixture]
    public class UserServiceFixture : DbTest<UserService, User>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IUserRepository>(Mocker.Resolve<UserRepository>());
        }

        [Test]
        public void should_store_a_salted_hash_not_the_sha256()
        {
            var user = Subject.Add("admin", "password");

            user.Salt.Should().NotBeNullOrWhiteSpace();
            user.Iterations.Should().Be(220000);
            user.Password.Should().NotBe("password".SHA256Hash());
        }

        [Test]
        public void should_find_a_user_with_the_right_password()
        {
            Subject.Add("admin", "password");

            Subject.FindUser("Admin", "password").Should().NotBeNull();
        }

        [Test]
        public void should_not_find_a_user_with_the_wrong_password()
        {
            Subject.Add("admin", "password");

            Subject.FindUser("admin", "wrong").Should().BeNull();
        }

        [Test]
        public void should_upgrade_an_unsalted_sha256_password_on_login()
        {
            Db.Insert(new User { Identifier = Guid.NewGuid(), Username = "admin", Password = "password".SHA256Hash() });

            Subject.FindUser("admin", "password").Should().NotBeNull();

            var stored = Db.Single<User>();
            stored.Salt.Should().NotBeNullOrWhiteSpace();
            stored.Password.Should().NotBe("password".SHA256Hash());
            Subject.FindUser("admin", "password").Should().NotBeNull();
        }

        [Test]
        public void should_rehash_a_password_stored_with_fewer_iterations()
        {
            var user = Subject.Add("admin", "password");
            user.Iterations = 10000;
            user.Password = Convert.ToBase64String(Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivation.Pbkdf2(
                "password", Convert.FromBase64String(user.Salt), Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivationPrf.HMACSHA512, 10000, 32));
            Db.Update(user);

            Subject.FindUser("admin", "password").Should().NotBeNull();

            Db.Single<User>().Iterations.Should().Be(220000);
        }
    }
}
