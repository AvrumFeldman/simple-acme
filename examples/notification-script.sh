#!/bin/bash
# Example notification script for simple-acme
# This script receives notification events from simple-acme and logs them to a file
# Parameters are passed positionally: $1=EventType, $2=RenewalId, $3=FriendlyName, $4=Errors, $5=Log

# Read positional parameters
EVENT_TYPE="${1:-unknown}"
RENEWAL_ID="${2:-}"
FRIENDLY_NAME="${3:-}"
ERRORS="${4:-}"
LOG="${5:-}"

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_FILE="$SCRIPT_DIR/notifications.log"

# Create log entry
TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
LOG_ENTRY="[$TIMESTAMP] Notification Event
Event Type: $EVENT_TYPE
Renewal ID: $RENEWAL_ID
Friendly Name: $FRIENDLY_NAME"

if [ -n "$ERRORS" ]; then
    LOG_ENTRY="$LOG_ENTRY
Errors: $ERRORS"
fi

if [ -n "$LOG" ]; then
    LOG_ENTRY="$LOG_ENTRY

Log Output:
$LOG"
fi

LOG_ENTRY="$LOG_ENTRY
$(printf '%*s\n' 80 '' | tr ' ' '-')"

# Write to log file
echo "$LOG_ENTRY" >> "$LOG_FILE"

# Output to console
echo "Notification logged to $LOG_FILE"
echo "Event Type: $EVENT_TYPE"
echo "Renewal ID: $RENEWAL_ID"
echo "Friendly Name: $FRIENDLY_NAME"

# Exit with success
exit 0
