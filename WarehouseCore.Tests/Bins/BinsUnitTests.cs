
using Application.Bins;
using Application.Bins.Requests;
using Application.CompanyContext;
using Application.Features;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace WarehouseCore.Tests.Bins
{
    public class BinsUnitTests : MyIAsyncLifetime
    {
        private BinBuilder _builder = null!;

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

            _builder = new BinBuilder(
                _wmsDbContext,
                _companyContextMock.Object);

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

        // Add Section

        [Fact]
        public async Task AddSectionAsync_ReturnSuccessDTO()
        {
            var company = await AddCompany();
            var warehouse = await AddWarehouse(company.Id);
            var zone = await AddWarehouseZone(warehouse.Id);

            _companyContextMock
                .SetupGet(x => x.CompanyId)
                .Returns(company.Id);

            var request = new AddSectionRequest(
                _userId,
                zone.Id,
                1,
                2,
                BinTypeByCapacity.Standart);

            var result = await _builder.AddSectionAsync(
                request,
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);
            Assert.Null(result.ErrorMessage);
            Assert.Null(result.ErrorType);

            var bins = await _wmsDbContext.Bins
                .Where(x => x.WarehouseZoneId == zone.Id)
                .ToListAsync(CancellationToken.None);

            Assert.Equal(2, bins.Count);
            Assert.All(bins, x =>
            {
                Assert.Equal(1, x.Row);
                Assert.Equal(1, x.Section);
            });

            Assert.Contains(bins, x => x.Level == 1);
            Assert.Contains(bins, x => x.Level == 2);
        }

        [Fact]
        public async Task AddSectionAsync_ZoneNotFound_ReturnNotFound()
        {
            var request = new AddSectionRequest(
                _userId,
                Ulid.NewUlid(),
                1,
                2,
                BinTypeByCapacity.Standart);

            var result = await _builder.AddSectionAsync(
                request,
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("Zone not found", result.ErrorMessage);
        }

        [Fact]
        public async Task AddSectionAsync_SectionAlreadyExists_CreateNextSection()
        {
            var company = await AddCompany();
            var warehouse = await AddWarehouse(company.Id);
            var zone = await AddWarehouseZone(warehouse.Id);

            _companyContextMock
                .SetupGet(x => x.CompanyId)
                .Returns(company.Id);

            var firstRequest = new AddSectionRequest(
                _userId, zone.Id, 1, 1, BinTypeByCapacity.Standart);

            var secondRequest = new AddSectionRequest(
                _userId, zone.Id, 1, 1, BinTypeByCapacity.Standart);

            await _builder.AddSectionAsync(firstRequest, CancellationToken.None);

            var result = await _builder.AddSectionAsync(
                secondRequest,
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);

            var bins = await _wmsDbContext.Bins
                .Where(x => x.WarehouseZoneId == zone.Id)
                .ToListAsync(CancellationToken.None);

            Assert.Equal(2, bins.Count);
            Assert.Contains(bins, x => x.Section == 1);
            Assert.Contains(bins, x => x.Section == 2);
        }

        // Add Levels

        [Fact]
        public async Task AddLevelsAsync_ReturnSuccessDTO()
        {
            var company = await AddCompany();
            var warehouse = await AddWarehouse(company.Id);
            var zone = await AddWarehouseZone(warehouse.Id);

            _companyContextMock
                .SetupGet(x => x.CompanyId)
                .Returns(company.Id);

            // First create a section with one level.
            var sectionRequest = new AddSectionRequest(
                _userId,
                zone.Id,
                1,
                1,
                BinTypeByCapacity.Standart);

            await _builder.AddSectionAsync(
                sectionRequest,
                CancellationToken.None);

            var request = new AddLevelRequest(
                _userId,
                zone.Id,
                1,
                1,
                2,
                BinTypeByCapacity.Standart);

            var result = await _builder.AddLevelsAsync(
                request,
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);

            var bins = await _wmsDbContext.Bins
                .Where(x => x.WarehouseZoneId == zone.Id)
                .Where(x => x.Row == 1)
                .Where(x => x.Section == 1)
                .OrderBy(x => x.Level)
                .ToListAsync(CancellationToken.None);

            Assert.Equal(3, bins.Count);
            Assert.Equal(new[] { 1, 2, 3 }, bins.Select(x => x.Level));
        }

        [Fact]
        public async Task AddLevelsAsync_ZoneNotFound_ReturnNotFound()
        {
            var request = new AddLevelRequest(
                _userId,
                Ulid.NewUlid(),
                1,
                1,
                2,
                BinTypeByCapacity.Standart);

            var result = await _builder.AddLevelsAsync(
                request,
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("Zone not found", result.ErrorMessage);
        }

        [Fact]
        public async Task AddLevelsAsync_SectionNotFound_ReturnNotFound()
        {
            var company = await AddCompany();
            var warehouse = await AddWarehouse(company.Id);
            var zone = await AddWarehouseZone(warehouse.Id);

            var request = new AddLevelRequest(
                _userId,
                zone.Id,
                1,
                1,
                2,
                BinTypeByCapacity.Standart);

            var result = await _builder.AddLevelsAsync(
                request,
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("Section not found", result.ErrorMessage);
        }

        // Add Row

        [Fact]
        public async Task AddRowAsync_ReturnSuccessDTO()
        {
            var company = await AddCompany();
            var warehouse = await AddWarehouse(company.Id);
            var zone = await AddWarehouseZone(warehouse.Id);

            _companyContextMock
                .SetupGet(x => x.CompanyId)
                .Returns(company.Id);

            var request = new AddRowRequest(
                _userId,
                zone.Id,
                2,
                2,
                BinTypeByCapacity.Standart);

            var result = await _builder.AddRowAsync(
                request,
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(4, result.Data.Count);

            var bins = await _wmsDbContext.Bins
                .Where(x => x.WarehouseZoneId == zone.Id)
                .ToListAsync(CancellationToken.None);

            Assert.Equal(4, bins.Count);
            Assert.All(bins, x => Assert.Equal(1, x.Row));

            Assert.Contains(bins, x => x.Section == 1 && x.Level == 1);
            Assert.Contains(bins, x => x.Section == 1 && x.Level == 2);
            Assert.Contains(bins, x => x.Section == 2 && x.Level == 1);
            Assert.Contains(bins, x => x.Section == 2 && x.Level == 2);
        }

        [Fact]
        public async Task AddRowAsync_ZoneNotFound_ReturnNotFound()
        {
            var request = new AddRowRequest(
                _userId,
                Ulid.NewUlid(),
                2,
                2,
                BinTypeByCapacity.Standart);

            var result = await _builder.AddRowAsync(
                request,
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.Equal(ErrorType.NotFound, result.ErrorType);
            Assert.Equal("Zone not found", result.ErrorMessage);
        }

        [Fact]
        public async Task AddRowAsync_RowAlreadyExists_CreateNextRow()
        {
            var company = await AddCompany();
            var warehouse = await AddWarehouse(company.Id);
            var zone = await AddWarehouseZone(warehouse.Id);

            _companyContextMock
                .SetupGet(x => x.CompanyId)
                .Returns(company.Id);

            var request = new AddRowRequest(
                _userId,
                zone.Id,
                1,
                1,
                BinTypeByCapacity.Standart);

            await _builder.AddRowAsync(request, CancellationToken.None);

            var result = await _builder.AddRowAsync(
                request,
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);

            var bins = await _wmsDbContext.Bins
                .Where(x => x.WarehouseZoneId == zone.Id)
                .ToListAsync(CancellationToken.None);

            Assert.Equal(2, bins.Count);
            Assert.Contains(bins, x => x.Row == 1);
            Assert.Contains(bins, x => x.Row == 2);
        }
    }
}
