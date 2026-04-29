import * as signalR from '@microsoft/signalr';

const BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

export function createNotificationConnection(token: string) {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${BASE}/hubs/notifications`, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build();
}
