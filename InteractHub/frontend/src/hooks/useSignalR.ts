import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import type { Notification } from '../types';

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

export function useSignalR(onNotification: (n: Notification) => void) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/notifications`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on('ReceiveNotification', (data: Notification) => {
      onNotification(data);
    });

    connection.start().catch(err => console.warn('SignalR connection failed:', err));
    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [onNotification]);

  return connectionRef;
}
