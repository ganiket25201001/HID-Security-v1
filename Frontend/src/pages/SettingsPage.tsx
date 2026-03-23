import React, { useState, useEffect } from 'react';
import { mockSettings } from '../mock/data';
import { AppSettings } from '../types/ipc-types';
import './SettingsPage.css';

type SettingsTab = 'general' | 'notifications' | 'security' | 'advanced';

/**
 * Settings Page
 * 
 * Application configuration and preferences.
 */
const SettingsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<SettingsTab>('general');
  const [settings, setSettings] = useState<AppSettings & {
    alertSoundEnabled: boolean;
    emailNotifications: boolean;
    emailRecipients: string[];
    logRetentionDays: number;
    maxLogSize: string;
  }>(mockSettings);
  const [hasChanges, setHasChanges] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [saveMessage, setSaveMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  useEffect(() => {
    const loadSettings = async (): Promise<void> => {
      try {
        if (window.electronAPI) {
          const result = await window.electronAPI.invoke<{ settings: AppSettings }>(
            window.electronAPI.channels.SETTINGS_GET
          );
          setSettings({ ...mockSettings, ...result.settings });
        }
      } catch (error) {
        console.error('[Settings] Failed to load settings:', error);
      }
    };

    loadSettings();
  }, []);

  const handleSettingChange = <K extends keyof typeof settings>(
    key: K,
    value: typeof settings[K]
  ): void => {
    setSettings(prev => ({ ...prev, [key]: value }));
    setHasChanges(true);
    setSaveMessage(null);
  };

  const handleSave = async (): Promise<void> => {
    setIsSaving(true);
    setSaveMessage(null);

    try {
      if (window.electronAPI) {
        await window.electronAPI.invoke(
          window.electronAPI.channels.SETTINGS_UPDATE,
          { settings }
        );
        setSaveMessage({ type: 'success', text: 'Settings saved successfully' });
        setHasChanges(false);
      }
    } catch (error) {
      setSaveMessage({ type: 'error', text: 'Failed to save settings' });
    } finally {
      setIsSaving(false);
      setTimeout(() => setSaveMessage(null), 3000);
    }
  };

  const tabs: { id: SettingsTab; label: string; icon: string }[] = [
    { id: 'general', label: 'General', icon: '⚙️' },
    { id: 'notifications', label: 'Notifications', icon: '🔔' },
    { id: 'security', label: 'Security', icon: '🔒' },
    { id: 'advanced', label: 'Advanced', icon: '🔧' },
  ];

  return (
    <div className="settings-page">
      <div className="page-header">
        <div>
          <h1>Settings</h1>
          <p className="page-subtitle">Configure application preferences</p>
        </div>
        {hasChanges && (
          <div className="save-bar">
            <span className="unsaved-changes">You have unsaved changes</span>
            <button className="btn-save" onClick={handleSave} disabled={isSaving}>
              {isSaving ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        )}
      </div>

      {saveMessage && (
        <div className={`save-message ${saveMessage.type}`}>
          {saveMessage.type === 'success' ? '✓' : '✕'} {saveMessage.text}
        </div>
      )}

      <div className="settings-layout">
        {/* Settings Tabs */}
        <aside className="settings-tabs">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              className={`tab-btn ${activeTab === tab.id ? 'active' : ''}`}
              onClick={() => setActiveTab(tab.id)}
            >
              <span className="tab-icon">{tab.icon}</span>
              <span className="tab-label">{tab.label}</span>
            </button>
          ))}

          <div className="settings-info">
            <h4>Application Info</h4>
            <div className="info-item">
              <span className="info-label">Version</span>
              <span className="info-value">1.0.0</span>
            </div>
            <div className="info-item">
              <span className="info-label">Build</span>
              <span className="info-value">2024.03.24</span>
            </div>
            <div className="info-item">
              <span className="info-label">Electron</span>
              <span className="info-value">31.4.0</span>
            </div>
          </div>
        </aside>

        {/* Settings Content */}
        <main className="settings-content">
          {activeTab === 'general' && (
            <div className="settings-section">
              <h2>General Settings</h2>
              <p className="section-description">Basic application preferences and appearance</p>

              <div className="setting-group">
                <h3>Appearance</h3>
                
                <div className="setting-item">
                  <div className="setting-info">
                    <label htmlFor="theme">Theme</label>
                    <p className="setting-description">Choose your preferred color theme</p>
                  </div>
                  <select
                    id="theme"
                    value={settings.theme}
                    onChange={(e) => handleSettingChange('theme', e.target.value as 'dark' | 'light')}
                  >
                    <option value="dark">Dark</option>
                    <option value="light">Light</option>
                  </select>
                </div>

                <div className="setting-item">
                  <div className="setting-info">
                    <label htmlFor="language">Language</label>
                    <p className="setting-description">Select display language</p>
                  </div>
                  <select
                    id="language"
                    value={settings.language}
                    onChange={(e) => handleSettingChange('language', e.target.value)}
                  >
                    <option value="en">English</option>
                    <option value="es">Español</option>
                    <option value="fr">Français</option>
                    <option value="de">Deutsch</option>
                  </select>
                </div>
              </div>

              <div className="setting-group">
                <h3>Behavior</h3>
                
                <div className="setting-item">
                  <div className="setting-info">
                    <label htmlFor="refresh">Auto-refresh Interval</label>
                    <p className="setting-description">How often to refresh data from the backend</p>
                  </div>
                  <select
                    id="refresh"
                    value={settings.autoRefreshInterval.toString()}
                    onChange={(e) => handleSettingChange('autoRefreshInterval', parseInt(e.target.value))}
                  >
                    <option value="15">15 seconds</option>
                    <option value="30">30 seconds</option>
                    <option value="60">1 minute</option>
                    <option value="300">5 minutes</option>
                  </select>
                </div>

                <div className="setting-item">
                  <div className="setting-info">
                    <label>System Tray</label>
                    <p className="setting-description">Show icon in system tray</p>
                  </div>
                  <label className="toggle-switch">
                    <input
                      type="checkbox"
                      checked={settings.showSystemTray}
                      onChange={(e) => handleSettingChange('showSystemTray', e.target.checked)}
                    />
                    <span className="toggle-slider"></span>
                  </label>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'notifications' && (
            <div className="settings-section">
              <h2>Notification Settings</h2>
              <p className="section-description">Configure how you receive alerts and notifications</p>

              <div className="setting-group">
                <h3>Alert Preferences</h3>
                
                <div className="setting-item">
                  <div className="setting-info">
                    <label>Enable Notifications</label>
                    <p className="setting-description">Show desktop notifications for alerts</p>
                  </div>
                  <label className="toggle-switch">
                    <input
                      type="checkbox"
                      checked={settings.notificationsEnabled}
                      onChange={(e) => handleSettingChange('notificationsEnabled', e.target.checked)}
                    />
                    <span className="toggle-slider"></span>
                  </label>
                </div>

                <div className="setting-item">
                  <div className="setting-info">
                    <label>Alert Sound</label>
                    <p className="setting-description">Play sound for new alerts</p>
                  </div>
                  <label className="toggle-switch">
                    <input
                      type="checkbox"
                      checked={settings.alertSoundEnabled}
                      onChange={(e) => handleSettingChange('alertSoundEnabled', e.target.checked)}
                    />
                    <span className="toggle-slider"></span>
                  </label>
                </div>
              </div>

              <div className="setting-group">
                <h3>Email Notifications</h3>
                
                <div className="setting-item">
                  <div className="setting-info">
                    <label>Email Alerts</label>
                    <p className="setting-description">Send critical alerts via email</p>
                  </div>
                  <label className="toggle-switch">
                    <input
                      type="checkbox"
                      checked={settings.emailNotifications}
                      onChange={(e) => handleSettingChange('emailNotifications', e.target.checked)}
                    />
                    <span className="toggle-slider"></span>
                  </label>
                </div>

                {settings.emailNotifications && (
                  <div className="setting-item setting-item-input">
                    <div className="setting-info">
                      <label>Email Recipients</label>
                      <p className="setting-description">Comma-separated list of email addresses</p>
                    </div>
                    <input
                      type="email"
                      placeholder="admin@example.com, security@example.com"
                      className="input-full"
                    />
                  </div>
                )}
              </div>
            </div>
          )}

          {activeTab === 'security' && (
            <div className="settings-section">
              <h2>Security Settings</h2>
              <p className="section-description">Security and authentication preferences</p>

              <div className="setting-group">
                <h3>Authentication</h3>
                
                <div className="setting-item">
                  <div className="setting-info">
                    <label>Session Timeout</label>
                    <p className="setting-description">Automatically log out after inactivity</p>
                  </div>
                  <select className="select-width">
                    <option>15 minutes</option>
                    <option>30 minutes</option>
                    <option>1 hour</option>
                    <option>4 hours</option>
                    <option>Never</option>
                  </select>
                </div>

                <div className="setting-item">
                  <div className="setting-info">
                    <label>Two-Factor Authentication</label>
                    <p className="setting-description">Require 2FA for login</p>
                  </div>
                  <label className="toggle-switch">
                    <input type="checkbox" defaultChecked />
                    <span className="toggle-slider"></span>
                  </label>
                </div>
              </div>

              <div className="setting-group">
                <h3>Access Control</h3>
                
                <div className="setting-item">
                  <div className="setting-info">
                    <label>Audit Logging</label>
                    <p className="setting-description">Log all administrative actions</p>
                  </div>
                  <label className="toggle-switch">
                    <input type="checkbox" defaultChecked />
                    <span className="toggle-slider"></span>
                  </label>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'advanced' && (
            <div className="settings-section">
              <h2>Advanced Settings</h2>
              <p className="section-description">Advanced configuration for power users</p>

              <div className="setting-group">
                <h3>Data Management</h3>
                
                <div className="setting-item">
                  <div className="setting-info">
                    <label htmlFor="retention">Log Retention</label>
                    <p className="setting-description">How long to keep logs before automatic deletion</p>
                  </div>
                  <select
                    id="retention"
                    value={settings.logRetentionDays.toString()}
                    onChange={(e) => handleSettingChange('logRetentionDays', parseInt(e.target.value))}
                  >
                    <option value="30">30 days</option>
                    <option value="60">60 days</option>
                    <option value="90">90 days</option>
                    <option value="180">180 days</option>
                    <option value="365">1 year</option>
                  </select>
                </div>

                <div className="setting-item">
                  <div className="setting-info">
                    <label htmlFor="logsize">Max Log Size</label>
                    <p className="setting-description">Maximum size for individual log files</p>
                  </div>
                  <select
                    id="logsize"
                    value={settings.maxLogSize}
                    onChange={(e) => handleSettingChange('maxLogSize', e.target.value)}
                  >
                    <option value="50MB">50 MB</option>
                    <option value="100MB">100 MB</option>
                    <option value="500MB">500 MB</option>
                    <option value="1GB">1 GB</option>
                  </select>
                </div>
              </div>

              <div className="setting-group danger-zone">
                <h3>Danger Zone</h3>
                <p className="danger-description">
                  These actions cannot be undone. Proceed with caution.
                </p>
                
                <div className="danger-actions">
                  <button className="btn-danger-action">
                    Clear All Local Data
                  </button>
                  <button className="btn-danger-action">
                    Reset to Default Settings
                  </button>
                  <button className="btn-danger-action critical">
                    Factory Reset
                  </button>
                </div>
              </div>
            </div>
          )}
        </main>
      </div>
    </div>
  );
};

export default SettingsPage;
