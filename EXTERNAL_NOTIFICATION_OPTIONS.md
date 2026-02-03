# Notification External Options

Simple-acme now supports specifying notification settings via command-line arguments, allowing you to override settings.json configuration for specific renewals or operations.

## Overview

Just like installation plugins can be configured via command-line, notification settings can now be provided as command-line arguments. This is useful for:

- **Testing different notification configs** without modifying settings.json
- **Per-renewal notification settings** in automated scripts
- **CI/CD pipelines** where settings are provided as environment variables
- **Quick configuration changes** without editing files

## Command-Line Arguments

All notification settings from settings.json can be overridden via command-line arguments.

### Email Notification Options

| Argument | Type | Description |
|----------|------|-------------|
| `--smtpserver` | string | SMTP server hostname or IP address |
| `--smtpport` | int | SMTP server port (default: 25) |
| `--smtpuser` | string | SMTP username for authentication |
| `--smtppassword` | string | SMTP password (supports vault references) |
| `--smtpsecure` | bool | Enable TLS/SSL for SMTP |
| `--emailsendername` | string | Display name for email sender |
| `--emailsender` | string | Email address of sender |
| `--emailreceiver` | string | Email address(es) of receiver(s), comma-separated |
| `--emailonsuccess` | bool | Send emails on success, not just failures |

### Webhook Notification Options

| Argument | Type | Description |
|----------|------|-------------|
| `--webhookurl` | string | Webhook endpoint URL |
| `--webhookhttpmethod` | string | HTTP method: POST or GET (default: POST) |
| `--webhookauthmethod` | string | Authentication: none, bearer, basic, apikey |
| `--webhookbearertoken` | string | Bearer token (supports vault references) |
| `--webhookbasicusername` | string | Basic auth username |
| `--webhookbasicpassword` | string | Basic auth password (supports vault references) |
| `--webhookapikey` | string | API key (supports vault references) |
| `--webhookapikeyheader` | string | Header name for API key (default: X-API-Key) |
| `--webhooktimeoutseconds` | int | HTTP timeout in seconds (default: 30) |
| `--webhookmaxretries` | int | Max retry attempts (default: 3) |
| `--webhookretrydelayseconds` | int | Initial retry delay (default: 30) |

### General Options

| Argument | Type | Description |
|----------|------|-------------|
| `--notificationcomputername` | string | Override computer name in notifications |

## Examples

### Send Webhook Notification for Specific Renewal

```bash
simple-acme.exe --renew --id renewal123 \
  --webhookurl "https://api.example.com/webhooks/cert-renewed" \
  --webhookauthmethod bearer \
  --webhookbearertoken "vault://json/webhook-token"
```

### Test Email Notification with Custom SMTP

```bash
simple-acme.exe --renew --friendlyname "example.com" \
  --smtpserver "smtp.gmail.com" \
  --smtpport 587 \
  --smtpsecure true \
  --smtpuser "notifications@example.com" \
  --smtppassword "vault://env/SMTP_PASSWORD" \
  --emailsender "notifications@example.com" \
  --emailreceiver "admin@example.com,ops@example.com" \
  --emailonsuccess true
```

### Use Both Email and Webhook

```bash
simple-acme.exe --renew \
  --emailreceiver "admin@example.com" \
  --emailonsuccess true \
  --webhookurl "https://hooks.slack.com/services/YOUR/WEBHOOK/URL"
```

### Override Webhook URL for Testing

```bash
simple-acme.exe --renew --test \
  --webhookurl "https://webhook.site/unique-id"
```

## Integration with Settings.json

Command-line arguments **override** settings.json values:

1. **settings.json** provides default values
2. **Command-line arguments** override for the current execution
3. **Original settings.json** remains unchanged

Example flow:
```
settings.json: WebhookUrl = "https://prod.example.com/webhook"
Command-line:  --webhookurl "https://test.example.com/webhook"
Result:        Webhook sent to https://test.example.com/webhook
```

## Security Best Practices

### Use Vault References for Secrets

Instead of passing secrets directly:

```bash
# Bad - exposes secret in command line
simple-acme.exe --smtppassword "mysecretpassword"

# Good - use vault reference
simple-acme.exe --smtppassword "vault://json/smtp-password"
```

### Store Secrets First

```bash
# Store secret in vault
simple-acme.exe --vault-store \
  --vault-key "vault://json/webhook-token" \
  --vault-secret "your-secret-token"

# Use in renewal
simple-acme.exe --renew \
  --webhookurl "https://api.example.com/webhook" \
  --webhookauthmethod bearer \
  --webhookbearertoken "vault://json/webhook-token"
```

## Common Use Cases

### CI/CD Pipeline

```bash
#!/bin/bash
# In CI/CD, settings come from environment variables

simple-acme.exe --renew --force \
  --webhookurl "$WEBHOOK_URL" \
  --webhookauthmethod bearer \
  --webhookbearertoken "vault://env/WEBHOOK_TOKEN" \
  --emailreceiver "$ADMIN_EMAIL"
```

### Multi-Environment Setup

```bash
# Production
simple-acme.exe --renew \
  --webhookurl "https://prod-api.example.com/webhook" \
  --emailreceiver "ops@example.com"

# Staging
simple-acme.exe --renew --test \
  --webhookurl "https://staging-api.example.com/webhook" \
  --emailreceiver "dev@example.com"
```

### Testing Notification Configuration

```bash
# Test webhook without affecting settings.json
simple-acme.exe --notificationtest \
  --webhookurl "https://webhook.site/your-unique-id" \
  --webhookauthmethod none
```

## Precedence Rules

When the same setting is defined in multiple places:

1. **Command-line arguments** (highest priority)
2. **Server-specific settings.json** (in configuration path)
3. **Global settings.json** (default)

## Limitations

- Command-line overrides apply **only to the current execution**
- Settings.json is **not modified** by command-line arguments
- For permanent changes, edit settings.json directly
- Custom headers for webhooks must still be defined in settings.json (cannot be passed via command-line)

## Help

To see all available notification arguments:

```bash
simple-acme.exe --help
```

Look for arguments starting with `--smtp`, `--email`, `--webhook`, and `--notification`.
