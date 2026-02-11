using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Hetzner.Container.Management.Schemas.Input;

public sealed partial record InfrastructureComponentUpdateInput
    : IValidatable<InfrastructureComponentUpdateInput>
{
    [JsonPropertyName("dockerHubDetails")]
    [JsonRequired]
    public required DockerHubDetails DockerHubDetails { get; init; }

    [JsonPropertyName("containerName")]
    [JsonRequired]
    public required string ContainerName { get; init; }

    [JsonPropertyName("externalPortNumber")]
    [JsonRequired]
    public required int PublicFacingPortNumber { get; init; }

    [JsonPropertyName("internalPortNumber")]
    [JsonRequired]
    public required int InternalPortNumber { get; init; }

    [JsonPropertyName("imageTag")]
    public string ImageTag { get; init; } = "latest";

    [JsonPropertyName("configMap")]
    public Dictionary<string, string?> ConfigMap { get; init; } = new();

    [JsonPropertyName("volumeInfo")]
    public VolumeInfo? Volume { get; init; }

    public Func<(bool, string?)>[] ValidatorFunctions =>
        [
            IsPublicFacingPortNumberValid,
            IsInternalPortNumberValid,
            IsValidConfigMap,
            IsValidContainerName,
        ];
    public Func<(Task<bool>, string?)>[] AsyncValidatorFunctions => [];

    public string[] CreateEnvStringArrayFromConfigMap(string splitter = "=") =>
        ConfigMap.Select(kv => $"{kv.Key}{splitter}{kv.Value}").ToArray();

    private (bool, string?) IsInternalPortNumberValid() => IsValidPortNumber(InternalPortNumber);

    private (bool, string?) IsPublicFacingPortNumberValid() =>
        IsValidPortNumber(PublicFacingPortNumber);

    private (bool, string?) IsValidConfigMap()
    {
        const string configMapErrorMessage = "Invalid config map provided";
        if (ConfigMap.Keys.Any(string.IsNullOrWhiteSpace))
        {
            return (false, configMapErrorMessage);
        }
        return (true, null);
    }

    private (bool, string?) IsValidContainerName() =>
        (ContainerNameRegex().IsMatch(ContainerName), "Invalid container name provided");

    [GeneratedRegex(@"^/?[a-zA-Z0-9][a-zA-Z0-9_.-]+$")]
    private static partial Regex ContainerNameRegex();

    private static (bool, string?) IsValidPortNumber(int portNum) =>
        (portNum > 0 && portNum <= 65535, "Invalid port number provided");
}
