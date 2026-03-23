import React from 'react';
import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';
import TopBar from './TopBar';
import './AppLayout.css';
import { AuthStatusPayload } from '../types/ipc-types';

interface AppLayoutProps {
  user: AuthStatusPayload | null;
  onLogout: () => void;
}

/**
 * Main App Layout
 * 
 * Combines sidebar navigation with top bar and content area.
 */
const AppLayout: React.FC<AppLayoutProps> = ({ user, onLogout }) => {
  return (
    <div className="app-layout">
      <Sidebar />
      <div className="app-content">
        <TopBar user={user} onLogout={onLogout} />
        <main className="main-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default AppLayout;
