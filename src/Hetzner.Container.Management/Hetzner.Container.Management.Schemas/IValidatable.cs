namespace Hetzner.Container.Management.Schemas;

public interface IValidatable<TSelf>
    where TSelf : class, IValidatable<TSelf>
{
    IReadOnlyCollection<Func<TSelf, (bool, string?)>> ValidatorFunctions { get; }
    IReadOnlyCollection<Func<TSelf, (Task<bool>, string?)>> AsyncValidatorFunctions { get; }
}