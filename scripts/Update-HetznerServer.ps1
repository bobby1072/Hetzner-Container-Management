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

function Write-Log {
    param(
        [Parameter(Mandatory)][string]$Message,
        [ValidateSet('INFO', 'OK', 'WARN', 'ERROR')][string]$Level = 'INFO'
    )
    $line = "[$(Get-Date -Format 'HH:mm:ss')] [$Level] $Message"
    switch ($Level) {
        'WARN'  { Write-Warning $line }
        'OK'    { Write-Host $line -ForegroundColor Green }
        'ERROR' { Write-Host $line -ForegroundColor Red }
        default { Write-Host $line }
    }
}

function Write-Step {
    param([int]$Number, [int]$Total, [string]$Title)
    Write-Host ''
    Write-Host "===== Step ${Number}/${Total}: ${Title} =====" -ForegroundColor Cyan
}

function Get-ShortId {
    param([string]$Id)
    if (-not $Id) { return 'unknown' }
    $hex = $Id -replace '^sha256:', ''
    $hex.Substring(0, [Math]::Min(12, $hex.Length))
}

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

    Write-Log "$Description..."
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $result = Invoke-SSHCommand -SSHSession $Session -Command $Command -TimeOut $TimeoutSeconds
    $stopwatch.Stop()
    $seconds = [int]$stopwatch.Elapsed.TotalSeconds
    if ($result.Output -and -not $Quiet) {
        $result.Output | ForEach-Object { Write-Host "    $_" }
    }

    if ($result.ExitStatus -ne 0) {
        if ($result.Error) {
            $result.Error | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkYellow }
        }
        if ($AllowFailure) {
            Write-Log "$Description finished with exit code $($result.ExitStatus) after ${seconds}s, continuing." -Level WARN
        }
        else {
            throw "$Description failed with exit code $($result.ExitStatus) after ${seconds}s."
        }
    }
    else {
        Write-Log "$Description done (${seconds}s)." -Level OK
    }

    return $result
}

