using Application.Auth;
using Application.Auth.Requests;
using Application.Auth.Responses;
using Application.Auth.TokenService;
using Application.Companies;
using Application.CompanyContext;
using Application.Features;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Domain.Models;
using Infrastructure.ResponsePatterns;
namespace WarehouseCore.Tests.Auth
{
    public class AuthUnitTests: MyIAsyncLifetime
    {
        private Mock<UserLoginCreator> _loginCreatorMock;
        private Mock<ITokenService> _tokenServiceMock;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private AuthService _authService;
        public override async ValueTask InitializeAsync()
        {
            // Registry
            _registryConnection = new SqliteConnection("Data Source=:memory:");
            await _registryConnection.OpenAsync();
            var registryOptions = new DbContextOptionsBuilder<RegistryDbContext>()
                .UseSqlite(_registryConnection)
                .Options;
            _registryDbContext = new RegistryDbContext(registryOptions);
            await _registryDbContext.Database.EnsureCreatedAsync();

            // WMS DB
            _wmsConnection = new SqliteConnection("Data Source=:memory:");
            await _wmsConnection.OpenAsync();
            var wmsOptions = new DbContextOptionsBuilder<WmsDbContext>()
                .UseSqlite(_wmsConnection)
                .Options;
            _wmsDbContext = new WmsDbContext(wmsOptions);
            await _wmsDbContext.Database.EnsureCreatedAsync();

            // Mocks & services
            _userId = Ulid.NewUlid();
            _companyContextMock = new Mock<ICurrentCompanyContext>();
            _factoryMock = new Mock<IWmsDbContextFactory>();
            _validatorMock = new Mock<ICompanyAcessValidator>();

            // Validator default behavior
            _validatorMock
                .Setup(x => x.EnsureUserExistsAsync(_wmsDbContext, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _loginCreatorMock = new Mock<UserLoginCreator>();
            _tokenServiceMock = new Mock<ITokenService>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            _authService = new AuthService(_factoryMock.Object, _tokenServiceMock.Object, _registryDbContext,
                _loginCreatorMock.Object, _httpContextAccessorMock.Object, _companyContextMock.Object);

            await Task.CompletedTask;
        }

        public override async ValueTask DisposeAsync()
        {
            await _registryDbContext.DisposeAsync();
            await _registryConnection.DisposeAsync();
            await _wmsDbContext.DisposeAsync();
            await _wmsConnection.DisposeAsync();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task RegistrateUserAsync_ReturnSuccessDTO()
        {
            var company = await AddCompany();
            var firstAdmin = await AddUser(Ulid.NewUlid(), company.Id, "firstAdmin", Domain.Models.UserType.Admin);

            SetNewCompanySettings(company.Id);
           
            var request = new RegistrateUserRequest("Maxim", "Kvyatkovsky", "NewPassword123", UserType.Worker);

            _loginCreatorMock
                .Setup(x => x.CreateLogin(Ulid.NewUlid(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(WmsCallsignGenerator.Generate(request.FirstName, request.SecondName, Ulid.NewUlid().ToString()));

            var result = await _authService
                .RegistrateUserAsync(request, CancellationToken.None, company.Id);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Null(result.ErrorMessage);
            Assert.Null(result.ErrorType);
            Assert.IsAssignableFrom<RegistrateUserResponse>(result.Data);            
        }

        [Fact]
        public async Task RegistrateUserAsync_IncorrectLogin_ReturnValidation()
        {
            var company = await AddCompany();
            var firstAdmin = await AddUser(Ulid.NewUlid(), company.Id, "firstAdmin", Domain.Models.UserType.Admin);

            SetNewCompanySettings(company.Id);

            var request = new RegistrateUserRequest(null, null, "NewPassword123", UserType.Worker);

            var result = await _authService
                .RegistrateUserAsync(request, CancellationToken.None, company.Id);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.NotNull(result.ErrorType);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(ErrorType.Validation, result.ErrorType);
        }

        [Fact]
        public async Task RegistrateUserAsync_LoginAlreadyExists_ReturnConflict()
        {
            var company = await AddCompany();
            var firstAdmin = await AddUser(Ulid.NewUlid(), company.Id, "firstAdmin", Domain.Models.UserType.Admin);
            var user1 = await AddUser(Ulid.NewUlid(), company.Id, "Maxim", UserType.Worker);

            user1.SecondName = "Kvyatkovsky";
            user1.Login = "login123";

            _loginCreatorMock
                .Setup(x => x.CreateLogin(Ulid.NewUlid(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("login123");

            await _wmsDbContext.SaveChangesAsync(CancellationToken.None);

            var request = new RegistrateUserRequest(null, null, "NewPassword123", UserType.Worker);

            var result = await _authService
                .RegistrateUserAsync(request, CancellationToken.None, company.Id);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.NotNull(result.ErrorType);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(ErrorType.Conflict, result.ErrorType);
        }
    }
}