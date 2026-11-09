using Application.Bins;
using Application.Companies;
using Application.CompanyContext;
using Application.Features;
using Domain.Models;
using Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
namespace WarehouseCore.Tests.Companies;

public class CompaniesUnitTests
{
    [Fact]
    public async Task GetCurrentCompanyAsync_CompanyExists_ReturnsSuccessWithSuccessDTO()
    {
        // Arrange

        // 1. Создаём отдельную SQLite БД для реестра компаний
        await using var registryConnection =
            new SqliteConnection("Data Source=:memory:");

        await registryConnection.OpenAsync();

        var registryOptions = new DbContextOptionsBuilder<RegistryDbContext>()
            .UseSqlite(registryConnection)
            .Options;

        await using var registryDbContext =
            new RegistryDbContext(registryOptions);

        await registryDbContext.Database.EnsureCreatedAsync();

        // 2. Создаём компанию и сохраняем её в БД
        var newCompany = new Company("ООО Комунарка", "KMN");

        registryDbContext.Companies.Add(newCompany);
        await registryDbContext.SaveChangesAsync();

        // 3. Создаём отдельную SQLite БД для WMS
        await using var wmsConnection =
            new SqliteConnection("Data Source=:memory:");

        await wmsConnection.OpenAsync();

        var wmsOptions = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(wmsConnection)
            .Options;

        var wmsDbContext = new WmsDbContext(wmsOptions);

        await wmsDbContext.Database.EnsureCreatedAsync();

        // 4. Создаём идентификатор пользователя
        var userId = Ulid.NewUlid();

        // 5. Подменяем ICurrentCompanyContext
        var companyContextMock = new Mock<ICurrentCompanyContext>();

        companyContextMock
            .SetupGet(x => x.CompanyId)
            .Returns(newCompany.Id);

        // 6. Подменяем IWmsDbContextFactory
        var factoryMock = new Mock<IWmsDbContextFactory>();

        factoryMock
            .Setup(x => x.GetContextAsync(
                newCompany.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wmsDbContext);

        // 7. Подменяем валидатор
        var validatorMock = new Mock<ICompanyAcessValidator>();

        validatorMock
            .Setup(x => x.EnsureUserExistsAsync(
                wmsDbContext,
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // 8. Создаём тестируемый сервис
        var companyService = new CompanyService(
            factoryMock.Object,
            companyContextMock.Object,
            registryDbContext,
            validatorMock.Object);

        // Act

        var result = await companyService.GetCurrentCompanyAsync(
            userId,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        // Проверяем, что валидатор действительно был вызван
        validatorMock.Verify(
            x => x.EnsureUserExistsAsync(
                wmsDbContext,
                userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentCompanyAsync_CompanyDoesntExist_ReturnFailure()
    {
        // registry db
        await using var registryConnection = new SqliteConnection("Data Source=:memory:");

        await registryConnection.OpenAsync();

        var registryOptions = new DbContextOptionsBuilder<RegistryDbContext>()
            .UseSqlite(registryConnection)
            .Options;

        await using var registryDbContext =
            new RegistryDbContext(registryOptions);

        await registryDbContext.Database.EnsureCreatedAsync();

        // wms db
        await using var wmsConnection = new SqliteConnection("Data Source=:memory:");

        await wmsConnection.OpenAsync();

        var wmsOptions = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(wmsConnection)
            .Options;

        await using var wmsContext = new WmsDbContext(wmsOptions);

        await wmsContext.Database.EnsureCreatedAsync();

        // current user
        var userId = Ulid.NewUlid();

        var currentCompanyContextMock = new Mock<ICurrentCompanyContext>();

        currentCompanyContextMock
            .SetupGet(x => x.CompanyId)
            .Returns(Ulid.NewUlid());

        // wms factory
        var wmsDbContextFactoryMock = new Mock<IWmsDbContextFactory>();

        wmsDbContextFactoryMock
           .Setup(x => x.GetContextAsync(
                Ulid.NewUlid(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wmsContext);

        // company validator
        var companyValidatorMock = new Mock<ICompanyAcessValidator>();

        companyValidatorMock
            .Setup(x => 
                x.EnsureUserExistsAsync
                    (wmsContext, userId, CancellationToken.None))
            .ReturnsAsync(true);

        // company service
        var companyService = new CompanyService(
            wmsDbContextFactoryMock.Object,
            currentCompanyContextMock.Object,
            registryDbContext,
            companyValidatorMock.Object);

        var result = await companyService.GetCurrentCompanyAsync(
            userId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.NotNull(result.ErrorMessage);


        companyValidatorMock.Verify(
            x => x.EnsureUserExistsAsync(
                wmsContext,
                userId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
