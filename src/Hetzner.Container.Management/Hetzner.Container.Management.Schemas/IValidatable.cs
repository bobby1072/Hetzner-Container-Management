namespace Hetzner.Container.Management.Schemas;

public interface IValidatable<TSelf>
    where TSelf : class, IValidatable<TSelf>
{
    IReadOnlyCollection<Func<(bool, string?)>> ValidatorFunctions { get; }
    IReadOnlyCollection<Func<(Task<bool>, string?)>> AsyncValidatorFunctions { get; }
}