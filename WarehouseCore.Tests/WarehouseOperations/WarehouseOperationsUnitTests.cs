/*using Application.CompanyContext;
using Application.Features;
using Application.Stocks;
using Application.WarehouseOperations;
using Application.WarehouseOperations.Requests;
using Application.WarehouseOperations.Responses;
using Domain.Models;
using User = Domain.Models.User;
using Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json.Linq;
using Infrastructure.ResponsePatterns;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
namespace WarehouseCore.Tests.WarehouseOperations;

public class WarehouseOperationsUnitTests : MyIAsyncLifetime
{
    private Mock<IStockService> _stockServiceMock = null;
    private WarehouseOperationService _warehouseOperationService = null;
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
        _stockServiceMock = new Mock<IStockService>();

        _warehouseOperationService = new WarehouseOperationService(
            _factoryMock.Object,
            _companyContextMock.Object,
            _validatorMock.Object,
            _stockServiceMock.Object
            );

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
    public async Task CreateWarehouseOperation_ReturnSuccessDTO()
    {
        var company = await AddCompany();
        var assignedUser = await AddUser(Ulid.NewUlid(), company.Id, "AssignedUser", UserType.Worker);
        var currentUser = await AddUser(_userId, company.Id, "CurrentUser", UserType.Admin);
        var warehouse = await AddWarehouse(company.Id);

        SetNewCompanySettings(company.Id);
        SetValidatorMockEnsureUserHasAnyRoleSuccess([UserType.Admin, UserType.Worker], company.Id);
        SetValidatorMocknEsureUserExistsTrue(assignedUser.Id);

       var result = await _warehouseOperationService.CreateWarehouseOperationAsync(_userId, assignedUser.Id,
            new CreateWarehouseOperationRequest(WarehouseOperationType.Picking), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorType);
        Assert.IsAssignableFrom<WarehouseOperationDetailsResponse>(result.Data);

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
    public async Task CreateWarehouseOperation_UserRoleCheckFailure_ReturnNotFound()
    {
        var company = await AddCompany();
        var assignedUser = await AddUser(Ulid.NewUlid(), company.Id, "AssignedUser", UserType.Worker);
        var currentUser = await AddUser(_userId, company.Id, "CurrentUser", UserType.Admin);

        SetNewCompanySettings(company.Id);
        SetValidatorMockEnsureUserHasAnyRoleFailure([UserType.Admin, UserType.Worker], company.Id, ErrorType.NotFound, "User not found");

        var result = await _warehouseOperationService.CreateWarehouseOperationAsync(_userId, assignedUser.Id,
            new CreateWarehouseOperationRequest(WarehouseOperationType.Picking), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.ErrorType);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateWarehouseOperation_EnsureAssignedUserExistsFailure_ReturnConflict()
    {
        var company = await AddCompany();
        var assignedUser = await AddUser(Ulid.NewUlid(), company.Id, "AssignedUser", UserType.Worker);
        var currentUser = await AddUser(_userId, company.Id, "CurrentUser", UserType.Admin);

        SetNewCompanySettings(company.Id);
        SetValidatorMockEnsureUserHasAnyRoleSuccess([UserType.Admin, UserType.Worker], company.Id);
        SetValidatorMocknEsureUserExistsFalse(assignedUser.Id);

        var result = await _warehouseOperationService.CreateWarehouseOperationAsync(_userId, assignedUser.Id,
            new CreateWarehouseOperationRequest(WarehouseOperationType.Picking), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.ErrorType);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}*/


using Application.Stocks;
using Application.WarehouseOperations;
using Application.WarehouseOperations.Requests;
using Domain.Models;
using Infrastructure;
using Infrastructure.ResponsePatterns;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WarehouseCore.Tests.WarehouseOperations;

public class WarehouseOperationServiceUnitTests : MyIAsyncLifetime
{
    private WarehouseOperationService _service = null!;

    private Ulid _adminId;
    private Ulid _workerId;
    private Ulid _companyId;

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

        _companyContextMock = new();
        _factoryMock = new();
        _validatorMock = new();

        _adminId = Ulid.NewUlid();
        _workerId = Ulid.NewUlid();

        _userId = _adminId;

