import React, { useState, useEffect } from 'react';
import { mockPolicies } from '../mock/data';
import { PolicyConfig } from '../types/ipc-types';
import './PolicyPage.css';

/**
 * Policy View Page
 * 
 * Displays security policies and their rules.
 */
const PolicyPage: React.FC = () => {
  const [policies, setPolicies] = useState<PolicyConfig[]>([]);
  const [activePolicyId, setActivePolicyId] = useState('');
  const [selectedPolicy, setSelectedPolicy] = useState<PolicyConfig | null>(null);
  const [expandedRules, setExpandedRules] = useState<Set<string>>(new Set());
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadPolicies = async (): Promise<void> => {
      await new Promise(resolve => setTimeout(resolve, 500));
      setPolicies(mockPolicies);
      setActivePolicyId(mockPolicies[0].policyId);
      setSelectedPolicy(mockPolicies[0]);
      setIsLoading(false);
    };

    loadPolicies();
  }, []);

  const toggleRule = (ruleId: string): void => {
    setExpandedRules(prev => {
      const next = new Set(prev);
      if (next.has(ruleId)) {
        next.delete(ruleId);
      } else {
        next.add(ruleId);
      }
      return next;
    });
  };

  const getActionColor = (action: string): string => {
    switch (action) {
      case 'allow': return 'action-allow';
      case 'block': return 'action-block';
      case 'quarantine': return 'action-quarantine';
      case 'notify': return 'action-notify';
      default: return '';
    }
  };

  if (isLoading) {
    return (
      <div className="loading-state">
        <div className="loading-spinner-large"></div>
        <p>Loading policies...</p>
      </div>
    );
  }

  return (
    <div className="policy-page">
      <div className="page-header">
        <div>
          <h1>Security Policies</h1>
          <p className="page-subtitle">View and manage security policy configurations</p>
        </div>
        <button className="btn-primary">
          <span>📋</span> Create New Policy
        </button>
      </div>

      <div className="policy-layout">
        {/* Policy List */}
        <div className="policy-list-panel">
          <h2>Policies</h2>
          <div className="policy-list">
            {policies.map((policy) => (
              <div
                key={policy.policyId}
                className={`policy-item ${activePolicyId === policy.policyId ? 'active' : ''}`}
                onClick={() => {
                  setActivePolicyId(policy.policyId);
                  setSelectedPolicy(policy);
                }}
              >
                <div className="policy-item-header">
                  <h3>{policy.name}</h3>
                  {activePolicyId === policy.policyId && (
                    <span className="active-badge">Active</span>
                  )}
                </div>
                <p className="policy-version">Version {policy.version}</p>
                <p className="policy-meta">
                  {policy.rules.length} rules • Updated {new Date(policy.lastUpdated).toLocaleDateString()}
                </p>
              </div>
            ))}
          </div>

          <div className="policy-info-card">
            <h4>Policy Information</h4>
            {selectedPolicy && (
              <>
                <div className="info-row">
                  <span className="info-label">Policy ID:</span>
                  <span className="info-value font-mono">{selectedPolicy.policyId}</span>
                </div>
                <div className="info-row">
                  <span className="info-label">Version:</span>
                  <span className="info-value">{selectedPolicy.version}</span>
                </div>
                <div className="info-row">
                  <span className="info-label">Last Updated:</span>
                  <span className="info-value">{new Date(selectedPolicy.lastUpdated).toLocaleString()}</span>
                </div>
                <div className="info-row">
                  <span className="info-label">Enforced By:</span>
                  <span className="info-value">{selectedPolicy.enforcedBy}</span>
                </div>
                <div className="info-row">
                  <span className="info-label">Total Rules:</span>
                  <span className="info-value">{selectedPolicy.rules.length}</span>
                </div>
              </>
            )}
          </div>
        </div>

        {/* Policy Details */}
        <div className="policy-detail-panel">
          {selectedPolicy ? (
            <>
              <div className="policy-detail-header">
                <h2>{selectedPolicy.name}</h2>
                <div className="policy-actions">
                  <button className="btn-secondary">Edit Policy</button>
                  <button className="btn-secondary">Export</button>
                  <button className="btn-primary">Activate Policy</button>
                </div>
              </div>

              <div className="rules-section">
                <div className="section-header">
                  <h3>Policy Rules</h3>
                  <span className="rule-count">{selectedPolicy.rules.length} rules defined</span>
                </div>

                <div className="rules-list">
                  {selectedPolicy.rules.map((rule, index) => (
                    <div
                      key={rule.ruleId}
                      className={`rule-card ${expandedRules.has(rule.ruleId) ? 'expanded' : ''}`}
                    >
                      <div className="rule-header" onClick={() => toggleRule(rule.ruleId)}>
                        <div className="rule-priority">{index + 1}</div>
                        <div className="rule-info">
                          <h4>{rule.name}</h4>
                          <p>{rule.description}</p>
                        </div>
                        <div className="rule-status">
                          <span className={`enabled-badge ${rule.enabled ? 'enabled' : 'disabled'}`}>
                            {rule.enabled ? 'Enabled' : 'Disabled'}
                          </span>
                          <span className={`action-badge ${getActionColor(rule.action)}`}>
                            {rule.action.toUpperCase()}
                          </span>
                        </div>
                        <span className="expand-icon">
                          {expandedRules.has(rule.ruleId) ? '▼' : '▶'}
                        </span>
                      </div>

                      {expandedRules.has(rule.ruleId) && (
                        <div className="rule-details">
                          <div className="detail-row">
                            <span className="detail-label">Rule ID:</span>
                            <span className="detail-value font-mono">{rule.ruleId}</span>
                          </div>
                          <div className="detail-row">
                            <span className="detail-label">Priority:</span>
                            <span className="detail-value">{rule.priority}</span>
                          </div>
                          <div className="detail-row">
                            <span className="detail-label">Action:</span>
                            <span className={`action-badge ${getActionColor(rule.action)}`}>
                              {rule.action}
                            </span>
                          </div>
                          <div className="detail-row">
                            <span className="detail-label">Conditions:</span>
                            <pre className="conditions-json">
                              {JSON.stringify(rule.conditions, null, 2)}
                            </pre>
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            </>
          ) : (
            <div className="no-selection">
              <span className="empty-icon">📋</span>
              <h3>Select a policy</h3>
              <p>Choose a policy from the list to view its details</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default PolicyPage;
