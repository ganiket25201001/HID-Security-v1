using System.ServiceProcess;
using Microsoft.Extensions.Hosting;

namespace HIDSecurityService.Installers;

/// <summary>
/// Windows Service installer configuration.
/// </summary>
public static class ServiceInstaller
{
    /// <summary>
    /// Service name for installation.
    /// </summary>
    public const string ServiceName = "HIDSecurityService";
    
    /// <summary>
    /// Service display name.
    /// </summary>
    public const string ServiceDisplayName = "HID Security Service";
    
    /// <summary>
    /// Service description.
    /// </summary>
    public const string ServiceDescription = 
        "Monitors USB/HID devices for security threats, enforces device policies, " +
        "and provides real-time threat detection and response capabilities.";

    /// <summary>
    /// Installs the Windows Service with recovery options.
    /// </summary>
    public static void Install()
    {
        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        
        Console.WriteLine("Installing HID Security Service...");
        Console.WriteLine($"Executable: {exePath}");
        
        try
        {
            // Use sc.exe to create the service
            var createCommand = $"sc create \"{ServiceName}\" binPath=\"{exePath}\" start=auto DisplayName=\"{ServiceDisplayName}\"";
            var createResult = RunCommand("cmd.exe", $"/c {createCommand}");
            
            if (createResult.ExitCode != 0)
            {
                throw new Exception($"Failed to create service: {createResult.Output}");
            }
            
            Console.WriteLine("Service created successfully");
            
            // Set service description
            var descCommand = $"sc description \"{ServiceName}\" \"{ServiceDescription}\"";
            RunCommand("cmd.exe", $"/c {descCommand}");
            
            Console.WriteLine("Service description set");
            
            // Configure service recovery options
            // Restart on failure, wait 1 minute, then 5 minutes, then 10 minutes
            var recoveryCommand = $"sc failure \"{ServiceName}\" reset=86400 actions=restart/60000/restart/300000/restart/600000";
            RunCommand("cmd.exe", $"/c {recoveryCommand}");
            
            Console.WriteLine("Service recovery configured");
            
            // Set service to run as LocalSystem (can be changed to NetworkService or custom account)
            var configCommand = $"sc config \"{ServiceName}\" obj=LocalSystem";
            RunCommand("cmd.exe", $"/c {configCommand}");
            
            Console.WriteLine("Service account configured");
            
            // Start the service
            var startCommand = $"sc start \"{ServiceName}\"";
            RunCommand("cmd.exe", $"/c {startCommand}");
            
            Console.WriteLine("Service started");
            Console.WriteLine();
            Console.WriteLine("===========================================");
            Console.WriteLine("HID Security Service installed successfully!");
            Console.WriteLine($"Service Name: {ServiceName}");
            Console.WriteLine($"Display Name: {ServiceDisplayName}");
            Console.WriteLine("===========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Installation failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Uninstalls the Windows Service.
    /// </summary>
    public static void Uninstall()
    {
        Console.WriteLine("Uninstalling HID Security Service...");
        
        try
        {
            // Stop the service first
            var stopCommand = $"sc stop \"{ServiceName}\"";
            RunCommand("cmd.exe", $"/c {stopCommand}");
            
            Console.WriteLine("Service stopped");
            
            // Delete the service
            var deleteCommand = $"sc delete \"{ServiceName}\"";
            RunCommand("cmd.exe", $"/c {deleteCommand}");
            
            Console.WriteLine("Service deleted");
            Console.WriteLine();
            Console.WriteLine("HID Security Service uninstalled successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Uninstallation failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Checks if the service is installed.
    /// </summary>
    public static bool IsInstalled()
    {
        try
        {
            using var scm = System.ServiceProcess.ServiceController.GetDevices();
            foreach (var service in scm)
            {
                if (service.ServiceName == ServiceName)
                {
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current service status.
    /// </summary>
    public static ServiceControllerStatus? GetStatus()
    {
        try
        {
            using var service = new System.ServiceProcess.ServiceController(ServiceName);
            return service.Status;
        }
        catch
        {
            return null;
        }
    }

    private static (int ExitCode, string Output) RunCommand(string fileName, string arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        var output = process?.StandardOutput.ReadToEnd() ?? "";
        var error = process?.StandardError.ReadToEnd() ?? "";
        process?.WaitForExit();

        return (process?.ExitCode ?? -1, output + error);
    }
}

/// <summary>
/// Program entry point for installation operations.
/// </summary>
public class InstallerProgram
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        var command = args[0].ToLowerInvariant();

        try
        {
            switch (command)
            {
                case "install":
                case "-i":
                case "/i":
                    if (!Environment.IsPrivilegedProcess)
                    {
                        Console.WriteLine("Error: Installation requires administrator privileges");
                        Console.WriteLine("Please run as Administrator");
                        Environment.Exit(1);
                    }
                    ServiceInstaller.Install();
                    break;

                case "uninstall":
                case "-u":
                case "/u":
                    if (!Environment.IsPrivilegedProcess)
                    {
                        Console.WriteLine("Error: Uninstallation requires administrator privileges");
                        Console.WriteLine("Please run as Administrator");
                        Environment.Exit(1);
                    }
                    ServiceInstaller.Uninstall();
                    break;

                case "status":
                case "-s":
                case "/s":
                    if (ServiceInstaller.IsInstalled())
                    {
                        var status = ServiceInstaller.GetStatus();
                        Console.WriteLine($"Service Status: {status}");
                    }
                    else
                    {
                        Console.WriteLine("Service is not installed");
                    }
                    break;

                case "check":
                case "-c":
                case "/c":
                    Console.WriteLine($"Installed: {ServiceInstaller.IsInstalled()}");
                    break;

                default:
                    PrintUsage();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("HID Security Service Installer");
        Console.WriteLine();
        Console.WriteLine("Usage: HIDSecurityService.exe [command]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  install, -i, /i     Install the service (requires admin)");
        Console.WriteLine("  uninstall, -u, /u   Uninstall the service (requires admin)");
        Console.WriteLine("  status, -s, /s      Show service status");
        Console.WriteLine("  check, -c, /c       Check if service is installed");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  HIDSecurityService.exe install");
        Console.WriteLine("  HIDSecurityService.exe -u");
    }
}
