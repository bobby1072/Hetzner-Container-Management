using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Input;

public sealed record ContainerUpdateDocument : IValidatable<ContainerUpdateDocument>
{
    [JsonPropertyName("dockerHubDetails")]
    [JsonRequired]
    public required DockerHubDetailsWithRepositoryName DockerHubDetails { get; init; }

    [JsonPropertyName("containerName")]
    [JsonRequired]
    public required string ContainerName { get; init; }

    [JsonPropertyName("portNumber")]
    [JsonRequired]
    public required int PublicFacingPortNumber { get; init; }

    [JsonPropertyName("imageTag")]
    public string ImageTag { get; init; } = "latest";

    [JsonPropertyName("configMap")]
    public Dictionary<string, object?> ConfigMap { get; init; } = new();
    public IReadOnlyCollection<Func<(bool, string?)>> ValidatorFunctions =>
        [IsValidPortNumber, IsValidConfigMap, IsValidContainerName];
    public IReadOnlyCollection<Func<(Task<bool>, string?)>> AsyncValidatorFunctions => [];

    private (bool, string?) IsValidPortNumber() =>
        (
            PublicFacingPortNumber > 0 && PublicFacingPortNumber <= 65535,
            "Invalid port number provided"
        );

    private (bool, string?) IsValidConfigMap()
    {
        const string configMapErrorMessage = "Invalid config map provided";
        if (ConfigMap.Keys.Any(string.IsNullOrWhiteSpace))
        {
            return (false, configMapErrorMessage);
        }
        if (ConfigMap.Values.Any(x => x is not null && !IsValidConfigMapValue(x.GetType())))
        {
            return (false, configMapErrorMessage);
        }
        return (true, null);
    }

    private (bool, string?) IsValidContainerName() =>
        (ContainerName.Length < 1 || ContainerName.Length > 255, "Invalid container name provided");

    private static bool IsValidConfigMapValue(Type type)
    {
        if (type.IsPrimitive)
            return true;

        return type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid);
    }
}
