namespace Infrastructure.ResponsePatterns
{
    public class Result<T> where T : class
    {
        public bool IsSuccess { get; init; }
        public ErrorType? ErrorType { get; init; }
        public string? ErrorMessage { get; init; }
        public T? Data { get; init; }
        public Result(bool isSuccess, T? data, ErrorType? error, string? errorMsg)
        {
            IsSuccess = isSuccess;
            ErrorType = error;
            ErrorMessage = errorMsg;
            Data = data;
        }
        public static Result<T> Success(T data) => new Result<T>(true, data, null, null);
        public static Result<T> Fail(ErrorType type, string errorMsg) => new Result<T>(false, null, type, errorMsg);
    }
}