import React, { useState, useEffect } from 'react';
import { Navbar } from './components/Navbar';
import { RechargePage } from './pages/RechargePage';
import { TransactionsPage } from './pages/TransactionsPage';
import { CardImportPage } from './pages/CardImportPage';
import { CardInventoryPage } from './pages/CardInventoryPage';
import { AnalyticsPage } from './pages/AnalyticsPage';
import { getApiBaseUrl, getApiKey, rechargeApi, setApiBaseUrl, setApiKey } from './services/api';
import { KeyRound, X, CheckCircle2 } from 'lucide-react';

export const App: React.FC = () => {
  const [activeTab, setActiveTab] = useState('recharge');
  const [isBackendOnline, setIsBackendOnline] = useState(false);
  const [showSettings, setShowSettings] = useState(false);

  // Settings state
  const [baseUrlInput, setBaseUrlInput] = useState(getApiBaseUrl());
  const [apiKeyInput, setApiKeyInput] = useState(getApiKey());
  const [settingsSavedMsg, setSettingsSavedMsg] = useState(false);

  const checkStatus = async () => {
    const isOnline = await rechargeApi.checkHealth();
    setIsBackendOnline(isOnline);
  };

  useEffect(() => {
    checkStatus();
    const timer = setInterval(checkStatus, 10000);
    return () => clearInterval(timer);
  }, []);

  const handleSaveSettings = (e: React.FormEvent) => {
    e.preventDefault();
    setApiBaseUrl(baseUrlInput.trim());
    setApiKey(apiKeyInput.trim());
    setSettingsSavedMsg(true);
    setTimeout(() => {
      setSettingsSavedMsg(false);
      setShowSettings(false);
      checkStatus();
    }, 1200);
  };

  return (
    <div className="app-container">
      <Navbar
        activeTab={activeTab}
        setActiveTab={setActiveTab}
        isBackendOnline={isBackendOnline}
        onOpenSettings={() => setShowSettings(true)}
      />

      <main className="main-content">
        {activeTab === 'recharge' && <RechargePage />}
        {activeTab === 'transactions' && <TransactionsPage />}
        {activeTab === 'cards' && <CardImportPage />}
        {activeTab === 'inventory' && <CardInventoryPage />}
        {activeTab === 'analytics' && <AnalyticsPage />}
      </main>

      {/* API Auth & Base URL Settings Modal */}
      {showSettings && (
        <div className="modal-backdrop" onClick={() => setShowSettings(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <KeyRound size={20} color="#3b82f6" />
                <h3 style={{ fontSize: '1.1rem', fontWeight: 700 }}>API Authentication Settings</h3>
              </div>
              <button
                onClick={() => setShowSettings(false)}
                style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}
              >
                <X size={18} />
              </button>
            </div>

            <form onSubmit={handleSaveSettings}>
              <div className="form-group">
                <label className="form-label">
                  <span>Main Recharge API Base URL</span>
                </label>
                <input
                  type="text"
                  className="form-input"
                  value={baseUrlInput}
                  onChange={(e) => setBaseUrlInput(e.target.value)}
                  placeholder="http://localhost:5000/api"
                  required
                />
              </div>

              <div className="form-group">
                <label className="form-label">
                  <span>X-Api-Key Header Value</span>
                  <span style={{ fontSize: '0.75rem', color: '#94a3b8' }}>Configured via middleware</span>
                </label>
                <input
                  type="text"
                  className="form-input"
                  value={apiKeyInput}
                  onChange={(e) => setApiKeyInput(e.target.value)}
                  placeholder="pos_super_secret_api_key_2026"
                  required
                />
              </div>

              <div style={{ background: 'rgba(15, 23, 42, 0.6)', border: '1px solid var(--border-color)', borderRadius: '6px', padding: '0.75rem', marginBottom: '1.25rem', fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                <p>
                  <strong>Authentication Note:</strong> All requests to <code>/api/recharge</code> and <code>/api/cards</code> pass the <code>X-Api-Key</code> header. You can change this to test <code>401 Unauthorized</code> responses.
                </p>
              </div>

              {settingsSavedMsg && (
                <div className="duplicate-alert" style={{ background: 'rgba(16, 185, 129, 0.15)', borderColor: '#10b981', color: '#34d399' }}>
                  <CheckCircle2 size={16} />
                  <span>Configuration saved successfully!</span>
                </div>
              )}

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
                <button
                  type="button"
                  className="btn-secondary"
                  onClick={() => {
                    setBaseUrlInput('http://localhost:5000/api');
                    setApiKeyInput('pos_super_secret_api_key_2026');
                  }}
                >
                  Reset Defaults
                </button>
                <button type="submit" className="btn-primary" style={{ width: 'auto' }}>
                  Save Configuration
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default App;
