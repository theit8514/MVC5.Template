using AutoMapper;
using AutoMapper.QueryableExtensions;
using MvcTemplate.Components.Security;
using MvcTemplate.Data.Core;
using MvcTemplate.Objects;
using MvcTemplate.Services;
using MvcTemplate.Tests.Data;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using Xunit;
using Xunit.Extensions;

namespace MvcTemplate.Tests.Unit.Services
{
    public class AccountServiceTests : IDisposable
    {
        private AccountService service;
        private TestingContext context;
        private Account account;
        private IHasher hasher;

        public AccountServiceTests()
        {
            context = new TestingContext();
            hasher = Substitute.For<IHasher>();
            hasher.HashPassword(Arg.Any<String>()).Returns(info => info.Arg<String>() + "Hashed");

            context.DropData();
            SetUpData();

            Authorization.Provider = Substitute.For<IAuthorizationProvider>();
            service = new AccountService(new UnitOfWork(context), hasher);
            service.CurrentAccountId = account.Id;
        }
        public void Dispose()
        {
            Authorization.Provider = null;
            HttpContext.Current = null;
            service.Dispose();
            context.Dispose();
        }

        #region Get<TView>(Int32 id)

        [Fact]
        public void Get_ReturnsViewById()
        {
            AccountView actual = service.Get<AccountView>(account.Id);
            AccountView expected = Mapper.Map<AccountView>(account);

            Assert.Equal(expected.CreationDate, actual.CreationDate);
            Assert.Equal(expected.RoleTitle, actual.RoleTitle);
            Assert.Equal(expected.IsLocked, actual.IsLocked);
            Assert.Equal(expected.Username, actual.Username);
            Assert.Equal(expected.Email, actual.Email);
            Assert.Equal(expected.Id, actual.Id);
        }

        #endregion

        #region GetViews()

        [Fact]
        public void GetViews_ReturnsAccountViews()
        {
            using (IEnumerator<AccountView> actual = service.GetViews().GetEnumerator())
            using (IEnumerator<AccountView> expected = context
                .Set<Account>()
                .ProjectTo<AccountView>()
                .OrderByDescending(view => view.Id)
                .GetEnumerator())
            {
                while (expected.MoveNext() | actual.MoveNext())
                {
                    Assert.Equal(expected.Current.CreationDate, actual.Current.CreationDate);
                    Assert.Equal(expected.Current.RoleTitle, actual.Current.RoleTitle);
                    Assert.Equal(expected.Current.IsLocked, actual.Current.IsLocked);
                    Assert.Equal(expected.Current.Username, actual.Current.Username);
                    Assert.Equal(expected.Current.Email, actual.Current.Email);
                    Assert.Equal(expected.Current.Id, actual.Current.Id);
                }
            }
        }

        #endregion

        #region IsLoggedIn(IPrincipal user)

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsLoggedIn_ReturnsIsAuthenticated(Boolean isAuthenticated)
        {
            IPrincipal user = Substitute.For<IPrincipal>();
            user.Identity.IsAuthenticated.Returns(isAuthenticated);

            Boolean actual = service.IsLoggedIn(user);
            Boolean expected = isAuthenticated;

            Assert.Equal(expected, actual);
        }

        #endregion

