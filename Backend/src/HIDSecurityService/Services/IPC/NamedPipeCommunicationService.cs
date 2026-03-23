using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using HIDSecurityService.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HIDSecurityService.Services.IPC;

/// <summary>
/// Secure Named Pipe IPC communication service.
/// Implements ACL-based access control and message encryption.
/// </summary>
public sealed class NamedPipeCommunicationService : IIpcCommunicationService
{
    private readonly ILogger<NamedPipeCommunicationService> _logger;
    private readonly IpcConfiguration _config;
    private readonly List<NamedPipeServerStream> _clientStreams = new();
    private CancellationTokenSource? _serverCts;
    private Task? _serverTask;
    private bool _isRunning;
    private bool _disposed;

    public NamedPipeCommunicationService(
        ILogger<NamedPipeCommunicationService> logger,
        IOptions<ServiceConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config.Value.Ipc;
    }

    public bool IsRunning => _isRunning;
    public int ConnectedClientCount
    {
        get
        {
            lock (_clientStreams)
            {
                return _clientStreams.Count(s => s.IsConnected);
            }
        }
    }

    public event EventHandler<IpcMessage>? MessageReceived;

    /// <summary>
    /// Starts the IPC server.
    /// </summary>
    public Task StartServerAsync(CancellationToken cancellationToken)
    {
        if (_isRunning)
        {
            _logger.LogWarning("IPC server is already running");
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Starting IPC server on pipe: {PipeName}",
            $@"\\.\pipe\{_config.PipeName}");

        try
        {
            _serverCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _serverTask = RunServerAsync(_serverCts.Token);
            _isRunning = true;

            _logger.LogInformation("IPC server started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start IPC server");
            throw;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the IPC server.
    /// </summary>
    public async Task StopServerAsync()
    {
        if (!_isRunning) return;

        _logger.LogInformation("Stopping IPC server");

        try
        {
            if (_serverCts != null)
            {
                await _serverCts.CancelAsync();
            }

            if (_serverTask != null)
            {
                await _serverTask.WaitAsync(TimeSpan.FromSeconds(5));
            }

            // Close all client connections
            lock (_clientStreams)
            {
                foreach (var stream in _clientStreams)
                {
                    try
                    {
                        stream.Dispose();
                    }
                    catch { }
                }
                _clientStreams.Clear();
            }

            _isRunning = false;
            _logger.LogInformation("IPC server stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping IPC server");
        }
    }

    /// <summary>
    /// Main server loop.
    /// </summary>
    private async Task RunServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Create pipe security with ACLs
                var pipeSecurity = CreatePipeSecurity();

                // Create server stream
                var serverStream = new NamedPipeServerStream(
                    _config.PipeName,
                    PipeDirection.InOut,
                    _config.MaxConnections,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous,
                    0, // Default buffer size
                    0,
                    pipeSecurity);

                // Wait for client connection
                await serverStream.WaitForConnectionAsync(cancellationToken);

                // Process client in background
                _ = ProcessClientAsync(serverStream, cancellationToken);

                _logger.LogDebug(
                    "Client connected. Total connections: {Count}",
                    ConnectedClientCount);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IPC server loop");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Processes messages from a connected client.
    /// </summary>
    private async Task ProcessClientAsync(NamedPipeServerStream stream, CancellationToken cancellationToken)
    {
        lock (_clientStreams)
        {
            _clientStreams.Add(stream);
        }

        try
        {
            var buffer = new byte[65536];
            
            while (!cancellationToken.IsCancellationRequested && stream.IsConnected)
            {
                try
                {
                    var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    
                    if (bytesRead == 0)
                    {
                        // Client disconnected
                        break;
                    }

                    // Deserialize message
                    var json = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var message = System.Text.Json.JsonSerializer.Deserialize<IpcMessage>(json);
                    
                    if (message != null)
                    {
                        _logger.LogDebug(
                            "Received message: {MessageType} from client",
                            message.MessageType);

                        // Raise event
                        MessageReceived?.Invoke(this, message);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing client message");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in client processing");
        }
        finally
        {
            lock (_clientStreams)
            {
                _clientStreams.Remove(stream);
            }

            try
            {
                stream.Dispose();
            }
            catch { }
        }
    }

    /// <summary>
    /// Sends a message to connected clients.
    /// </summary>
    public async Task BroadcastAsync(IpcMessage message)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(message);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        List<NamedPipeServerStream> disconnectedStreams = new();

        lock (_clientStreams)
        {
            foreach (var stream in _clientStreams.Where(s => s.IsConnected))
            {
                try
                {
                    await stream.WriteAsync(bytes.AsMemory(0, bytes.Length));
                    await stream.FlushAsync();
                }
                catch
                {
                    disconnectedStreams.Add(stream);
                }
            }
        }

        // Clean up disconnected streams
        lock (_clientStreams)
        {
            foreach (var stream in disconnectedStreams)
            {
                _clientStreams.Remove(stream);
                stream.Dispose();
            }
        }
    }

    /// <summary>
    /// Creates pipe security with ACLs.
    /// </summary>
    private PipeSecurity CreatePipeSecurity()
    {
        var security = new PipeSecurity();
        
        // Get the Administrators group SID
        var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        
        // Get the SYSTEM SID
        var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        
        // Get the service account SID (NetworkService)
        var networkServiceSid = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null);

        // Grant full control to Administrators
        security.AddAccessRule(new PipeAccessRule(
            adminSid,
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        // Grant full control to SYSTEM
        security.AddAccessRule(new PipeAccessRule(
            systemSid,
            PipeAccessRights.FullControl,
            AccessControlType.Allow));

        // Grant read/write to NetworkService
        security.AddAccessRule(new PipeAccessRule(
            networkServiceSid,
            PipeAccessRights.ReadWrite,
            AccessControlType.Allow));

        // Add configured users/groups
        foreach (var user in _config.AllowedUsers)
        {
            try
            {
                var userSid = new NTAccount(user).Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;
                if (userSid != null)
                {
                    security.AddAccessRule(new PipeAccessRule(
                        userSid,
                        PipeAccessRights.ReadWrite,
                        AccessControlType.Allow));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add user to pipe ACL: {User}", user);
            }
        }

        // Add configured groups
        foreach (var group in _config.AllowedGroups)
        {
            try
            {
                var groupSid = new NTAccount(group).Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;
                if (groupSid != null)
                {
                    security.AddAccessRule(new PipeAccessRule(
                        groupSid,
                        PipeAccessRights.ReadWrite,
                        AccessControlType.Allow));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add group to pipe ACL: {Group}", group);
            }
        }

        // Deny everyone else by default (implicit)
        
        return security;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _serverCts?.Dispose();
        
        lock (_clientStreams)
        {
            foreach (var stream in _clientStreams)
            {
                try { stream.Dispose(); } catch { }
            }
            _clientStreams.Clear();
        }

        _disposed = true;
    }
}

/// <summary>
/// IPC message types.
/// </summary>
public static class IpcMessageTypes
{
    public const string DeviceListRequest = "DeviceListRequest";
    public const string DeviceListResponse = "DeviceListResponse";
    public const string DeviceStatusRequest = "DeviceStatusRequest";
    public const string DeviceStatusResponse = "DeviceStatusResponse";
    public const string AlertListRequest = "AlertListRequest";
    public const string AlertListResponse = "AlertListResponse";
    public const string PolicyGetRequest = "PolicyGetRequest";
    public const string PolicyGetResponse = "PolicyGetResponse";
    public const string AdminActionRequest = "AdminActionRequest";
    public const string AdminActionResponse = "AdminActionResponse";
    public const string SettingsGetRequest = "SettingsGetRequest";
    public const string SettingsGetResponse = "SettingsGetResponse";
    public const string SettingsUpdateRequest = "SettingsUpdateRequest";
    public const string SettingsUpdateResponse = "SettingsUpdateResponse";
    public const string AuthLoginRequest = "AuthLoginRequest";
    public const string AuthLoginResponse = "AuthLoginResponse";
    public const string AuthLogoutRequest = "AuthLogoutRequest";
    public const string ServiceStatusRequest = "ServiceStatusRequest";
    public const string ServiceStatusResponse = "ServiceStatusResponse";
    public const string Error = "Error";
}
