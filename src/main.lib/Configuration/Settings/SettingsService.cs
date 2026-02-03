using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Services;
using System;
using System.IO;

namespace PKISharp.WACS.Configuration.Settings
{
    public class SettingsService
    {
        private const string _fileName = "settings.json";
        private readonly ILogService _log;
        private readonly FolderHelpers _folderHelpers;
        private readonly MainArguments? _arguments;
        private readonly NotificationArguments? _notificationArguments;
        private InheritSettings _settings = new();
        public ISettings Current => _settings;

        public SettingsService(ILogService log, ArgumentsParser parser)
        {
            _log = log;
            _folderHelpers = new FolderHelpers(log);

            if (!parser.ValidateMain())
            {
                return;
            }

            _arguments = parser.GetArguments<MainArguments>();
            _notificationArguments = parser.GetArguments<NotificationArguments>();
            if (_arguments == null)
            {
                return;
            }
            if (!LoadGlobalSettings())
            {
                return;
            }
            var configRoot = _settings.Client.ConfigRoot;
            var configPath = _settings.Client.ConfigurationPath;
            try
            {
                _folderHelpers.EnsureFolderExists(configRoot, "global configuration", true);
                _folderHelpers.EnsureFolderExists(configPath, "server configuration", false);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error initializing program");
                return;
            }
            var serverSettings = LoadServerSettings();
            
            // Merge notification arguments with settings
            MergeNotificationArguments();
            
            try
            {
                if (serverSettings)
                {
                    if (configRoot != _settings.Client.ConfigRoot)
                    {
                        configRoot = _settings.Client.ConfigRoot;
                        _folderHelpers.EnsureFolderExists(configRoot, "global configuration", true);
                    }
                    if (configPath != _settings.Client.ConfigurationPath)
                    {
                        configPath = _settings.Client.ConfigurationPath;
                        _folderHelpers.EnsureFolderExists(configPath, "server configuration", true);
                    }
                }
                var pathCompareMode =
                    OperatingSystem.IsWindows() ?
                    StringComparison.OrdinalIgnoreCase :
                    StringComparison.Ordinal;
                _folderHelpers.EnsureFolderExists(_settings.Client.LogPath, "log", !_settings.Client.LogPath.StartsWith(configPath, pathCompareMode));
                _folderHelpers.EnsureFolderExists(_settings.Cache.CachePath, "cache", !_settings.Client.LogPath.StartsWith(configPath, pathCompareMode));

                // Configure disk logger
                _log.ApplyClientSettings(Current.Client);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error initializing program");
                return;
            }
            _settings.Valid = true;
        }

        private bool LoadGlobalSettings()
        {
            var globalFile = EnsureGlobalSettingsFile();
            try
            {
                _settings = new InheritSettings(Settings.Load(globalFile));
            }
            catch (Exception ex)
            {
                _log.Error($"Unable to load {globalFile.Name}");
                while (ex.InnerException != null)
                {
                    _log.Error(ex.InnerException.Message);
                    ex = ex.InnerException;
                }
                return false;
            }
            try
            {
                _settings.BaseUri = ChooseBaseUri();
            }
            catch
            {
                _log.Error("Error choosing ACME server");
                return false;
            }
            return true;
        }

        private bool LoadServerSettings()
        {
            // Load overrides for settings at the server level
            var settingsFileName = _fileName;
            _log.Verbose("Looking for {settingsFileName} in {path}", settingsFileName, _settings.Client.ConfigurationPath);
            var settings = new FileInfo(Path.Combine(_settings.Client.ConfigurationPath, settingsFileName));
            if (settings.Exists)
            {
                try
                {
                    _settings = _settings.MergeTyped(Settings.Load(settings));
                    return true;
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to load server settings from {settingsFileName}", settingsFileName);
                }
            }
            return false;
        }

