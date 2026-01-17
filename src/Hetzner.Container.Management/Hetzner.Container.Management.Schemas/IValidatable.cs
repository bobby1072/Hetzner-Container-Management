namespace Hetzner.Container.Management.Schemas;

public interface IValidatable<TSelf>
    where TSelf : class, IValidatable<TSelf>
{
    Func<(bool, string?)>[] ValidatorFunctions { get; }
    Func<(Task<bool>, string?)>[] AsyncValidatorFunctions { get; }
}