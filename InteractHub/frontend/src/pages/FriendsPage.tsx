import React from 'react';
import { Link } from 'react-router-dom';
import { UserCheck, UserX, Users } from 'lucide-react';
import { useFriends } from '../hooks';
import { Avatar, Button, Card, EmptyState } from '../components/ui';

const FriendsPage: React.FC = () => {
  const { friends, pendingRequests, loading, respond } = useFriends();

  if (loading) return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      <div className="space-y-3">
        {[1, 2, 3].map(i => (
          <div key={i} className="h-16 bg-gray-100 rounded-xl animate-pulse" />
        ))}
      </div>
    </div>
  );

  return (
    <div className="max-w-2xl mx-auto px-4 py-4 space-y-6">
      {/* Pending Requests */}
      {pendingRequests.length > 0 && (
        <section>
          <h2 className="font-semibold text-gray-700 mb-3">Friend Requests ({pendingRequests.length})</h2>
          <div className="space-y-2">
            {pendingRequests.map(req => (
              <Card key={req.id} className="flex items-center justify-between p-4">
                <Link to={`/profile/${req.otherUser.id}`} className="flex items-center gap-3">
                  <Avatar src={req.otherUser.avatarUrl} name={req.otherUser.fullName} />
                  <div>
                    <p className="font-semibold text-sm">{req.otherUser.fullName}</p>
                    <p className="text-xs text-gray-400">@{req.otherUser.userName}</p>
                  </div>
                </Link>
                <div className="flex gap-2">
                  <Button size="sm" onClick={() => respond(req.id, true)}>
                    <UserCheck size={14} /> Accept
                  </Button>
                  <Button size="sm" variant="secondary" onClick={() => respond(req.id, false)}>
                    <UserX size={14} /> Decline
                  </Button>
                </div>
              </Card>
            ))}
          </div>
        </section>
      )}

      {/* Friends List */}
      <section>
        <h2 className="font-semibold text-gray-700 mb-3">Friends ({friends.length})</h2>
        {friends.length === 0 ? (
          <EmptyState
            title="No friends yet"
            description="Search for people you know and send friend requests."
            icon={<Users size={40} />}
          />
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {friends.map(f => (
              <Card key={f.id} className="flex items-center gap-3 p-4">
                <Avatar src={f.otherUser.avatarUrl} name={f.otherUser.fullName} />
                <div className="flex-1 min-w-0">
                  <Link to={`/profile/${f.otherUser.id}`}>
                    <p className="font-semibold text-sm hover:text-blue-600 truncate">{f.otherUser.fullName}</p>
                  </Link>
                  <p className="text-xs text-gray-400 truncate">@{f.otherUser.userName}</p>
                </div>
              </Card>
            ))}
          </div>
        )}
      </section>
    </div>
  );
};

export default FriendsPage;
