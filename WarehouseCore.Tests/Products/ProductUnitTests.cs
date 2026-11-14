/*using Application.Companies;
using Application.CompanyContext;
using Application.Features;
using Application.Products;
using Application.Products.Requests;
using Application.Products.Responses;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json.Linq;
using System.ComponentModel.Design;
using WarehouseCore.Tests;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using User = Domain.Models.User;
namespace WarehouseCore.Tests.Products;
public class ProductUnitTests : MyIAsyncLifetime
{
    private IProductService _productService;
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

        _productService = new ProductService(
            _factoryMock.Object,
            _companyContextMock.Object,
            _validatorMock.Object);

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

    private async Task<Company> AddCompany()
    {
        var company = new Company("company1", "CM1");
        _registryDbContext.Companies.Add(company);
        await _registryDbContext.SaveChangesAsync(CancellationToken.None);
        return company;
    }

    private async Task SetCompaniesSettings(Ulid companyId)
    {
        _factoryMock
            .Setup(x => x.GetContextAsync(companyId, CancellationToken.None))
            .ReturnsAsync(_wmsDbContext);

        _companyContextMock
            .SetupGet(x => x.CompanyId)
            .Returns(companyId);
    }

    private void SetValidatorMockSuccess(IReadOnlyCollection<UserType> types, Ulid companyId)
    {
        _validatorMock
           .Setup(x => x.EnsureUserHasAnyRoleAsync(_wmsDbContext, _userId, It.IsAny<string>(),
               It.Is<IReadOnlyCollection<UserType>>(roles => roles != null &&
                    roles.Intersect(types).Any()), CancellationToken.None))
           .ReturnsAsync(Result<User>.Success(new User("example", companyId)));
    }

    private void SetValidatorMockFailure(IReadOnlyCollection<UserType> types, Ulid companyId, ErrorType errorType, string errorMessage)
    {
        _validatorMock
          .Setup(x => x.EnsureUserHasAnyRoleAsync(_wmsDbContext, _userId, It.IsAny<string>(),
              It.Is<IReadOnlyCollection<UserType>>(roles => roles != null &&
                    roles.Intersect(types).Any()), CancellationToken.None))
          .ReturnsAsync(Result<User>.Fail(errorType, errorMessage));
    }

    private async Task<Product> AddProductAsync()
    {
        var product = new Product(
            "Example_product",
            "Example_description",
            ProductCategory.AudioDevices,
            "HyperX",
            UnitOfMeasure.Set,
            120,
            "Black");
        _wmsDbContext.Products.Add(product);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);
        return product;
    }

    *//*private void VerifyValidator(Times t)*//*
        
    // Add product

    [Fact]
    public async Task AddProductAsync_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);
        SetValidatorMockSuccess([UserType.Admin, UserType.Manager], company.Id);

        var result = await _productService
            .AddProductAsync(_userId,
            new CreateProductRequest(
                "Example_Product",
                "Description",
                ProductCategory.Accessories,
                "Apple",
                UnitOfMeasure.Pcs,
                13,
                null),
            CancellationToken.None);

        Assert.True(result?.IsSuccess);
        Assert.NotNull(result?.Data);
        Assert.IsType<ProductDetailsResponse>(result?.Data);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(_wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddProductAsync_UserRoleFail_ReturnFailure()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);
        SetValidatorMockFailure([UserType.Admin, UserType.Manager], company.Id, ErrorType.NotFound, "User not found");

        var result = await _productService
            .AddProductAsync(_userId,
            new CreateProductRequest(
                "Example_Product",
                "Description",
                ProductCategory.Accessories,
                "Apple",
                UnitOfMeasure.Pcs,
                13,
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.ErrorType);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);

        _validatorMock.Verify(x =>
        x.EnsureUserHasAnyRoleAsync(_wmsDbContext,
            _userId,
            It.IsAny<string>(),
            It.IsAny<IReadOnlyCollection<UserType>>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
    }

    // Remove product

    [Fact]
    public async Task RemoveProductAsync_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);
        var product = await AddProductAsync();
        SetValidatorMockSuccess([UserType.Manager, UserType.Admin], company.Id);
       
        var result = await _productService
            .RemoveProductAsync(_userId, product.Id, CancellationToken.None);

        Assert.True(result?.IsSuccess);
        Assert.Equal<string>(result?.Data, "OK");
        Assert.Null(result?.ErrorType);
        Assert.Null(result?.ErrorMessage);

        _validatorMock.Verify(x =>
        x.EnsureUserHasAnyRoleAsync(_wmsDbContext,
            _userId,
            It.IsAny<string>(),
            It.IsAny<IReadOnlyCollection<UserType>>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
    }

    [Fact]
    public async Task RemoveProductAsync_
}
*/


using Application.Companies;
using Application.CompanyContext;
using Application.Features;
using Application.Products;
using Application.Products.Requests;
using Application.Products.Responses;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using WarehouseCore.Tests;
using User = Domain.Models.User;

namespace WarehouseCore.Tests.Products;

