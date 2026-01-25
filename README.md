# Hetzner Container Management API

A lightweight, open-source REST API for managing Docker containers and infrastructure on Hetzner and other Ubuntu-based Docker systems. This API provides programmatic control over your containerized infrastructure, enabling automated deployments, updates, and orchestration.

## Features

- 🐳 **Container Orchestration**: Queue and manage infrastructure updates
- 🔄 **Docker Integration**: Direct integration with Docker Engine API and Docker Hub
- 🔐 **API Key Authentication**: Secure your API with configurable API keys
- 📊 **Health Checks**: Built-in health monitoring endpoints
- 🚀 **Queue-based Operations**: Asynchronous infrastructure updates
- ⚙️ **Configurable**: Flexible configuration via environment variables and appsettings

## Docker Image

The official Docker image is available on Docker Hub:

**[bobby1072/hetzner-container-management](https://hub.docker.com/repository/docker/bobby1072/hetzner-container-management/general)**

## Quick Start

### Prerequisites

- Docker installed on your system
- Docker daemon accessible (Unix socket or HTTP endpoint)
- Ubuntu-based system (or compatible Docker environment)

### Running with Docker

```bash
docker run -d \
  -p 8080:8080 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ApiKey__0="your-secret-api-key-here" \
  -e ApiKey__1="another-api-key-if-needed" \
  bobby1072/hetzner-container-management:latest
```

### Environment Variables

#### Required Configuration

- **`ApiKey__0`**: Primary API key for authentication (REQUIRED)
- **`ApiKey__1`, `ApiKey__2`, etc.**: Additional API keys (optional)

#### Optional Configuration

- **`RequestTimeout`**: Request timeout in seconds (default: 60)
- **`InfrastructureJsonPath`**: Path to infrastructure configuration JSON
- **`DockerEngineApiSettings__UnixDomainSocketEndPoint`**: Docker socket path (default: `/var/run/docker.sock`)
- **`DockerEngineApiSettings__UseTestHttpEndPoint`**: Use HTTP endpoint instead of socket (default: true)
- **`DockerEngineApiSettings__TestUnixHttpEndPoint`**: HTTP endpoint for Docker (default: `http://localhost:2375`)
- **`DockerEngineApiSettings__TimeoutInSeconds`**: Docker API timeout (default: 30)
- **`DockerHubApiSettings__BaseUrl`**: Docker Hub API URL (default: `https://registry.hub.docker.com`)
- **`Logging__LogLevel__Default`**: Log level (default: Debug)

### Example docker-compose.yml

```yaml
version: "3.8"

services:
  hetzner-container-management:
    image: bobby1072/hetzner-container-management:latest
    ports:
      - "8080:8080"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - ApiKey__0=your-super-secret-api-key
      - ApiKey__1=optional-second-api-key
      - RequestTimeout=60
      - DockerEngineApiSettings__UseTestHttpEndPoint=false
    restart: unless-stopped
```

## API Endpoints

### Queue Infrastructure Update

**POST** `/Api/ContainerManagement/QueueInfrastructureUpdate`

Queue an infrastructure update operation asynchronously. Returns immediately with an operation ID.

**Headers:**

```
X-API-Key: your-api-key-here
Content-Type: application/json
```

**Request Body:**

```ts
[
  {
    "containerName": string,
    "externalPortNumber": int,
    "internalPortNumber": int,
    "imageTag": string | null = "latest",
    "dockerHubDetails": {
      "username": string,
      "password": string,
      "repositoryName": string
    },
    "configMap": {
      [EnvVarLabel:string]: string | null,
      [EnvVarLabel:string]: string | null
    } | null = {},
    "volumeName": string | null
  }
]
```

**Response:**

```json
{
  "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### Queue and Wait for Infrastructure Update

**POST** `/Api/ContainerManagement/QueueAndWaitForInfrastructureUpdate`

Queue an infrastructure update operation and wait for it to complete. Returns the updated infrastructure document.

**Headers:**

```
X-API-Key: your-api-key-here
Content-Type: application/json
```

**Request Body:**

```ts
[
  {
    "containerName": string,
    "externalPortNumber": int,
    "internalPortNumber": int,
    "imageTag": string | null = "latest",
    "dockerHubDetails": {
      "username": string,
      "password": string,
      "repositoryName": string
    },
    "configMap": {
      [EnvVarLabel:string]: string | null,
      [EnvVarLabel:string]: string | null
    } | null = {},
    "volumeName": string | null
  }
]
```

**Response:** Returns the updated infrastructure document with component details.

### Health Check

**GET** `/health`

Check API health status.

## Security

⚠️ **IMPORTANT**: Always override the default API keys using environment variables. Never use the placeholder values in production.

### API Key Authentication

The API uses middleware-based API key authentication. Include your API key in the request header:

```
X-API-Key: your-api-key-here
```

## Development

### Building from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/Hetzner-Container-Management.git
cd Hetzner-Container-Management

# Build the Docker image
docker build -f src/Hetzner.Container.Management/dockerfile.api -t hetzner-container-management:local .

# Run locally
docker run -p 8080:8080 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e ApiKey__0="dev-api-key" \
  hetzner-container-management:local
```

### Project Structure

```
src/
├── Hetzner.Container.Management.Api/       # Main API application
├── Hetzner.Container.Management.Schemas/   # Data models and validation
├── Hetzner.Container.Management.Services/  # Business logic and services
└── Hetzner.Container.Management.Tests/     # Unit tests
```

## Azure Pipeline Integration

This repository includes a reusable Azure Pipeline template task for deploying infrastructure updates.

### Using the Deploy Task

Add the template to your Azure Pipeline:

```yaml
steps:
  - template: azure-pipeline-templates/deploy-infrastructure.yml@hetzner-management
    parameters:
      serverUrl: 'https://your-server.com:8080'
      apiKey: '$(HETZNER_API_KEY)'  # Use secret variable
      requestBodyPath: '$(Build.SourcesDirectory)/infrastructure.json'
      waitForCompletion: true
      timeoutInMinutes: 15
```

### Template Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `serverUrl` | string | Yes | - | URL of your Hetzner Container Management API |
| `apiKey` | string | Yes | - | API key for authentication (use secret variables) |
| `requestBodyPath` | string | Yes | - | Path to JSON file containing infrastructure configuration |
| `waitForCompletion` | boolean | No | true | Wait for deployment to complete vs queue only |
| `timeoutInMinutes` | number | No | 10 | Maximum time to wait for deployment |

### Output Variables

When `waitForCompletion: true`:
- `DeployedComponentCount`: Number of components successfully deployed

When `waitForCompletion: false`:
- `OperationId`: GUID of the queued operation

### Example Files

See the `azure-pipeline-templates/` directory for:
- `deploy-infrastructure.yml` - The reusable template task
- `example-usage.yml` - Complete pipeline examples
- `example-request-body.json` - Sample infrastructure configuration

## Use Cases

- **Automated Container Updates**: Programmatically update container images
- **CI/CD Integration**: Integrate with deployment pipelines using Azure Pipeline tasks
- **Infrastructure as Code**: Manage infrastructure through API calls and JSON configuration
- **Multi-Container Orchestration**: Coordinate updates across multiple services
- **Self-hosted Management**: Control your Hetzner VPS containers remotely

## Limitations

- Designed for single-host Docker environments
- Requires Docker socket or HTTP API access
- Not a replacement for Kubernetes or Docker Swarm for large-scale orchestration

## Contributing

Contributions are welcome! Please feel free to submit issues, fork the repository, and create pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Disclaimer

This software is provided "as is", without warranty of any kind. Use at your own risk. The authors and contributors assume no liability for any damages or issues arising from the use of this software.

## Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/Hetzner-Container-Management/issues)
- **Docker Hub**: [bobby1072/hetzner-container-management](https://hub.docker.com/repository/docker/bobby1072/hetzner-container-management/general)

---

Made with ❤️ for the self-hosting community