        _service = new WarehouseOperationService(
            _factoryMock.Object,
            _companyContextMock.Object,
            _validatorMock.Object,
            new StockService());
    }

    public override async ValueTask DisposeAsync()
    {
        await _registryDbContext.DisposeAsync();
        await _registryConnection.DisposeAsync();

        await _wmsDbContext.DisposeAsync();
        await _wmsConnection.DisposeAsync();
    }

    // Test setup

    private async Task SetupCompanyAndUsers()
    {
        var company = await AddCompany();

        _companyId = company.Id;

        SetNewCompanySettings(_companyId);

        await AddUser(_adminId, _companyId, "Admin", UserType.Admin);
        await AddUser(_workerId, _companyId, "Worker", UserType.Worker);

        SetValidatorMockEnsureUserHasAnyRoleSuccess(
            new[] { UserType.Admin, UserType.Manager },
            _companyId);

        SetValidatorMocknEsureUserExistsTrue(_workerId);
    }

    private void SetupUserExists(Ulid userId)
    {
        SetValidatorMocknEsureUserExistsTrue(userId);
    }

    private void SetupRoleFailure(ErrorType errorType, string message)
    {
        SetValidatorMockEnsureUserHasAnyRoleFailure(
            new[] { UserType.Admin, UserType.Manager },
            _companyId,
            errorType,
            message);
    }

    private async Task<WarehouseOperation> CreateOperation(
        WarehouseOperationType type = WarehouseOperationType.Picking)
    {
        return await AddWarehouseOperation(type, _adminId, _workerId);
    }

    // CreateWarehouseOperationAsync

    [Fact]
    public async Task CreateWarehouseOperationAsync_ReturnSuccess()
    {
        await SetupCompanyAndUsers();

        var result = await _service.CreateWarehouseOperationAsync(
            _adminId,
            _workerId,
            new CreateWarehouseOperationRequest(WarehouseOperationType.Picking),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(
            WarehouseOperationType.Picking.ToString(),
            result.Data.Type);
    }

    [Fact]
    public async Task CreateWarehouseOperationAsync_UserHasNoPermission_ReturnFailure()
    {
        await SetupCompanyAndUsers();

        SetupRoleFailure(ErrorType.Forbidden, "Access denied");

        var result = await _service.CreateWarehouseOperationAsync(
            _adminId,
            _workerId,
            new CreateWarehouseOperationRequest(WarehouseOperationType.Picking),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("Access denied", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateWarehouseOperationAsync_AssignedUserNotFound_ReturnConflict()
    {
        await SetupCompanyAndUsers();

        SetValidatorMocknEsureUserExistsFalse(_workerId);

        var result = await _service.CreateWarehouseOperationAsync(
            _adminId,
            _workerId,
            new CreateWarehouseOperationRequest(WarehouseOperationType.Picking),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Assigned user doesn't belong to company",
            result.ErrorMessage);
    }

    // GetWarehouseOperationByIdAsync

    [Fact]
    public async Task GetWarehouseOperationByIdAsync_ReturnSuccess()
    {
        await SetupCompanyAndUsers();

        var operation = await CreateOperation();

        SetupUserExists(_adminId);

        var result = await _service.GetWarehouseOperationByIdAsync(
            _adminId,
            operation.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(operation.Id, result.Data.Id);
        Assert.Equal(_adminId, result.Data.CreatedById);
    }

    [Fact]
    public async Task GetWarehouseOperationByIdAsync_UserNotFound_ReturnNotFound()
    {
        await SetupCompanyAndUsers();

        var result = await _service.GetWarehouseOperationByIdAsync(
            Ulid.NewUlid(),
            Ulid.NewUlid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("User doesn't belong to company", result.ErrorMessage);
    }

    [Fact]
    public async Task GetWarehouseOperationByIdAsync_OperationNotFound_ReturnNotFound()
    {
        await SetupCompanyAndUsers();

        SetupUserExists(_adminId);

        var result = await _service.GetWarehouseOperationByIdAsync(
            _adminId,
            Ulid.NewUlid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Warehouse operation not found", result.ErrorMessage);
    }

    // GetWarehouseOperationsAsync

    [Fact]
    public async Task GetWarehouseOperationsAsync_ReturnSuccess()
    {
        await SetupCompanyAndUsers();

        await CreateOperation();
        await CreateOperation(WarehouseOperationType.Transfer);

        SetupUserExists(_adminId);

        var result = await _service.GetWarehouseOperationsAsync(
            _adminId,
            new GetWarehouseOperationsRequest(
                null, null, null, null, null,
                null, null, null, null,
                1, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.TotalCount);
        Assert.Equal(2, result.Data.Items.Count());
    }

    [Fact]
    public async Task GetWarehouseOperationsAsync_EmptyList_ReturnSuccess()
    {
        await SetupCompanyAndUsers();

        SetupUserExists(_adminId);

        var result = await _service.GetWarehouseOperationsAsync(
            _adminId,
            new GetWarehouseOperationsRequest(
                null, null, null, null, null,
                null, null, null, null,
                1, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Items);
        Assert.Equal(0, result.Data.TotalCount);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task GetWarehouseOperationsAsync_IncorrectPagination_ReturnValidation(
        int page,
        int pageSize)
    {
        var result = await _service.GetWarehouseOperationsAsync(
            _adminId,
            new GetWarehouseOperationsRequest(
                null, null, null, null, null,
                null, null, null, null,
                page, pageSize),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Validation error", result.ErrorMessage);
    }

    [Fact]
    public async Task GetWarehouseOperationsAsync_UserNotFound_ReturnNotFound()
    {
        await SetupCompanyAndUsers();

        SetValidatorMocknEsureUserExistsFalse(_adminId);

        var result = await _service.GetWarehouseOperationsAsync(
            _adminId,
            new GetWarehouseOperationsRequest(
                null, null, null, null, null,
                null, null, null, null,
                1, 10),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetWarehouseOperationsAsync_FilterByType_ReturnFilteredList()
    {
        await SetupCompanyAndUsers();

        await CreateOperation(WarehouseOperationType.Picking);
        await CreateOperation(WarehouseOperationType.Transfer);
        await CreateOperation(WarehouseOperationType.Receiving);

        SetupUserExists(_adminId);

        var result = await _service.GetWarehouseOperationsAsync(
            _adminId,
            new GetWarehouseOperationsRequest(
                null,
                new List<WarehouseOperationType>
                {
                    WarehouseOperationType.Transfer
                },
                null, null, null,
                null, null, null, null,
                1, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Items);
        Assert.Equal(WarehouseOperationType.Transfer.ToString(), result.Data.Items.FirstOrDefault()?.Type);
    }

    [Fact]
    public async Task GetWarehouseOperationsAsync_FilterByStatus_ReturnFilteredList()
    {
        await SetupCompanyAndUsers();

        var operation1 = await CreateOperation();
        var operation2 = await CreateOperation();

        operation2.Status = WarehouseOperationStatus.Completed;

        await _wmsDbContext.SaveChangesAsync();

        SetupUserExists(_adminId);

        var result = await _service.GetWarehouseOperationsAsync(
            _adminId,
            new GetWarehouseOperationsRequest(
                null, null,
                new List<WarehouseOperationStatus>
                {
                    WarehouseOperationStatus.Completed
                },
                null, null,
                null, null, null, null,
                1, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.Items);
        Assert.Equal(operation2.Id, result.Data.Items.FirstOrDefault()?.Id);
    }

    [Fact]
    public async Task GetWarehouseOperationsAsync_Pagination_ReturnCorrectPage()
    {
        await SetupCompanyAndUsers();

        await CreateOperation();
        await CreateOperation(WarehouseOperationType.Transfer);
        await CreateOperation(WarehouseOperationType.Receiving);

        SetupUserExists(_adminId);

        var result = await _service.GetWarehouseOperationsAsync(
            _adminId,
            new GetWarehouseOperationsRequest(
                null, null, null, null, null,
                null, null, null, null,
                2, 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Single(result.Data.Items);
        Assert.Equal(2, result.Data.Page);
    }

    // CompleteWarehouseOperationAsync

    [Fact]
    public async Task CompleteWarehouseOperationAsync_ReturnSuccess()
    {
        await SetupCompanyAndUsers();

        var operation = await CreateOperation();

        SetupUserExists(_workerId);

        var result = await _service.CompleteWarehouseOperationAsync(
            _workerId,
            operation.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(
            WarehouseOperationStatus.Completed.ToString(),
            result.Data.Status);
    }

    [Fact]
    public async Task CompleteWarehouseOperationAsync_UserNotFound_ReturnNotFound()
    {
        await SetupCompanyAndUsers();

        SetValidatorMocknEsureUserExistsFalse(_workerId);

        var result = await _service.CompleteWarehouseOperationAsync(
            _workerId,
            Ulid.NewUlid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("User doesn't belong to company", result.ErrorMessage);
    }

    [Fact]
    public async Task CompleteWarehouseOperationAsync_OperationNotFound_ReturnNotFound()
    {
        await SetupCompanyAndUsers();

        SetupUserExists(_workerId);

        var result = await _service.CompleteWarehouseOperationAsync(
            _workerId,
            Ulid.NewUlid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Warehouse operation not found", result.ErrorMessage);
    }

    [Fact]
    public async Task CompleteWarehouseOperationAsync_WrongAssignedUser_ReturnNotFound()
    {
        await SetupCompanyAndUsers();

        var operation = await CreateOperation();

        SetupUserExists(_adminId);

        var result = await _service.CompleteWarehouseOperationAsync(
            _adminId,
            operation.Id,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Theory]
    [InlineData(WarehouseOperationStatus.Completed, "already completed")]
    [InlineData(WarehouseOperationStatus.Cancelled, "already cancelled")]
    public async Task CompleteWarehouseOperationAsync_OperationNotInProgress_ReturnConflict(
        WarehouseOperationStatus status,
        string expectedMessage)
    {
        await SetupCompanyAndUsers();

        var operation = await CreateOperation();

        operation.Status = status;

        await _wmsDbContext.SaveChangesAsync();

        SetupUserExists(_workerId);

        var result = await _service.CompleteWarehouseOperationAsync(
            _workerId,
            operation.Id,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            $"Warehouse operation {expectedMessage}",
            result.ErrorMessage);
    }

    // CancelWarehouseOperationAsync

    [Fact]
    public async Task CancelWarehouseOperationAsync_ReturnSuccess()
    {
        await SetupCompanyAndUsers();

        var operation = await CreateOperation();

        SetupUserExists(_workerId);

        var result = await _service.CancelWarehouseOperationAsync(
            _workerId,
            operation.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(
            WarehouseOperationStatus.Cancelled.ToString(),
            result.Data.Status);
    }

    [Fact]
    public async Task CancelWarehouseOperationAsync_UserNotFound_ReturnNotFound()
    {
        await SetupCompanyAndUsers();

        SetValidatorMocknEsureUserExistsFalse(_workerId);

        var result = await _service.CancelWarehouseOperationAsync(
            _workerId,
            Ulid.NewUlid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("User doesn't belong to company", result.ErrorMessage);
    }

    [Fact]
    public async Task CancelWarehouseOperationAsync_OperationNotFound_ReturnNotFound()
    {
        await SetupCompanyAndUsers();

        SetupUserExists(_workerId);

        var result = await _service.CancelWarehouseOperationAsync(
            _workerId,
            Ulid.NewUlid(),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CancelWarehouseOperationAsync_WrongAssignedUser_ReturnNotFound()
    {
        await SetupCompanyAndUsers();

        var operation = await CreateOperation();

        SetupUserExists(_adminId);

        var result = await _service.CancelWarehouseOperationAsync(
            _adminId,
            operation.Id,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Theory]
    [InlineData(WarehouseOperationStatus.Completed, "already completed")]
    [InlineData(WarehouseOperationStatus.Cancelled, "already cancelled")]
    public async Task CancelWarehouseOperationAsync_OperationNotInProgress_ReturnConflict(
        WarehouseOperationStatus status,
        string expectedMessage)
    {
        await SetupCompanyAndUsers();

        var operation = await CreateOperation();

        operation.Status = status;

        await _wmsDbContext.SaveChangesAsync();

        SetupUserExists(_workerId);

        var result = await _service.CancelWarehouseOperationAsync(
            _workerId,
            operation.Id,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            $"Warehouse operation {expectedMessage}",
            result.ErrorMessage);
    }
}
