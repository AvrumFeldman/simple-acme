using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Configuration.Arguments
{
    internal class NotificationArguments : BaseArguments
    {
        public override string Name => "Notification";

        // Email notification options
        [CommandLine(Description = "SMTP server for email notifications. Overrides settings.json value.")]
        public string? SmtpServer { get; set; }

        [CommandLine(Description = "SMTP server port. Default is 25. Overrides settings.json value.")]
        public int? SmtpPort { get; set; }

        [CommandLine(Description = "SMTP username for authenticated SMTP. Overrides settings.json value.")]
        public string? SmtpUser { get; set; }

        [CommandLine(Description = "SMTP password for authenticated SMTP. Supports vault references. Overrides settings.json value.", Secret = true)]
        public string? SmtpPassword { get; set; }

        [CommandLine(Description = "Enable secure SMTP (TLS/SSL). Overrides settings.json value.")]
        public bool? SmtpSecure { get; set; }

        [CommandLine(Description = "Sender name for notification emails. Overrides settings.json value.")]
        public string? EmailSenderName { get; set; }

        [CommandLine(Description = "Sender email address for notifications. Overrides settings.json value.")]
        public string? EmailSender { get; set; }

        [CommandLine(Description = "Receiver email address(es) for notifications. Comma-separated for multiple addresses. Overrides settings.json value.")]
        public string? EmailReceiver { get; set; }

        [CommandLine(Description = "Send email notifications for successful certificate operations, not just failures. Overrides settings.json value.")]
        public bool? EmailOnSuccess { get; set; }

        // Webhook notification options
        [CommandLine(Description = "Webhook URL for HTTP notifications. Overrides settings.json value.")]
        public string? WebhookUrl { get; set; }

        [CommandLine(Description = "HTTP method for webhook (POST or GET). Default is POST. Overrides settings.json value.")]
        public string? WebhookHttpMethod { get; set; }

        [CommandLine(Description = "Webhook authentication method: none, bearer, basic, or apikey. Overrides settings.json value.")]
        public string? WebhookAuthMethod { get; set; }

        [CommandLine(Description = "Bearer token for webhook authentication. Supports vault references. Overrides settings.json value.", Secret = true)]
        public string? WebhookBearerToken { get; set; }

        [CommandLine(Description = "Username for basic authentication. Overrides settings.json value.")]
        public string? WebhookBasicUsername { get; set; }

        [CommandLine(Description = "Password for basic authentication. Supports vault references. Overrides settings.json value.", Secret = true)]
        public string? WebhookBasicPassword { get; set; }

        [CommandLine(Description = "API key for webhook authentication. Supports vault references. Overrides settings.json value.", Secret = true)]
        public string? WebhookApiKey { get; set; }

        [CommandLine(Description = "Header name for API key authentication. Default is X-API-Key. Overrides settings.json value.")]
        public string? WebhookApiKeyHeader { get; set; }

        [CommandLine(Description = "Webhook HTTP request timeout in seconds. Default is 30. Overrides settings.json value.")]
        public int? WebhookTimeoutSeconds { get; set; }

        [CommandLine(Description = "Maximum number of webhook retry attempts. Default is 3. Overrides settings.json value.")]
        public int? WebhookMaxRetries { get; set; }

        [CommandLine(Description = "Initial delay in seconds before webhook retry. Default is 30. Overrides settings.json value.")]
        public int? WebhookRetryDelaySeconds { get; set; }

        // General notification options
        [CommandLine(Description = "Computer name to include in notifications. Overrides settings.json value.")]
        public string? NotificationComputerName { get; set; }
    }
}