        private FileInfo EnsureGlobalSettingsFile()
        {
            var settingsFileTemplateName = "settings_default.json";
            _log.Verbose("Looking for {settingsFileName} in {path}", _fileName, VersionService.SettingsPath);
            var settings = new FileInfo(Path.Combine(VersionService.SettingsPath, _fileName));
            var settingsTemplate = new FileInfo(Path.Combine(VersionService.ResourcePath, settingsFileTemplateName));
            var useFile = settings;
            if (!settings.Exists)
            {
                if (!settingsTemplate.Exists)
                {
                    // For .NET tool case
                    settingsTemplate = new FileInfo(Path.Combine(VersionService.ResourcePath, _fileName));
                }
                if (!settingsTemplate.Exists)
                {
                    _log.Warning("Unable to locate {settings}", _fileName);
                }
                else
                {
                    _log.Verbose("Copying {settingsFileTemplateName} to {settingsFileName}", settingsFileTemplateName, _fileName);
                    try
                    {
                        if (!settings.Directory!.Exists)
                        {
                            settings.Directory.Create();
                        }
                        settingsTemplate.CopyTo(settings.FullName);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Unable to create {settingsFileName}, falling back to defaults", _fileName);
                        useFile = settingsTemplate;
                    }
                }
            }
            return useFile;
        }

        /// <summary>
        /// Choose the base URI based on command line options and/or global settings defaults
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private Uri ChooseBaseUri()
        {
            if (!string.IsNullOrWhiteSpace(_arguments?.BaseUri))
            {
                try
                {
                    return new Uri(_arguments.BaseUri);
                } 
                catch (Exception ex)
                {
                    _log.Error(ex, "Invalid --baseuri specified");
                    throw;
                }
            }
            if (_arguments?.Test ?? false)
            {
                if (_settings.Acme.DefaultBaseUriTest?.IsAbsoluteUri ?? false)
                {
                    return _settings.Acme.DefaultBaseUriTest;
                } 
                else
                {
                    _log.Warning("Setting Acme.DefaultBaseUriTest is unspecified or invalid, fallback to Acme.DefaultBaseUri");
                }
            }
            if (_settings.Acme.DefaultBaseUri?.IsAbsoluteUri ?? false)
            {
                return _settings.Acme.DefaultBaseUri;
            }
            else
            {
                _log.Error("Setting Acme.DefaultBaseUri is unspecified or invalid, please specify a valid absolute URI");
                throw new Exception();
            }
        }

