using System.Text.Json;
using BT.Common.Polly.Extensions;
using BT.Common.Polly.Models.Concrete;
using Hetzner.Container.Management.Schemas.Infrastructure;
using Hetzner.Container.Management.Services.Infrastructure.Abstract;

namespace Hetzner.Container.Management.Api.Services;

internal sealed class CurrentInfrastructureExplorer: ICurrentInfrastructureExplorer
{
    private static readonly PollyRetrySettings _writeToFileRetrySettings = new()
    {
        TotalAttempts = 3
    };
    
    private readonly string _infraJsonLocation;
    private readonly ILogger<CurrentInfrastructureExplorer> _logger;

    public CurrentInfrastructureExplorer(string infraJsonLocation, ILogger<CurrentInfrastructureExplorer> logger)
    {
        _infraJsonLocation = infraJsonLocation;
        _logger = logger;
    }

    public async Task ReplaceCurrentInfrastructureAsync(InfrastructureDocument infrastructureDocument, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to replace current infrastructure document at path: {InfraPath}", _infraJsonLocation);
        
        var newInfrastructureDocument = infrastructureDocument with { LastUpdated = DateTime.UtcNow };
        var serialisedDocument = JsonSerializer.Serialize(newInfrastructureDocument);

        var retryPipeline = _writeToFileRetrySettings.ToPipeline();
        await retryPipeline.ExecuteAsync(async (ct) => await File.WriteAllTextAsync(_infraJsonLocation, serialisedDocument, ct), cancellationToken);
    }

    public async Task<InfrastructureDocument?> TryGetCurrentInfrastructureDocumentAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to get current infrastructure document at path: {InfraPath}",
                _infraJsonLocation);
            var readFile = await File.ReadAllTextAsync(_infraJsonLocation, cancellationToken);

            var parsedFile = JsonSerializer.Deserialize<RawInfrastructureDocument>(readFile)
                             ?? throw new JsonException("Unable to parse infrastructure document json");

            return parsedFile.ToActualDocument();
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unable to get current infrastructure document at path: {InfraPath}", _infraJsonLocation);
            return null;
        }
    }
}