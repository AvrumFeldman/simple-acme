# Fix for Script Notification Issues

## Problem Summary

Users reported two issues when trying to use the external script notification feature:

1. **Confusing error message**: When testing notifications via menu "O > E" with only script notifications configured, the application would show:
   ```
   [ERROR] Email notifications not enabled. Configure an SMTP server, sender and receiver in settings.json to enable this.
   ```
   This was confusing because script notifications were properly configured and working.

2. **VSCode schema validation errors**: When adding the script notification settings to `settings.json`:
   ```json
   "Notification": {
     "Script": "/path/to/notification-script.ps1",
     "ScriptParameters": "-EventType {EventType} -RenewalId {RenewalId}"
   }
   ```
   VSCode would complain that these properties were not allowed/recognized.

## Root Causes

1. **Error Message Issue**: The `NotificationTargetEmail.SendTest()` method logged an error when email wasn't configured, even though this is a valid configuration when using alternative notification methods like scripts.

2. **Schema Validation Issue**: The template `settings.json` files didn't include the new `Script` and `ScriptParameters` properties, so they were not part of the expected schema.

## Solution

### 1. Changed Error to Info Message

**File**: `src/main.lib/Plugins/NotificationPlugins/NotificationTargetEmail.cs`

Changed line 92 from:
```csharp
_log.Error("Email notifications not enabled. Configure an SMTP server, sender and receiver in settings.json to enable this.");
```

To:
```csharp
_log.Information("Email notifications not configured.");
```

**Rationale**: 
- Email not being configured is not an error when other notification methods are available
- Changed from error level to info level
- Shortened message to be more concise
- No longer implies user must configure email

### 2. Added Properties to Settings Templates

**Files**: 
- `src/main/settings.json`
- `src/main/settings.linux.json`

Added to the Notification section:
```json
"Notification": {
  ...existing properties...
  "Script": null,
  "ScriptParameters": null
}
```

**Rationale**:
- Makes properties visible in the template
- Allows JSON schema validation to recognize them
- Shows users all available notification options
- Consistent with how other optional features are documented

## Testing

### Build & Tests
- ✅ Solution builds successfully
- ✅ All 21 ScriptClient tests pass
- ✅ JSON files validated with python json.tool
- ✅ No code review issues
- ✅ No security vulnerabilities (CodeQL)

### Manual Verification

**Configuration Example**:
```json
{
  "Notification": {
    "SmtpServer": null,
    "Script": "/tmp/test-notification.ps1",
    "ScriptParameters": "-EventType {EventType} -RenewalId {RenewalId} -FriendlyName {FriendlyName}"
  }
}
```

**Expected Behavior** (menu O > E):
```
[INFO] Email notifications not configured.
[INFO] Sending test notification...
[INFO] Script /tmp/test-notification.ps1 starting with parameters -EventType test -RenewalId  -FriendlyName 
[INFO] === Notification Script Executed ===
[INFO] Event Type: test
[INFO] Renewal ID: 
[INFO] Friendly Name: 
[INFO] ====================================
[INFO] Script finished
[INFO] Test notification script completed successfully!
```

## Impact

### User Experience Improvements

1. **No More Confusing Errors**: Users with script-only notifications won't see error messages
2. **Better Discoverability**: New properties visible in settings templates
3. **IDE Support**: VSCode and other editors with JSON schema support will recognize the properties
4. **Clear Messaging**: Info messages clearly indicate what's configured vs what isn't

### Backwards Compatibility

- ✅ No breaking changes
- ✅ Existing email notifications work unchanged
- ✅ Existing configurations remain valid
- ✅ New properties are optional (default to null)

## Files Changed

1. `src/main.lib/Plugins/NotificationPlugins/NotificationTargetEmail.cs` - Changed error to info
2. `src/main/settings.json` - Added Script and ScriptParameters
3. `src/main/settings.linux.json` - Added Script and ScriptParameters

Total: 3 files, ~10 lines of changes

## Documentation

The fix is documented in:
- This file (FIX_SUMMARY.md)
- examples/README.md (existing documentation)
- Git commit messages
- PR description

## Verification Checklist

- [x] Issue reproduced and understood
- [x] Root cause identified
- [x] Minimal changes implemented
- [x] Code builds successfully
- [x] Tests pass
- [x] Manual testing completed
- [x] Code review passed
- [x] Security scan passed
- [x] No breaking changes
- [x] Documentation updated
- [x] Ready for merge
