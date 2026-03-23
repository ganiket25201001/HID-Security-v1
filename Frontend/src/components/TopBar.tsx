import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import './TopBar.css';
import { ServiceStatusPayload, ServiceHealthPayload, AuthStatusPayload } from '../types/ipc-types';

interface TopBarProps {
  user: AuthStatusPayload | null;
  onLogout: () => void;
}

/**
 * Top Bar Component
 * 
 * Shows service status, notifications, and user menu.
 */
const TopBar: React.FC<TopBarProps> = ({ user, onLogout }) => {
  const [serviceStatus, setServiceStatus] = useState<ServiceStatusPayload | null>(null);
  const [healthStatus, setHealthStatus] = useState<ServiceHealthPayload | null>(null);
  const [showUserMenu, setShowUserMenu] = useState(false);
  const [currentTime, setCurrentTime] = useState(new Date());

  useEffect(() => {
    // Fetch service status
    const fetchStatus = async (): Promise<void> => {
      try {
        if (window.electronAPI) {
          const [status, health] = await Promise.all([
            window.electronAPI.invoke<ServiceStatusPayload>(
              window.electronAPI.channels.SERVICE_STATUS
            ),
            window.electronAPI.invoke<ServiceHealthPayload>(
              window.electronAPI.channels.SERVICE_HEALTH
            ),
          ]);
          setServiceStatus(status);
          setHealthStatus(health);
        }
      } catch (error) {
        console.error('[TopBar] Failed to fetch status:', error);
      }
    };

    fetchStatus();

    // Update time every second
    const timeInterval = setInterval(() => {
      setCurrentTime(new Date());
    }, 1000);

    // Refresh status every 30 seconds
    const statusInterval = setInterval(fetchStatus, 30000);

    return () => {
      clearInterval(timeInterval);
      clearInterval(statusInterval);
    };
  }, []);

  const getStatusColor = (): string => {
    if (!serviceStatus?.backendConnected) return 'status-warning';
    if (healthStatus?.status === 'unhealthy') return 'status-error';
    if (healthStatus?.status === 'degraded') return 'status-warning';
    return 'status-ok';
  };

  const getStatusText = (): string => {
    if (!serviceStatus?.backendConnected) return 'Backend Disconnected';
    if (healthStatus?.status === 'unhealthy') return 'System Unhealthy';
    if (healthStatus?.status === 'degraded') return 'System Degraded';
    return 'All Systems Operational';
  };

  const formatTime = (date: Date): string => {
    return date.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
    });
  };

  const formatDate = (date: Date): string => {
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
    });
  };

  return (
    <header className="topbar">
      <div className="topbar-left">
        <h1 className="page-title">Security Console</h1>
      </div>

      <div className="topbar-center">
        <div className={`service-status ${getStatusColor()}`}>
          <span className="status-indicator"></span>
          <span className="status-text">{getStatusText()}</span>
        </div>

        {healthStatus && serviceStatus?.backendConnected && (
          <div className="health-indicators">
            {Object.entries(healthStatus.components).map(([component, status]) => (
              <div
                key={component}
                className={`health-item health-${status}`}
                title={`${component}: ${status}`}
              >
                <span className="health-dot"></span>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="topbar-right">
        <div className="datetime">
          <span className="time">{formatTime(currentTime)}</span>
          <span className="date">{formatDate(currentTime)}</span>
        </div>

        <button className="notification-btn">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
            <path d="M13.73 21a2 2 0 0 1-3.46 0" />
          </svg>
          <span className="notification-badge">3</span>
        </button>

        <div className="user-menu-container">
          <button
            className="user-menu-btn"
            onClick={() => setShowUserMenu(!showUserMenu)}
          >
            <div className="user-avatar">
              {user?.username?.charAt(0).toUpperCase() ?? 'A'}
            </div>
            <span className="user-name">{user?.username ?? 'Admin'}</span>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="6 9 12 15 18 9" />
            </svg>
          </button>

          {showUserMenu && (
            <div className="user-menu-dropdown">
              <div className="user-menu-header">
                <div className="user-avatar large">
                  {user?.username?.charAt(0).toUpperCase() ?? 'A'}
                </div>
                <div className="user-menu-info">
                  <span className="user-menu-name">{user?.username ?? 'Admin'}</span>
                  <span className="user-menu-role">{user?.role ?? 'Viewer'}</span>
                </div>
              </div>

              <div className="user-menu-items">
                <Link to="/settings" className="user-menu-item" onClick={() => setShowUserMenu(false)}>
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <circle cx="12" cy="12" r="3" />
                    <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z" />
                  </svg>
                  Settings
                </Link>
                <button className="user-menu-item logout" onClick={onLogout}>
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                    <polyline points="16 17 21 12 16 7" />
                    <line x1="21" y1="12" x2="9" y2="12" />
                  </svg>
                  Logout
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};

export default TopBar;
