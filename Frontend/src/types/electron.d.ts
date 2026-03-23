/**
 * Electron API Type Declarations
 * 
 * This file declares the types for the electronAPI exposed via contextBridge.
 */

import { IPC_CHANNELS, ChannelName } from './types/ipc-types';

export interface ElectronAPI {
  /**
   * Send IPC message to main process and get response
   */
  invoke<T>(channel: string, payload?: unknown): Promise<T>;
  
  /**
   * Subscribe to IPC events from main process
   */
  on(channel: string, callback: (data: unknown) => void): () => void;
  
  /**
   * Send one-way message to main process
   */
  send(channel: string, payload?: unknown): void;
  
  /**
   * Report error from renderer to main process
   */
  reportError(error: Error, component: string): void;
  
  /**
   * Platform information
   */
  platform: {
    isWindows: boolean;
    isMac: boolean;
    isLinux: boolean;
  };
  
  /**
   * Available IPC channel names
   */
  channels: typeof IPC_CHANNELS;
}

declare global {
  interface Window {
    electronAPI: ElectronAPI;
  }
}

export {};
