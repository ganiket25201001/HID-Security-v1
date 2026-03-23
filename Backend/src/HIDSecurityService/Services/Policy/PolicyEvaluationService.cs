using HIDSecurityService.Core.Events;
using HIDSecurityService.Core.Interfaces;
using HIDSecurityService.Core.Models;
using Microsoft.Extensions.Logging;

namespace HIDSecurityService.Services.Policy;

/// <summary>
/// Policy evaluation service placeholder.
/// Implements basic rule-based checks with hooks for full policy engine.
/// </summary>
public sealed class PolicyEvaluationService : IPolicyEvaluationService
{
    private readonly ILogger<PolicyEvaluationService> _logger;
    private PolicyDocument? _activePolicy;
    private readonly List<PolicyRule> _defaultRules;

    public PolicyEvaluationService(ILogger<PolicyEvaluationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize default rules
        _defaultRules = CreateDefaultRules();
    }

    public event EventHandler<PolicyDocument>? PolicyUpdated;

    /// <summary>
    /// Evaluates a device against current policies.
    /// </summary>
    public Task<PolicyDecision> EvaluateDeviceAsync(UsbDevice device)
    {
        if (device == null) throw new ArgumentNullException(nameof(device));

        _logger.LogDebug(
            "Evaluating device: {DeviceName} (VID:{VendorId:X4}, PID:{ProductId:X4})",
            device.DeviceName,
            device.VendorId,
            device.ProductId);

        var decision = new PolicyDecision
        {
            IsAllowed = true,
            Reason = "No policy violations detected",
            EnforcementAction = EnforcementAction.Allow
        };

        // Get active policy or use defaults
        var rules = _activePolicy?.Rules ?? _defaultRules;

        // Evaluate rules in priority order
        foreach (var rule in rules.OrderBy(r => r.Priority))
        {
            if (!rule.Enabled) continue;

            var ruleResult = EvaluateRule(rule, device);
            if (ruleResult.HasValue)
            {
                decision.IsAllowed = ruleResult.Value;
                decision.Reason = rule.Name;
                decision.Action = rule.Action;
                decision.PolicyId = _activePolicy?.PolicyId;
                decision.RuleIds.Add(rule.RuleId);

                _logger.LogInformation(
                    "Policy rule matched: {RuleName} -> {Action}",
                    rule.Name,
                    rule.Action);

                break; // First matching rule wins
            }
        }

        // Update device authorization status
        device.IsAuthorized = decision.IsAllowed;
        device.RiskLevel = CalculateRiskLevel(device, decision);

        return Task.FromResult(decision);
    }

    /// <summary>
    /// Evaluates a device action against current policies.
    /// </summary>
    public Task<PolicyDecision> EvaluateActionAsync(string deviceId, string action)
    {
        // Placeholder for action-based policy evaluation
        return Task.FromResult(new PolicyDecision
        {
            IsAllowed = true,
            Reason = "Action evaluation not yet implemented",
            EnforcementAction = EnforcementAction.LogOnly
        });
    }

    /// <summary>
    /// Gets the active policy.
    /// </summary>
    public Task<PolicyDocument?> GetActivePolicyAsync()
    {
        return Task.FromResult(_activePolicy);
    }

    /// <summary>
    /// Reloads policies from storage.
    /// </summary>
    public Task ReloadPoliciesAsync()
    {
        // Placeholder - will be implemented with file watching
        _logger.LogInformation("Policy reload requested (not yet implemented)");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Evaluates a single rule against a device.
    /// </summary>
    private bool? EvaluateRule(PolicyRule rule, UsbDevice device)
    {
        var condition = rule.Condition;
        bool? result = null;

        // Check vendor ID conditions
        if (condition.BlockedVendorIds?.Contains(device.VendorId) == true)
        {
            result = false; // Blocked
        }
        else if (condition.AllowedVendorIds?.Contains(device.VendorId) == true)
        {
            result = true; // Allowed
        }

        // Check product ID conditions
        if (condition.BlockedProductIds?.Contains(device.ProductId) == true)
        {
            result = false;
        }
        else if (condition.AllowedProductIds?.Contains(device.ProductId) == true)
        {
            result = true;
        }

        // Check device class conditions
        if (condition.BlockedDeviceClasses?.Contains(device.DeviceClass) == true)
        {
            result = false;
        }

        // Check serial number conditions
        if (condition.RequireSerialNumber == true && string.IsNullOrEmpty(device.SerialNumber))
        {
            result = false;
        }

        if (condition.BlockedSerialNumbers?.Contains(device.SerialNumber ?? "") == true)
        {
            result = false;
        }

        // Check unknown device blocking
        if (condition.BlockUnknownDevices == true && string.IsNullOrEmpty(device.SerialNumber))
        {
            result = false;
        }

        return result;
    }

    /// <summary>
    /// Calculates risk level based on device properties and policy decision.
    /// </summary>
    private RiskLevel CalculateRiskLevel(UsbDevice device, PolicyDecision decision)
    {
        if (!decision.IsAllowed)
        {
            return RiskLevel.High;
        }

        if (string.IsNullOrEmpty(device.SerialNumber))
        {
            return RiskLevel.Medium; // No serial number is suspicious
        }

        if (device.DeviceClass == UsbDeviceClasses.MassStorage)
        {
            return RiskLevel.Low; // Storage devices get low risk by default
        }

        if (device.DeviceClass == UsbDeviceClasses.HumanInterfaceDevice)
        {
            // HID devices need extra scrutiny
            if (string.IsNullOrEmpty(device.Manufacturer))
            {
                return RiskLevel.Medium;
            }
        }

        return RiskLevel.None;
    }

    /// <summary>
    /// Creates default policy rules.
    /// </summary>
    private List<PolicyRule> CreateDefaultRules()
    {
        return new List<PolicyRule>
        {
            new PolicyRule
            {
                RuleId = "default-allow-known",
                Name = "Allow Known Devices",
                Description = "Allow devices with valid serial numbers",
                Enabled = true,
                Priority = 100,
                Condition = new RuleCondition
                {
                    RequireSerialNumber = false
                },
                Action = EnforcementAction.Allow
            },
            new PolicyRule
            {
                RuleId = "default-block-no-serial",
                Name = "Block Devices Without Serial",
                Description = "Block devices that don't provide a serial number",
                Enabled = true,
                Priority = 50,
                Condition = new RuleCondition
                {
                    RequireSerialNumber = true
                },
                Action = EnforcementAction.Block
            },
            new PolicyRule
            {
                RuleId = "default-notify-mass-storage",
                Name = "Notify on Mass Storage",
                Description = "Log all mass storage device connections",
                Enabled = true,
                Priority = 200,
                Condition = new RuleCondition
                {
                    AllowedDeviceClasses = new List<byte> { UsbDeviceClasses.MassStorage }
                },
                Action = EnforcementAction.Notify
            }
        };
    }

    /// <summary>
    /// Loads policy from file (placeholder).
    /// </summary>
    public async Task<PolicyDocument?> LoadPolicyFromFileAsync(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                _logger.LogWarning("Policy file not found: {Path}", path);
                return null;
            }

            var json = await File.ReadAllTextAsync(path);
            var policy = System.Text.Json.JsonSerializer.Deserialize<PolicyDocument>(json);
            
            if (policy != null)
            {
                _activePolicy = policy;
                PolicyUpdated?.Invoke(this, policy);
                
                _logger.LogInformation("Loaded policy: {PolicyName} v{Version}", policy.Name, policy.Version);
            }

            return policy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load policy from: {Path}", path);
            return null;
        }
    }
}
