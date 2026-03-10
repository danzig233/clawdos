#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Clawdos Windows Service installer.
.DESCRIPTION
    Supports install / uninstall / start / stop / status operations.
.EXAMPLE
    .\Install-ClawdosService.ps1 install
    .\Install-ClawdosService.ps1 uninstall
    .\Install-ClawdosService.ps1 start
#>
param(
    [Parameter(Position = 0, Mandatory = $true)]
    [ValidateSet("install", "uninstall", "start", "stop", "status")]
    [string]$Action,
    [string]$ServiceName = "Clawdos",
    [string]$DisplayName = "Clawdos - OpenClaw Windows Companion",
    [string]$Description = "OpenClaw in Windows Service Mode",
    [string]$ExePath     = (Join-Path $PSScriptRoot "..\src\bin\Debug\net8.0-windows\win-x64\Clawdos.exe")
)
$ErrorActionPreference = "Stop"
function Write-Info  { param($msg) Write-Host "[INFO]  $msg" -ForegroundColor Cyan }
function Write-Ok    { param($msg) Write-Host "[OK]    $msg" -ForegroundColor Green }
function Write-Err   { param($msg) Write-Host "[ERROR] $msg" -ForegroundColor Red }
switch ($Action) {
    "install" {
        $fullExe = (Resolve-Path $ExePath -ErrorAction SilentlyContinue).Path
        if (-not $fullExe -or -not (Test-Path $fullExe)) {
            Write-Err "Executable not found: $ExePath"
            Write-Err "Please run: dotnet publish -c Release -r win-x64 --self-contained"
            exit 1
        }
        $existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($existing) {
            Write-Info "Service already exists, stopping and removing..."
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
            sc.exe delete $ServiceName | Out-Null
            Start-Sleep -Seconds 2
        }
        Write-Info "Registering service: $ServiceName"
        sc.exe create $ServiceName `
            binPath= "`"$fullExe`"" `
            start= auto `
            DisplayName= $DisplayName
        if ($LASTEXITCODE -ne 0) {
            Write-Err "sc create failed (exit code: $LASTEXITCODE)"
            exit 1
        }
        sc.exe description $ServiceName $Description | Out-Null
        # for auto-restart on failure
        sc.exe failure $ServiceName reset= 86400 `
            actions= restart/10000/restart/10000/restart/10000 | Out-Null
        Write-Ok "Service registered: $ServiceName"
        Write-Info "Start: .\Install-ClawdosService.ps1 start"
    }
    "uninstall" {
        Write-Info "Stopping service: $ServiceName"
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        sc.exe delete $ServiceName | Out-Null
        Write-Ok "Service uninstalled"
    }
    "start" {
        Write-Info "Starting service: $ServiceName"
        Start-Service -Name $ServiceName
        $svc = Get-Service -Name $ServiceName
        Write-Ok "Service status: $($svc.Status)"
    }
    "stop" {
        Write-Info "Stopping service: $ServiceName"
        Stop-Service -Name $ServiceName -Force
        Write-Ok "Service stopped"
    }
    "status" {
        $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($svc) {
            Write-Ok "Service status: $($svc.Status)"
        } else {
            Write-Err "Service not installed: $ServiceName"
        }
    }
}