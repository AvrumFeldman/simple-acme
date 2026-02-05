#!/usr/bin/env pwsh
# Example notification script for simple-acme
# This script receives notification events from simple-acme and logs them to a file
# Parameters are passed positionally: EventType, RenewalId, FriendlyName, Errors, Log

param(
    [string]$EventType = "unknown",
    [string]$RenewalId = "",
    [string]$FriendlyName = "",
    [string]$Errors = "",
    [string]$Log = ""
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
    try {
        $decodedBytes = [System.Convert]::FromBase64String($Log)
        $Log = [System.Text.Encoding]::UTF8.GetString($decodedBytes)
    } catch {
        # Fallback for plain text or invalid base64
    }

    # Truncate log to first 500 characters if it's too long
    $truncatedLog = if ($Log.Length -gt 500) { $Log.Substring(0, 500) + "..." } else { $Log }
    $logEntry += "`n`nLog Output:`n$truncatedLog"
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
