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
    public int? PublicFacingPortNumber { get; init; }

    [JsonPropertyName("internalPortNumber")]
    public int? InternalPortNumber { get; init; }

    [JsonPropertyName("imageTag")]
    public string ImageTag { get; init; } = "latest";

    [JsonPropertyName("configMap")]
    public IReadOnlyDictionary<string, string?> ConfigMap { get; init; } = new Dictionary<string, string?>();

    [JsonPropertyName("labels")]
    public IReadOnlyDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();

    [JsonPropertyName("volumeInfo")]
    public VolumeInfo? Volume { get; init; }

    [JsonPropertyName("networks")]
    public IReadOnlyCollection<string> Networks { get; init; } = new List<string>();
    
    [JsonPropertyName("restartPolicy")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RestartPolicyEnum?  RestartPolicy { get; init; }

    public Func<(bool, string?)>[] ValidatorFunctions =>
        [IsPublicFacingPortNumberValid, IsInternalPortNumberValid, IsValidContainerName];
    public Func<(Task<bool>, string?)>[] AsyncValidatorFunctions => [];

    public string[] CreateEnvStringArrayFromConfigMap(string splitter = "=") =>
        ConfigMap.Select(kv => $"{kv.Key}{splitter}{kv.Value}").ToArray();

    private (bool, string?) IsInternalPortNumberValid() => IsValidPortNumber(InternalPortNumber);

    private (bool, string?) IsPublicFacingPortNumberValid() =>
        IsValidPortNumber(PublicFacingPortNumber);

    private (bool, string?) IsValidContainerName() =>
        (ContainerNameRegex().IsMatch(ContainerName), "Invalid container name provided");

    [GeneratedRegex(@"^/?[a-zA-Z0-9][a-zA-Z0-9_.-]+$")]
    private static partial Regex ContainerNameRegex();

    private static (bool, string?) IsValidPortNumber(int? portNum) => 
        (portNum is null || (portNum > 0 && portNum <= 65535), "Invalid port number provided");
}
