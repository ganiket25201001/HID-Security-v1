import React, { useState, useEffect } from 'react';
import { mockDevices } from '../mock/data';
import { DeviceInfo } from '../types/ipc-types';
import './DevicesPage.css';

type DeviceFilter = 'all' | 'connected' | 'authorized' | 'blocked' | 'quarantined';
type DeviceTypeFilter = 'all' | 'keyboard' | 'mouse' | 'storage' | 'network' | 'other';

/**
 * Connected Devices Page
 * 
 * Lists all connected devices with filtering and status information.
 */
const DevicesPage: React.FC = () => {
  const [devices, setDevices] = useState<DeviceInfo[]>([]);
  const [filter, setFilter] = useState<DeviceFilter>('all');
  const [typeFilter, setTypeFilter] = useState<DeviceTypeFilter>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [selectedDevice, setSelectedDevice] = useState<DeviceInfo | null>(null);

  useEffect(() => {
    const loadDevices = async (): Promise<void> => {
      await new Promise(resolve => setTimeout(resolve, 500));
      setDevices(mockDevices);
      setIsLoading(false);
    };

    loadDevices();
  }, []);

  const filteredDevices = devices.filter(device => {
    // Status filter
    if (filter === 'connected' && !device.isConnected) return false;
    if (filter === 'authorized' && !device.isAuthorized) return false;
    if (filter === 'blocked' && (device.isConnected || device.isAuthorized)) return false;
    
    // Type filter
    if (typeFilter !== 'all' && device.deviceType !== typeFilter) return false;
    
    // Search filter
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      return (
        device.deviceName.toLowerCase().includes(query) ||
        device.deviceId.toLowerCase().includes(query) ||
        device.vendorId.toLowerCase().includes(query) ||
        device.productId.toLowerCase().includes(query)
      );
    }
    
    return true;
  });

  const getDeviceIcon = (type: string): string => {
    switch (type) {
      case 'keyboard': return '⌨️';
      case 'mouse': return '🖱️';
      case 'storage': return '💾';
      case 'network': return '🌐';
      default: return '📦';
    }
  };

  const getStatusBadge = (device: DeviceInfo): React.ReactNode => {
    if (!device.isConnected) {
      return <span className="badge badge-muted">Disconnected</span>;
    }
    if (!device.isAuthorized) {
      return <span className="badge badge-danger">Blocked</span>;
    }
    return <span className="badge badge-success">Authorized</span>;
  };

  const stats = {
    total: devices.length,
    connected: devices.filter(d => d.isConnected).length,
    authorized: devices.filter(d => d.isAuthorized).length,
    blocked: devices.filter(d => !d.isAuthorized).length,
  };

  return (
    <div className="devices-page">
      <div className="page-header">
        <div>
          <h1>Connected Devices</h1>
          <p className="page-subtitle">Monitor and manage all connected hardware</p>
        </div>
        <button className="btn-primary">
          <span>🔄</span> Scan for Devices
        </button>
      </div>

      {/* Stats Bar */}
      <div className="devices-stats">
        <div className="device-stat">
          <span className="stat-number">{stats.total}</span>
          <span className="stat-label">Total</span>
        </div>
        <div className="device-stat">
          <span className="stat-number text-success">{stats.connected}</span>
          <span className="stat-label">Connected</span>
        </div>
        <div className="device-stat">
          <span className="stat-number text-info">{stats.authorized}</span>
          <span className="stat-label">Authorized</span>
        </div>
        <div className="device-stat">
          <span className="stat-number text-danger">{stats.blocked}</span>
          <span className="stat-label">Blocked</span>
        </div>
      </div>

      {/* Filters */}
      <div className="devices-filters">
        <div className="filter-group">
          <label>Status:</label>
          <div className="filter-buttons">
            {(['all', 'connected', 'authorized', 'blocked'] as const).map((f) => (
              <button
                key={f}
                className={`filter-btn ${filter === f ? 'active' : ''}`}
                onClick={() => setFilter(f)}
              >
                {f.charAt(0).toUpperCase() + f.slice(1)}
              </button>
            ))}
          </div>
        </div>

        <div className="filter-group">
          <label>Type:</label>
          <select
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value as DeviceTypeFilter)}
          >
            <option value="all">All Types</option>
            <option value="keyboard">Keyboards</option>
            <option value="mouse">Mice</option>
            <option value="storage">Storage</option>
            <option value="network">Network</option>
            <option value="other">Other</option>
          </select>
        </div>

        <div className="search-group">
          <input
            type="text"
            placeholder="Search devices..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="search-input"
          />
          <span className="search-icon">🔍</span>
        </div>
      </div>

      {/* Devices Grid */}
      {isLoading ? (
        <div className="loading-state">
          <div className="loading-spinner-large"></div>
          <p>Loading devices...</p>
        </div>
      ) : filteredDevices.length === 0 ? (
        <div className="empty-state">
          <span className="empty-icon">📭</span>
          <h3>No devices found</h3>
          <p>Try adjusting your filters or search query</p>
        </div>
      ) : (
        <div className="devices-grid">
          {filteredDevices.map((device) => (
            <div
              key={device.deviceId}
              className={`device-card ${selectedDevice?.deviceId === device.deviceId ? 'selected' : ''}`}
              onClick={() => setSelectedDevice(device)}
            >
              <div className="device-header">
                <span className="device-type-icon">{getDeviceIcon(device.deviceType)}</span>
                <div className="device-status-indicator">
                  <span className={`status-dot ${device.isConnected ? 'online' : 'offline'}`}></span>
                </div>
              </div>

              <div className="device-info">
                <h3 className="device-name">{device.deviceName}</h3>
                <p className="device-id">{device.deviceId}</p>
              </div>

              <div className="device-details">
                <div className="detail-row">
                  <span className="detail-label">Type:</span>
                  <span className="detail-value">{device.deviceType}</span>
                </div>
                <div className="detail-row">
                  <span className="detail-label">Vendor:</span>
                  <span className="detail-value font-mono">{device.vendorId}</span>
                </div>
                <div className="detail-row">
                  <span className="detail-label">Product:</span>
                  <span className="detail-value font-mono">{device.productId}</span>
                </div>
                {device.serialNumber && (
                  <div className="detail-row">
                    <span className="detail-label">Serial:</span>
                    <span className="detail-value font-mono">{device.serialNumber}</span>
                  </div>
                )}
              </div>

              <div className="device-footer">
                {getStatusBadge(device)}
                <span className={`risk-badge risk-${device.riskLevel}`}>
                  {device.riskLevel.toUpperCase()}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Device Detail Panel */}
      {selectedDevice && (
        <div className="device-detail-panel" onClick={() => setSelectedDevice(null)}>
          <div className="detail-content" onClick={(e) => e.stopPropagation()}>
            <button className="close-btn" onClick={() => setSelectedDevice(null)}>×</button>
            
            <div className="detail-header">
              <span className="detail-type-icon">{getDeviceIcon(selectedDevice.deviceType)}</span>
              <h2>{selectedDevice.deviceName}</h2>
            </div>

            <div className="detail-section">
              <h3>Device Information</h3>
              <div className="detail-grid">
                <div className="detail-item">
                  <span className="item-label">Device ID</span>
                  <span className="item-value">{selectedDevice.deviceId}</span>
                </div>
                <div className="detail-item">
                  <span className="item-label">Type</span>
                  <span className="item-value">{selectedDevice.deviceType}</span>
                </div>
                <div className="detail-item">
                  <span className="item-label">Vendor ID</span>
                  <span className="item-value font-mono">{selectedDevice.vendorId}</span>
                </div>
                <div className="detail-item">
                  <span className="item-label">Product ID</span>
                  <span className="item-value font-mono">{selectedDevice.productId}</span>
                </div>
                {selectedDevice.serialNumber && (
                  <div className="detail-item">
                    <span className="item-label">Serial Number</span>
                    <span className="item-value font-mono">{selectedDevice.serialNumber}</span>
                  </div>
                )}
                <div className="detail-item">
                  <span className="item-label">Last Seen</span>
                  <span className="item-value">{new Date(selectedDevice.lastSeen).toLocaleString()}</span>
                </div>
              </div>
            </div>

            <div className="detail-section">
              <h3>Status</h3>
              <div className="status-grid">
                <div className="status-item">
                  <span className="status-label">Connection</span>
                  <span className={`status-value ${selectedDevice.isConnected ? 'text-success' : 'text-muted'}`}>
                    {selectedDevice.isConnected ? '● Connected' : '○ Disconnected'}
                  </span>
                </div>
                <div className="status-item">
                  <span className="status-label">Authorization</span>
                  <span className={`status-value ${selectedDevice.isAuthorized ? 'text-success' : 'text-danger'}`}>
                    {selectedDevice.isAuthorized ? '● Authorized' : '○ Not Authorized'}
                  </span>
                </div>
                <div className="status-item">
                  <span className="status-label">Risk Level</span>
                  <span className={`risk-badge risk-${selectedDevice.riskLevel}`}>
                    {selectedDevice.riskLevel.toUpperCase()}
                  </span>
                </div>
              </div>
            </div>

            <div className="detail-actions">
              {selectedDevice.isAuthorized ? (
                <button className="btn-danger">Block Device</button>
              ) : (
                <button className="btn-primary">Authorize Device</button>
              )}
              <button className="btn-secondary">Quarantine</button>
              <button className="btn-secondary">View Logs</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default DevicesPage;
