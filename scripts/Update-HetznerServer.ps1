<#
.SYNOPSIS
Updates the Hetzner server, reboots it if required, and redeploys the Hetzner Container Management container from the latest image.

.DESCRIPTION
The server IP is read from the Dev_Hetzner_SSH environment variable and the root password from Dev_Hetzner_SSH_Root_Password.
Both are looked up in the process, user and machine scopes, so they work even if the shell was opened before they were set.

Steps:
  1. apt-get update, upgrade and autoremove
  2. Reboot if /var/run/reboot-required exists, then wait for the server to come back
  3. Read the existing container's configuration (env, labels, networks, ports, volumes, restart policy)
  4. Remove the existing container and its image, pull the latest image and start a new container with the same configuration
  5. Check the container health endpoint

Secrets stay on the server. Values are only passed over the SSH connection and are never printed.

.PARAMETER ContainerName
Name of the running management container on the server.

.PARAMETER Image
Image to pull and run.

.PARAMETER InternalPort
Port the API listens on inside the container, used for the health check.

.PARAMETER DryRun
Connects and reports the current state only. Nothing is updated, rebooted or changed.

.EXAMPLE
.\scripts\Update-HetznerServer.ps1 -DryRun

.EXAMPLE
.\scripts\Update-HetznerServer.ps1
#>
[CmdletBinding()]
param(
    [string]$ContainerName = 'container-management',
    [string]$Image = 'bobby1072/hetzner-container-management:latest',
    [int]$InternalPort = 80,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

function Get-EnvValue {
    param([string]$Name)
    foreach ($scope in 'Process', 'User', 'Machine') {
        $value = [Environment]::GetEnvironmentVariable($Name, $scope)
        if (-not [string]::IsNullOrWhiteSpace($value)) { return $value.Trim() }
    }
    return $null
}

function ConvertTo-ShellArgument {
    param([string]$Value)
    "'" + $Value.Replace("'", "'\''") + "'"
}

function Test-TcpPort {
    param([string]$HostName, [int]$Port = 22, [int]$TimeoutMs = 3000)
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        return ($client.ConnectAsync($HostName, $Port).Wait($TimeoutMs) -and $client.Connected)
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Open-ServerSession {
    param([string]$HostName, [pscredential]$Credential)
    New-SSHSession -ComputerName $HostName -Credential $Credential -AcceptKey -ConnectionTimeout 30 -ErrorAction Stop
}

function Invoke-RemoteCommand {
    param(
        [Parameter(Mandatory)]$Session,
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][string]$Command,
        [int]$TimeoutSeconds = 300,
        [switch]$AllowFailure,
        [switch]$Quiet
    )

    Write-Host "==> $Description" -ForegroundColor Cyan
    $result = Invoke-SSHCommand -SSHSession $Session -Command $Command -TimeOut $TimeoutSeconds
    if ($result.Output -and -not $Quiet) {
        $result.Output | ForEach-Object { Write-Host "    $_" }
    }

    if ($result.ExitStatus -ne 0) {
        if ($AllowFailure) {
            Write-Host "    (exit code $($result.ExitStatus), continuing)" -ForegroundColor DarkYellow
        }
        else {
            throw "Remote step '$Description' failed with exit code $($result.ExitStatus)."
        }
    }

    return $result
}

function Wait-ForReboot {
    param([string]$HostName, [pscredential]$Credential)

    Write-Host 'Waiting for the server to shut down...' -ForegroundColor Cyan
    $shutdownDeadline = (Get-Date).AddMinutes(3)
    while (Test-TcpPort -HostName $HostName) {
        if ((Get-Date) -gt $shutdownDeadline) {
            throw 'The server did not shut down within 3 minutes of the reboot request.'
        }
        Start-Sleep -Seconds 3
    }

    Write-Host 'Waiting for the server to come back online...' -ForegroundColor Cyan
    $startDeadline = (Get-Date).AddMinutes(10)
    while ($true) {
        if (Test-TcpPort -HostName $HostName) {
            try {
                return Open-ServerSession -HostName $HostName -Credential $Credential
            }
            catch {
                Write-Verbose "SSH not ready yet: $($_.Exception.Message)"
            }
        }
        if ((Get-Date) -gt $startDeadline) {
            throw 'The server did not come back online within 10 minutes of the reboot.'
        }
        Start-Sleep -Seconds 10
    }
}

function Get-ContainerRunPlan {
    param($Container, $ImageConfig, [string]$ImageName)

    $arguments = [System.Collections.Generic.List[string]]::new()
    $hostConfig = $Container.HostConfig
    $config = $Container.Config

    $restart = $hostConfig.RestartPolicy
    if ($restart -and $restart.Name -and $restart.Name -ne 'no') {
        $policy = $restart.Name
        if ($policy -eq 'on-failure' -and $restart.MaximumRetryCount -gt 0) {
            $policy = "on-failure:$($restart.MaximumRetryCount)"
        }
        $arguments.Add("--restart $policy")
    }

    if ($config.User -and $config.User -ne $ImageConfig.User) {
        $arguments.Add("--user $(ConvertTo-ShellArgument $config.User)")
    }

    $networks = @($Container.NetworkSettings.Networks.PSObject.Properties.Name)
    if ($networks.Count -gt 0) {
        $arguments.Add("--network $(ConvertTo-ShellArgument $networks[0])")
    }

    $imageEnv = @($ImageConfig.Env)
    $envCount = 0
    foreach ($entry in @($config.Env | Where-Object { $_ })) {
        if ($imageEnv -notcontains $entry) {
            $arguments.Add("-e $(ConvertTo-ShellArgument $entry)")
            $envCount++
        }
    }

    $labelCount = 0
    if ($config.Labels) {
        foreach ($label in $config.Labels.PSObject.Properties) {
            $imageLabel = if ($ImageConfig.Labels) { $ImageConfig.Labels.PSObject.Properties[$label.Name] } else { $null }
            if (-not $imageLabel -or $imageLabel.Value -ne $label.Value) {
                $arguments.Add("--label $(ConvertTo-ShellArgument "$($label.Name)=$($label.Value)")")
                $labelCount++
            }
        }
    }

    if ($hostConfig.PortBindings) {
        foreach ($port in $hostConfig.PortBindings.PSObject.Properties) {
            foreach ($binding in @($port.Value | Where-Object { $_ })) {
                $publish = if ($binding.HostIp) { "$($binding.HostIp):$($binding.HostPort):$($port.Name)" } else { "$($binding.HostPort):$($port.Name)" }
                $arguments.Add("-p $(ConvertTo-ShellArgument $publish)")
            }
        }
    }

    $mountCount = 0
    foreach ($mount in @($Container.Mounts)) {
        $readOnly = if ($mount.RW) { '' } else { ':ro' }
        if ($mount.Type -eq 'bind') {
            $arguments.Add("-v $(ConvertTo-ShellArgument ($mount.Source + ':' + $mount.Destination + $readOnly))")
            $mountCount++
        }
        elseif ($mount.Type -eq 'volume') {
            $arguments.Add("-v $(ConvertTo-ShellArgument ($mount.Name + ':' + $mount.Destination + $readOnly))")
            $mountCount++
        }
    }

    $runCommand = "docker run -d --name $(ConvertTo-ShellArgument $ContainerName) $($arguments -join ' ') $(ConvertTo-ShellArgument $ImageName)"

    return [pscustomobject]@{
        RunCommand    = $runCommand
        ExtraNetworks = @($networks | Select-Object -Skip 1)
        EnvCount      = $envCount
        LabelCount    = $labelCount
        MountCount    = $mountCount
        NetworkCount  = $networks.Count
    }
}

$serverIp = Get-EnvValue -Name 'Dev_Hetzner_SSH'
$rootPassword = Get-EnvValue -Name 'Dev_Hetzner_SSH_Root_Password'
if (-not $serverIp) { throw 'Environment variable Dev_Hetzner_SSH (server IP) is not set.' }
if (-not $rootPassword) { throw 'Environment variable Dev_Hetzner_SSH_Root_Password is not set.' }

if (-not (Get-Module -ListAvailable -Name Posh-SSH)) {
    Write-Host 'Posh-SSH module not found, installing it for the current user...' -ForegroundColor Yellow
    Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Scope CurrentUser -Force | Out-Null
    Install-Module -Name Posh-SSH -Scope CurrentUser -Force -AllowClobber
}
Import-Module Posh-SSH

$credential = [pscredential]::new('root', (ConvertTo-SecureString $rootPassword -AsPlainText -Force))
$aptEnv = 'DEBIAN_FRONTEND=noninteractive NEEDRESTART_MODE=a'
$session = $null

try {
    Write-Host "Connecting to root@$serverIp ..." -ForegroundColor Cyan
    $session = Open-ServerSession -HostName $serverIp -Credential $credential

    $rebootState = Invoke-SSHCommand -SSHSession $session -Command 'test -f /var/run/reboot-required' -TimeOut 60
    $rebootRequired = ($rebootState.ExitStatus -eq 0)

    $inspectResult = Invoke-RemoteCommand -Session $session -Description "Reading configuration of '$ContainerName'" -Quiet -Command "docker inspect --format '{{json .}}' $(ConvertTo-ShellArgument $ContainerName)"
    $container = ($inspectResult.Output -join '') | ConvertFrom-Json
    $imageConfigResult = Invoke-RemoteCommand -Session $session -Description 'Reading configuration of the current image' -Quiet -Command "docker image inspect --format '{{json .Config}}' $(ConvertTo-ShellArgument $container.Image)"
    $imageConfig = ($imageConfigResult.Output -join '') | ConvertFrom-Json
    $plan = Get-ContainerRunPlan -Container $container -ImageConfig $imageConfig -ImageName $Image

    $apiKeyLine = @($container.Config.Env | Where-Object { $_ -like 'ApiKey__0=*' } | Select-Object -First 1)
    $apiKey = if ($apiKeyLine) { $apiKeyLine.Substring('ApiKey__0='.Length) } else { $null }

    Write-Host "Current container: state=$($container.State.Status), image=$($container.Config.Image), networks=$($plan.NetworkCount), env vars=$($plan.EnvCount), labels=$($plan.LabelCount), mounts=$($plan.MountCount)"

    if ($DryRun) {
        Invoke-RemoteCommand -Session $session -Description 'Host status' -Command 'hostname; uptime -p' | Out-Null
        if ($rebootRequired) { Write-Host 'Reboot required: yes' } else { Write-Host 'Reboot required: no' }
        Write-Host 'Dry run complete. No changes were made.' -ForegroundColor Green
        return
    }

    Invoke-RemoteCommand -Session $session -Description 'Updating package lists' -Command 'apt-get update' -TimeoutSeconds 900 | Out-Null
    Invoke-RemoteCommand -Session $session -Description 'Upgrading Linux packages' -TimeoutSeconds 3600 -Command "$aptEnv apt-get -y -o Dpkg::Options::=--force-confdef -o Dpkg::Options::=--force-confold upgrade" | Out-Null
    Invoke-RemoteCommand -Session $session -Description 'Removing unused packages' -TimeoutSeconds 900 -Command "$aptEnv apt-get -y autoremove" | Out-Null

    $rebootState = Invoke-SSHCommand -SSHSession $session -Command 'test -f /var/run/reboot-required' -TimeOut 60
    if ($rebootState.ExitStatus -eq 0) {
        Write-Host 'Reboot required after the upgrade. Rebooting...' -ForegroundColor Yellow
        Invoke-RemoteCommand -Session $session -Description 'Scheduling reboot' -TimeoutSeconds 60 -Command "nohup sh -c 'sleep 3; systemctl reboot' </dev/null >/dev/null 2>&1 &" | Out-Null
        Remove-SSHSession -SSHSession $session | Out-Null
        $session = $null
        $session = Wait-ForReboot -HostName $serverIp -Credential $credential
        Write-Host 'Server is back online.' -ForegroundColor Green
    }
    else {
        Write-Host 'No reboot required.' -ForegroundColor Green
    }

    $dockerDeadline = (Get-Date).AddMinutes(3)
    while ($true) {
        $dockerReady = Invoke-SSHCommand -SSHSession $session -Command 'docker info >/dev/null 2>&1' -TimeOut 60
        if ($dockerReady.ExitStatus -eq 0) { break }
        if ((Get-Date) -gt $dockerDeadline) { throw 'Docker is not responding on the server.' }
        Start-Sleep -Seconds 5
    }

    Invoke-RemoteCommand -Session $session -Description "Removing container '$ContainerName'" -AllowFailure -Command "docker rm -f $(ConvertTo-ShellArgument $ContainerName)" | Out-Null
    Invoke-RemoteCommand -Session $session -Description "Removing image '$Image'" -AllowFailure -Command "docker rmi $(ConvertTo-ShellArgument $Image)" | Out-Null
    Invoke-RemoteCommand -Session $session -Description "Pulling latest image '$Image'" -TimeoutSeconds 1800 -Command "docker pull $(ConvertTo-ShellArgument $Image)" | Out-Null

    Invoke-RemoteCommand -Session $session -Description "Starting container '$ContainerName' with its previous configuration" -Quiet -Command $plan.RunCommand | Out-Null
    foreach ($network in $plan.ExtraNetworks) {
        Invoke-RemoteCommand -Session $session -Description "Connecting '$ContainerName' to network '$network'" -Command "docker network connect $(ConvertTo-ShellArgument $network) $(ConvertTo-ShellArgument $ContainerName)" | Out-Null
    }

    $healthHeader = if ($apiKey) { "--header $(ConvertTo-ShellArgument "x-api-key: $apiKey")" } else { '' }
    $healthCommand = "docker exec $(ConvertTo-ShellArgument $ContainerName) wget -q -O /dev/null $healthHeader $(ConvertTo-ShellArgument "http://localhost:$InternalPort/Api/Healthz")"
    $healthy = $false
    for ($attempt = 1; $attempt -le 30 -and -not $healthy; $attempt++) {
        Start-Sleep -Seconds 2
        $health = Invoke-SSHCommand -SSHSession $session -Command $healthCommand -TimeOut 30
        $healthy = ($health.ExitStatus -eq 0)
    }
    if ($healthy) {
        Write-Host 'Health check passed (/Api/Healthz returned HTTP 200 inside the container).' -ForegroundColor Green
    }
    else {
        Write-Warning "Health check did not succeed. Check the logs on the server with: docker logs $ContainerName"
    }

    Invoke-RemoteCommand -Session $session -Description 'Final container status' -Command "docker ps --filter $(ConvertTo-ShellArgument "name=^$($ContainerName)$") --format '{{.Names}}  {{.Image}}  {{.Status}}'" | Out-Null
    Write-Host 'Update complete.' -ForegroundColor Green
}
finally {
    if ($session) {
        Remove-SSHSession -SSHSession $session -ErrorAction SilentlyContinue | Out-Null
    }
}
