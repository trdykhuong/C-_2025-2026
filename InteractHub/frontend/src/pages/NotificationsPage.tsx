import React from 'react';
import { Bell, Heart, MessageCircle, UserPlus, Check } from 'lucide-react';
import { useNotifications } from '../hooks';
import { Card, Button, EmptyState } from '../components/ui';

const iconMap: Record<string, React.ReactNode> = {
  like: <Heart size={14} className="text-red-500" />,
  comment: <MessageCircle size={14} className="text-blue-500" />,
  friend_request: <UserPlus size={14} className="text-green-500" />,
  friend_accepted: <UserPlus size={14} className="text-green-500" />,
  general: <Bell size={14} className="text-gray-400" />,
};

const NotificationsPage: React.FC = () => {
  const { notifications, unreadCount, markAllRead } = useNotifications();

  return (
    <div className="max-w-2xl mx-auto px-4 py-4 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="font-bold text-xl">Notifications</h1>
        {unreadCount > 0 && (
          <Button variant="ghost" size="sm" onClick={markAllRead}>
            <Check size={14} /> Mark all read
          </Button>
        )}
      </div>

      {notifications.length === 0 ? (
        <EmptyState
          title="No notifications"
          description="When people like or comment on your posts, you'll see it here."
          icon={<Bell size={40} />}
        />
      ) : (
        <div className="space-y-2">
          {notifications.map(n => (
            <Card
              key={n.id}
              className={`flex items-start gap-3 p-4 ${!n.isRead ? 'border-blue-200 bg-blue-50/50' : ''}`}
            >
              <div className="w-8 h-8 bg-gray-100 rounded-full flex items-center justify-center shrink-0">
                {iconMap[n.type] ?? iconMap.general}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm text-gray-800">{n.message}</p>
                <p className="text-xs text-gray-400 mt-0.5">
                  {new Date(n.createdAt).toLocaleString()}
                </p>
              </div>
              {!n.isRead && (
                <div className="w-2 h-2 rounded-full bg-blue-500 shrink-0 mt-1" />
              )}
            </Card>
          ))}
        </div>
      )}
    </div>
  );
};

export default NotificationsPage;
