namespace Hetzner.Container.Management.Schemas.Extensions;

public static class IValidatableExtensions
{
    public static ValidationResult Validate<T>(this IValidatable<T> schema)
        where T : class, IValidatable<T>
    {
        var errorMessageList = ExecuteSyncFuncs(schema);

        if (schema.AsyncValidatorFunctions.Length > 0)
        {
            errorMessageList = errorMessageList
                .Concat(ExecuteAsyncFuncsAsync(schema).GetAwaiter().GetResult())
                .ToList();
        }

        return new ValidationResult
        {
            Errors = errorMessageList
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToArray(),
            IsValid = errorMessageList.Count < 1,
        };
    }

    public static async Task<ValidationResult> ValidateAsync<T>(this IValidatable<T> schema)
        where T : class, IValidatable<T>
    {
        var errorMessageList = ExecuteSyncFuncs(schema);

        errorMessageList = errorMessageList.Concat(await ExecuteAsyncFuncsAsync(schema)).ToList();

        return new ValidationResult
        {
            Errors = errorMessageList
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToArray(),
            IsValid = errorMessageList.Count < 1,
        };
    }

    private static List<string?> ExecuteSyncFuncs<T>(this IValidatable<T> schema)
        where T : class, IValidatable<T>
    {
        var errorMessageList = new List<string?>();

        foreach (var valFunc in schema.ValidatorFunctions)
        {
            var (res, resMessage) = valFunc.Invoke();
            if (!res)
            {
                errorMessageList.Add(resMessage);
            }
        }
        return errorMessageList;
    }

    private static async Task<List<string?>> ExecuteAsyncFuncsAsync<T>(this IValidatable<T> schema)
        where T : class, IValidatable<T>
    {
        var errorMessageList = new List<string?>();

        foreach (var asyncValFunc in schema.AsyncValidatorFunctions)
        {
            var (taskRes, resMessage) = asyncValFunc.Invoke();
            var res = await taskRes;
            if (!res)
            {
                errorMessageList.Add(resMessage);
            }
        }
        return errorMessageList;
    }
}
