# External Script Notification Examples

This directory contains example scripts that demonstrate how to use external scripts for notifications in simple-acme.

## Overview

Simple-acme supports running external scripts or programs to handle certificate notifications. This allows you to integrate with monitoring systems, send custom notifications, or trigger other automated processes.

## Configuration

To enable script notifications, add the following to your `settings.json`:

```json
{
  "Notification": {
    "Script": "/path/to/your/notification-script.ps1",
    "ScriptParameters": "-EventType {EventType} -RenewalId {RenewalId} -FriendlyName {FriendlyName} -Errors {Errors} -Log {Log}"
  }
}
```

### Available Tokens

The following tokens can be used in `ScriptParameters`:

- `{EventType}` - The type of notification event:
  - `created` - Certificate was created
  - `success` - Certificate renewal succeeded
  - `success-with-errors` - Certificate renewal succeeded but with errors
  - `failure` - Certificate renewal failed
  - `cancel` - Certificate renewal was cancelled
  - `test` - Test notification
- `{RenewalId}` - Unique identifier for the renewal
- `{FriendlyName}` - Friendly name of the certificate
- `{Errors}` - Error messages (available for failure events)
- `{Log}` - Full log output from the renewal process
- `{vault://json/key}` - Access secrets from the vault

## Supported Script Types

- **PowerShell** (`.ps1`) - Works on both Windows and Linux (with PowerShell Core)
- **Shell scripts** (`.sh`) - Linux/macOS
- **Executables** (`.exe`) - Windows
- **Batch files** (`.bat`, `.cmd`) - Windows

## Example Scripts

### PowerShell Example

See `notification-script.ps1` for a PowerShell example that logs notifications to a file.

Usage:
```json
{
  "Notification": {
    "Script": "C:\\path\\to\\notification-script.ps1",
    "ScriptParameters": "-EventType {EventType} -RenewalId {RenewalId} -FriendlyName {FriendlyName} -Errors {Errors} -Log {Log}"
  }
}
```

PowerShell parameters should use named parameters with the `-ParameterName` syntax.

### Bash Example

See `notification-script.sh` for a Bash shell example that logs notifications to a file.

Usage:
```json
{
  "Notification": {
    "Script": "/path/to/notification-script.sh",
    "ScriptParameters": "--event-type {EventType} --renewal-id {RenewalId} --friendly-name {FriendlyName} --errors {Errors} --log {Log}"
  }
}
```

Bash parameters should use named flags with the `--flag-name` syntax as shown in the example script.

## Custom Integration Examples

### Send to Slack

```powershell
param($EventType, $FriendlyName)

$webhook = "https://hooks.slack.com/services/YOUR/WEBHOOK/URL"
$body = @{
    text = "Certificate notification: $EventType for $FriendlyName"
} | ConvertTo-Json

Invoke-RestMethod -Uri $webhook -Method Post -Body $body -ContentType 'application/json'
```

### Send to Microsoft Teams

```powershell
param($EventType, $FriendlyName, $Errors)

$webhook = "https://outlook.office.com/webhook/YOUR/WEBHOOK/URL"
$message = "Certificate notification: $EventType for $FriendlyName"
if ($Errors) { $message += "`nErrors: $Errors" }

$body = @{
    text = $message
} | ConvertTo-Json

Invoke-RestMethod -Uri $webhook -Method Post -Body $body -ContentType 'application/json'
```

### Log to Syslog (Linux)

```bash
#!/bin/bash
EVENT_TYPE=$1
RENEWAL_ID=$2
FRIENDLY_NAME=$3

logger -t simple-acme "Certificate notification: $EVENT_TYPE for $FRIENDLY_NAME (ID: $RENEWAL_ID)"
```

## Testing

To test your notification script configuration:

```bash
# Linux/macOS
./wacs --test --notify

# Windows
wacs.exe --test --notify
```

This will trigger a test notification event with `{EventType}` set to "test".

## Script Exit Codes

Your script should return exit code `0` for success, or any non-zero value to indicate failure. Simple-acme will log script failures but will continue operating normally.

## Security Considerations

1. **Secrets**: Use the `{vault://json/key}` token to pass sensitive data rather than hardcoding credentials in your script
2. **Permissions**: Ensure your script has appropriate file system permissions
3. **Validation**: Validate all inputs in your script as they come from external sources
4. **Error Handling**: Implement proper error handling to prevent script failures from affecting simple-acme

## Troubleshooting

If your script is not executing:

1. Check that the script path in `settings.json` is absolute and correct
2. Verify the script has execute permissions (Linux/macOS: `chmod +x script.sh`)
3. Check the simple-acme logs for script execution errors
4. Test the script manually with sample parameters
5. Ensure the script extension is supported for your platform
