# Webhook Notification Plugin

The webhook notification plugin allows simple-acme to send HTTP notifications to external services when certificate lifecycle events occur.

## Features

- **Multiple HTTP methods**: POST (recommended) or GET
- **Flexible authentication**: Bearer token, Basic auth, API key, or no auth
- **Secrets management**: Full integration with simple-acme vault system
- **Customizable headers**: Add any custom headers to requests
- **Retry logic**: Automatic retry with exponential backoff
- **Rich payload**: Detailed certificate and renewal information in JSON format

## Configuration

Add webhook configuration to your `settings.json` under the `Notification` section:

```json
{
  "Notification": {
    "Webhook": {
      "WebhookUrl": "https://your-webhook-endpoint.com/certificates",
      "HttpMethod": "POST",
      "AuthMethod": "bearer",
      "BearerToken": "vault://json/webhook-token",
      "CustomHeaders": {
        "X-Custom-Header": "value"
      },
      "TimeoutSeconds": 30,
      "MaxRetries": 3,
      "RetryDelaySeconds": 30
    }
  }
}
```

## Configuration Options

### Required

- **WebhookUrl** (string): The URL endpoint to send notifications to. Must be HTTPS for production use.

### HTTP Method

- **HttpMethod** (string): HTTP method to use. Options: `POST` (recommended), `GET`. Default: `POST`

### Authentication Methods

Choose one authentication method using the `AuthMethod` option:

#### No Authentication
```json
"AuthMethod": "none"
```

#### Bearer Token (Recommended)
```json
"AuthMethod": "bearer",
"BearerToken": "vault://json/my-webhook-token"
```
or
```json
"AuthMethod": "bearer",
"BearerToken": "your-bearer-token-here"
```

#### Basic Authentication
```json
"AuthMethod": "basic",
"BasicAuthUsername": "vault://json/webhook-username",
"BasicAuthPassword": "vault://json/webhook-password"
```

#### API Key
```json
"AuthMethod": "apikey",
"ApiKey": "vault://json/webhook-apikey",
"ApiKeyHeader": "X-API-Key"
```

### Advanced Options

- **CustomHeaders** (object): Key-value pairs of custom headers to include in requests
- **TimeoutSeconds** (number): HTTP request timeout in seconds. Default: `30`
- **MaxRetries** (number): Maximum retry attempts for failed requests. Default: `3`
- **RetryDelaySeconds** (number): Initial delay before first retry in seconds. Doubles for each subsequent retry. Default: `30`

## Webhook Payload

The webhook sends a JSON payload for each certificate event:

```json
{
  "eventType": "certificate.renewed",
  "timestamp": "2026-02-03T15:30:00Z",
  "eventId": "evt_abc123xyz789",
  "data": {
    "certificateName": "example.com",
    "renewalId": "renewal_xyz789",
    "hosts": [
      "example.com",
      "*.example.com"
    ],
    "plugins": {
      "Source": "IIS",
      "Validation": "SelfHosting",
      "Store": "CertificateStore",
      "Installation": "IIS"
    }
  },
  "errors": null,
  "logs": [
    {
      "level": "Information",
      "message": "Certificate renewed successfully"
    }
  ]
}
```

## Event Types

The webhook plugin sends notifications for the following events:

- `certificate.created` - New certificate issued
- `certificate.renewed` - Certificate successfully renewed
- `certificate.renewed.with_errors` - Certificate renewed but with errors
- `certificate.failed` - Certificate renewal failed
- `certificate.cancelled` - Certificate renewal cancelled
- `test` - Test notification (triggered via --test command)

## Request Headers

Standard headers included in all webhook requests:

- `X-Webhook-ID`: Unique identifier for the webhook event
- `X-Webhook-Timestamp`: Unix timestamp of the event
- `User-Agent`: simple-acme version information
- `Content-Type`: application/json (for POST requests)

## Secrets Management

All authentication credentials support simple-acme's vault system:

### Using JSON Vault
```bash
# Store a secret
wacs --vault-store --vault-key vault://json/webhook-token --vault-secret "your-secret-token"

# Reference in settings.json
"BearerToken": "vault://json/webhook-token"
```

### Using Environment Variables
```bash
export WEBHOOK_TOKEN="your-secret-token"
```
```json
"BearerToken": "vault://env/WEBHOOK_TOKEN"
```

### Using Script Vault
```json
"BearerToken": "vault://script/get-webhook-token"
```

## Retry Behavior

Failed webhook requests are automatically retried with exponential backoff:

1. **Initial attempt**: Immediate
2. **Retry 1**: After 30 seconds
3. **Retry 2**: After 60 seconds (30 × 2)
4. **Retry 3**: After 120 seconds (30 × 2²)

**Note**: 4xx client errors (authentication, bad request) are NOT retried, only 5xx server errors and network failures.

## Testing

Test your webhook configuration:

```bash
# Test notification
wacs --notificationtest
```

This sends a test webhook with `eventType: "test"` to verify your configuration.

## Security Best Practices

1. **Always use HTTPS** for webhook URLs in production
2. **Store credentials in vault** rather than plain text in settings.json
3. **Use Bearer tokens or API keys** instead of Basic authentication when possible
4. **Implement webhook signature verification** on the receiving endpoint
5. **Limit webhook URL exposure** to trusted networks
6. **Monitor failed webhook attempts** in logs
7. **Rotate credentials regularly**

## Examples

### Discord Webhook
```json
{
  "WebhookUrl": "https://discord.com/api/webhooks/YOUR_WEBHOOK_ID/YOUR_WEBHOOK_TOKEN",
  "HttpMethod": "POST",
  "AuthMethod": "none"
}
```

### Slack Incoming Webhook
```json
{
  "WebhookUrl": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
  "HttpMethod": "POST",
  "AuthMethod": "none"
}
```

### Custom API with Bearer Token
```json
{
  "WebhookUrl": "https://api.example.com/webhooks/certificates",
  "HttpMethod": "POST",
  "AuthMethod": "bearer",
  "BearerToken": "vault://json/api-bearer-token",
  "CustomHeaders": {
    "X-API-Version": "v1",
    "X-Client-ID": "simple-acme"
  }
}
```

### Azure Function with API Key
```json
{
  "WebhookUrl": "https://myfunction.azurewebsites.net/api/certificate-webhook",
  "HttpMethod": "POST",
  "AuthMethod": "apikey",
  "ApiKey": "vault://json/azure-function-key",
  "ApiKeyHeader": "x-functions-key"
}
```

## Troubleshooting

### Webhook not firing
- Check that `WebhookUrl` is configured in settings.json
- Verify the URL is reachable from the server
- Check logs for errors: `wacs --verbose`

### Authentication failures
- Verify credentials are correct
- Check vault secrets are properly configured
- Ensure the `AuthMethod` matches your endpoint's requirements

### Timeout errors
- Increase `TimeoutSeconds` if your endpoint is slow
- Check network connectivity
- Verify the webhook endpoint is responding

### Retry exhaustion
- Check endpoint availability
- Review server logs for 5xx errors
- Consider increasing `MaxRetries` or `RetryDelaySeconds`

## Logging

Webhook activity is logged at various levels:

- `Verbose`: Individual webhook attempts
- `Debug`: Successful webhook deliveries
- `Information`: Retry attempts
- `Warning`: Failed webhook attempts
- `Error`: Final failure after all retries

Enable verbose logging to diagnose issues:
```bash
wacs --verbose
```
