/**
 * Mock Data for Development and Testing
 * 
 * This data simulates backend responses until the actual backend is integrated.
 */

import { DeviceInfo, AlertEvent, PolicyConfig, PolicyRule } from '../types/ipc-types';

// ============================================================================
// Mock Devices
// ============================================================================

export const mockDevices: DeviceInfo[] = [
  {
    deviceId: 'dev-001',
    deviceName: 'Logitech MX Master 3',
    deviceType: 'mouse',
    vendorId: '046D',
    productId: 'C52B',
    serialNumber: 'LGT-MX-001234',
    isConnected: true,
    isAuthorized: true,
    lastSeen: new Date().toISOString(),
    riskLevel: 'low',
  },
  {
    deviceId: 'dev-002',
    deviceName: 'Corsair K95 RGB Platinum',
    deviceType: 'keyboard',
    vendorId: '1B1C',
    productId: '1B02',
    serialNumber: 'CSR-K95-567890',
    isConnected: true,
    isAuthorized: true,
    lastSeen: new Date().toISOString(),
    riskLevel: 'low',
  },
  {
    deviceId: 'dev-003',
    deviceName: 'SanDisk Ultra USB 3.0',
    deviceType: 'storage',
    vendorId: '0781',
    productId: '5581',
    serialNumber: '4C530001234567890',
    isConnected: true,
    isAuthorized: false,
    lastSeen: new Date().toISOString(),
    riskLevel: 'high',
  },
  {
    deviceId: 'dev-004',
    deviceName: 'Unknown HID Device',
    deviceType: 'other',
    vendorId: 'DEAD',
    productId: 'BEEF',
    isConnected: true,
    isAuthorized: false,
    lastSeen: new Date().toISOString(),
    riskLevel: 'critical',
  },
  {
    deviceId: 'dev-005',
    deviceName: 'Intel AX210 WiFi Adapter',
    deviceType: 'network',
    vendorId: '8086',
    productId: '2725',
    serialNumber: 'INT-AX210-ABC123',
    isConnected: true,
    isAuthorized: true,
    lastSeen: new Date().toISOString(),
    riskLevel: 'medium',
  },
  {
    deviceId: 'dev-006',
    deviceName: 'Razer DeathAdder V2',
    deviceType: 'mouse',
    vendorId: '1532',
    productId: '0084',
    serialNumber: 'RZR-DA-987654',
    isConnected: false,
    isAuthorized: true,
    lastSeen: new Date(Date.now() - 3600000).toISOString(),
    riskLevel: 'low',
  },
];

// ============================================================================
// Mock Alerts
// ============================================================================

export const mockAlerts: AlertEvent[] = [
  {
    alertId: 'alert-001',
    type: 'device_connected',
    severity: 'high',
    title: 'Unauthorized USB Storage Connected',
    description: 'An unauthorized USB storage device was connected to workstation WS-042. Device has been blocked pending admin review.',
    deviceId: 'dev-003',
    userId: 'user-001',
    timestamp: new Date(Date.now() - 300000).toISOString(),
    acknowledged: false,
    dismissed: false,
    metadata: {
      workstation: 'WS-042',
      user: 'john.doe',
      action: 'blocked',
    },
  },
  {
    alertId: 'alert-002',
    type: 'suspicious_activity',
    severity: 'critical',
    title: 'Unknown HID Device Detected',
    description: 'A potentially malicious HID device with suspicious vendor/product IDs was detected. This may indicate a BadUSB attack.',
    deviceId: 'dev-004',
    userId: 'user-002',
    timestamp: new Date(Date.now() - 600000).toISOString(),
    acknowledged: false,
    dismissed: false,
    metadata: {
      workstation: 'WS-015',
      threatLevel: 'critical',
      action: 'quarantined',
    },
  },
  {
    alertId: 'alert-003',
    type: 'policy_violation',
    severity: 'medium',
    title: 'Network Adapter Policy Violation',
    description: 'A network adapter was enabled that does not meet the approved hardware list policy.',
    deviceId: 'dev-005',
    timestamp: new Date(Date.now() - 7200000).toISOString(),
    acknowledged: true,
    dismissed: false,
    metadata: {
      policyId: 'policy-002',
      violation: 'unapproved_hardware',
    },
  },
  {
    alertId: 'alert-004',
    type: 'device_connected',
    severity: 'low',
    title: 'New Authorized Device Connected',
    description: 'A new authorized mouse was connected to workstation WS-001.',
    deviceId: 'dev-001',
    userId: 'user-003',
    timestamp: new Date(Date.now() - 86400000).toISOString(),
    acknowledged: true,
    dismissed: true,
    metadata: {
      workstation: 'WS-001',
      action: 'authorized',
    },
  },
  {
    alertId: 'alert-005',
    type: 'unauthorized_access',
    severity: 'high',
    title: 'Repeated Authentication Failures',
    description: 'Multiple failed authentication attempts detected from workstation WS-028.',
    userId: 'user-004',
    timestamp: new Date(Date.now() - 1800000).toISOString(),
    acknowledged: false,
    dismissed: false,
    metadata: {
      workstation: 'WS-028',
      attemptCount: 5,
      action: 'account_locked',
    },
  },
];

// ============================================================================
// Mock Policies
// ============================================================================

