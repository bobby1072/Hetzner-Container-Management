using System.Text.Json.Serialization;
using BT.Common.Api.Helpers.Exceptions;

namespace Hetzner.Container.Management.Schemas.Infrastructure;

public sealed record ContainerUpdateJobState
{
    public required Guid JobId { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ContainerUpdateJobStatusEnum Status { get; init; }
    [JsonIgnore]
    public ApiException? ApiException { get; init; }
}