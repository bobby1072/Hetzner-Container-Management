using System.Text.Json.Serialization;

namespace Hetzner.Container.Management.Schemas.Docker;

public sealed record ContainerSummaryResponse
{
    [JsonPropertyName("Id")]
    public required string Id { get; init; }

    [JsonPropertyName("Names")]
    public string[] Names { get; init; } = [];

    [JsonPropertyName("Image")]
    public required string Image { get; init; }

    [JsonPropertyName("ImageID")]
    public required string ImageId { get; init; }

    [JsonPropertyName("Command")]
    public required string Command { get; init; }

    [JsonPropertyName("Created")]
    public required long Created { get; init; }

    [JsonPropertyName("State")]
    public required string State { get; init; }

    [JsonPropertyName("Status")]
    public required string Status { get; init; }
}
