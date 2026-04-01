import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { UserPlus, UserCheck, Clock, Settings } from 'lucide-react';
import { usersApi, friendsApi, postsApi } from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import type { UserProfile, Post } from '../types';
import { Avatar, Button, Card, PostSkeleton, Input } from '../components/ui';
import { PostCard } from '../components/posts/PostComponents';
import { usePosts } from '../hooks';

const ProfilePage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const { user: currentUser } = useAuth();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [posts, setPosts] = useState<Post[]>([]);
  const [loading, setLoading] = useState(true);
  const [postsLoading, setPostsLoading] = useState(true);
  const [friendStatus, setFriendStatus] = useState<'none' | 'pending' | 'accepted'>('none');
  const [editing, setEditing] = useState(false);

  const isOwner = id === currentUser?.userId;

  const { register, handleSubmit, formState: { isSubmitting } } = useForm<{ fullName: string; bio: string }>();

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    usersApi.getProfile(id)
      .then(({ data }) => {
        if (data.success && data.data) {
          setProfile(data.data);
          setFriendStatus(data.data.friendshipStatus as any);
        }
      })
      .finally(() => setLoading(false));

    setPostsLoading(true);
    postsApi.getUserPosts(id)
      .then(({ data }) => setPosts(data.items))
      .finally(() => setPostsLoading(false));
  }, [id]);

  const handleSendRequest = async () => {
    if (!id) return;
    const ok = await friendsApi.sendRequest(id);
    if (ok.data.success) setFriendStatus('pending');
  };

  const onSaveProfile = async (data: { fullName: string; bio: string }) => {
    await usersApi.updateProfile(data);
    setProfile(prev => prev ? { ...prev, ...data } : prev);
    setEditing(false);
  };

  const toggleLike = async (postId: number) => {
    setPosts(prev => prev.map(p =>
      p.id === postId
        ? { ...p, isLikedByCurrentUser: !p.isLikedByCurrentUser, likeCount: p.isLikedByCurrentUser ? p.likeCount - 1 : p.likeCount + 1 }
        : p
    ));
    await postsApi.toggleLike(postId);
  };

  const deletePost = async (postId: number) => {
    await postsApi.delete(postId);
    setPosts(prev => prev.filter(p => p.id !== postId));
  };

  if (loading) return (
    <div className="max-w-2xl mx-auto px-4 py-8 space-y-4">
      <div className="h-40 bg-gray-200 rounded-xl animate-pulse" />
    </div>
  );

  if (!profile) return <p className="text-center mt-20 text-gray-500">User not found.</p>;

  return (
    <div className="max-w-2xl mx-auto px-4 py-4 space-y-4">
      {/* Cover & Avatar */}
      <Card>
        <div className="h-36 bg-gradient-to-r from-blue-400 to-indigo-500 rounded-t-xl" />
        <div className="px-5 pb-5">
          <div className="flex justify-between items-end -mt-8 mb-3">
            <Avatar src={profile.avatarUrl} name={profile.fullName} size="xl" className="border-4 border-white shadow" />
            <div className="flex gap-2 mt-10">
              {isOwner ? (
                <Button variant="secondary" size="sm" onClick={() => setEditing(v => !v)}>
                  <Settings size={14} /> Edit Profile
                </Button>
              ) : (
                <>
                  {friendStatus === 'none' && (
                    <Button size="sm" onClick={handleSendRequest}><UserPlus size={14} /> Add Friend</Button>
                  )}
                  {friendStatus === 'pending' && (
                    <Button variant="secondary" size="sm" disabled><Clock size={14} /> Pending</Button>
                  )}
                  {friendStatus === 'accepted' && (
                    <Button variant="secondary" size="sm" disabled><UserCheck size={14} /> Friends</Button>
                  )}
                </>
              )}
            </div>
          </div>

          <h2 className="text-xl font-bold">{profile.fullName}</h2>
          <p className="text-sm text-gray-500">@{profile.userName}</p>
          {profile.bio && <p className="text-sm text-gray-700 mt-2">{profile.bio}</p>}

          <div className="flex gap-6 mt-4 text-sm">
            <span><strong>{profile.postCount}</strong> <span className="text-gray-500">Posts</span></span>
            <span><strong>{profile.friendCount}</strong> <span className="text-gray-500">Friends</span></span>
          </div>
        </div>
      </Card>

      {/* Edit Form */}
      {editing && (
        <Card className="p-4">
          <form onSubmit={handleSubmit(onSaveProfile)} className="space-y-3">
            <Input label="Full Name" defaultValue={profile.fullName} {...register('fullName', { required: true })} />
            <Input label="Bio" defaultValue={profile.bio ?? ''} {...register('bio')} />
            <div className="flex gap-2">
              <Button type="submit" size="sm" loading={isSubmitting}>Save</Button>
              <Button type="button" variant="secondary" size="sm" onClick={() => setEditing(false)}>Cancel</Button>
            </div>
          </form>
        </Card>
      )}

      {/* Posts */}
      {postsLoading ? (
        <div className="space-y-4">{[1, 2].map(i => <PostSkeleton key={i} />)}</div>
      ) : posts.length === 0 ? (
        <Card className="p-8 text-center text-gray-400 text-sm">No posts yet.</Card>
      ) : (
        <div className="space-y-4">
          {posts.map(post => (
            <PostCard key={post.id} post={post} onLike={toggleLike} onDelete={deletePost} />
          ))}
        </div>
      )}
    </div>
  );
};

export default ProfilePage;
