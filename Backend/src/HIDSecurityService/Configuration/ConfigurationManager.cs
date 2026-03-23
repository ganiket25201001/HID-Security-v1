using System.Security.Cryptography;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceConfiguration = HIDSecurityService.Core.Interfaces.ServiceConfiguration;
using ConfigurationValidationResult = HIDSecurityService.Core.Interfaces.ConfigurationValidationResult;

namespace HIDSecurityService.Configuration;

/// <summary>
/// Configuration manager with DPAPI encryption support.
/// </summary>
public sealed class ConfigurationManager : Core.Interfaces.IConfigurationManager
{
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly ServiceConfiguration _config;
    private readonly string _configFilePath;
    private readonly string _integrityFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ConfigurationManager(
        ILogger<ConfigurationManager> logger,
        IOptions<ServiceConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value;
        _configFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.service.json");
        _integrityFilePath = Path.Combine(AppContext.BaseDirectory, _config.Security.IntegrityKeyPath);
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    public ServiceConfiguration GetConfiguration() => _config;

    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    public T GetValue<T>(string key, T defaultValue)
    {
        try
        {
            var parts = key.Split(':');
            object? value = _config;

            foreach (var part in parts)
            {
                var prop = value?.GetType().GetProperty(part);
                if (prop == null) return defaultValue;
                value = prop.GetValue(value);
            }

            return value is T typedValue ? typedValue : defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration value: {Key}", key);
            return defaultValue;
        }
    }

    /// <summary>
    /// Gets a decrypted configuration value.
    /// </summary>
    public T GetDecryptedValue<T>(string key, T defaultValue)
    {
        try
        {
            var encryptedValue = GetValue<string>(key, null);
            if (string.IsNullOrEmpty(encryptedValue)) return defaultValue;

            var decryptedValue = DpapiHelper.Decrypt(encryptedValue, _config.Security.DpapiScope);
            
            if (decryptedValue is T typedValue)
            {
                return typedValue;
            }

            return (T)Convert.ChangeType(decryptedValue, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt configuration value: {Key}", key);
            return defaultValue;
        }
    }

    /// <summary>
    /// Updates configuration.
    /// </summary>
    public async Task UpdateConfigurationAsync(Action<ServiceConfiguration> update)
    {
        await _lock.WaitAsync();
        try
        {
            update(_config);
            
            // Save to file
            var json = System.Text.Json.JsonSerializer.Serialize(_config, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_configFilePath, json);
            
            // Update integrity hash
            await UpdateIntegrityHashAsync();
            
            _logger.LogInformation("Configuration updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Validates configuration integrity.
    /// </summary>
    public ConfigurationValidationResult ValidateConfiguration()
    {
        var result = new ConfigurationValidationResult { IsValid = true };

        try
        {
            // Validate logging configuration
            if (string.IsNullOrEmpty(_config.Logging.LogPath))
            {
                result.Errors.Add("Logging.LogPath cannot be empty");
                result.IsValid = false;
            }

            if (_config.Logging.RetentionDays < 1 || _config.Logging.RetentionDays > 3650)
            {
                result.Warnings.Add("Logging.RetentionDays should be between 1 and 3650");
            }

            // Validate monitoring configuration
            if (_config.Monitoring.PollingIntervalMs < 100)
            {
                result.Warnings.Add("Monitoring.PollingIntervalMs below 100ms may impact performance");
            }

            if (_config.Monitoring.MaxTrackedDevices < 1)
            {
                result.Errors.Add("Monitoring.MaxTrackedDevices must be at least 1");
                result.IsValid = false;
            }

            // Validate IPC configuration
            if (string.IsNullOrEmpty(_config.Ipc.PipeName))
            {
                result.Errors.Add("Ipc.PipeName cannot be empty");
                result.IsValid = false;
            }

            if (_config.Ipc.MaxConnections < 1)
            {
                result.Errors.Add("Ipc.MaxConnections must be at least 1");
                result.IsValid = false;
            }

            // Validate security configuration
            if (_config.Security.EnableIntegrityChecks)
            {
                var integrityValid = ValidateIntegrityHash();
                if (!integrityValid)
                {
                    result.Errors.Add("Configuration integrity check failed");
                    result.IsValid = false;
                }
            }

            // Validate protected paths exist
            foreach (var path in _config.Security.ProtectedPaths)
            {
                var fullPath = Path.Combine(AppContext.BaseDirectory, path);
                if (!Directory.Exists(fullPath) && !File.Exists(fullPath))
                {
                    result.Warnings.Add($"Protected path does not exist: {path}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// Validates the integrity hash.
    /// </summary>
    private bool ValidateIntegrityHash()
    {
        try
        {
            if (!File.Exists(_integrityFilePath))
            {
                _logger.LogWarning("Integrity hash file not found");
                return false;
            }

            var storedHash = File.ReadAllText(_integrityFilePath);
            var currentHash = ComputeConfigHash();

            return storedHash == currentHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate integrity hash");
            return false;
        }
    }

    /// <summary>
    /// Updates the integrity hash.
    /// </summary>
    private async Task UpdateIntegrityHashAsync()
    {
        try
        {
            var hash = ComputeConfigHash();
            var hashDir = Path.GetDirectoryName(_integrityFilePath)!;
            Directory.CreateDirectory(hashDir);
            await File.WriteAllTextAsync(_integrityFilePath, hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update integrity hash");
        }
    }

    private string ComputeConfigHash()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_config);
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hash);
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}

/// <summary>
/// DPAPI encryption helper.
/// </summary>
public static class DpapiHelper
{
    /// <summary>
    /// Encrypts data using DPAPI.
    /// </summary>
    public static string Encrypt(string plainText, Core.Interfaces.DataProtectionScope scope = Core.Interfaces.DataProtectionScope.LocalMachine)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        try
        {
            var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var protectedScope = scope == Core.Interfaces.DataProtectionScope.LocalMachine
                ? System.Security.Cryptography.DataProtectionScope.LocalMachine
                : System.Security.Cryptography.DataProtectionScope.CurrentUser;

            var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(plainBytes, null, protectedScope);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Failed to encrypt data using DPAPI", ex);
        }
    }

    /// <summary>
    /// Decrypts data using DPAPI.
    /// </summary>
    public static string Decrypt(string encryptedText, Core.Interfaces.DataProtectionScope scope = Core.Interfaces.DataProtectionScope.LocalMachine)
    {
        if (string.IsNullOrEmpty(encryptedText)) return encryptedText;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var protectedScope = scope == Core.Interfaces.DataProtectionScope.LocalMachine
                ? System.Security.Cryptography.DataProtectionScope.LocalMachine
                : System.Security.Cryptography.DataProtectionScope.CurrentUser;

            var plainBytes = System.Security.Cryptography.ProtectedData.Unprotect(encryptedBytes, null, protectedScope);
            return System.Text.Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Failed to decrypt data using DPAPI", ex);
        }
    }

    /// <summary>
    /// Encrypts a byte array.
    /// </summary>
    public static byte[] Encrypt(byte[] plainData, byte[]? optionalEntropy = null, Core.Interfaces.DataProtectionScope scope = Core.Interfaces.DataProtectionScope.LocalMachine)
    {
        try
        {
            var protectedScope = scope == Core.Interfaces.DataProtectionScope.LocalMachine
                ? System.Security.Cryptography.DataProtectionScope.LocalMachine
                : System.Security.Cryptography.DataProtectionScope.CurrentUser;

            return System.Security.Cryptography.ProtectedData.Protect(plainData, optionalEntropy, protectedScope);
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Failed to encrypt data", ex);
        }
    }

    /// <summary>
    /// Decrypts a byte array.
    /// </summary>
    public static byte[] Decrypt(byte[] encryptedData, byte[]? optionalEntropy = null, Core.Interfaces.DataProtectionScope scope = Core.Interfaces.DataProtectionScope.LocalMachine)
    {
        try
        {
            var protectedScope = scope == Core.Interfaces.DataProtectionScope.LocalMachine
                ? System.Security.Cryptography.DataProtectionScope.LocalMachine
                : System.Security.Cryptography.DataProtectionScope.CurrentUser;

            return System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, optionalEntropy, protectedScope);
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Failed to decrypt data", ex);
        }
    }
}
