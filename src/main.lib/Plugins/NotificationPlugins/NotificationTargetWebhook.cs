using PKISharp.WACS.Configuration.Settings.Types;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Interfaces;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.NotificationPlugins
{
    internal class NotificationTargetWebhook : INotificationTarget
    {
        private readonly ILogService _log;
        private readonly ISettings _settings;
        private readonly IPluginService _plugin;
        private readonly ICacheService _cacheService;
        private readonly DueDateStaticService _dueDate;
        private readonly ProxyService _proxyService;
        private readonly SecretServiceManager _secretService;

        public NotificationTargetWebhook(
            ILogService log,
            ISettings settings,
            IPluginService pluginService,
            ICacheService cacheService,
            DueDateStaticService dueDate,
            ProxyService proxyService,
            SecretServiceManager secretService)
        {
            _log = log;
            _settings = settings;
            _plugin = pluginService;
            _cacheService = cacheService;
            _dueDate = dueDate;
            _proxyService = proxyService;
            _secretService = secretService;
        }

        public async Task SendCreated(Renewal renewal, IEnumerable<MemoryEntry> log)
        {
            var payload = new WebhookPayload
            {
                EventType = "certificate.created",
                Timestamp = DateTime.UtcNow,
                EventId = Guid.NewGuid().ToString(),
                Data = new WebhookCertificateData
                {
                    CertificateName = renewal.LastFriendlyName,
                    RenewalId = renewal.Id,
                    Hosts = GetHosts(renewal),
                    Plugins = GetPlugins(renewal)
                },
                Logs = log.Select(l => new WebhookLogEntry
                {
                    Level = l.Level.ToString(),
                    Message = l.Message
                }).ToList()
            };

            await SendWebhookAsync(payload);
        }

        public async Task SendSuccess(Renewal renewal, IEnumerable<MemoryEntry> log)
        {
            var withErrors = log.Any(l => l.Level == LogEventLevel.Error);
            var payload = new WebhookPayload
            {
                EventType = withErrors ? "certificate.renewed.with_errors" : "certificate.renewed",
                Timestamp = DateTime.UtcNow,
                EventId = Guid.NewGuid().ToString(),
                Data = new WebhookCertificateData
                {
                    CertificateName = renewal.LastFriendlyName,
                    RenewalId = renewal.Id,
                    Hosts = GetHosts(renewal),
                    Plugins = GetPlugins(renewal)
                },
                Logs = log.Select(l => new WebhookLogEntry
                {
                    Level = l.Level.ToString(),
                    Message = l.Message
                }).ToList()
            };

            await SendWebhookAsync(payload);
        }

        public async Task SendFailure(Renewal renewal, IEnumerable<MemoryEntry> log, IEnumerable<string> errors)
        {
            var payload = new WebhookPayload
            {
                EventType = "certificate.failed",
                Timestamp = DateTime.UtcNow,
                EventId = Guid.NewGuid().ToString(),
                Data = new WebhookCertificateData
                {
                    CertificateName = renewal.LastFriendlyName,
                    RenewalId = renewal.Id,
                    Hosts = GetHosts(renewal),
                    Plugins = GetPlugins(renewal)
                },
                Errors = errors.ToList(),
                Logs = log.Select(l => new WebhookLogEntry
                {
                    Level = l.Level.ToString(),
                    Message = l.Message
                }).ToList()
            };

            await SendWebhookAsync(payload);
        }

        public Task SendCancel(Renewal renewal)
        {
            var payload = new WebhookPayload
            {
                EventType = "certificate.cancelled",
                Timestamp = DateTime.UtcNow,
                EventId = Guid.NewGuid().ToString(),
                Data = new WebhookCertificateData
                {
                    CertificateName = renewal.LastFriendlyName,
                    RenewalId = renewal.Id,
                    Hosts = GetHosts(renewal),
                    Plugins = GetPlugins(renewal)
                }
            };

            return SendWebhookAsync(payload);
        }

        public async Task SendTest()
        {
            var webhookSettings = _settings.Notification.Webhook;
            if (!IsEnabled())
            {
                _log.Error("Webhook notifications not enabled. Configure WebhookUrl in settings.json to enable this.");
            }
            else
            {
                _log.Information("Sending test webhook...");
                var payload = new WebhookPayload
                {
                    EventType = "test",
                    Timestamp = DateTime.UtcNow,
                    EventId = Guid.NewGuid().ToString(),
                    Data = new WebhookCertificateData
                    {
                        CertificateName = "Test Certificate"
                    }
                };

                var success = await SendWebhookAsync(payload);
                if (success)
                {
                    _log.Information("Test webhook sent successfully!");
                }
            }
        }

        private bool IsEnabled()
        {
            var webhookSettings = _settings.Notification.Webhook;
            return !string.IsNullOrWhiteSpace(webhookSettings?.WebhookUrl);
        }

        private async Task<bool> SendWebhookAsync(WebhookPayload payload)
        {
            var webhookSettings = _settings.Notification.Webhook;
            
            if (!IsEnabled())
            {
                _log.Verbose("Webhook notifications not configured, skipping");
                return true;
            }

            try
            {
                var url = webhookSettings.WebhookUrl;
                var method = webhookSettings.HttpMethod?.ToUpperInvariant() == "GET" ? HttpMethod.Get : HttpMethod.Post;

                using var client = await _proxyService.GetHttpClient();
                
                // Configure timeout
                var timeoutSeconds = webhookSettings.TimeoutSeconds ?? 30;
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                // Send request with retry logic
                var maxRetries = webhookSettings.MaxRetries ?? 3;
                var retryDelaySeconds = webhookSettings.RetryDelaySeconds ?? 30;

                // Prepare JSON payload once for all attempts
                string? jsonPayload = null;
                if (method == HttpMethod.Post)
                {
                    jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    });
                }

                for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
                {
                    try
                    {
                        // Create a new request for each attempt
                        using var request = new HttpRequestMessage(method, url);

                        // Add authentication
                        await AddAuthenticationAsync(request, webhookSettings);

                        // Add custom headers
                        if (webhookSettings.CustomHeaders != null)
                        {
                            foreach (var header in webhookSettings.CustomHeaders)
                            {
                                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            }
                        }

                        // Add standard headers
                        request.Headers.Add("X-Webhook-ID", payload.EventId);
                        request.Headers.Add("X-Webhook-Timestamp", new DateTimeOffset(payload.Timestamp).ToUnixTimeSeconds().ToString());
                        request.Headers.Add("User-Agent", $"simple-acme/{VersionService.SoftwareVersion}");

                        // Add payload for POST requests
                        if (jsonPayload != null)
                        {
                            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                        }

                        _log.Verbose($"Sending webhook to {url} (attempt {attempt}/{maxRetries + 1})");
                        var response = await client.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            _log.Debug($"Webhook sent successfully: {response.StatusCode}");
                            return true;
                        }

                        var responseBody = await response.Content.ReadAsStringAsync();
                        _log.Warning($"Webhook failed with status {response.StatusCode}: {responseBody}");

                        // Don't retry on 4xx errors (client errors)
                        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                        {
                            _log.Error($"Webhook request rejected (HTTP {response.StatusCode}), not retrying");
                            return false;
                        }

                        // Retry on 5xx errors
                        if (attempt <= maxRetries)
                        {
                            var delay = retryDelaySeconds * (int)Math.Pow(2, attempt - 1);
                            _log.Information($"Retrying webhook in {delay} seconds...");
                            await Task.Delay(TimeSpan.FromSeconds(delay));
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        _log.Warning($"Webhook request failed (attempt {attempt}/{maxRetries + 1}): {ex.Message}");
                        if (attempt <= maxRetries)
                        {
                            var delay = retryDelaySeconds * (int)Math.Pow(2, attempt - 1);
                            await Task.Delay(TimeSpan.FromSeconds(delay));
                        }
                        else
                        {
                            _log.Error($"Webhook failed after {maxRetries + 1} attempts: {ex.Message}");
                            return false;
                        }
                    }
                    catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                    {
                        _log.Warning($"Webhook request timed out after {timeoutSeconds} seconds (attempt {attempt}/{maxRetries + 1})");
                        if (attempt > maxRetries)
                        {
                            _log.Error($"Webhook failed after {maxRetries + 1} timeout attempts");
                            return false;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unexpected error sending webhook");
                return false;
            }
        }

        private async Task AddAuthenticationAsync(HttpRequestMessage request, IWebhookSettings webhookSettings)
        {
            var authMethod = webhookSettings.AuthMethod?.ToLowerInvariant();

            switch (authMethod)
            {
                case "bearer":
                    var bearerToken = await ResolveSecretAsync(webhookSettings.BearerToken);
                    if (!string.IsNullOrWhiteSpace(bearerToken))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                    }
                    break;

                case "basic":
                    var username = await ResolveSecretAsync(webhookSettings.BasicAuthUsername);
                    var password = await ResolveSecretAsync(webhookSettings.BasicAuthPassword);
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                    {
                        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    }
                    break;

                case "apikey":
                    var apiKey = await ResolveSecretAsync(webhookSettings.ApiKey);
                    var headerName = webhookSettings.ApiKeyHeader ?? "X-API-Key";
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        request.Headers.TryAddWithoutValidation(headerName, apiKey);
                    }
                    break;

                default:
                    // No authentication
                    break;
            }
        }

        private async Task<string?> ResolveSecretAsync(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            try
            {
                return await _secretService.EvaluateSecret(value);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to resolve secret: {value}");
                return null;
            }
        }

        private List<string> GetHosts(Renewal renewal)
        {
            var hosts = new List<string>();
            try
            {
                var orders = _dueDate.CurrentOrders(renewal);
                foreach (var order in orders)
                {
                    var cert = _cacheService.PreviousInfo(renewal, order.Key);
                    if (cert != null)
                    {
                        hosts.AddRange(cert.SanNames.Select(x => x.Value));
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug("Error retrieving hosts for webhook notification: {message}", ex.Message);
            }
            return hosts.Distinct().ToList();
        }

        private Dictionary<string, string> GetPlugins(Renewal renewal)
        {
            try
            {
                return new Dictionary<string, string>
                {
                    { "Source", _plugin.GetPlugin(renewal.TargetPluginOptions).Name },
                    { "Validation", _plugin.GetPlugin(renewal.ValidationPluginOptions).Name },
                    { "Order", renewal.OrderPluginOptions != null ? _plugin.GetPlugin(renewal.OrderPluginOptions).Name : "N/A" },
                    { "Csr", renewal.CsrPluginOptions != null ? _plugin.GetPlugin(renewal.CsrPluginOptions).Name : "N/A" },
                    { "Store", string.Join(", ", renewal.StorePluginOptions.Select(x => _plugin.GetPlugin(x).Name)) },
                    { "Installation", string.Join(", ", renewal.InstallationPluginOptions.Select(x => _plugin.GetPlugin(x).Name)) }
                };
            }
            catch (Exception ex)
            {
                _log.Debug("Error retrieving plugin information for webhook notification: {message}", ex.Message);
                return new Dictionary<string, string>();
            }
        }

        // Payload classes
        private class WebhookPayload
        {
            public string EventType { get; set; } = "";
            public DateTime Timestamp { get; set; }
            public string EventId { get; set; } = "";
            public WebhookCertificateData? Data { get; set; }
            public List<string>? Errors { get; set; }
            public List<WebhookLogEntry>? Logs { get; set; }
        }

        private class WebhookCertificateData
        {
            public string? CertificateName { get; set; }
            public string? RenewalId { get; set; }
            public List<string>? Hosts { get; set; }
            public Dictionary<string, string>? Plugins { get; set; }
        }

        private class WebhookLogEntry
        {
            public string Level { get; set; } = "";
            public string Message { get; set; } = "";
        }
    }
}
