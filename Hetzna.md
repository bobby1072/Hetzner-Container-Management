# Hetzner Server Setup

Scripts and commands for provisioning a Hetzner server with Portainer, Traefik, and the Container Management API.

## Prerequisites

- A Hetzner server running Ubuntu/Debian
- Docker installed
- Docker Hub credentials

## 1. Docker Hub Login

```bash
docker login registry-1.docker.io -u LOGIN -p PASSWORD
```

## 2. Portainer (Container Management UI)

Create a persistent volume and run Portainer:

```bash
docker volume create portainer_data

docker run -d \
  -p 8000:8000 \
  -p 9443:9443 \
  --name=portainer \
  --restart=always \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v portainer_data:/data \
  portainer/portainer-ce:latest
```

Access the Portainer dashboard at `https://<server-ip>:9443`.

## 3. Traefik (Reverse Proxy & TLS)

Create the shared Docker network, then run Traefik:

```bash
docker network create web

docker run -d \
  --name traefik \
  --restart always \
  --network web \
  -p 7107:443 \
  -p 80:80 \
  -v /var/run/docker.sock:/var/run/docker.sock:ro \
  -v /root/traefik/letsencrypt:/letsencrypt \
  traefik:v3.0 \
  --api.dashboard=true \
  --providers.docker=true \
  --providers.docker.exposedbydefault=false \
  --entrypoints.web.address=:80 \
  --entrypoints.websecure.address=:443 \
  --certificatesresolvers.myresolver.acme.httpchallenge=true \
  --certificatesresolvers.myresolver.acme.httpchallenge.entrypoint=web \
  --certificatesresolvers.myresolver.acme.email=<your-email> \
  --certificatesresolvers.myresolver.acme.storage=/letsencrypt/acme.json
```

**Key details:**

| Setting             | Value                                 |
| ------------------- | ------------------------------------- |
| HTTP entrypoint     | `:80`                                 |
| HTTPS entrypoint    | `:443` (mapped to host `7107`)        |
| TLS provider        | Let's Encrypt (HTTP challenge)        |
| Certificate storage | `/root/traefik/letsencrypt/acme.json` |

## 4. Container Management API

Run the API behind Traefik on the `web` network:

```bash
docker run -d \
  --name container-management \
  --restart=always \
  --network web \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e DockerEngineApiSettings__UseTestHttpEndPoint=false \
  -e ApiKey__0=<your-api-key> \
  -e ASPNETCORE_URLS=http://+:80 \
  -l 'traefik.enable=true' \
  -l 'traefik.http.routers.container-management.rule=Host(`<your-hostname>`)' \
  -l 'traefik.http.routers.container-management.entrypoints=websecure' \
  -l 'traefik.http.routers.container-management.tls.certresolver=myresolver' \
  -l 'traefik.http.services.container-management.loadbalancer.server.port=80' \
  bobby1072/hetzner-container-management
```

**Key details:**

| Setting       | Value                                   |
| ------------- | --------------------------------------- |
| Hostname      | `<your-hostname>`                       |
| API Key       | `<your-api-key>`                        |
| Internal port | `80`                                    |
| TLS           | Terminated by Traefik via Let's Encrypt |

## Architecture

```
Internet
  │
  ├─ :80  ──► Traefik (HTTP → HTTPS redirect / ACME challenge)
  ├─ :443 ──► Traefik ──► container-management API (:80)
  └─ :9443 ─► Portainer UI
```

## Notes

- Replace `LOGIN` / `PASSWORD` with your Docker Hub credentials.
- Replace the API key with a strong, unique value in production.
- The `web` Docker network must be created before starting Traefik and the API container.
- Portainer runs independently and is not routed through Traefik.
