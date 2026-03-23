using System.Diagnostics;
using HIDSecurityService.Core.Events;
using HIDSecurityService.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace HIDSecurityService.Services.Logging;

/// <summary>
/// Security event logging service with file and Windows Event Log support.
/// </summary>
public sealed class SecurityEventLoggerService : ISecurityEventLogger
{
    private readonly ILogger<SecurityEventLoggerService> _logger;
    private readonly LoggingConfiguration _config;
    private readonly string _logFilePath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _eventLogSourceCreated;

    public SecurityEventLoggerService(
        ILogger<SecurityEventLoggerService> logger,
        IOptions<ServiceConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value.Logging;
        
        // Ensure log directory exists
        var logDir = Path.Combine(AppContext.BaseDirectory, _config.LogPath);
        Directory.CreateDirectory(logDir);
        
        _logFilePath = Path.Combine(logDir, "security-events.log");
    }

    /// <summary>
    /// Logs a security event to all configured outputs.
    /// </summary>
    public async Task LogEventAsync(SecurityEvent securityEvent)
    {
        if (securityEvent == null) throw new ArgumentNullException(nameof(securityEvent));

        try
        {
            // Log to file
            if (_config.EnableFileLog)
            {
                await LogToFileAsync(securityEvent);
            }

            // Log to Windows Event Log
            if (_config.EnableEventLog)
            {
                LogToEventLog(securityEvent);
            }

            // Log via Serilog
            LogViaSerilog(securityEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event: {EventId}", securityEvent.EventId);
        }
    }

    /// <summary>
    /// Logs a security event to Windows Event Log.
    /// </summary>
    public void LogToEventLog(SecurityEvent securityEvent)
    {
        try
        {
            EnsureEventLogSource();

            var entryType = securityEvent.Severity switch
            {
                EventSeverity.Informational => EventLogEntryType.Information,
                EventSeverity.Warning => EventLogEntryType.Warning,
                EventSeverity.Error => EventLogEntryType.Error,
                EventSeverity.Critical => EventLogEntryType.FailureAudit,
                _ => EventLogEntryType.Information
            };

            var eventId = GenerateEventId(securityEvent);
            var category = securityEvent.Category.ToString();
            var message = FormatEventMessage(securityEvent);

            EventLog.WriteEntry(
                _config.EventLogSource,
                message,
                entryType,
                eventId,
                0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to Event Log");
        }
    }

    /// <summary>
    /// Logs a security event to file.
    /// </summary>
    public async Task LogToFileAsync(SecurityEvent securityEvent)
    {
        await _writeLock.WaitAsync();
        try
        {
            var logEntry = new
            {
                Timestamp = securityEvent.Timestamp.ToString("O"),
                EventId = securityEvent.EventId,
                EventType = securityEvent.EventType,
                Severity = securityEvent.Severity.ToString(),
                Category = securityEvent.Category.ToString(),
                Title = securityEvent.Title,
                Description = securityEvent.Description,
                Source = securityEvent.Source,
                ComputerName = securityEvent.ComputerName,
                DeviceId = securityEvent.DeviceId,
                UserSid = securityEvent.UserSid,
                SessionId = securityEvent.SessionId,
                Data = securityEvent.Data
            };

            var jsonLine = System.Text.Json.JsonSerializer.Serialize(logEntry);
            
            // Append to file with rotation check
            await CheckAndRotateLogFileAsync();
            await File.AppendAllTextAsync(_logFilePath, jsonLine + Environment.NewLine);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Gets recent events from the log file.
    /// </summary>
    public async Task<IReadOnlyList<SecurityEvent>> GetRecentEventsAsync(int count = 100)
    {
        var events = new List<SecurityEvent>();

        try
        {
            if (!File.Exists(_logFilePath))
            {
                return events;
            }

            var lines = await File.ReadAllLinesAsync(_logFilePath);
            
            foreach (var line in lines.Reverse().Take(count))
            {
                try
                {
                    var doc = System.Text.Json.JsonDocument.Parse(line);
                    var eventType = doc.RootElement.GetProperty("EventType").GetString();
                    
                    var securityEvent = eventType switch
                    {
                        nameof(DeviceConnectedEvent) => new DeviceConnectedEvent(),
                        nameof(DeviceDisconnectedEvent) => new DeviceDisconnectedEvent(),
                        nameof(PolicyViolationEvent) => new PolicyViolationEvent(),
                        nameof(SecurityThreatEvent) => new SecurityThreatEvent(),
                        _ => new SecurityThreatEvent() // Default
                    };

                    // Populate common properties
                    securityEvent.EventId = doc.RootElement.GetProperty("EventId").GetString() ?? "";
                    securityEvent.Timestamp = doc.RootElement.GetProperty("Timestamp").GetDateTime();
                    securityEvent.Title = doc.RootElement.GetProperty("Title").GetString() ?? "";
                    securityEvent.Description = doc.RootElement.GetProperty("Description").GetString() ?? "";
                    securityEvent.Severity = Enum.Parse<EventSeverity>(
                        doc.RootElement.GetProperty("Severity").GetString() ?? "Informational");
                    securityEvent.Category = Enum.Parse<EventCategory>(
                        doc.RootElement.GetProperty("Category").GetString() ?? "System");

                    events.Add(securityEvent);
                }
                catch
                {
                    // Skip malformed entries
                }
            }

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read recent events");
            return events;
        }
    }

    /// <summary>
    /// Exports events to a file.
    /// </summary>
    public async Task ExportEventsAsync(string filePath, DateTime? from = null, DateTime? to = null)
    {
        var events = await GetRecentEventsAsync(10000);
        
        var filteredEvents = events.Where(e =>
        {
            if (from.HasValue && e.Timestamp < from) return false;
            if (to.HasValue && e.Timestamp > to) return false;
            return true;
        }).ToList();

        var exportData = new
        {
            ExportedAt = DateTime.UtcNow,
            From = from,
            To = to,
            EventCount = filteredEvents.Count,
            ComputerName = Environment.MachineName,
            Events = filteredEvents
        };

        var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json);
        
        _logger.LogInformation("Exported {Count} events to {FilePath}", filteredEvents.Count, filePath);
    }

    private void EnsureEventLogSource()
    {
        if (_eventLogSourceCreated) return;

        try
        {
            if (!EventLog.SourceExists(_config.EventLogSource))
            {
                EventLog.CreateEventSource(_config.EventLogSource, "Application");
            }
            _eventLogSourceCreated = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create event log source");
        }
    }

    private void LogViaSerilog(SecurityEvent securityEvent)
    {
        var logLevel = securityEvent.Severity switch
        {
            EventSeverity.Informational => LogEventLevel.Information,
            EventSeverity.Warning => LogEventLevel.Warning,
            EventSeverity.Error => LogEventLevel.Error,
            EventSeverity.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };

        Log.Logger.Write(logLevel, 
            "[{Category}] [{Severity}] {Title} - {Description} (DeviceId: {DeviceId}, EventId: {EventId})",
            securityEvent.Category,
            securityEvent.Severity,
            securityEvent.Title,
            securityEvent.Description,
            securityEvent.DeviceId ?? "N/A",
            securityEvent.EventId);
    }

    private int GenerateEventId(SecurityEvent securityEvent)
    {
        // Generate a deterministic event ID based on category and type
        var hash = securityEvent.Category.GetHashCode() ^ securityEvent.EventType.GetHashCode();
        return Math.Abs(hash % 65535); // Event IDs must be 0-65535
    }

    private string FormatEventMessage(SecurityEvent securityEvent)
    {
        return $"""
            Event: {securityEvent.Title}
            Type: {securityEvent.EventType}
            Category: {securityEvent.Category}
            Severity: {securityEvent.Severity}
            Description: {securityEvent.Description}
            Device: {securityEvent.DeviceId ?? "N/A"}
            Time: {securityEvent.Timestamp:O}
            Computer: {securityEvent.ComputerName}
            """;
    }

    private async Task CheckAndRotateLogFileAsync()
    {
        if (!File.Exists(_logFilePath)) return;

        var fileInfo = new FileInfo(_logFilePath);
        var maxSizeBytes = _config.MaxFileSizeMb * 1024 * 1024;

        if (fileInfo.Length < maxSizeBytes) return;

        // Rotate log file
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var rotatedPath = Path.Combine(
            Path.GetDirectoryName(_logFilePath)!,
            $"security-events_{timestamp}.log");

        await File.MoveAsync(_logFilePath, rotatedPath);
        
        _logger.LogInformation("Rotated log file to {RotatedPath}", rotatedPath);

        // Cleanup old rotated logs
        await CleanupOldLogsAsync();
    }

    private async Task CleanupOldLogsAsync()
    {
        try
        {
            var logDir = Path.GetDirectoryName(_logFilePath)!;
            var retentionDate = DateTime.UtcNow.AddDays(-_config.RetentionDays);

            var oldLogs = Directory.GetFiles(logDir, "security-events_*.log")
                .Where(f =>
                {
                    try
                    {
                        return File.GetCreationTimeUtc(f) < retentionDate;
                    }
                    catch
                    {
                        return false;
                    }
                });

            foreach (var log in oldLogs)
            {
                try
                {
                    File.Delete(log);
                    _logger.LogDebug("Deleted old log: {Log}", log);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete old log: {Log}", log);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old logs");
        }
    }

    public void Dispose()
    {
        _writeLock.Dispose();
    }
}

/// <summary>
/// Serilog configuration extension.
/// </summary>
public static class SecurityEventLoggerExtensions
{
    public static IHostBuilder UseSecurityLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            var config = services.BuildServiceProvider().GetRequiredService<IOptions<ServiceConfiguration>>();
            var loggingConfig = config.Value.Logging;

            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId();

            if (loggingConfig.EnableFileLog)
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, loggingConfig.LogPath, "service-.log");
                configuration.WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: loggingConfig.RetentionDays,
                    fileSizeLimitBytes: loggingConfig.MaxFileSizeMb * 1024 * 1024,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
            }

            if (loggingConfig.EnableEventLog)
            {
                configuration.WriteTo.EventLog(
                    loggingConfig.EventLogSource,
                    manageEventSource: true);
            }
        });
    }
}

/// <summary>
/// Serilog enricher for machine name.
/// </summary>
public class MachineNameEnricher : Serilog.Core.IEnricher
{
    public void Enrich(LogEvent logEvent, Serilog.Core.ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MachineName", Environment.MachineName));
    }
}

public static class LoggerEnrichmentConfigurationExtensions
{
    public static LoggerConfiguration WithMachineName(this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        return enrichmentConfiguration.With<MachineNameEnricher>();
    }
}
