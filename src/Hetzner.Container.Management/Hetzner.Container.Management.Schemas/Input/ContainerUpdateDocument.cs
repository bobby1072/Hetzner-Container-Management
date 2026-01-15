using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Input;

public sealed record ContainerUpdateDocument: IValidatable<ContainerUpdateDocument>
{
    [JsonPropertyName("dockerHubDetails")]
    [JsonRequired]
    public required DockerHubDetails DockerHubDetails { get; init; }
    [JsonPropertyName("portNumber")]
    [JsonRequired]
    public required int PublicFacingPortNumber { get; init; } 
    [JsonPropertyName("version")]
    public string Version { get; init; } = "latest";
    [JsonPropertyName("configMap")]
    public Dictionary<string, object?> ConfigMap { get; init; } = new();

    
    public IReadOnlyCollection<Func<ContainerUpdateDocument, (bool, string?)>> ValidatorFunctions => [IsValidPortNumber];
    public IReadOnlyCollection<Func<ContainerUpdateDocument, (Task<bool>, string?)>> AsyncValidatorFunctions => [];
    private static (bool, string?) IsValidPortNumber(ContainerUpdateDocument document) =>
        (document.PublicFacingPortNumber > 0 && document.PublicFacingPortNumber <= 65535, "Invalid port number provided");
}