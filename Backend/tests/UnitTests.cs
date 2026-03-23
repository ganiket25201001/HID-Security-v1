using FluentAssertions;
using HIDSecurityService.Core.Models;
using Xunit;

namespace HIDSecurityService.Tests;

/// <summary>
/// Tests for UsbDevice model.
/// </summary>
public class UsbDeviceTests
{
    [Fact]
    public void GetFingerprint_ShouldReturnConsistentValue()
    {
        // Arrange
        var device = new UsbDevice
        {
            VendorId = 0x046D,
            ProductId = 0xC52B,
            SerialNumber = "ABC123"
        };

        // Act
        var fingerprint1 = device.GetFingerprint();
        var fingerprint2 = device.GetFingerprint();

        // Assert
        fingerprint1.Should().Be(fingerprint2);
        fingerprint1.Should().Be("046D:C52B:ABC123");
    }

    [Fact]
    public void GetFingerprint_ShouldHandleNullSerialNumber()
    {
        // Arrange
        var device = new UsbDevice
        {
            VendorId = 0x046D,
            ProductId = 0xC52B,
            SerialNumber = null
        };

        // Act
        var fingerprint = device.GetFingerprint();

        // Assert
        fingerprint.Should().Be("046D:C52B:NOSERIAL");
    }

    [Fact]
    public void GetVidPid_ShouldReturnFormattedString()
    {
        // Arrange
        var device = new UsbDevice
        {
            VendorId = 0x046D,
            ProductId = 0xC52B
        };

        // Act
        var vidPid = device.GetVidPid();

        // Assert
        vidPid.Should().Be("046D:C52B");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var device = new UsbDevice();

        // Assert
        device.IsConnected.Should().BeFalse();
        device.IsAuthorized.Should().BeFalse();
        device.Status.Should().Be(DeviceStatus.Unknown);
        device.RiskLevel.Should().Be(RiskLevel.None);
        device.Capabilities.Should().BeEmpty();
        device.Properties.Should().BeEmpty();
    }
}

/// <summary>
/// Tests for DPAPI helper.
/// </summary>
public class DpapiHelperTests
{
    [Fact]
    public void EncryptDecrypt_ShouldRoundTrip()
    {
        // Arrange
        var plainText = "SecretPassword123!";

        // Act
        var encrypted = Configuration.DpapiHelper.Encrypt(plainText);
        var decrypted = Configuration.DpapiHelper.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_ShouldProduceDifferentOutputEachTime()
    {
        // Arrange
        var plainText = "TestData";

        // Act
        var encrypted1 = Configuration.DpapiHelper.Encrypt(plainText);
        var encrypted2 = Configuration.DpapiHelper.Encrypt(plainText);

        // Assert - DPAPI includes random salt, so outputs should differ
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public void Decrypt_EmptyString_ShouldReturnEmptyString()
    {
        // Act
        var result = Configuration.DpapiHelper.Decrypt("");

        // Assert
        result.Should().Be("");
    }
}

/// <summary>
/// Tests for Policy Evaluation Service.
/// </summary>
public class PolicyEvaluationServiceTests
{
    [Fact]
    public async Task EvaluateDevice_AllowedVendor_ShouldAllow()
    {
        // Arrange
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<Services.Policy.PolicyEvaluationService>>();
        var policyService = new Services.Policy.PolicyEvaluationService(logger);
        
        var device = new UsbDevice
        {
            VendorId = 0x046D, // Logitech
            ProductId = 0xC52B,
            DeviceName = "Test Mouse",
            SerialNumber = "TEST123"
        };

        // Act
        var decision = await policyService.EvaluateDeviceAsync(device);

        // Assert
        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateDevice_NoSerialNumber_ShouldBlock()
    {
        // Arrange
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<Services.Policy.PolicyEvaluationService>>();
        var policyService = new Services.Policy.PolicyEvaluationService(logger);
        
        var device = new UsbDevice
        {
            VendorId = 0x046D,
            ProductId = 0xC52B,
            DeviceName = "Test Device",
            SerialNumber = null // No serial number
        };

        // Act
        var decision = await policyService.EvaluateDeviceAsync(device);

        // Assert - Default policy blocks devices without serial
        decision.IsAllowed.Should().BeFalse();
    }
}
