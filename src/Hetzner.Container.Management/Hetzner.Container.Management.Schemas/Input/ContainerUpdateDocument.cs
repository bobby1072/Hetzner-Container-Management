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

    
    public IReadOnlyCollection<Func<ContainerUpdateDocument, (bool, string?)>> ValidatorFunctions => 
    [
        IsValidPortNumber,
        IsValidConfigMap
    ];
    public IReadOnlyCollection<Func<ContainerUpdateDocument, (Task<bool>, string?)>> AsyncValidatorFunctions => [];
    private static (bool, string?) IsValidPortNumber(ContainerUpdateDocument document) =>
        (document.PublicFacingPortNumber > 0 && document.PublicFacingPortNumber <= 65535, "Invalid port number provided");

    private static (bool, string?) IsValidConfigMap(ContainerUpdateDocument document)
    {
        const string configMapErrorMessage = "Invalid config map provided";
        if (document.ConfigMap.Keys.Any(string.IsNullOrWhiteSpace))
        {
            return (false, configMapErrorMessage);
        }
        if (document
            .ConfigMap
            .Values
            .Any(x => x is not null && 
                    !IsValidConfigMapValue(x.GetType())))
        {
            return (false, configMapErrorMessage);
        }
        return (true, null);
    }
    
    
    public static bool IsValidConfigMapValue(Type type)
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