const mockPolicyRules: PolicyRule[] = [
  {
    ruleId: 'rule-001',
    name: 'Block Unapproved USB Storage',
    description: 'Automatically block any USB storage devices not on the approved list',
    enabled: true,
    action: 'block',
    conditions: {
      deviceType: 'storage',
      isAuthorized: false,
    },
    priority: 1,
  },
  {
    ruleId: 'rule-002',
    name: 'Quarantine Suspicious HID Devices',
    description: 'Quarantine any HID device with suspicious characteristics',
    enabled: true,
    action: 'quarantine',
    conditions: {
      deviceType: ['keyboard', 'mouse'],
      riskLevel: ['high', 'critical'],
    },
    priority: 2,
  },
  {
    ruleId: 'rule-003',
    name: 'Notify on New Device Connection',
    description: 'Send notification when any new device is connected',
    enabled: true,
    action: 'notify',
    conditions: {
      isNewDevice: true,
    },
    priority: 10,
  },
  {
    ruleId: 'rule-004',
    name: 'Allow Approved Keyboards',
    description: 'Automatically allow keyboards from approved vendors',
    enabled: true,
    action: 'allow',
    conditions: {
      deviceType: 'keyboard',
      approvedVendors: ['1B1C', '046D', '045E'],
    },
    priority: 5,
  },
];

export const mockPolicies: PolicyConfig[] = [
  {
    policyId: 'policy-001',
    name: 'Default Security Policy',
    version: '1.2.0',
    rules: mockPolicyRules,
    lastUpdated: new Date(Date.now() - 86400000).toISOString(),
    enforcedBy: 'system',
  },
  {
    policyId: 'policy-002',
    name: 'High Security Mode',
    version: '2.0.1',
    rules: [
      ...mockPolicyRules,
      {
        ruleId: 'rule-005',
        name: 'Block All Network Adapters',
        description: 'Block all network adapters except approved ones',
        enabled: true,
        action: 'block',
        conditions: {
          deviceType: 'network',
          isApproved: false,
        },
        priority: 1,
      },
    ],
    lastUpdated: new Date(Date.now() - 172800000).toISOString(),
    enforcedBy: 'admin-001',
  },
];

// ============================================================================
// Mock Statistics
// ============================================================================

export const mockStats = {
  totalDevices: 24,
  connectedDevices: 18,
  authorizedDevices: 15,
  blockedDevices: 3,
  quarantinedDevices: 1,
  totalAlerts: 47,
  criticalAlerts: 2,
  highAlerts: 8,
  mediumAlerts: 15,
  lowAlerts: 22,
  acknowledgedAlerts: 35,
  pendingAlerts: 12,
  activePolicies: 2,
  policyViolations: 8,
};

// ============================================================================
// Mock Recent Events
// ============================================================================

export const mockRecentEvents = [
  {
    id: 'evt-001',
    type: 'device_connected',
    message: 'Logitech MX Master 3 connected to WS-001',
    timestamp: new Date(Date.now() - 60000).toISOString(),
  },
  {
    id: 'evt-002',
    type: 'policy_applied',
    message: 'Default Security Policy applied to all workstations',
    timestamp: new Date(Date.now() - 300000).toISOString(),
  },
  {
    id: 'evt-003',
    type: 'device_blocked',
    message: 'SanDisk Ultra USB 3.0 blocked on WS-042',
    timestamp: new Date(Date.now() - 600000).toISOString(),
  },
  {
    id: 'evt-004',
    type: 'alert_acknowledged',
    message: 'Alert alert-003 acknowledged by admin',
    timestamp: new Date(Date.now() - 900000).toISOString(),
  },
  {
    id: 'evt-005',
    type: 'policy_updated',
    message: 'High Security Policy updated by admin-001',
    timestamp: new Date(Date.now() - 3600000).toISOString(),
  },
];

// ============================================================================
// Mock Admin Actions
// ============================================================================

export const mockAdminActions = [
  {
    id: 'action-001',
    type: 'restart_service',
    name: 'Restart Security Service',
    description: 'Restart the HID security monitoring service',
    requiresConfirmation: true,
    dangerLevel: 'medium',
  },
  {
    id: 'action-002',
    type: 'reload_policy',
    name: 'Reload Policies',
    description: 'Reload all security policies from disk',
    requiresConfirmation: false,
    dangerLevel: 'low',
  },
  {
    id: 'action-003',
    type: 'flush_logs',
    name: 'Flush Event Logs',
    description: 'Clear all event logs (this action cannot be undone)',
    requiresConfirmation: true,
    dangerLevel: 'high',
  },
  {
    id: 'action-004',
    type: 'export_data',
    name: 'Export Audit Data',
    description: 'Export all audit logs and device history',
    requiresConfirmation: false,
    dangerLevel: 'low',
  },
  {
    id: 'action-005',
    type: 'system_scan',
    name: 'Run Full System Scan',
    description: 'Perform a comprehensive scan of all connected devices',
    requiresConfirmation: true,
    dangerLevel: 'medium',
  },
];

// ============================================================================
// Mock Settings
// ============================================================================

export const mockSettings = {
  theme: 'dark' as const,
  notificationsEnabled: true,
  autoRefreshInterval: 30,
  language: 'en',
  showSystemTray: true,
  alertSoundEnabled: true,
  emailNotifications: false,
  emailRecipients: [],
  logRetentionDays: 90,
  maxLogSize: '100MB',
};
