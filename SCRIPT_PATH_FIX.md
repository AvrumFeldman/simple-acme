# Script Path Handling Fix - Summary

## Problem Statement

User reported that notification scripts with relative paths were failing with PowerShell errors:
```
Script error: & : The term 'notification-script.ps1' is not recognized as the name of a cmdlet, 
function, script file, or operable program.
```

The user noted that installation scripts work fine with the same pattern, suggesting the notification script implementation was different/incorrect.

## Root Cause

The `ScriptClient.CreatePsi()` method had a bug in how it handled script paths:

1. Line 218: `var actualScript = Environment.ExpandEnvironmentVariables(script);` - expanded env vars
2. Line 222-223: `actualScript` was reassigned to the PowerShell executable path ("pwsh" or "powershell.exe")
3. Line 229: Used the original `script` variable (not expanded, not absolute) in the command string

This meant:
- Environment variables in paths were not expanded in the final command
- Relative paths like "notification-script.ps1" were passed directly to PowerShell
- PowerShell couldn't locate the script without an absolute path or `./` prefix

## Solution

Updated `ScriptClient.CreatePsi()` to properly handle script paths:

### Before:
```csharp
private ProcessStartInfo CreatePsi(string script, string? parameters)
{
    var actualScript = Environment.ExpandEnvironmentVariables(script);
    var actualParameters = parameters;
    if (actualScript.EndsWith(".ps1"))
    {
        actualScript = settings.Script.PowershellExecutablePath ?? "powershell.exe";
        // Uses 'script' instead of expanded path!
        actualParameters = $"{baseParameters} -command \"&{{&'{script.Replace("'", "''")}' {parameters}...}}\"";
    }
    // ...
}
```

### After:
```csharp
private ProcessStartInfo CreatePsi(string script, string? parameters)
{
    var expandedScriptPath = Environment.ExpandEnvironmentVariables(script);
    // Convert to absolute path (handles relative paths)
    string absoluteScriptPath;
    try
    {
        absoluteScriptPath = new FileInfo(expandedScriptPath).FullName;
    }
    catch (Exception ex)
    {
        logService.Error("Invalid script path {path}: {message}", script, ex.Message);
        throw;
    }
    
    var actualScript = absoluteScriptPath;
    var actualParameters = parameters;
    if (actualScript.EndsWith(".ps1"))
    {
        actualScript = settings.Script.PowershellExecutablePath ?? "powershell.exe";
        // Uses absoluteScriptPath - fully expanded and absolute!
        actualParameters = $"{baseParameters} -command \"&{{&'{absoluteScriptPath.Replace("'", "''")}' {parameters}...}}\"";
    }
    // ...
}
```

### Key Changes:
1. Preserve the expanded script path before reassigning `actualScript`
2. Convert to absolute path using `FileInfo.FullName` (same as `ValidFile` extension)
3. Use the absolute path in PowerShell/shell command construction
4. Added try-catch for better error messages on invalid paths

## Why This Works

`FileInfo.FullName` automatically:
- Converts relative paths to absolute paths based on current working directory
- Resolves `.` and `..` in paths
- Returns the canonical absolute path

This is the same pattern used by:
- `ValidFile` extension method for path validation
- Installation script plugin (indirectly through validation)
- All existing tests (which use full paths)

## Testing

### Unit Tests
✅ All 21 ScriptClient tests pass (no regressions)

### Manual Testing
Created test scripts:
- `notification-test.ps1` in working directory (relative path)
- `/tmp/notification-absolute.ps1` (absolute path)

**Test 1: Relative Path**
```json
{
  "Notification": {
    "Script": "notification-test.ps1",
    "ScriptParameters": "{EventType} {RenewalId} {FriendlyName}"
  }
}
```
Result: ✅ SUCCESS - Script executed, log file created

**Test 2: Absolute Path**
```json
{
  "Notification": {
    "Script": "/tmp/notification-absolute.ps1",
    "ScriptParameters": "{EventType} {RenewalId} {FriendlyName}"
  }
}
```
Result: ✅ SUCCESS - Script executed, log file created

## Impact

### Benefits
1. **Fixes notification scripts**: Relative paths now work
2. **Fixes all script plugins**: Installation and validation scripts also benefit
3. **Better error handling**: Clear error messages for invalid paths
4. **Consistency**: All script handling now uses the same pattern
5. **No breaking changes**: Absolute paths still work

### Backward Compatibility
- ✅ Absolute paths continue to work
- ✅ Environment variables continue to be expanded
- ✅ All existing tests pass
- ✅ No changes to public APIs

## Files Changed

1. **ScriptClient.cs**:
   - Added `using System.IO;`
   - Modified `CreatePsi()` method to handle paths correctly
   - Added error handling for invalid paths

Total changes: ~15 lines modified/added

## Code Review Feedback Addressed

1. ✅ Added error handling for invalid paths
2. ✅ Maintained `actualScript` variable (needed for executable name)
3. ✅ Added descriptive error messages
4. ✅ Used try-catch to handle path parsing errors

## Conclusion

The fix successfully resolves the reported issue by ensuring notification scripts (and all scripts using ScriptClient) properly handle relative paths. The implementation now matches how installation scripts work and aligns with the existing `ValidFile` pattern for path handling.

Users can now use either relative or absolute paths for their notification scripts, just like they can with installation scripts.
