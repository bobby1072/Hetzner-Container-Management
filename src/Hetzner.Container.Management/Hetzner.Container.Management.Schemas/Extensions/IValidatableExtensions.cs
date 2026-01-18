using System.Reflection;

namespace Hetzner.Container.Management.Schemas.Extensions;

public static class IValidatableExtensions
{
    public static ValidationResult Validate<T>(this IValidatable<T> schema)
        where T : class, IValidatable<T>
    {
        var errorMessageList = new List<string>();
        var isValid = true;
        
        foreach (var valFunc in schema.ValidatorFunctions)
        {
            var (res, resMessage) = valFunc.Invoke();

            if (!res)
            {
                isValid = false;
            }
            if (!string.IsNullOrWhiteSpace(resMessage))
            {
                errorMessageList.Add(resMessage);
            }
        }
        
        foreach (var asyncValFunc in schema.AsyncValidatorFunctions)
        {
            var (taskRes, resMessage) = asyncValFunc.Invoke();
            var res = taskRes.GetAwaiter().GetResult();

            if (!res)
            {
                isValid = false;
            }
            if (!string.IsNullOrWhiteSpace(resMessage))
            {
                errorMessageList.Add(resMessage);
            }
        }
        
        return new ValidationResult
        {
            Errors = errorMessageList.ToArray(),
            IsValid = isValid
        };
    }
    
    public static async Task<ValidationResult> ValidateAsync<T>(this IValidatable<T> schema)
        where T : class, IValidatable<T>
    {
        var errorMessageList = new List<string>();
        var isValid = true;
        
        foreach (var valFunc in schema.ValidatorFunctions)
        {
            var (res, resMessage) = valFunc.Invoke();

            if (!res)
            {
                isValid = false;
            }
            if (!string.IsNullOrWhiteSpace(resMessage))
            {
                errorMessageList.Add(resMessage);
            }
        }
        
        foreach (var asyncValFunc in schema.AsyncValidatorFunctions)
        {
            var (taskRes, resMessage) = asyncValFunc.Invoke();
            var res = await taskRes;

            if (!res)
            {
                isValid = false;
            }
            if (!string.IsNullOrWhiteSpace(resMessage))
            {
                errorMessageList.Add(resMessage);
            }
        }

        var typeProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in typeProperties)
        {
            var propType = prop.PropertyType;
            var validatableInterface = propType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidatable<>));

            if (validatableInterface != null)
            {
                var propValue = prop.GetValue(schema);
                if (propValue != null)
                {
                    var validateAsyncMethod = validatableInterface.GetMethod(nameof(ValidateAsync));
                    if (validateAsyncMethod != null)
                    {
                        var resultTask = validateAsyncMethod.Invoke(propValue, null) as Task<ValidationResult>;
                        if (resultTask != null)
                        {
                            var result = await resultTask;
                            if (result?.IsValid != true)
                            {
                                isValid = false;
                                if (result?.Errors.Length > 0)
                                {
                                    errorMessageList.AddRange(result.Errors);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        return new ValidationResult
        {
            Errors = errorMessageList.ToArray(),
            IsValid = isValid
        };
    }
}