function Wait-ForReboot {
    param([string]$HostName, [pscredential]$Credential)

    $rebootStart = Get-Date
    Write-Log 'Waiting for the server to shut down (up to 3 minutes)...'
    $shutdownDeadline = (Get-Date).AddMinutes(3)
    while (Test-TcpPort -HostName $HostName) {
        if ((Get-Date) -gt $shutdownDeadline) {
            throw 'The server did not shut down within 3 minutes of the reboot request.'
        }
        Start-Sleep -Seconds 3
    }

    Write-Log "Server is offline after $([int]((Get-Date) - $rebootStart).TotalSeconds)s. Waiting for it to come back online (up to 10 minutes)..."
    $startDeadline = (Get-Date).AddMinutes(10)
    $lastProgress = Get-Date
    $lastError = $null
    while ($true) {
        if (Test-TcpPort -HostName $HostName) {
            try {
                $session = Open-ServerSession -HostName $HostName -Credential $Credential
                Write-Log "SSH is available again after $([int]((Get-Date) - $rebootStart).TotalSeconds)s." -Level OK
                return $session
            }
            catch {
                $lastError = $_.Exception.Message
            }
        }
        if ((Get-Date) -gt $startDeadline) {
            throw 'The server did not come back online within 10 minutes of the reboot.'
        }
        if (((Get-Date) - $lastProgress).TotalSeconds -ge 30) {
            $detail = if ($lastError) { " (last error: $lastError)" } else { '' }
            Write-Log "Still waiting for SSH after $([int]((Get-Date) - $rebootStart).TotalSeconds)s$detail"
            $lastProgress = Get-Date
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

$runStart = Get-Date
Write-Log "Starting Hetzner server update (container '$ContainerName', image '$Image'$(if ($DryRun) { ', dry run' }))."

$serverIp = Get-EnvValue -Name 'Dev_Hetzner_SSH'
$rootPassword = Get-EnvValue -Name 'Dev_Hetzner_SSH_Root_Password'
if (-not $serverIp) { throw 'Environment variable Dev_Hetzner_SSH (server IP) is not set.' }
if (-not $rootPassword) { throw 'Environment variable Dev_Hetzner_SSH_Root_Password is not set.' }
Write-Log 'Server address and root password loaded from environment variables.'

if (-not (Get-Module -ListAvailable -Name Posh-SSH)) {
    Write-Log 'Posh-SSH module not found, installing it for the current user...'
    Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Scope CurrentUser -Force | Out-Null
    Install-Module -Name Posh-SSH -Scope CurrentUser -Force -AllowClobber
}
Import-Module Posh-SSH
Write-Log "Posh-SSH $((Get-Module -Name Posh-SSH).Version) loaded."

$credential = [pscredential]::new('root', (ConvertTo-SecureString $rootPassword -AsPlainText -Force))
$aptEnv = 'DEBIAN_FRONTEND=noninteractive NEEDRESTART_MODE=a'
$session = $null

try {
    Write-Step 1 6 'Connecting and reading the current state'
    Write-Log 'Connecting to the server as root...'
    $session = Open-ServerSession -HostName $serverIp -Credential $credential
    Write-Log 'Connected.' -Level OK

    $rebootState = Invoke-SSHCommand -SSHSession $session -Command 'test -f /var/run/reboot-required' -TimeOut 60
    $rebootRequired = ($rebootState.ExitStatus -eq 0)

    $inspectResult = Invoke-RemoteCommand -Session $session -Description "Reading configuration of '$ContainerName'" -Quiet -Command "docker inspect --format '{{json .}}' $(ConvertTo-ShellArgument $ContainerName)"
    $container = ($inspectResult.Output -join '') | ConvertFrom-Json
    $imageConfigResult = Invoke-RemoteCommand -Session $session -Description 'Reading configuration of the current image' -Quiet -Command "docker image inspect --format '{{json .Config}}' $(ConvertTo-ShellArgument $container.Image)"
    $imageConfig = ($imageConfigResult.Output -join '') | ConvertFrom-Json
    $plan = Get-ContainerRunPlan -Container $container -ImageConfig $imageConfig -ImageName $Image
    $previousImageId = $container.Image

    $apiKeyLine = @($container.Config.Env | Where-Object { $_ -like 'ApiKey__0=*' } | Select-Object -First 1)
    $apiKey = if ($apiKeyLine) { $apiKeyLine.Substring('ApiKey__0='.Length) } else { $null }

    Write-Log "Current container: state=$($container.State.Status), image=$($container.Config.Image), networks=$($plan.NetworkCount), env vars=$($plan.EnvCount), labels=$($plan.LabelCount), mounts=$($plan.MountCount)"

    if ($DryRun) {
        Invoke-RemoteCommand -Session $session -Description 'Host status' -Command 'hostname; uptime -p' | Out-Null
        Write-Log "Reboot pending: $(if ($rebootRequired) { 'yes' } else { 'no' })"
        Write-Log 'Dry run complete. No changes were made.' -Level OK
        return
    }

    Write-Step 2 6 'Updating Linux packages'
    Invoke-RemoteCommand -Session $session -Description 'Updating package lists' -Command 'apt-get update' -TimeoutSeconds 900 | Out-Null
    Invoke-RemoteCommand -Session $session -Description 'Upgrading Linux packages' -TimeoutSeconds 3600 -Command "$aptEnv apt-get -y -o Dpkg::Options::=--force-confdef -o Dpkg::Options::=--force-confold upgrade" | Out-Null
    Invoke-RemoteCommand -Session $session -Description 'Removing unused packages' -TimeoutSeconds 900 -Command "$aptEnv apt-get -y autoremove" | Out-Null

    Write-Step 3 6 'Rebooting if required'
    $rebootState = Invoke-SSHCommand -SSHSession $session -Command 'test -f /var/run/reboot-required' -TimeOut 60
    if ($rebootState.ExitStatus -eq 0) {
        Write-Log 'Reboot required after the upgrade. Rebooting the server...'
        Invoke-RemoteCommand -Session $session -Description 'Scheduling reboot' -TimeoutSeconds 60 -Command "nohup sh -c 'sleep 3; systemctl reboot' </dev/null >/dev/null 2>&1 &" | Out-Null
        Remove-SSHSession -SSHSession $session | Out-Null
        $session = $null
        $session = Wait-ForReboot -HostName $serverIp -Credential $credential
    }
    else {
        Write-Log 'No reboot required after the upgrade.' -Level OK
    }

    Write-Step 4 6 'Checking Docker'
    Write-Log 'Waiting for Docker to respond...'
    $dockerDeadline = (Get-Date).AddMinutes(3)
    while ($true) {
        $dockerReady = Invoke-SSHCommand -SSHSession $session -Command 'docker info >/dev/null 2>&1' -TimeOut 60
        if ($dockerReady.ExitStatus -eq 0) { break }
        if ((Get-Date) -gt $dockerDeadline) { throw 'Docker is not responding on the server.' }
        Start-Sleep -Seconds 5
    }

    Write-Log 'Docker is ready.' -Level OK

    Write-Step 5 6 'Replacing the container and image'
    Invoke-RemoteCommand -Session $session -Description "Removing container '$ContainerName'" -AllowFailure -Command "docker rm -f $(ConvertTo-ShellArgument $ContainerName)" | Out-Null
    Invoke-RemoteCommand -Session $session -Description "Removing image '$Image'" -AllowFailure -Command "docker rmi $(ConvertTo-ShellArgument $Image)" | Out-Null
    Invoke-RemoteCommand -Session $session -Description "Pulling latest image '$Image'" -TimeoutSeconds 1800 -Command "docker pull $(ConvertTo-ShellArgument $Image)" | Out-Null
    $newImage = Invoke-RemoteCommand -Session $session -Description 'Checking the pulled image' -Quiet -Command "docker image inspect --format '{{.Id}}' $(ConvertTo-ShellArgument $Image)"
    $newImageId = ($newImage.Output -join '').Trim()
    if ($newImageId -eq $previousImageId) {
        Write-Log "Image is unchanged ($(Get-ShortId $newImageId)), so the server already had the latest version." -Level OK
    }
    else {
        Write-Log "Image updated: $(Get-ShortId $previousImageId) -> $(Get-ShortId $newImageId)" -Level OK
    }

    $runResult = Invoke-RemoteCommand -Session $session -Description "Starting container '$ContainerName' with its previous configuration" -Quiet -Command $plan.RunCommand
    Write-Log "Container started: $(Get-ShortId (($runResult.Output -join '').Trim()))" -Level OK
    foreach ($network in $plan.ExtraNetworks) {
        Invoke-RemoteCommand -Session $session -Description "Connecting '$ContainerName' to network '$network'" -Command "docker network connect $(ConvertTo-ShellArgument $network) $(ConvertTo-ShellArgument $ContainerName)" | Out-Null
    }

    Write-Step 6 6 'Checking the health endpoint'
    $healthHeader = if ($apiKey) { "--header $(ConvertTo-ShellArgument "x-api-key: $apiKey")" } else { '' }
    $healthCommand = "docker exec $(ConvertTo-ShellArgument $ContainerName) wget -q -O /dev/null $healthHeader $(ConvertTo-ShellArgument "http://localhost:$InternalPort/Api/Healthz")"
    Write-Log "Checking http://localhost:$InternalPort/Api/Healthz inside the container (up to 30 attempts)..."
    $healthy = $false
    for ($attempt = 1; $attempt -le 30 -and -not $healthy; $attempt++) {
        Start-Sleep -Seconds 2
        $health = Invoke-SSHCommand -SSHSession $session -Command $healthCommand -TimeOut 30
        $healthy = ($health.ExitStatus -eq 0)
        if ($healthy) {
            Write-Log "Attempt $attempt/30: the health endpoint responded." -Level OK
        }
        else {
            Write-Log "Attempt $attempt/30: not ready yet."
        }
    }
    if ($healthy) {
        Write-Log 'Health check passed (/Api/Healthz returned HTTP 200 inside the container).' -Level OK
    }
    else {
        throw "Health check did not succeed after 30 attempts. Check the logs on the server with: docker logs $ContainerName"
    }

    Invoke-RemoteCommand -Session $session -Description 'Final container status' -Command "docker ps --filter $(ConvertTo-ShellArgument "name=^$($ContainerName)$") --format '{{.Names}}  {{.Image}}  {{.Status}}'" | Out-Null
    $elapsed = (Get-Date) - $runStart
    Write-Log "Update complete in $([math]::Round($elapsed.TotalMinutes, 1)) minutes." -Level OK
}
catch {
    Write-Log "Update failed: $($_.Exception.Message)" -Level ERROR
    throw
}
finally {
    if ($session) {
        Write-Log 'Closing the SSH session.'
        Remove-SSHSession -SSHSession $session -ErrorAction SilentlyContinue | Out-Null
    }
}