        /// <summary>
        /// Merge notification command-line arguments with settings
        /// </summary>
        private void MergeNotificationArguments()
        {
            if (_notificationArguments == null)
            {
                return;
            }

            // Check if any notification arguments were provided
            var hasNotificationArgs = 
                _notificationArguments.SmtpServer != null ||
                _notificationArguments.SmtpPort != null ||
                _notificationArguments.SmtpUser != null ||
                _notificationArguments.SmtpPassword != null ||
                _notificationArguments.SmtpSecure != null ||
                _notificationArguments.EmailSenderName != null ||
                _notificationArguments.EmailSender != null ||
                _notificationArguments.EmailReceiver != null ||
                _notificationArguments.EmailOnSuccess != null ||
                _notificationArguments.WebhookUrl != null ||
                _notificationArguments.WebhookHttpMethod != null ||
                _notificationArguments.WebhookAuthMethod != null ||
                _notificationArguments.WebhookBearerToken != null ||
                _notificationArguments.WebhookBasicUsername != null ||
                _notificationArguments.WebhookBasicPassword != null ||
                _notificationArguments.WebhookApiKey != null ||
                _notificationArguments.WebhookApiKeyHeader != null ||
                _notificationArguments.WebhookTimeoutSeconds != null ||
                _notificationArguments.WebhookMaxRetries != null ||
                _notificationArguments.WebhookRetryDelaySeconds != null ||
                _notificationArguments.NotificationComputerName != null;

            if (!hasNotificationArgs)
            {
                return;
            }

            // Create notification settings from command-line arguments
            var notificationSettings = new Types.NotificationSettings();

            if (_notificationArguments.SmtpServer != null)
                notificationSettings.SmtpServer = _notificationArguments.SmtpServer;
            if (_notificationArguments.SmtpPort != null)
                notificationSettings.SmtpPort = _notificationArguments.SmtpPort;
            if (_notificationArguments.SmtpUser != null)
                notificationSettings.SmtpUser = _notificationArguments.SmtpUser;
            if (_notificationArguments.SmtpPassword != null)
                notificationSettings.SmtpPassword = _notificationArguments.SmtpPassword;
            if (_notificationArguments.SmtpSecure != null)
                notificationSettings.SmtpSecure = _notificationArguments.SmtpSecure;
            if (_notificationArguments.EmailSenderName != null)
                notificationSettings.SenderName = _notificationArguments.EmailSenderName;
            if (_notificationArguments.EmailSender != null)
                notificationSettings.SenderAddress = _notificationArguments.EmailSender;
            if (_notificationArguments.EmailReceiver != null)
            {
                // Split comma-separated email addresses
                var receivers = _notificationArguments.EmailReceiver.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                notificationSettings.ReceiverAddresses = [..receivers];
            }
            if (_notificationArguments.EmailOnSuccess != null)
                notificationSettings.EmailOnSuccess = _notificationArguments.EmailOnSuccess;
            if (_notificationArguments.NotificationComputerName != null)
                notificationSettings.ComputerName = _notificationArguments.NotificationComputerName;

            // Webhook settings
            if (_notificationArguments.WebhookUrl != null ||
                _notificationArguments.WebhookHttpMethod != null ||
                _notificationArguments.WebhookAuthMethod != null ||
                _notificationArguments.WebhookBearerToken != null ||
                _notificationArguments.WebhookBasicUsername != null ||
                _notificationArguments.WebhookBasicPassword != null ||
                _notificationArguments.WebhookApiKey != null ||
                _notificationArguments.WebhookApiKeyHeader != null ||
                _notificationArguments.WebhookTimeoutSeconds != null ||
                _notificationArguments.WebhookMaxRetries != null ||
                _notificationArguments.WebhookRetryDelaySeconds != null)
            {
                notificationSettings.Webhook = new Types.WebhookSettings();
                
                if (_notificationArguments.WebhookUrl != null)
                    notificationSettings.Webhook.WebhookUrl = _notificationArguments.WebhookUrl;
                if (_notificationArguments.WebhookHttpMethod != null)
                    notificationSettings.Webhook.HttpMethod = _notificationArguments.WebhookHttpMethod;
                if (_notificationArguments.WebhookAuthMethod != null)
                    notificationSettings.Webhook.AuthMethod = _notificationArguments.WebhookAuthMethod;
                if (_notificationArguments.WebhookBearerToken != null)
                    notificationSettings.Webhook.BearerToken = _notificationArguments.WebhookBearerToken;
                if (_notificationArguments.WebhookBasicUsername != null)
                    notificationSettings.Webhook.BasicAuthUsername = _notificationArguments.WebhookBasicUsername;
                if (_notificationArguments.WebhookBasicPassword != null)
                    notificationSettings.Webhook.BasicAuthPassword = _notificationArguments.WebhookBasicPassword;
                if (_notificationArguments.WebhookApiKey != null)
                    notificationSettings.Webhook.ApiKey = _notificationArguments.WebhookApiKey;
                if (_notificationArguments.WebhookApiKeyHeader != null)
                    notificationSettings.Webhook.ApiKeyHeader = _notificationArguments.WebhookApiKeyHeader;
                if (_notificationArguments.WebhookTimeoutSeconds != null)
                    notificationSettings.Webhook.TimeoutSeconds = _notificationArguments.WebhookTimeoutSeconds;
                if (_notificationArguments.WebhookMaxRetries != null)
                    notificationSettings.Webhook.MaxRetries = _notificationArguments.WebhookMaxRetries;
                if (_notificationArguments.WebhookRetryDelaySeconds != null)
                    notificationSettings.Webhook.RetryDelaySeconds = _notificationArguments.WebhookRetryDelaySeconds;
            }

            // Create a new Settings object with only notification settings
            var argumentSettings = new Settings
            {
                Notification = notificationSettings
            };

            // Merge with existing settings (command-line arguments take precedence)
            _settings = _settings.MergeTyped(argumentSettings);
            _log.Verbose("Merged notification command-line arguments with settings");
        }

    }
}