        #region IsActive(Int32 id)

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsActive_ReturnsAccountState(Boolean isLocked)
        {
            context.Set<Account>().Attach(account);
            account.IsLocked = isLocked;
            context.SaveChanges();

            Boolean actual = service.IsActive(account.Id);
            Boolean expected = !isLocked;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsActive_NoAccount_ReturnsFalse()
        {
            Assert.False(service.IsActive(0));
        }

        #endregion

        #region Recover(AccountRecoveryView view)

        [Fact]
        public void Recover_NoEmail_ReturnsNull()
        {
            AccountRecoveryView view = ObjectFactory.CreateAccountRecoveryView();
            view.Email = "not@existing.email";

            Assert.Null(service.Recover(view));
        }

        [Fact]
        public void Recover_Information()
        {
            AccountRecoveryView view = ObjectFactory.CreateAccountRecoveryView();
            account.RecoveryTokenExpirationDate = DateTime.Now.AddMinutes(30);
            view.Email = view.Email.ToUpper();

            String expectedToken = service.Recover(view);

            Account actual = context.Set<Account>().AsNoTracking().Single();
            Account expected = account;

            Assert.InRange(actual.RecoveryTokenExpirationDate.Value.Ticks,
                expected.RecoveryTokenExpirationDate.Value.Ticks - TimeSpan.TicksPerSecond,
                expected.RecoveryTokenExpirationDate.Value.Ticks + TimeSpan.TicksPerSecond);
            Assert.NotEqual(expected.RecoveryToken, actual.RecoveryToken);
            Assert.Equal(expected.CreationDate, actual.CreationDate);
            Assert.Equal(expectedToken, actual.RecoveryToken);
            Assert.Equal(expected.IsLocked, actual.IsLocked);
            Assert.Equal(expected.Passhash, actual.Passhash);
            Assert.Equal(expected.Username, actual.Username);
            Assert.Equal(expected.RoleId, actual.RoleId);
            Assert.Equal(expected.Email, actual.Email);
            Assert.Equal(expected.Id, actual.Id);
            Assert.NotNull(actual.RecoveryToken);
        }

        #endregion

        #region Reset(AccountResetView view)

        [Fact]
        public void Reset_Account()
        {
            AccountResetView view = ObjectFactory.CreateAccountResetView();
            account.Passhash = hasher.HashPassword(view.NewPassword);
            account.RecoveryTokenExpirationDate = null;
            account.RecoveryToken = null;

            service.Reset(view);

            Account actual = context.Set<Account>().AsNoTracking().Single();
            Account expected = account;

            Assert.Equal(expected.RecoveryTokenExpirationDate, actual.RecoveryTokenExpirationDate);
            Assert.Equal(expected.RecoveryToken, actual.RecoveryToken);
            Assert.Equal(expected.CreationDate, actual.CreationDate);
            Assert.Equal(expected.IsLocked, actual.IsLocked);
            Assert.Equal(expected.Passhash, actual.Passhash);
            Assert.Equal(expected.Username, actual.Username);
            Assert.Equal(expected.RoleId, actual.RoleId);
            Assert.Equal(expected.Email, actual.Email);
            Assert.Equal(expected.Id, actual.Id);
        }

        #endregion

        #region Create(AccountCreateView view)

        [Fact]
        public void Create_Account()
        {
            AccountCreateView view = ObjectFactory.CreateAccountCreateView(1);
            view.Email = view.Email.ToUpper();
            view.RoleId = account.RoleId;

            service.Create(view);

            Account actual = context.Set<Account>().AsNoTracking().Single(model => model.Id != account.Id);
            AccountCreateView expected = view;

            Assert.Equal(hasher.HashPassword(expected.Password), actual.Passhash);
            Assert.Equal(expected.CreationDate, actual.CreationDate);
            Assert.Equal(expected.Email.ToLower(), actual.Email);
            Assert.Equal(expected.Username, actual.Username);
            Assert.Null(actual.RecoveryTokenExpirationDate);
            Assert.Equal(expected.RoleId, actual.RoleId);
            Assert.Null(actual.RecoveryToken);
            Assert.False(actual.IsLocked);
        }

        [Fact]
        public void Create_RefreshesAuthorization()
        {
            AccountCreateView view = ObjectFactory.CreateAccountCreateView(1);
            view.RoleId = null;

            service.Create(view);

            Authorization.Provider.Received().Refresh();
        }

        #endregion

        #region Edit(AccountEditView view)

        [Fact]
        public void Edit_Account()
        {
            AccountEditView view = ObjectFactory.CreateAccountEditView(account.Id);
            view.IsLocked = account.IsLocked = !account.IsLocked;
            view.Email = (account.Email += "s").ToUpper();
            view.Username = account.Username += "Test";
            view.RoleId = account.RoleId = null;

            service.Edit(view);

            Account actual = context.Set<Account>().AsNoTracking().Single();
            Account expected = account;

            Assert.Equal(expected.RecoveryTokenExpirationDate, actual.RecoveryTokenExpirationDate);
            Assert.Equal(expected.RecoveryToken, actual.RecoveryToken);
            Assert.Equal(expected.CreationDate, actual.CreationDate);
            Assert.Equal(expected.IsLocked, actual.IsLocked);
            Assert.Equal(expected.Username, actual.Username);
            Assert.Equal(expected.Passhash, actual.Passhash);
            Assert.Equal(expected.RoleId, actual.RoleId);
            Assert.Equal(expected.Email, actual.Email);
            Assert.Equal(expected.Id, actual.Id);
        }

        [Fact]
        public void Edit_RefreshesAuthorization()
        {
            AccountEditView view = ObjectFactory.CreateAccountEditView(account.Id);
            view.RoleId = account.RoleId;

            service.Edit(view);

            Authorization.Provider.Received().Refresh();
        }

        #endregion

        #region Edit(ProfileEditView view)

        [Fact]
        public void Edit_Profile()
        {
            ProfileEditView view = ObjectFactory.CreateProfileEditView();
            account.Passhash = hasher.HashPassword(view.NewPassword);
            view.Username = account.Username += "Test";
            view.Email = account.Email += "Test";

            service.Edit(view);

            Account actual = context.Set<Account>().AsNoTracking().Single();
            Account expected = account;

            Assert.Equal(expected.RecoveryTokenExpirationDate, actual.RecoveryTokenExpirationDate);
            Assert.Equal(expected.RecoveryToken, actual.RecoveryToken);
            Assert.Equal(expected.CreationDate, actual.CreationDate);
            Assert.Equal(expected.Email.ToLower(), actual.Email);
            Assert.Equal(expected.IsLocked, actual.IsLocked);
            Assert.Equal(expected.Username, actual.Username);
            Assert.Equal(expected.Passhash, actual.Passhash);
            Assert.Equal(expected.RoleId, actual.RoleId);
            Assert.Equal(expected.Id, actual.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void Edit_NullOrEmptyNewPassword_DoesNotEditPassword(String newPassword)
        {
            ProfileEditView view = ObjectFactory.CreateProfileEditView();
            view.NewPassword = newPassword;

            service.Edit(view);

            String actual = context.Set<Account>().AsNoTracking().Single().Passhash;
            String expected = account.Passhash;

            Assert.Equal(expected, actual);
        }

        #endregion

        #region Delete(Int32 id)

        [Fact]
        public void Delete_Account()
        {
            service.Delete(account.Id);

            Assert.Empty(context.Set<Account>());
        }

        [Fact]
        public void Delete_RefreshesAuthorization()
        {
            service.Delete(account.Id);

            Authorization.Provider.Received().Refresh();
        }

        #endregion

        #region Login(String username)

        [Fact]
        public void Login_IsCaseInsensitive()
        {
            HttpContext.Current = HttpContextFactory.CreateHttpContext();

            service.Login(account.Username.ToUpper());

            String actual = FormsAuthentication.Decrypt(HttpContext.Current.Response.Cookies[0].Value).Name;
            String expected = account.Id.ToString();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Login_CreatesCookie()
        {
            HttpContext.Current = HttpContextFactory.CreateHttpContext();

            service.Login(account.Username);

            FormsAuthenticationTicket actual = FormsAuthentication.Decrypt(HttpContext.Current.Response.Cookies[0].Value);
            FormsAuthenticationTicket expected = new FormsAuthenticationTicket(account.Id.ToString(), true, 0);

            Assert.Equal(expected.IsPersistent, actual.IsPersistent);
            Assert.Equal(expected.Name, actual.Name);
        }

        #endregion

        #region Logout()

        [Fact]
        public void Logout_ExpiresCookie()
        {
            HttpContext.Current = HttpContextFactory.CreateHttpContext();

            service.Login(account.Username);
            service.Logout();

            DateTime expirationDate = HttpContext.Current.Response.Cookies[0].Expires;

            Assert.True(expirationDate < DateTime.Now);
        }

        #endregion

        #region Test helpers

        private void SetUpData()
        {
            account = ObjectFactory.CreateAccount();

            using (TestingContext testingContext = new TestingContext())
            {
                testingContext.Set<Account>().Add(account);
                testingContext.SaveChanges();
            }
        }

        #endregion
    }
}
