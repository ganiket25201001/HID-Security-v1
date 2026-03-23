/**
 * Preload Script - Secure Bridge Between Main and Renderer
 * 
 * SECURITY:
 * - Runs in isolated context (contextIsolation: true)
 * - Only exposes explicitly defined APIs via contextBridge
 * - Validates ALL IPC messages before sending to main process
 * - No direct access to Node.js APIs from renderer
 * - No filesystem, shell, or process exposure
 */

import { contextBridge, ipcRenderer, IpcRendererEvent } from 'electron';
import { 
  IPC_CHANNELS, 
  ChannelName, 
  isValidChannel,
  IPCMessage,
} from '../../src/types/ipc-types';

/**
 * Whitelist of allowed channels for renderer -> main communication
 * This provides defense-in-depth even if types are bypassed
 */
const ALLOWED_CHANNELS: ChannelName[] = [
  IPC_CHANNELS.APP_READY,
  IPC_CHANNELS.APP_VERSION,
  IPC_CHANNELS.APP_QUIT,
  IPC_CHANNELS.BACKEND_SEND,
  IPC_CHANNELS.DEVICE_LIST,
  IPC_CHANNELS.DEVICE_STATUS,
  IPC_CHANNELS.DEVICE_UPDATE,
  IPC_CHANNELS.ALERT_LIST,
  IPC_CHANNELS.ALERT_ACKNOWLEDGE,
  IPC_CHANNELS.ALERT_DISMISS,
  IPC_CHANNELS.POLICY_GET,
  IPC_CHANNELS.POLICY_LIST,
  IPC_CHANNELS.ADMIN_ACTION,
  IPC_CHANNELS.ADMIN_STATUS,
  IPC_CHANNELS.SETTINGS_GET,
  IPC_CHANNELS.SETTINGS_UPDATE,
  IPC_CHANNELS.AUTH_LOGIN,
  IPC_CHANNELS.AUTH_LOGOUT,
  IPC_CHANNELS.AUTH_STATUS,
  IPC_CHANNELS.AUTH_MFA_VERIFY,
  IPC_CHANNELS.SERVICE_STATUS,
  IPC_CHANNELS.SERVICE_HEALTH,
  IPC_CHANNELS.RENDERER_ERROR,
];

/**
 * Validate channel name against whitelist
 */
function validateChannel(channel: string): channel is ChannelName {
  return isValidChannel(channel) && ALLOWED_CHANNELS.includes(channel as ChannelName);
}

/**
 * Generate unique message ID for tracking
 */
function generateMessageId(): string {
  return `${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

/**
 * Safe IPC API exposed to renderer
 */
const electronAPI = {
  /**
   * Send IPC message to main process with validation
   */
  async invoke<T>(channel: string, payload?: unknown): Promise<T> {
    if (!validateChannel(channel)) {
      const error = `Blocked IPC invoke to unauthorized channel: ${channel}`;
      console.error('[Preload Security]', error);
      throw new Error(error);
    }

    const message: IPCMessage = {
      channel: channel as ChannelName,
      payload: payload ?? {},
      timestamp: Date.now(),
      messageId: generateMessageId(),
    };

    try {
      return await ipcRenderer.invoke(channel, message) as T;
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown IPC error';
      console.error(`[IPC Invoke Error] Channel: ${channel}, Error: ${errorMessage}`);
      throw error;
    }
  },

  /**
   * Subscribe to IPC events from main process
   */
  on(channel: string, callback: (data: unknown) => void): () => void {
    if (!validateChannel(channel)) {
      console.error('[Preload Security]', `Blocked IPC subscription to unauthorized channel: ${channel}`);
      return () => {};
    }

    const subscription = (_event: IpcRendererEvent, data: unknown) => {
      callback(data);
    };

    ipcRenderer.on(channel as ChannelName, subscription);

    // Return unsubscribe function
    return () => {
      ipcRenderer.removeListener(channel as ChannelName, subscription);
    };
  },

  /**
   * Send one-way message (no response expected)
   */
  send(channel: string, payload?: unknown): void {
    if (!validateChannel(channel)) {
      console.error('[Preload Security]', `Blocked IPC send to unauthorized channel: ${channel}`);
      return;
    }

    const message: IPCMessage = {
      channel: channel as ChannelName,
      payload: payload ?? {},
      timestamp: Date.now(),
      messageId: generateMessageId(),
    };

    ipcRenderer.send(channel as ChannelName, message);
  },

  /**
   * Report error from renderer to main process
   */
  reportError(error: Error, component: string): void {
    const payload = {
      error: error.message,
      stack: error.stack,
      component,
      timestamp: new Date().toISOString(),
    };
    
    // Don't throw if error reporting fails
    try {
      this.send(IPC_CHANNELS.RENDERER_ERROR, payload);
    } catch {
      console.error('[Preload] Failed to report error:', error);
    }
  },

  /**
   * Platform info (safe, read-only)
   */
  platform: {
    isWindows: process.platform === 'win32',
    isMac: process.platform === 'darwin',
    isLinux: process.platform === 'linux',
  },

  /**
   * Channel names for reference
   */
  channels: IPC_CHANNELS,
};

/**
 * Expose API to renderer via contextBridge
 * This is the ONLY way the renderer can communicate with main process
 */
contextBridge.exposeInMainWorld('electronAPI', electronAPI);

/**
 * Type declaration for the exposed API
 * This allows TypeScript to know about the electronAPI in the renderer
 */
declare global {
  interface Window {
    electronAPI: typeof electronAPI;
  }
}
