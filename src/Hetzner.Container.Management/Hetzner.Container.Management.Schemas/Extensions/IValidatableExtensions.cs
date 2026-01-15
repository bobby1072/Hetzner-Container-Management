namespace Hetzner.Container.Management.Schemas.Extensions;

public static class IValidatableExtensions
{
    public static ValidationResult Validate<T>(this IValidatable<T> schema)
        where T : class, IValidatable<T>
    {
        var errorMessageList = new List<string>();

        foreach (var valFunc in schema.ValidatorFunctions)
        {
            var (res, resMessage) = valFunc.Invoke(schema);
        }
    }
}