using Application.Bins;
using Application.CompanyContext;
using Application.Features;
using Application.Zones;
using Application.Zones.Requests;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace WarehouseCore.Tests.Zones;

public class ZonesUnitTests : MyIAsyncLifetime
{
    private ZoneService _zoneService = null!;

    public override async ValueTask InitializeAsync()
    {
        // Registry DB
        _registryConnection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        await _registryConnection.OpenAsync();

        var registryOptions = new DbContextOptionsBuilder<RegistryDbContext>()
            .UseSqlite(_registryConnection)
            .Options;

        _registryDbContext = new RegistryDbContext(registryOptions);
        await _registryDbContext.Database.EnsureCreatedAsync();

        // WMS DB
        _wmsConnection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        await _wmsConnection.OpenAsync();

        var wmsOptions = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(_wmsConnection)
            .Options;

        _wmsDbContext = new WmsDbContext(wmsOptions);
        await _wmsDbContext.Database.EnsureCreatedAsync();

        _userId = Ulid.NewUlid();

        _factoryMock = new Mock<IWmsDbContextFactory>();
        _companyContextMock = new Mock<ICurrentCompanyContext>();
        _validatorMock = new Mock<ICompanyAcessValidator>();

        _zoneService = new ZoneService(
            _factoryMock.Object,
            _companyContextMock.Object,
            _validatorMock.Object);
    }

    public override async ValueTask DisposeAsync()
    {
        await _registryDbContext.DisposeAsync();
        await _registryConnection.DisposeAsync();

        await _wmsDbContext.DisposeAsync();
        await _wmsConnection.DisposeAsync();
    }

    // -------------------------
    // Test helpers
    // -------------------------

