import React, { useState } from 'react';
import { mockAdminActions } from '../mock/data';
import './AdminPage.css';

const actions = mockAdminActions as Array<{
  id: string;
  type: string;
  name: string;
  description: string;
  requiresConfirmation: boolean;
  dangerLevel: 'low' | 'medium' | 'high';
}>;

/**
 * Admin Actions Page
 * 
 * Administrative tools and system management actions.
 */
const AdminPage: React.FC = () => {
  const [isExecuting, setIsExecuting] = useState<string | null>(null);
  const [showConfirm, setShowConfirm] = useState<string | null>(null);
  const [executionLog, setExecutionLog] = useState<Array<{
    action: string;
    status: 'success' | 'error';
    message: string;
    timestamp: string;
  }>>([]);

  const handleExecute = async (action: typeof actions[number]): Promise<void> => {
    if (action.requiresConfirmation) {
      setShowConfirm(action.id);
      return;
    }

    await executeAction(action);
  };

  const executeAction = async (action: typeof actions[number]): Promise<void> => {
    setIsExecuting(action.id);
    setShowConfirm(null);

    // Simulate action execution
    await new Promise(resolve => setTimeout(resolve, 2000));

    const success = Math.random() > 0.1; // 90% success rate for demo

    setExecutionLog(prev => {
      const newEntry = {
        action: action.name,
        status: success ? ('success' as const) : ('error' as const),
        message: success ? 'Action completed successfully' : 'Action failed - see logs for details',
        timestamp: new Date().toISOString(),
      };
      return [newEntry, ...prev].slice(0, 10);
    });

    setIsExecuting(null);
  };

  const getDangerColor = (level: string): string => {
    switch (level) {
      case 'high': return 'danger-high';
      case 'medium': return 'danger-medium';
      default: return 'danger-low';
    }
  };

  return (
    <div className="admin-page">
      <div className="page-header">
        <div>
          <h1>Admin Actions</h1>
          <p className="page-subtitle">System administration and maintenance tools</p>
        </div>
        <div className="admin-badge">
          <span className="badge-icon">🛡️</span>
          <span>Administrator Mode</span>
        </div>
      </div>

      <div className="admin-layout">
        {/* Actions Panel */}
        <div className="actions-panel">
          <h2>Available Actions</h2>
          <p className="panel-description">
            Select an action to perform system administration tasks. 
            Actions marked with confirmation require additional verification.
          </p>

          <div className="actions-grid">
            {actions.map((action) => (
              <div
                key={action.id}
                className={`action-card ${getDangerColor(action.dangerLevel)} ${isExecuting === action.id ? 'executing' : ''}`}
              >
                <div className="action-header">
                  <div className="action-icon">
                    {action.type === 'restart_service' && '🔄'}
                    {action.type === 'reload_policy' && '📋'}
                    {action.type === 'flush_logs' && '🗑️'}
                    {action.type === 'export_data' && '📤'}
                    {action.type === 'system_scan' && '🔍'}
                  </div>
                  <div className="action-info">
                    <h3>{action.name}</h3>
                    <p>{action.description}</p>
                  </div>
                </div>

                <div className="action-footer">
                  <div className="action-meta">
                    {action.requiresConfirmation && (
                      <span className="confirm-badge">⚠️ Confirmation Required</span>
                    )}
                    <span className={`danger-badge danger-${action.dangerLevel}`}>
                      {action.dangerLevel.toUpperCase()} RISK
                    </span>
                  </div>
                  <button
                    className={`btn-execute ${action.dangerLevel}`}
                    onClick={() => handleExecute(action)}
                    disabled={isExecuting !== null}
                  >
                    {isExecuting === action.id ? (
                      <>
                        <span className="executing-spinner"></span>
                        Executing...
                      </>
                    ) : (
                      'Execute'
                    )}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Execution Log Panel */}
        <div className="log-panel">
          <div className="log-header">
            <h2>Execution Log</h2>
            <button 
              className="btn-clear"
              onClick={() => setExecutionLog([])}
              disabled={executionLog.length === 0}
            >
              Clear Log
            </button>
          </div>

          {executionLog.length === 0 ? (
            <div className="empty-log">
              <span className="empty-icon">📝</span>
              <p>No actions executed yet</p>
            </div>
          ) : (
            <div className="log-list">
              {executionLog.map((log, index) => (
                <div key={index} className={`log-item log-${log.status}`}>
                  <div className="log-status">
                    <span className={`status-dot ${log.status}`}></span>
                  </div>
                  <div className="log-content">
                    <span className="log-action">{log.action}</span>
                    <span className="log-message">{log.message}</span>
                  </div>
                  <span className="log-time">
                    {new Date(log.timestamp).toLocaleTimeString()}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Confirmation Modal */}
      {showConfirm && (
        <div className="confirm-modal-overlay" onClick={() => setShowConfirm(null)}>
          <div className="confirm-modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-icon">⚠️</div>
            <h2>Confirm Action</h2>
            <p>
              You are about to execute a potentially dangerous administrative action. 
              This action may affect system stability.
            </p>
            <div className="modal-actions">
              <button 
                className="btn-cancel"
                onClick={() => setShowConfirm(null)}
              >
                Cancel
              </button>
              <button 
                className="btn-confirm"
                onClick={() => {
                  const action = actions.find(a => a.id === showConfirm);
                  if (action) executeAction(action);
                }}
              >
                Confirm Execution
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminPage;
