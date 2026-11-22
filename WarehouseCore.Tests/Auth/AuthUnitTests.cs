using Application.Auth;
using Application.Auth.Requests;
using Application.Auth.Responses;
using Application.Auth.TokenService;
using Application.Companies;
using Application.CompanyContext;
using Application.Features;
using Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
namespace WarehouseCore.Tests.Auth
{
    public class AuthUnitTests: MyIAsyncLifetime
    {
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

            _authService = new AuthService(_factoryMock.Object, _registryDbContext);

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
    }
}