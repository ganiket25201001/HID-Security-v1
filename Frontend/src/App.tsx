import React, { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import ErrorBoundary from './components/ErrorBoundary';
import AppLayout from './components/AppLayout';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import DevicesPage from './pages/DevicesPage';
import AlertsPage from './pages/AlertsPage';
import PolicyPage from './pages/PolicyPage';
import AdminPage from './pages/AdminPage';
import SettingsPage from './pages/SettingsPage';
import { AuthStatusPayload } from './types/ipc-types';
import './styles/global.css';

/**
 * Main App Component
 * 
 * Sets up routing and authentication state management.
 * All routes are wrapped with ErrorBoundary for graceful error handling.
 */
function App(): React.ReactElement {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [authStatus, setAuthStatus] = useState<AuthStatusPayload | null>(null);

  useEffect(() => {
    // Check authentication status on mount
    const checkAuthStatus = async (): Promise<void> => {
      try {
        if (window.electronAPI) {
          const status = await window.electronAPI.invoke<AuthStatusPayload>(
            window.electronAPI.channels.AUTH_STATUS
          );
          setAuthStatus(status);
          setIsAuthenticated(status.isAuthenticated ?? false);
        }
      } catch (error) {
        console.error('[App] Failed to check auth status:', error);
      } finally {
        setIsLoading(false);
      }
    };

    checkAuthStatus();
  }, []);

  const handleLogin = (status: AuthStatusPayload): void => {
    setIsAuthenticated(true);
    setAuthStatus(status);
  };

  const handleLogout = async (): Promise<void> => {
    try {
      if (window.electronAPI) {
        await window.electronAPI.invoke(window.electronAPI.channels.AUTH_LOGOUT);
      }
    } catch (error) {
      console.error('[App] Logout error:', error);
    } finally {
      setIsAuthenticated(false);
      setAuthStatus(null);
    }
  };

  if (isLoading) {
    return (
      <div className="app-loading">
        <div className="loading-spinner"></div>
        <p>Loading HID Security Console...</p>
      </div>
    );
  }

  return (
    <Router>
      <Routes>
        {/* Public routes */}
        <Route
          path="/login"
          element={
            <ErrorBoundary componentId="LoginPage">
              {!isAuthenticated ? (
                <LoginPage onLogin={handleLogin} />
              ) : (
                <Navigate to="/dashboard" replace />
              )}
            </ErrorBoundary>
          }
        />

        {/* Protected routes */}
        <Route
          path="/"
          element={
            isAuthenticated ? (
              <AppLayout user={authStatus} onLogout={handleLogout} />
            ) : (
              <Navigate to="/login" replace />
            )
          }
        >
          <Route
            index
            element={
              <ErrorBoundary componentId="DashboardPage">
                <DashboardPage />
              </ErrorBoundary>
            }
          />
          <Route
            path="dashboard"
            element={
              <ErrorBoundary componentId="DashboardPage">
                <DashboardPage />
              </ErrorBoundary>
            }
          />
          <Route
            path="devices"
            element={
              <ErrorBoundary componentId="DevicesPage">
                <DevicesPage />
              </ErrorBoundary>
            }
          />
          <Route
            path="alerts"
            element={
              <ErrorBoundary componentId="AlertsPage">
                <AlertsPage />
              </ErrorBoundary>
            }
          />
          <Route
            path="policy"
            element={
              <ErrorBoundary componentId="PolicyPage">
                <PolicyPage />
              </ErrorBoundary>
            }
          />
          <Route
            path="admin"
            element={
              <ErrorBoundary componentId="AdminPage">
                <AdminPage />
              </ErrorBoundary>
            }
          />
          <Route
            path="settings"
            element={
              <ErrorBoundary componentId="SettingsPage">
                <SettingsPage />
              </ErrorBoundary>
            }
          />
        </Route>

        {/* Catch-all redirect */}
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </Router>
  );
}

export default App;
