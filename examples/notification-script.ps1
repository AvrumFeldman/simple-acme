#!/usr/bin/env pwsh
# Example notification script for simple-acme
# This script receives notification events from simple-acme and logs them to a file

param(
    [string]$EventType,
    [string]$RenewalId,
    [string]$FriendlyName,
    [string]$Errors,
    [string]$Log
)

# Configuration
$LogFile = "$PSScriptRoot/notifications.log"

# Create log entry
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$logEntry = @"
[$timestamp] Notification Event
Event Type: $EventType
Renewal ID: $RenewalId
Friendly Name: $FriendlyName
"@

if ($Errors) {
    $logEntry += "`nErrors: $Errors"
}

if ($Log) {
    $logEntry += "`n`nLog Output:`n$Log"
}

$logEntry += "`n" + ("-" * 80) + "`n"

# Write to log file
Add-Content -Path $LogFile -Value $logEntry

# Output to console
Write-Host "Notification logged to $LogFile"
Write-Host "Event Type: $EventType"
Write-Host "Renewal ID: $RenewalId"
Write-Host "Friendly Name: $FriendlyName"

# Exit with success
exit 0
