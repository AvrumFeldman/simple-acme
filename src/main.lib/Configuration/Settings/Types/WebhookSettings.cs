using System.Collections.Generic;

namespace PKISharp.WACS.Configuration.Settings.Types
{
    public interface IWebhookSettings
    {
        /// <summary>
        /// URL to send webhook notifications to
        /// </summary>
        string? WebhookUrl { get; }

        /// <summary>
        /// HTTP method to use (GET or POST). POST is recommended.
        /// </summary>
        string? HttpMethod { get; }

        /// <summary>
        /// Authentication method: none, bearer, basic, or apikey
        /// </summary>
        string? AuthMethod { get; }

        /// <summary>
        /// Bearer token for bearer authentication
        /// </summary>
        string? BearerToken { get; }

        /// <summary>
        /// Username for basic authentication
        /// </summary>
        string? BasicAuthUsername { get; }

        /// <summary>
        /// Password for basic authentication
        /// </summary>
        string? BasicAuthPassword { get; }

        /// <summary>
        /// API key for API key authentication
        /// </summary>
        string? ApiKey { get; }

        /// <summary>
        /// Header name for API key (defaults to X-API-Key)
        /// </summary>
        string? ApiKeyHeader { get; }

        /// <summary>
        /// Custom headers to include in webhook requests
        /// </summary>
        Dictionary<string, string>? CustomHeaders { get; }

        /// <summary>
        /// Timeout in seconds for webhook requests
        /// </summary>
        int? TimeoutSeconds { get; }

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        int? MaxRetries { get; }

        /// <summary>
        /// Initial delay in seconds before first retry (doubles for each subsequent retry)
        /// </summary>
        int? RetryDelaySeconds { get; }
    }

    internal class InheritWebhookSettings(params IEnumerable<WebhookSettings?> chain) : InheritSettings<WebhookSettings>(chain), IWebhookSettings
    {
        public string? WebhookUrl => Get(x => x.WebhookUrl);
        public string? HttpMethod => Get(x => x.HttpMethod) ?? "POST";
        public string? AuthMethod => Get(x => x.AuthMethod) ?? "none";
        public string? BearerToken => Get(x => x.BearerToken);
        public string? BasicAuthUsername => Get(x => x.BasicAuthUsername);
        public string? BasicAuthPassword => Get(x => x.BasicAuthPassword);
        public string? ApiKey => Get(x => x.ApiKey);
        public string? ApiKeyHeader => Get(x => x.ApiKeyHeader) ?? "X-API-Key";
        public Dictionary<string, string>? CustomHeaders => Get(x => x.CustomHeaders);
        public int? TimeoutSeconds => Get(x => x.TimeoutSeconds) ?? 30;
        public int? MaxRetries => Get(x => x.MaxRetries) ?? 3;
        public int? RetryDelaySeconds => Get(x => x.RetryDelaySeconds) ?? 30;
    }

    public class WebhookSettings
    {
        [SettingsValue(
            Description = "URL endpoint to send webhook notifications to. Required for webhook notifications.",
            SubType = "url")]
        public string? WebhookUrl { get; set; }

        [SettingsValue(
            Default = "POST",
            Description = "HTTP method to use for webhook requests. Valid values are <code>POST</code> (recommended) or <code>GET</code>.")]
        public string? HttpMethod { get; set; }

        [SettingsValue(
            Default = "none",
            Description = "Authentication method for webhook requests. Valid values are <code>none</code>, <code>bearer</code>, <code>basic</code>, or <code>apikey</code>.")]
        public string? AuthMethod { get; set; }

        [SettingsValue(
            SubType = "secret",
            Description = "Bearer token for bearer authentication. Supports vault references like <code>vault://json/webhook-token</code>.")]
        public string? BearerToken { get; set; }

        [SettingsValue(
            Description = "Username for basic authentication. Supports vault references.")]
        public string? BasicAuthUsername { get; set; }

        [SettingsValue(
            SubType = "secret",
            Description = "Password for basic authentication. Supports vault references like <code>vault://json/webhook-password</code>.")]
        public string? BasicAuthPassword { get; set; }

        [SettingsValue(
            SubType = "secret",
            Description = "API key for API key authentication. Supports vault references like <code>vault://json/webhook-apikey</code>.")]
        public string? ApiKey { get; set; }

        [SettingsValue(
            Default = "X-API-Key",
            Description = "Header name for API key authentication. Defaults to <code>X-API-Key</code>.")]
        public string? ApiKeyHeader { get; set; }

        [SettingsValue(
            Description = "Custom headers to include in webhook requests as key-value pairs. Example: <code>{\"X-Custom-Header\": \"value\"}</code>")]
        public Dictionary<string, string>? CustomHeaders { get; set; }

        [SettingsValue(
            Default = "30",
            Description = "Timeout in seconds for webhook HTTP requests.")]
        public int? TimeoutSeconds { get; set; }

        [SettingsValue(
            Default = "3",
            Description = "Maximum number of retry attempts for failed webhook requests. Retries use exponential backoff.")]
        public int? MaxRetries { get; set; }

        [SettingsValue(
            Default = "30",
            Description = "Initial delay in seconds before first retry. Delay doubles for each subsequent retry (exponential backoff).")]
        public int? RetryDelaySeconds { get; set; }
    }
}
