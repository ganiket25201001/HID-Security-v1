import React, { useState, useEffect } from 'react';
import { mockAlerts } from '../mock/data';
import { AlertEvent } from '../types/ipc-types';
import './AlertsPage.css';

type SeverityFilter = 'all' | 'critical' | 'high' | 'medium' | 'low';
type StatusFilter = 'all' | 'pending' | 'acknowledged' | 'dismissed';

/**
 * Alerts / Risk Events Page
 * 
 * Displays all security alerts with filtering and management capabilities.
 */
const AlertsPage: React.FC = () => {
  const [alerts, setAlerts] = useState<AlertEvent[]>([]);
  const [severityFilter, setSeverityFilter] = useState<SeverityFilter>('all');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [selectedAlert, setSelectedAlert] = useState<AlertEvent | null>(null);

  useEffect(() => {
    const loadAlerts = async (): Promise<void> => {
      await new Promise(resolve => setTimeout(resolve, 500));
      setAlerts(mockAlerts);
      setIsLoading(false);
    };

    loadAlerts();
  }, []);

  const filteredAlerts = alerts.filter(alert => {
    // Severity filter
    if (severityFilter !== 'all' && alert.severity !== severityFilter) return false;
    
    // Status filter
    if (statusFilter === 'pending' && (alert.acknowledged || alert.dismissed)) return false;
    if (statusFilter === 'acknowledged' && (!alert.acknowledged || alert.dismissed)) return false;
    if (statusFilter === 'dismissed' && !alert.dismissed) return false;
    
    // Search filter
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      return (
        alert.title.toLowerCase().includes(query) ||
        alert.description.toLowerCase().includes(query) ||
        alert.alertId.toLowerCase().includes(query) ||
        alert.type.toLowerCase().includes(query)
      );
    }
    
    return true;
  });

  const handleAcknowledge = async (alertId: string): Promise<void> => {
    setAlerts(prev => prev.map(alert => 
      alert.alertId === alertId ? { ...alert, acknowledged: true } : alert
    ));
    if (selectedAlert?.alertId === alertId) {
      setSelectedAlert(prev => prev ? { ...prev, acknowledged: true } : null);
    }
  };

  const handleDismiss = async (alertId: string): Promise<void> => {
    setAlerts(prev => prev.map(alert => 
      alert.alertId === alertId ? { ...alert, dismissed: true, acknowledged: true } : alert
    ));
    if (selectedAlert?.alertId === alertId) {
      setSelectedAlert(prev => prev ? { ...prev, dismissed: true, acknowledged: true } : null);
    }
  };

  const handleAcknowledgeAll = async (): Promise<void> => {
    setAlerts(prev => prev.map(alert => ({ ...alert, acknowledged: true })));
  };

  const stats = {
    total: alerts.length,
    pending: alerts.filter(a => !a.acknowledged && !a.dismissed).length,
    critical: alerts.filter(a => a.severity === 'critical' && !a.dismissed).length,
    high: alerts.filter(a => a.severity === 'high' && !a.dismissed).length,
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

  return (
    <div className="alerts-page">
      <div className="page-header">
        <div>
          <h1>Alerts & Risk Events</h1>
          <p className="page-subtitle">Monitor and respond to security incidents</p>
        </div>
        <div className="header-actions">
          <button className="btn-secondary" onClick={handleAcknowledgeAll}>
            Acknowledge All
          </button>
          <button className="btn-primary">
            <span>📤</span> Export Report
          </button>
        </div>
      </div>

      {/* Stats Bar */}
      <div className="alerts-stats">
        <div className="alert-stat">
          <span className="stat-number">{stats.total}</span>
          <span className="stat-label">Total Alerts</span>
        </div>
        <div className="alert-stat stat-pending">
          <span className="stat-number">{stats.pending}</span>
          <span className="stat-label">Pending</span>
        </div>
        <div className="alert-stat stat-critical">
          <span className="stat-number">{stats.critical}</span>
          <span className="stat-label">Critical</span>
        </div>
        <div className="alert-stat stat-high">
          <span className="stat-number">{stats.high}</span>
          <span className="stat-label">High</span>
        </div>
      </div>

      {/* Filters */}
      <div className="alerts-filters">
        <div className="filter-tabs">
          {(['all', 'pending', 'acknowledged', 'dismissed'] as const).map((status) => (
            <button
              key={status}
              className={`filter-tab ${statusFilter === status ? 'active' : ''}`}
              onClick={() => setStatusFilter(status)}
            >
              {status.charAt(0).toUpperCase() + status.slice(1)}
            </button>
          ))}
        </div>

        <div className="filter-right">
          <select
            value={severityFilter}
            onChange={(e) => setSeverityFilter(e.target.value as SeverityFilter)}
            className="severity-select"
          >
            <option value="all">All Severities</option>
            <option value="critical">Critical</option>
            <option value="high">High</option>
            <option value="medium">Medium</option>
            <option value="low">Low</option>
          </select>

          <div className="search-group">
            <input
              type="text"
              placeholder="Search alerts..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="search-input"
            />
            <span className="search-icon">🔍</span>
          </div>
        </div>
      </div>

      {/* Alerts List */}
      {isLoading ? (
        <div className="loading-state">
          <div className="loading-spinner-large"></div>
          <p>Loading alerts...</p>
        </div>
      ) : filteredAlerts.length === 0 ? (
        <div className="empty-state">
          <span className="empty-icon">🎉</span>
          <h3>No alerts found</h3>
          <p>All clear! No alerts match your current filters.</p>
        </div>
      ) : (
        <div className="alerts-list-container">
          <div className="alerts-table">
            <div className="table-header">
              <div className="col-severity">Severity</div>
              <div className="col-type">Type</div>
              <div className="col-title">Alert</div>
              <div className="col-device">Device</div>
              <div className="col-time">Time</div>
              <div className="col-status">Status</div>
              <div className="col-actions">Actions</div>
            </div>

            {filteredAlerts.map((alert) => (
              <div
                key={alert.alertId}
                className={`table-row ${selectedAlert?.alertId === alert.alertId ? 'selected' : ''} ${alert.dismissed ? 'dismissed' : ''}`}
                onClick={() => setSelectedAlert(alert)}
              >
                <div className="col-severity">
                  <span className={`severity-badge severity-${alert.severity}`}>
                    {alert.severity.toUpperCase()}
                  </span>
                </div>
                <div className="col-type">
                  <span className="type-icon">{getAlertIcon(alert.type)}</span>
                  <span className="type-label">{alert.type.replace(/_/g, ' ')}</span>
                </div>
                <div className="col-title">
                  <span className="alert-title">{alert.title}</span>
                </div>
                <div className="col-device">
                  {alert.deviceId ? (
                    <span className="device-badge">{alert.deviceId}</span>
                  ) : (
                    <span className="text-muted">-</span>
                  )}
                </div>
                <div className="col-time">
                  <span className="time-ago">{formatTimeAgo(alert.timestamp)}</span>
                </div>
                <div className="col-status">
                  {alert.dismissed ? (
                    <span className="status-badge status-dismissed">Dismissed</span>
                  ) : alert.acknowledged ? (
                    <span className="status-badge status-acknowledged">Acknowledged</span>
                  ) : (
                    <span className="status-badge status-pending">Pending</span>
                  )}
                </div>
                <div className="col-actions">
                  {!alert.acknowledged && (
                    <button
                      className="action-btn action-acknowledge"
                      onClick={(e) => {
                        e.stopPropagation();
                        handleAcknowledge(alert.alertId);
                      }}
                      title="Acknowledge"
                    >
                      ✓
                    </button>
                  )}
                  {!alert.dismissed && (
                    <button
                      className="action-btn action-dismiss"
                      onClick={(e) => {
                        e.stopPropagation();
                        handleDismiss(alert.alertId);
                      }}
                      title="Dismiss"
                    >
                      ×
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Alert Detail Panel */}
      {selectedAlert && (
        <div className="alert-detail-panel" onClick={() => setSelectedAlert(null)}>
          <div className="detail-content" onClick={(e) => e.stopPropagation()}>
            <button className="close-btn" onClick={() => setSelectedAlert(null)}>×</button>
            
            <div className="detail-header">
              <div className={`severity-indicator severity-${selectedAlert.severity}`}></div>
              <div>
                <h2>{selectedAlert.title}</h2>
                <p className="alert-id">{selectedAlert.alertId}</p>
              </div>
            </div>

            <div className="detail-grid">
              <div className="detail-item">
                <span className="item-label">Type</span>
                <span className="item-value">{selectedAlert.type.replace(/_/g, ' ')}</span>
              </div>
              <div className="detail-item">
                <span className="item-label">Severity</span>
                <span className={`severity-badge severity-${selectedAlert.severity}`}>
                  {selectedAlert.severity.toUpperCase()}
                </span>
              </div>
              <div className="detail-item">
                <span className="item-label">Time</span>
                <span className="item-value">{new Date(selectedAlert.timestamp).toLocaleString()}</span>
              </div>
              <div className="detail-item">
                <span className="item-label">Status</span>
                <span className="item-value">
                  {selectedAlert.dismissed ? 'Dismissed' : selectedAlert.acknowledged ? 'Acknowledged' : 'Pending'}
                </span>
              </div>
              {selectedAlert.deviceId && (
                <div className="detail-item">
                  <span className="item-label">Device ID</span>
                  <span className="item-value font-mono">{selectedAlert.deviceId}</span>
                </div>
              )}
              {selectedAlert.userId && (
                <div className="detail-item">
                  <span className="item-label">User ID</span>
                  <span className="item-value font-mono">{selectedAlert.userId}</span>
                </div>
              )}
            </div>

            <div className="detail-section">
              <h3>Description</h3>
              <p className="description-text">{selectedAlert.description}</p>
            </div>

            {selectedAlert.metadata && Object.keys(selectedAlert.metadata).length > 0 && (
              <div className="detail-section">
                <h3>Metadata</h3>
                <div className="metadata-grid">
                  {Object.entries(selectedAlert.metadata).map(([key, value]) => (
                    <div key={key} className="metadata-item">
                      <span className="meta-key">{key}:</span>
                      <span className="meta-value">{String(value)}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="detail-actions">
              {!selectedAlert.acknowledged && (
                <button className="btn-primary" onClick={() => handleAcknowledge(selectedAlert.alertId)}>
                  Acknowledge Alert
                </button>
              )}
              {!selectedAlert.dismissed && (
                <button className="btn-secondary" onClick={() => handleDismiss(selectedAlert.alertId)}>
                  Dismiss Alert
                </button>
              )}
              <button className="btn-secondary">View Related Logs</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AlertsPage;
