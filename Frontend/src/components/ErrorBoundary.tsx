import { Component, ErrorInfo, ReactNode } from 'react';
import './ErrorBoundary.css';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  componentId: string;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

/**
 * Error Boundary Component
 * 
 * Catches JavaScript errors anywhere in the component tree,
 * logs those errors, and displays a fallback UI.
 * 
 * SECURITY: Reports errors to main process for logging and monitoring.
 */
class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    console.error('[ErrorBoundary] Caught error:', error, errorInfo);
    
    // Report to main process for logging
    if (window.electronAPI) {
      window.electronAPI.reportError(error, this.props.componentId);
    }

    // Call custom error handler if provided
    if (this.props.onError) {
      this.props.onError(error, errorInfo);
    }
  }

  handleRetry = (): void => {
    this.setState({ hasError: false, error: null });
  };

  handleReload = (): void => {
    window.location.reload();
  };

  render(): ReactNode {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <div className="error-boundary">
          <div className="error-boundary-content">
            <div className="error-icon">⚠️</div>
            <h2>Component Error</h2>
            <p className="error-component">{this.props.componentId}</p>
            {this.state.error && (
              <p className="error-message">{this.state.error.message}</p>
            )}
            <div className="error-actions">
              <button onClick={this.handleRetry} className="btn-retry">
                Try Again
              </button>
              <button onClick={this.handleReload} className="btn-reload">
                Reload App
              </button>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
