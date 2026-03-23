import { Component, ReactNode } from 'react';
import './BackendDisconnectBoundary.css';

interface Props {
  children: ReactNode;
}

interface State {
  isDisconnected: boolean;
  lastError: string | null;
  attemptCount: number;
}

/**
 * Backend Disconnect Boundary
 * 
 * Handles backend disconnection scenarios gracefully.
 * Shows a user-friendly message and auto-retry functionality.
 */
class BackendDisconnectBoundary extends Component<Props, State> {
  private retryInterval: NodeJS.Timeout | null = null;
  private readonly RETRY_INTERVAL_MS = 5000;

  constructor(props: Props) {
    super(props);
    this.state = {
      isDisconnected: false,
      lastError: null,
      attemptCount: 0,
    };
  }

  componentDidMount(): void {
    this.startHealthCheck();
  }

  componentWillUnmount(): void {
    this.stopHealthCheck();
  }

  startHealthCheck = (): void => {
    this.retryInterval = setInterval(async () => {
      if (this.state.isDisconnected) {
        try {
          if (window.electronAPI) {
            const status = await window.electronAPI.invoke<{ backendConnected: boolean }>(
              window.electronAPI.channels.SERVICE_STATUS
            );
            if (status.backendConnected) {
              this.setState({ isDisconnected: false, lastError: null, attemptCount: 0 });
            } else {
              this.setState(prev => ({ attemptCount: prev.attemptCount + 1 }));
            }
          }
        } catch {
          this.setState(prev => ({ attemptCount: prev.attemptCount + 1 }));
        }
      }
    }, this.RETRY_INTERVAL_MS);
  };

  stopHealthCheck = (): void => {
    if (this.retryInterval) {
      clearInterval(this.retryInterval);
      this.retryInterval = null;
    }
  };

  handleManualRetry = (): void => {
    this.setState({ attemptCount: 0 });
    this.startHealthCheck();
  };

  render(): ReactNode {
    if (this.state.isDisconnected) {
      return (
        <div className="backend-disconnect">
          <div className="disconnect-content">
            <div className="disconnect-icon">🔌</div>
            <h2>Backend Disconnected</h2>
            <p className="disconnect-message">
              The security service is currently unavailable. 
              Some features may be limited until connection is restored.
            </p>
            {this.state.lastError && (
              <p className="disconnect-error">{this.state.lastError}</p>
            )}
            <div className="disconnect-status">
              <span>Retry attempt: {this.state.attemptCount}</span>
              <span>Next retry in 5s</span>
            </div>
            <button onClick={this.handleManualRetry} className="btn-retry-connect">
              Retry Now
            </button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

export default BackendDisconnectBoundary;