    private void SetAdminSuccess(Ulid companyId)
    {
        _validatorMock
            .Setup(x => x.EnsureUserIsAdminAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(Result<User>.Success(
                new User("example", companyId)));
    }

    private void SetAdminFailure(
        ErrorType errorType,
        string errorMessage)
    {
        _validatorMock
            .Setup(x => x.EnsureUserIsAdminAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(Result<User>.Fail(errorType, errorMessage));
    }

    private void SetUserExists(bool exists)
    {
        _validatorMock
            .Setup(x => x.EnsureUserExistsAsync(
                _wmsDbContext,
                _userId,
                CancellationToken.None))
            .ReturnsAsync(exists);
    }

    private void SetContextForUserId()
    {
        // GetZonesAsync currently requests the context using userId.
        _factoryMock
            .Setup(x => x.GetContextAsync(
                _userId,
                CancellationToken.None))
            .ReturnsAsync(_wmsDbContext);
    }

    private async Task<WarehouseZone> AddZone(
        Ulid warehouseId,
        string name = "Example zone",
        WarehouseZoneType type = WarehouseZoneType.Picking)
    {
        var zone = new WarehouseZone(
            name,
            "Example description",
            warehouseId,
            type);

        _wmsDbContext.WarehouseZones.Add(zone);
        await _wmsDbContext.SaveChangesAsync();

        return zone;
    }

    // -------------------------
    // CreateZoneAsync
    // -------------------------

    [Fact]
    public async Task CreateZoneAsync_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);

        SetNewCompanySettings(company.Id);
        SetAdminSuccess(company.Id);

        var result = await _zoneService.CreateZoneAsync(
            _userId,
            new CreateZoneRequest(
                "example_zone",
                null,
                WarehouseZoneType.Picking,
                warehouse.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorType);

        Assert.Equal("example_zone", result.Data.Name);
        Assert.Equal(warehouse.Name, result.Data.WarehouseName);
        Assert.Equal(WarehouseZoneType.Picking.ToString(), result.Data.Type);

        Assert.Equal(1, await _wmsDbContext.WarehouseZones.CountAsync());

        _validatorMock.Verify(
            x => x.EnsureUserIsAdminAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task CreateZoneAsync_ReturnFailure_WhenUserIsNotAdmin()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);

        SetNewCompanySettings(company.Id);
        SetAdminFailure(ErrorType.Forbidden, "Only admins can create zones");

        var result = await _zoneService.CreateZoneAsync(
            _userId,
            new CreateZoneRequest(
                "example_zone",
                null,
                WarehouseZoneType.Picking,
                warehouse.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("Only admins can create zones", result.ErrorMessage);

        Assert.Empty(await _wmsDbContext.WarehouseZones.ToListAsync());
    }

    // -------------------------
    // GetZoneAsync
    // -------------------------

    [Fact]
    public async Task GetZoneAsync_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);
        var zone = await AddZone(warehouse.Id);

        SetNewCompanySettings(company.Id);
        SetValidatorMocknEsureUserExistsTrue(_userId);

        var result = await _zoneService.GetZoneAsync(
            _userId,
            zone.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorType);

        Assert.Equal(zone.Id, result.Data.Id);
        Assert.Equal(zone.Name, result.Data.Name);
        Assert.Equal(zone.Description, result.Data.Description);
        Assert.Equal(zone.Code, result.Data.Code);
        Assert.Equal(zone.Type.ToString(), result.Data.Type);
        Assert.Equal(warehouse.Name, result.Data.WarehouseName);
        Assert.Equal(0, result.Data.BinsCount);

        _validatorMock.Verify(
            x => x.EnsureUserExistsAsync(
                _wmsDbContext,
                _userId,
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task GetZoneAsync_ReturnNotFound_WhenUserDoesNotBelongToCompany()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);
        var zone = await AddZone(warehouse.Id);

        SetNewCompanySettings(company.Id);
        SetUserExists(false);

        var result = await _zoneService.GetZoneAsync(
            _userId,
            zone.Id,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("User doesn't belong to company", result.ErrorMessage);
    }

    [Fact]
    public async Task GetZoneAsync_ReturnNotFound_WhenZoneDoesNotExist()
    {
        var company = await AddCompany();

        SetNewCompanySettings(company.Id);
        SetUserExists(true);

        var result = await _zoneService.GetZoneAsync(
            _userId,
            Ulid.NewUlid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Zone not found", result.ErrorMessage);
    }

    // -------------------------
    // RemoveZoneAsync
    // -------------------------

    [Fact]
    public async Task RemoveZoneAsync_ReturnSuccess_WhenZoneHasNoActiveBins()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);
        var zone = await AddZone(warehouse.Id);

        SetNewCompanySettings(company.Id);
        SetAdminSuccess(company.Id);

        var result = await _zoneService.RemoveZoneAsync(
            _userId,
            zone.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("OK", result.Data);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorType);

        var updatedZone = await _wmsDbContext.WarehouseZones
            .FirstAsync(x => x.Id == zone.Id);

        Assert.False(updatedZone.IsActive);
    }

    [Fact]
    public async Task RemoveZoneAsync_ReturnFailure_WhenUserIsNotAdmin()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);
        var zone = await AddZone(warehouse.Id);

        SetNewCompanySettings(company.Id);
        SetAdminFailure(ErrorType.Forbidden, "Only admins can remove zones");

        var result = await _zoneService.RemoveZoneAsync(
            _userId,
            zone.Id,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("Only admins can remove zones", result.ErrorMessage);

        var unchangedZone = await _wmsDbContext.WarehouseZones
            .FirstAsync(x => x.Id == zone.Id);

        Assert.True(unchangedZone.IsActive);
    }

    [Fact]
    public async Task RemoveZoneAsync_ReturnNotFound_WhenZoneDoesNotExist()
    {
        var company = await AddCompany();

        SetNewCompanySettings(company.Id);
        SetAdminSuccess(company.Id);

        var result = await _zoneService.RemoveZoneAsync(
            _userId,
            Ulid.NewUlid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Zone not found", result.ErrorMessage);
    }

    [Fact]
    public async Task RemoveZoneAsync_ReturnConflict_WhenZoneHasActiveBins()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);
        var zone = await AddZone(warehouse.Id);

        await AddBin(company.Id, zone.Id);

        SetNewCompanySettings(company.Id);
        SetAdminSuccess(company.Id);

        var result = await _zoneService.RemoveZoneAsync(
            _userId,
            zone.Id,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Zone containts active bins", result.ErrorMessage);

        var unchangedZone = await _wmsDbContext.WarehouseZones
            .FirstAsync(x => x.Id == zone.Id);

        Assert.True(unchangedZone.IsActive);
    }

    // -------------------------
    // ActivateZoneAsync
    // -------------------------

    [Fact]
    public async Task ActivateZoneAsync_ReturnSuccess_WhenZoneExists()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);
        var zone = await AddZone(warehouse.Id);

