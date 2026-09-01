import React, { useState, useEffect, useRef } from 'react';
import {
  Smartphone,
  History,
  UploadCloud,
  Layers,
  BarChart3,
  Clock,
  Sun,
  Moon,
  Settings
} from 'lucide-react';

interface NavbarProps {
  activeTab: string;
  setActiveTab: (tab: string) => void;
  isBackendOnline?: boolean;
  onOpenSettings?: () => void;
}

export const Navbar: React.FC<NavbarProps> = ({
  activeTab,
  setActiveTab,
  onOpenSettings,
}) => {
  const [timeStr, setTimeStr] = useState<string>('');
  const [theme, setTheme] = useState<'dark' | 'light'>(() => {
    return (localStorage.getItem('theme') as 'dark' | 'light') || 'dark';
  });
  const navTabsRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);
  }, [theme]);

  useEffect(() => {
    const updateTime = () => {
      const now = new Date();
      setTimeStr(now.toLocaleTimeString('en-IN', { hour12: false }));
    };
    updateTime();
    const interval = setInterval(updateTime, 1000);
    return () => clearInterval(interval);
  }, []);

  const toggleTheme = () => setTheme(prev => prev === 'dark' ? 'light' : 'dark');

  const navItems = [
    { id: 'recharge', label: 'New Recharge', icon: <Smartphone size={16} /> },
    { id: 'transactions', label: 'Transactions', icon: <History size={16} /> },
    { id: 'cards', label: 'Card Import', icon: <UploadCloud size={16} /> },
    { id: 'inventory', label: 'Card Inventory', icon: <Layers size={16} /> },
    { id: 'analytics', label: 'Analytics', icon: <BarChart3 size={16} /> },
  ];


  return (
    <header className="navbar">
      <div className="brand-section">
        <div className="brand-logo" title="Arova">
          <img src={`${import.meta.env.BASE_URL}favicon.svg`} alt="Arova" className="brand-logo-icon" />
        </div>
        <div className="brand-text-container">
          <div className="brand-header-row">
            <span className="brand-name">Arova</span>
          </div>
        </div>
      </div>

      <nav className="nav-tabs" ref={navTabsRef}>
        {navItems.map((item) => {
          const isActive = activeTab === item.id;
  useEffect(() => {
    const tabs = navTabsRef.current;
    if (!tabs) return;
    const active = tabs.querySelector('.nav-tab.active');
    if (active) {
      active.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
    }
  }, [activeTab]);

  return (
            <button
              key={item.id}
              className={`nav-tab ${isActive ? 'active' : ''}`}
              onClick={() => setActiveTab(item.id)}
            >
              <span className="nav-tab-icon">{item.icon}</span>
              <span className="nav-tab-label">{item.label}</span>
              {isActive && <span className="nav-tab-glow" />}
            </button>
          );
        })}
      </nav>

      <div className="nav-actions">
        <button
          className="theme-toggle"
          onClick={onOpenSettings}
          title="API Authentication Settings"
        >
          <Settings size={16} />
        </button>
        <button
          className="theme-toggle"
          onClick={toggleTheme}
          title={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
        >
          {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
        </button>
        <div className="nav-time-widget" title="Live Terminal Time (IST)">
          <Clock size={13} className="time-icon" />
          <span className="time-text">{timeStr || '00:00:00'}</span>
        </div>
      </div>
    </header>
  );
};