public class ProductUnitTests : MyIAsyncLifetime
{
    private IProductService _productService;

    public override async ValueTask InitializeAsync()
    {
        _registryConnection = new SqliteConnection("Data Source=:memory:");
        await _registryConnection.OpenAsync();

        var registryOptions = new DbContextOptionsBuilder<RegistryDbContext>()
            .UseSqlite(_registryConnection)
            .Options;

        _registryDbContext = new RegistryDbContext(registryOptions);
        await _registryDbContext.Database.EnsureCreatedAsync();

        _wmsConnection = new SqliteConnection("Data Source=:memory:");
        await _wmsConnection.OpenAsync();

        var wmsOptions = new DbContextOptionsBuilder<WmsDbContext>()
            .UseSqlite(_wmsConnection)
            .Options;

        _wmsDbContext = new WmsDbContext(wmsOptions);
        await _wmsDbContext.Database.EnsureCreatedAsync();

        _userId = Ulid.NewUlid();
        _companyContextMock = new Mock<ICurrentCompanyContext>();
        _factoryMock = new Mock<IWmsDbContextFactory>();
        _validatorMock = new Mock<ICompanyAcessValidator>();

        _validatorMock
            .Setup(x => x.EnsureUserExistsAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _productService = new ProductService(
            _factoryMock.Object,
            _companyContextMock.Object,
            _validatorMock.Object);

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

    private async Task<Company> AddCompany()
    {
        var company = new Company("company1", "CM1");

        _registryDbContext.Companies.Add(company);
        await _registryDbContext.SaveChangesAsync(CancellationToken.None);

        return company;
    }

    private async Task SetCompaniesSettings(Ulid companyId)
    {
        _factoryMock
            .Setup(x => x.GetContextAsync(companyId, CancellationToken.None))
            .ReturnsAsync(_wmsDbContext);

        _companyContextMock
            .SetupGet(x => x.CompanyId)
            .Returns(companyId);
    }

    private void SetValidatorMockSuccess(
        IReadOnlyCollection<UserType> types,
        Ulid companyId)
    {
        _validatorMock
            .Setup(x => x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.Is<IReadOnlyCollection<UserType>>(roles =>
                    roles != null && roles.Intersect(types).Any()),
                CancellationToken.None))
            .ReturnsAsync(Result<User>.Success(
                new User("example", companyId)));
    }

    private void SetValidatorMockFailure(
        IReadOnlyCollection<UserType> types,
        Ulid companyId,
        ErrorType errorType,
        string errorMessage)
    {
        _validatorMock
            .Setup(x => x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.Is<IReadOnlyCollection<UserType>>(roles =>
                    roles != null && roles.Intersect(types).Any()),
                CancellationToken.None))
            .ReturnsAsync(Result<User>.Fail(errorType, errorMessage));
    }

    private async Task<Product> AddProductAsync()
    {
        var product = new Product(
            "Example_product",
            "Example_description",
            ProductCategory.AudioDevices,
            "HyperX",
            UnitOfMeasure.Set,
            120,
            "Black");

        _wmsDbContext.Products.Add(product);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);