        zone.IsActive = false;
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);

        SetNewCompanySettings(company.Id);
        SetAdminSuccess(company.Id);

        var result = await _zoneService.ActivateZoneAsync(
            _userId,
            zone.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorType);

        Assert.Equal(zone.Id, result.Data.Id);
        Assert.True(result.Data.IsActive);
        Assert.Equal(warehouse.Name, result.Data.WarehouseName);

        var updatedZone = await _wmsDbContext.WarehouseZones
            .FirstAsync(x => x.Id == zone.Id, CancellationToken.None);
 
        Assert.True(updatedZone.IsActive);
    }

    [Fact]
    public async Task ActivateZoneAsync_ReturnFailure_WhenUserIsNotAdmin()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);
        var zone = await AddZone(warehouse.Id);

        zone.IsActive = false;
        await _wmsDbContext.SaveChangesAsync();

        SetNewCompanySettings(company.Id);
        SetAdminFailure(ErrorType.Forbidden, "Only admins can activate zones");

        var result = await _zoneService.ActivateZoneAsync(
            _userId,
            zone.Id,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
  
        var unchangedZone = await _wmsDbContext.WarehouseZones
            .FirstAsync(x => x.Id == zone.Id, CancellationToken.None);

        Assert.False(unchangedZone.IsActive);
    }

    [Fact]
    public async Task ActivateZoneAsync_ReturnNotFound_WhenZoneDoesNotExist()
    {
        var company = await AddCompany();

        SetNewCompanySettings(company.Id);
        SetAdminSuccess(company.Id);

        var result = await _zoneService.ActivateZoneAsync(
            _userId,
            Ulid.NewUlid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Zone not found", result.ErrorMessage);
    }

    // -------------------------
    // GetZonesAsync
    // -------------------------

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task GetZonesAsync_ReturnValidation_WhenPaginationIsInvalid(
        int page,
        int pageSize)
    {
        var result = await _zoneService.GetZonesAsync(
            _userId,
            new GetZonesQueryRequest(
                null,
                null,
                true,
                null,
                page,
                pageSize),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Page or page size invalid", result.ErrorMessage);

        _factoryMock.Verify(
            x => x.GetContextAsync(
                It.IsAny<Ulid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetZonesAsync_ReturnNotFound_WhenUserDoesNotBelongToCompany()
    {
        var company = await AddCompany();

        SetNewCompanySettings(company.Id);
        SetContextForUserId();
        SetUserExists(false);

        var result = await _zoneService.GetZonesAsync(
            _userId,
            new GetZonesQueryRequest(null, null, true, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("User doesn't belong to company", result.ErrorMessage);
    }

    [Fact]
    public async Task GetZonesAsync_ReturnAllActiveZones()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);

        var firstZone = await AddZone(
            warehouse.Id,
            "First zone",
            WarehouseZoneType.Picking);

        var secondZone = await AddZone(
            warehouse.Id,
            "Second zone",
            WarehouseZoneType.Sorting);

        SetNewCompanySettings(company.Id);
        SetContextForUserId();
        SetUserExists(true);

        var result = await _zoneService.GetZonesAsync(
            _userId,
            new GetZonesQueryRequest(null, null, true, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.TotalCount);
        Assert.Equal(2, result.Data.Items.Count());

        Assert.Contains(result.Data.Items, x => x.Id == firstZone.Id);
        Assert.Contains(result.Data.Items, x => x.Id == secondZone.Id);
    }

    [Fact]
    public async Task GetZonesAsync_ReturnFilteredZones_ByType()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);

        var pickingZone = await AddZone(
            warehouse.Id,
            "Picking zone",
            WarehouseZoneType.Picking);

        await AddZone(
            warehouse.Id,
            "Sorting zone",
            WarehouseZoneType.Sorting);

        SetNewCompanySettings(company.Id);
        SetContextForUserId();
        SetUserExists(true);

        var result = await _zoneService.GetZonesAsync(
            _userId,
            new GetZonesQueryRequest(
                [WarehouseZoneType.Picking],
                null,
                true,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal(pickingZone.Id, result.Data.Items.FirstOrDefault()?.Id);
    }

    [Fact]
    public async Task GetZonesAsync_ReturnFilteredZones_ByName()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);

        var matchingZone = await AddZone(
            warehouse.Id,
            "Main Picking Zone");

        await AddZone(
            warehouse.Id,
            "Storage Zone");

        SetNewCompanySettings(company.Id);
        SetContextForUserId();
        SetUserExists(true);

        var result = await _zoneService.GetZonesAsync(
            _userId,
            new GetZonesQueryRequest(
                null,
                null,
                true,
                "main picking"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal(matchingZone.Id, result.Data.Items.FirstOrDefault()?.Id);
    }

    [Fact]
    public async Task GetZonesAsync_ReturnPaginatedZones()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);

        await AddZone(warehouse.Id, "Zone 1");
        await AddZone(warehouse.Id, "Zone 2");
        await AddZone(warehouse.Id, "Zone 3");

        SetNewCompanySettings(company.Id);
        SetContextForUserId();
        SetUserExists(true);

        var result = await _zoneService.GetZonesAsync(
            _userId,
            new GetZonesQueryRequest(
                null,
                null,
                true,
                null,
                Page: 2,
                PageSize: 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Assert.Equal(3, result.Data.TotalCount);
        Assert.Equal(2, result.Data.Page);
        Assert.Equal(1, result.Data.PageSize);
        Assert.Single(result.Data.Items);
    }

    [Fact]
    public async Task GetZonesAsync_ReturnInactiveZones_WhenUserIsAdmin()
    {
        var company = await AddCompany();
        var warehouse = await AddWarehouse(company.Id);

        var inactiveZone = await AddZone(
            warehouse.Id,
            "Inactive zone");

        inactiveZone.IsActive = false;
        await _wmsDbContext.SaveChangesAsync();

        SetNewCompanySettings(company.Id);
        SetContextForUserId();
        SetUserExists(true);
        SetAdminSuccess(company.Id);

        var result = await _zoneService.GetZonesAsync(
            _userId,
            new GetZonesQueryRequest(
                null,
                null,
                false,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal(inactiveZone.Id, result.Data.Items.FirstOrDefault()?.Id);

        _validatorMock.Verify(
            x => x.EnsureUserIsAdminAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task GetZonesAsync_ReturnFailure_WhenNonAdminRequestsInactiveZones()
    {
        var company = await AddCompany();

        SetNewCompanySettings(company.Id);
        SetContextForUserId();
        SetUserExists(true);
        SetAdminFailure(
            ErrorType.Forbidden,
            "Only admins can see inactive zones");

        var result = await _zoneService.GetZonesAsync(
            _userId,
            new GetZonesQueryRequest(
                null,
                null,
                false,
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(
            "Only admins can see inactive zones",
            result.ErrorMessage);
    }

    [Fact]
    public async Task GetZonesAsync_ReturnFilteredZones_ByWarehouse()
    {
        var company = await AddCompany();

        var firstWarehouse = await AddWarehouse(company.Id);
        var secondWarehouse = await AddWarehouse(company.Id);

        var firstZone = await AddZone(
            firstWarehouse.Id,
            "First warehouse zone");

        await AddZone(
            secondWarehouse.Id,
            "Second warehouse zone");

        SetNewCompanySettings(company.Id);
        SetContextForUserId();
        SetUserExists(true);

        var result = await _zoneService.GetZonesAsync(
            _userId,
            new GetZonesQueryRequest(
                null,
                [firstWarehouse.Id],
                true,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Assert.Equal(1, result.Data.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal(firstZone.Id, result.Data.Items.FirstOrDefault()?.Id);
    }
}