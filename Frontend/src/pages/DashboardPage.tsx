import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { mockStats, mockDevices, mockAlerts, mockRecentEvents } from '../mock/data';
import './DashboardPage.css';

/**
 * Dashboard Page
 * 
 * Main overview with statistics, recent alerts, and device summary.
 */
const DashboardPage: React.FC = () => {
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Simulate loading data
    const loadData = async (): Promise<void> => {
      await new Promise(resolve => setTimeout(resolve, 500));
      setIsLoading(false);
    };

    loadData();
  }, []);

  const formatTimeAgo = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    return `${diffDays}d ago`;
  };

  const getAlertIcon = (type: string): string => {
    switch (type) {
      case 'device_connected': return '🔌';
      case 'policy_violation': return '⚠️';
      case 'suspicious_activity': return '🚨';
      case 'malware_detected': return '🦠';
      case 'unauthorized_access': return '🔒';
      default: return '📢';
    }
  };

  if (isLoading) {
    return (
      <div className="dashboard-loading">
        <div className="loading-spinner-large"></div>
        <p>Loading dashboard...</p>
      </div>
    );
  }

  return (
    <div className="dashboard-page">
      <div className="page-header">
        <h1>Dashboard</h1>
        <p className="page-subtitle">Security overview and system status</p>
      </div>

      {/* Statistics Cards */}
      <div className="stats-grid">
        <div className="stat-card stat-primary">
          <div className="stat-icon">📱</div>
          <div className="stat-content">
            <span className="stat-value">{mockStats.totalDevices}</span>
            <span className="stat-label">Total Devices</span>
          </div>
          <div className="stat-trend">
            <span className="trend-positive">+2 this week</span>
          </div>
        </div>

        <div className="stat-card stat-success">
          <div className="stat-icon">✅</div>
          <div className="stat-content">
            <span className="stat-value">{mockStats.authorizedDevices}</span>
            <span className="stat-label">Authorized</span>
          </div>
          <div className="stat-trend">
            <span className="trend-positive">{Math.round((mockStats.authorizedDevices / mockStats.totalDevices) * 100)}% compliance</span>
          </div>
        </div>

        <div className="stat-card stat-warning">
          <div className="stat-icon">⚠️</div>
          <div className="stat-content">
            <span className="stat-value">{mockStats.blockedDevices}</span>
            <span className="stat-label">Blocked</span>
          </div>
          <div className="stat-trend">
            <span className="trend-neutral">Requires review</span>
          </div>
        </div>

        <div className="stat-card stat-danger">
          <div className="stat-icon">🚨</div>
          <div className="stat-content">
            <span className="stat-value">{mockStats.pendingAlerts}</span>
            <span className="stat-label">Pending Alerts</span>
          </div>
          <div className="stat-trend">
            <span className="trend-negative">{mockStats.criticalAlerts} critical</span>
          </div>
        </div>
      </div>

      {/* Main Content Grid */}
      <div className="dashboard-grid">
        {/* Recent Alerts */}
        <div className="dashboard-card alerts-card">
          <div className="card-header">
            <h2>Recent Alerts</h2>
            <Link to="/alerts" className="view-all">View All →</Link>
          </div>
          <div className="alerts-list">
            {mockAlerts.slice(0, 5).map((alert) => (
              <div key={alert.alertId} className={`alert-item alert-${alert.severity} ${alert.acknowledged ? 'acknowledged' : ''}`}>
                <div className="alert-icon">{getAlertIcon(alert.type)}</div>
                <div className="alert-content">
                  <h4>{alert.title}</h4>
                  <p>{alert.description}</p>
                  <div className="alert-meta">
                    <span className={`risk-badge risk-${alert.severity}`}>
                      {alert.severity.toUpperCase()}
                    </span>
                    <span className="alert-time">{formatTimeAgo(alert.timestamp)}</span>
                  </div>
                </div>
                {!alert.acknowledged && (
                  <div className="alert-status">
                    <span className="status-dot"></span>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* Device Summary */}
        <div className="dashboard-card devices-card">
          <div className="card-header">
            <h2>Device Summary</h2>
            <Link to="/devices" className="view-all">View All →</Link>
          </div>
          <div className="device-summary">
            <div className="device-type-row">
              <span className="device-type-label">
                <span className="type-icon">🖱️</span> Mice
              </span>
              <span className="device-type-count">
                {mockDevices.filter(d => d.deviceType === 'mouse').length} devices
              </span>
            </div>
            <div className="device-type-row">
              <span className="device-type-label">
                <span className="type-icon">⌨️</span> Keyboards
              </span>
              <span className="device-type-count">
                {mockDevices.filter(d => d.deviceType === 'keyboard').length} devices
              </span>
            </div>
            <div className="device-type-row">
              <span className="device-type-label">
                <span className="type-icon">💾</span> Storage
              </span>
              <span className="device-type-count">
                {mockDevices.filter(d => d.deviceType === 'storage').length} devices
              </span>
            </div>
            <div className="device-type-row">
              <span className="device-type-label">
                <span className="type-icon">🌐</span> Network
              </span>
              <span className="device-type-count">
                {mockDevices.filter(d => d.deviceType === 'network').length} devices
              </span>
            </div>
            <div className="device-type-row">
              <span className="device-type-label">
                <span className="type-icon">📦</span> Other
              </span>
              <span className="device-type-count">
                {mockDevices.filter(d => d.deviceType === 'other').length} devices
              </span>
            </div>
          </div>

          {/* Risk Distribution */}
          <div className="risk-distribution">
            <h3>Risk Distribution</h3>
            <div className="risk-bars">
              {(['low', 'medium', 'high', 'critical'] as const).map((risk) => {
                const count = mockDevices.filter(d => d.riskLevel === risk).length;
                const percentage = (count / mockDevices.length) * 100;
                return (
                  <div key={risk} className="risk-bar-row">
                    <span className={`risk-label risk-${risk}`}>{risk.toUpperCase()}</span>
                    <div className="risk-bar">
                      <div 
                        className={`risk-fill risk-${risk}`} 
                        style={{ width: `${percentage}%` }}
                      ></div>
                    </div>
                    <span className="risk-count">{count}</span>
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        {/* Recent Events */}
        <div className="dashboard-card events-card">
          <div className="card-header">
            <h2>Recent Events</h2>
          </div>
          <div className="events-timeline">
            {mockRecentEvents.map((event, index) => (
              <div key={event.id} className="event-item">
                <div className="event-dot"></div>
                {index !== mockRecentEvents.length - 1 && <div className="event-line"></div>}
                <div className="event-content">
                  <p>{event.message}</p>
                  <span className="event-time">{formatTimeAgo(event.timestamp)}</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Quick Actions */}
        <div className="dashboard-card actions-card">
          <div className="card-header">
            <h2>Quick Actions</h2>
          </div>
          <div className="quick-actions">
            <Link to="/devices" className="quick-action-btn">
              <span className="action-icon">🔍</span>
              <span>Scan Devices</span>
            </Link>
            <Link to="/alerts" className="quick-action-btn">
              <span className="action-icon">✓</span>
              <span>Acknowledge All</span>
            </Link>
            <Link to="/policy" className="quick-action-btn">
              <span className="action-icon">📋</span>
              <span>View Policies</span>
            </Link>
            <Link to="/admin" className="quick-action-btn">
              <span className="action-icon">⚙️</span>
              <span>Admin Tools</span>
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