        return product;
    }

    // Add product

    [Fact]
    public async Task AddProductAsync_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        SetValidatorMockSuccess(
            [UserType.Admin, UserType.Manager],
            company.Id);

        var result = await _productService.AddProductAsync(
            _userId,
            new CreateProductRequest(
                "Example_Product",
                "Description",
                ProductCategory.Accessories,
                "Apple",
                UnitOfMeasure.Pcs,
                13,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.IsType<ProductDetailsResponse>(result.Data);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddProductAsync_UserRoleFail_ReturnFailure()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        SetValidatorMockFailure(
            [UserType.Admin, UserType.Manager],
            company.Id,
            ErrorType.NotFound,
            "User not found");

        var result = await _productService.AddProductAsync(
            _userId,
            new CreateProductRequest(
                "Example_Product",
                "Description",
                ProductCategory.Accessories,
                "Apple",
                UnitOfMeasure.Pcs,
                13,
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Null(result.Data);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Remove product

    [Fact]
    public async Task RemoveProductAsync_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        var product = await AddProductAsync();

        SetValidatorMockSuccess(
            [UserType.Manager, UserType.Admin],
            company.Id);

        var result = await _productService.RemoveProductAsync(
            _userId,
            product.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("OK", result.Data);
        Assert.Null(result.ErrorType);
        Assert.Null(result.ErrorMessage);
        Assert.False(product.IsActive);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveProductAsync_ProductDoesntExist_ReturnFailure()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        SetValidatorMockSuccess(
            [UserType.Manager, UserType.Admin],
            company.Id);

        var result = await _productService.RemoveProductAsync(
            _userId,
            Ulid.NewUlid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveProductAsync_UserRoleFail_ReturnFailure()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        var product = await AddProductAsync();

        SetValidatorMockFailure(
            [UserType.Manager, UserType.Admin],
            company.Id,
            ErrorType.Forbidden,
            "Access denied");

        var result = await _productService.RemoveProductAsync(
            _userId,
            product.Id,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.True(product.IsActive);
    }

    // Get product by id

    [Fact]
    public async Task GetProductByIdAsync_ProductExists_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        var product = await AddProductAsync();

        var result = await _productService.GetProductByIdAsync(
            _userId,
            product.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(product.Id, result.Data.Id);
        Assert.Equal(product.Name, result.Data.Name);
    }

    [Fact]
    public async Task GetProductByIdAsync_ProductDoesntExist_ReturnFailure()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        var result = await _productService.GetProductByIdAsync(
            _userId,
            Ulid.NewUlid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    // Get products by query

    [Fact]
    public async Task GetProductsByQuery_ReturnSuccessWithPagedResponse()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        await AddProductAsync();

        var product = new Product(
            "Another_product",
            "Description",
            ProductCategory.Accessories,
            "Apple",
            UnitOfMeasure.Pcs,
            10,
            "White");

        _wmsDbContext.Products.Add(product);
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);

        var request = new GetProductsQueryRequest(
            null, null, null, null, null, null,
            true, null, null, 1, 10);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.TotalCount);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(-1, 10)]
    [InlineData(1, -1)]
    public async Task GetProductsByQuery_InvalidPagination_ReturnFailure(
        int page,
        int pageSize)
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        var request = new GetProductsQueryRequest(
            null, null, null, null, null, null,
            true, null, null, page, pageSize);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetProductsByQuery_NoProducts_ReturnEmptyPagedResponse()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        var request = new GetProductsQueryRequest(
            null, null, null, null, null, null,
            true, null, null, 1, 10);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.TotalCount);
    }

    [Fact]
    public async Task GetProductsByQuery_NameFilter_ReturnMatchingProducts()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        await AddProductAsync();

        var request = new GetProductsQueryRequest(
            "Example", null, null, null, null, null,
            true, null, null, 1, 10);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
    }

    [Fact]
    public async Task GetProductsByQuery_DescriptionFilter_ReturnMatchingProducts()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        await AddProductAsync();

        var request = new GetProductsQueryRequest(
            null, "Example", null, null, null, null,
            true, null, null, 1, 10);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
    }

    [Fact]
    public async Task GetProductsByQuery_CategoryFilter_ReturnMatchingProducts()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        await AddProductAsync();

        var request = new GetProductsQueryRequest(
            null, null, [ProductCategory.AudioDevices], null, null, null,
            true, null, null, 1, 10);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
    }

    [Fact]
    public async Task GetProductsByQuery_BrandFilter_ReturnMatchingProducts()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        await AddProductAsync();

        var request = new GetProductsQueryRequest(
            null, null, null, "HyperX", null, null,
            true, null, null, 1, 10);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
    }

    [Fact]
    public async Task GetProductsByQuery_UnitOfMeasureFilter_ReturnMatchingProducts()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        await AddProductAsync();

        var request = new GetProductsQueryRequest(
            null, null, null, null, [UnitOfMeasure.Set], null,
            true, null, null, 1, 10);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
    }

    [Fact]
    public async Task GetProductsByQuery_WeightFilter_ReturnMatchingProducts()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        await AddProductAsync();

        var request = new GetProductsQueryRequest(
            null, null, null, null, null, 120,
            true, null, null, 1, 10);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
    }

    [Fact]
    public async Task GetProductsByQuery_SKUFilter_ReturnMatchingProducts()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        var product = await AddProductAsync();

        var request = new GetProductsQueryRequest(
            null, null, null, null, null, null,
            true, product.SKU, null, 1, 10);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
    }

    [Fact]
    public async Task GetProductsByQuery_ColorFilter_ReturnMatchingProducts()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        await AddProductAsync();

        var request = new GetProductsQueryRequest(
            null, null, null, null, null, null,
            true, null, "Black", 1, 10);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);
    }

    // Inactive products

    [Fact]
    public async Task GetProductsByQuery_InactiveProducts_AdminOrManager_ReturnSuccess()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        var product = await AddProductAsync();

        product.IsActive = false;
        await _wmsDbContext.SaveChangesAsync(CancellationToken.None);

        SetValidatorMockSuccess(
            [UserType.Manager, UserType.Admin],
            company.Id);

        var request = new GetProductsQueryRequest(
            null, null, null, null, null, null,
            false, null, null, 1, 10);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.TotalCount);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetProductsByQuery_InactiveProducts_UserRoleFail_ReturnFailure()
    {
        var company = await AddCompany();
        await SetCompaniesSettings(company.Id);

        SetValidatorMockFailure(
            [UserType.Manager, UserType.Admin],
            company.Id,
            ErrorType.Forbidden,
            "Access denied");

        var request = new GetProductsQueryRequest(
            null, null, null, null, null, null,
            false, null, null, 1, 10);

        var result = await _productService.GetProductsByQuery(
            _userId,
            request,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);

        _validatorMock.Verify(x =>
            x.EnsureUserHasAnyRoleAsync(
                _wmsDbContext,
                _userId,
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<UserType>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
