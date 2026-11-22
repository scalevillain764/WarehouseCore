using Application.Bins;
using Application.CompanyContext;
using Application.Zones;
using Application.Zones.Requests;
using Application.Zones.Responses;
using Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Domain.Models;
using Infrastructure.ResponsePatterns;
namespace WarehouseCore.Tests.Zones;
public class ZonesUnitTests : MyIAsyncLifetime
{
    private ZoneService _zoneService = null;
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

        _userId = Ulid.NewUlid();

        _companyContextMock = new Mock<ICurrentCompanyContext>();

        _zoneService = new ZoneService(_factoryMock.Object, _companyContextMock.Object, _validatorMock.Object);

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
    public async Task CreateZoneAsync_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse();

        SetNewCompanySettings(company.Id);
        SetValidatorMocknEsureUserExistsTrue(company.Id);

        var result = await _zoneService.CreateZoneAsync(_userId,
            new CreateZoneRequest("example_zone", null, WarehouseZoneType.Picking, warehouse.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.ErrorType);

        // Validator 
        _validatorMock.Verify(
            x => x.EnsureUserExistsAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }


}

