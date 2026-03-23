import React, { useState } from 'react';
import { AuthStatusPayload } from '../types/ipc-types';
import './LoginPage.css';

interface LoginPageProps {
  onLogin: (status: AuthStatusPayload) => void;
}

/**
 * Login Page with MFA Support
 * 
 * Mockup for admin authentication with multi-factor authentication.
 */
const LoginPage: React.FC<LoginPageProps> = ({ onLogin }) => {
  const [step, setStep] = useState<'credentials' | 'mfa'>('credentials');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [mfaCode, setMfaCode] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleCredentialsSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      // Validate credentials (default: admin/admin)
      if (username === 'admin' && password === 'admin') {
        // Try to call IPC if available, otherwise proceed with mock login
        if (window.electronAPI) {
          try {
            const result = await window.electronAPI.invoke<{
              success: boolean;
              requiresMfa: boolean;
              userId: string;
            }>(window.electronAPI.channels.AUTH_LOGIN, {
              username,
              password,
            });

            if (result.success && result.requiresMfa) {
              setStep('mfa');
              setIsLoading(false);
              return;
            }
          } catch (ipcError) {
            // IPC failed, proceed with mock login
            console.warn('IPC not available, using mock login');
          }
        }
        
        // Mock login success - go to MFA
        setStep('mfa');
      } else {
        setError('Invalid credentials. Use admin/admin for demo.');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed');
    } finally {
      setIsLoading(false);
    }
  };

  const handleMfaSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      // Validate MFA code (default: 123456)
      if (mfaCode === '123456') {
        // Try to call IPC if available, otherwise proceed with mock login
        if (window.electronAPI) {
          try {
            await window.electronAPI.invoke<{
              success: boolean;
              isAuthenticated: boolean;
              role: string;
            }>(window.electronAPI.channels.AUTH_MFA_VERIFY, {
              userId: 'mock-user-id',
              mfaCode,
            });
          } catch (ipcError) {
            // IPC failed, proceed with mock login
            console.warn('IPC not available, using mock login');
          }
        }
        
        // Mock login success
        onLogin({
          isAuthenticated: true,
          userId: 'mock-user-id',
          username,
          role: 'admin',
        });
      } else {
        setError('Invalid MFA code. Use 123456 for demo.');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'MFA verification failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-background">
        <div className="grid-overlay"></div>
      </div>

      <div className="login-container">
        <div className="login-header">
          <div className="login-logo">
            <svg viewBox="0 0 60 60" fill="none" className="login-logo-icon">
              <path d="M30 6L6 18v12c0 16.5 10.74 25.11 24 30 13.26-4.89 24-13.5 24-30V18L30 6z" fill="url(#login-logo-gradient)" />
              <path d="M30 12l18 9v9c0 12-8.05 18.83-18 22.5C20.05 48.83 12 42 12 21v-9L30 12z" fill="rgba(255,255,255,0.2)" />
              <defs>
                <linearGradient id="login-logo-gradient" x1="6" y1="6" x2="54" y2="54">
                  <stop stopColor="#4cc9f0" />
                  <stop offset="1" stopColor="#7209b7" />
                </linearGradient>
              </defs>
            </svg>
          </div>
          <h1>HID Security Console</h1>
          <p>Enterprise Device Security Management</p>
        </div>

        <div className="login-card">
          {step === 'credentials' ? (
            <form onSubmit={handleCredentialsSubmit} className="login-form">
              <h2>Sign In</h2>
              <p className="login-subtitle">Enter your credentials to access the console</p>

              {error && <div className="login-error">{error}</div>}

              <div className="form-group">
                <label htmlFor="username">Username</label>
                <input
                  id="username"
                  type="text"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  placeholder="Enter your username"
                  autoComplete="username"
                  required
                  disabled={isLoading}
                />
              </div>

              <div className="form-group">
                <label htmlFor="password">Password</label>
                <input
                  id="password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Enter your password"
                  autoComplete="current-password"
                  required
                  disabled={isLoading}
                />
              </div>

              <div className="form-options">
                <label className="checkbox-label">
                  <input type="checkbox" disabled={isLoading} />
                  <span>Remember me</span>
                </label>
                <a href="#" className="forgot-link">Forgot password?</a>
              </div>

              <button type="submit" className="btn-login" disabled={isLoading}>
                {isLoading ? (
                  <span className="loading-spinner-small"></span>
                ) : (
                  'Sign In'
                )}
              </button>

              <p className="login-hint">
                Demo credentials: <strong>admin / admin</strong>, MFA: <strong>123456</strong>
              </p>
            </form>
          ) : (
            <form onSubmit={handleMfaSubmit} className="login-form mfa-form">
              <h2>Two-Factor Authentication</h2>
              <p className="login-subtitle">Enter the code from your authenticator app</p>

              {error && <div className="login-error">{error}</div>}

              <div className="mfa-icon">🔐</div>

              <div className="form-group">
                <label htmlFor="mfa">Authentication Code</label>
                <input
                  id="mfa"
                  type="text"
                  value={mfaCode}
                  onChange={(e) => setMfaCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                  placeholder="000000"
                  maxLength={6}
                  autoComplete="one-time-code"
                  required
                  disabled={isLoading}
                  className="mfa-input"
                />
              </div>

              <div className="mfa-options">
                <button type="button" className="btn-link">
                  Use a different method
                </button>
              </div>

              <button type="submit" className="btn-login" disabled={isLoading}>
                {isLoading ? (
                  <span className="loading-spinner-small"></span>
                ) : (
                  'Verify'
                )}
              </button>

              <button
                type="button"
                className="btn-back"
                onClick={() => setStep('credentials')}
                disabled={isLoading}
              >
                ← Back to sign in
              </button>
            </form>
          )}
        </div>

        <div className="login-footer">
          <p>© 2024 HID Dev. All rights reserved.</p>
          <div className="login-links">
            <a href="#">Privacy Policy</a>
            <a href="#">Terms of Service</a>
            <a href="#">Support</a>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
