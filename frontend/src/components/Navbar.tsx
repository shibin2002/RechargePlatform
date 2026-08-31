import React from 'react';
import { Smartphone, History, UploadCloud, Layers, BarChart3, KeyRound, Radio } from 'lucide-react';

interface NavbarProps {
  activeTab: string;
  setActiveTab: (tab: string) => void;
  isBackendOnline: boolean;
  onOpenSettings: () => void;
}

export const Navbar: React.FC<NavbarProps> = ({
  activeTab,
  setActiveTab,
  isBackendOnline,
  onOpenSettings,
}) => {
  const navItems = [
    { id: 'recharge', label: 'New Recharge', icon: <Smartphone size={16} /> },
    { id: 'transactions', label: 'Transactions', icon: <History size={16} /> },
    { id: 'cards', label: 'Card Import', icon: <UploadCloud size={16} /> },
    { id: 'inventory', label: 'Card Inventory', icon: <Layers size={16} /> },
    { id: 'analytics', label: 'Analytics & SQL', icon: <BarChart3 size={16} /> },
  ];

  return (
    <header className="navbar">
      <div className="brand-section">
        <div className="brand-logo">
          <Radio size={20} />
        </div>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span className="brand-name">PayTelecom POS</span>
            <span className="brand-tag">India Platform</span>
          </div>
        </div>
      </div>

      <nav className="nav-tabs">
        {navItems.map((item) => (
          <button
            key={item.id}
            className={`nav-tab ${activeTab === item.id ? 'active' : ''}`}
            onClick={() => setActiveTab(item.id)}
          >
            {item.icon}
            <span>{item.label}</span>
          </button>
        ))}
      </nav>

      <div className="nav-actions">
        <div className="status-pill-header">
          <span className={`indicator-dot ${isBackendOnline ? 'online' : 'offline'}`} />
          <span style={{ color: isBackendOnline ? '#10b981' : '#ef4444', fontWeight: 600 }}>
            {isBackendOnline ? 'API Connected' : 'API Offline'}
          </span>
        </div>

        <button className="settings-btn" onClick={onOpenSettings} title="Configure API Key & Base URL">
          <KeyRound size={14} />
          <span>API Auth</span>
        </button>
      </div>
    </header>
  );
};
