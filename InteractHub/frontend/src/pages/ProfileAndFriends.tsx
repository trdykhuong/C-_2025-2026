import React, { useEffect, useState, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { UserPlus, UserCheck, Clock, UserMinus, Pencil, X, Check, Upload, Globe } from 'lucide-react';
import { usersApi, postsApi, friendsApi, uploadApi } from '../services/api';
import type { UserProfile, Post, Friendship } from '../types';
import { useAuth } from '../contexts/AuthContext';
import { Avatar } from '../components/ui/Avatar';

function getMediaType(url: string): 'video' | 'youtube' | 'image' {
  if (/\.(mp4|webm|ogg|mov)(\?|$)/i.test(url)) return 'video';
  if (/youtube\.com|youtu\.be/i.test(url))       return 'youtube';
  return 'image';
}
function getYouTubeId(url: string) {
  const m = url.match(/(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))([^&?/\s]+)/);
  return m ? m[1] : null;
}

function timeAgo(d: string) {
  const utc = /Z|[+-]\d{2}:?\d{2}$/.test(d) ? d : d + 'Z';
  const diff = Math.floor((Date.now() - new Date(utc).getTime()) / 60000);
  if (diff < 1)  return 'Vừa xong';
  if (diff < 60) return `${diff} phút trước`;
  const h = Math.floor(diff / 60);
  if (h < 24)    return `${h} giờ trước`;
  return new Date(utc).toLocaleDateString('vi-VN');
}

// ─── PROFILE PAGE ─────────────────────────────────────────────────────────────
export const ProfilePage: React.FC = () => {
  const { id }    = useParams<{ id: string }>();
  const { user }  = useAuth();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [posts, setPosts]     = useState<Post[]>([]);
  const [editing, setEditing] = useState(false);
  const [uploading, setUploading] = useState<'avatar' | 'cover' | null>(null);
  const avatarInputRef = useRef<HTMLInputElement>(null);
  const coverInputRef  = useRef<HTMLInputElement>(null);
  const isMe = id === user?.userId;

  const { register, handleSubmit, formState: { isSubmitting }, setValue } = useForm<any>();

  useEffect(() => {
    if (!id) return;
    usersApi.getProfile(id).then(r => { if (r.success) setProfile(r.data!); });
    postsApi.getByUser(id).then(r => setPosts(r.items));
  }, [id]);

  const sendRequest = async () => {
    if (!id) return;
    await friendsApi.send(id);
    setProfile(p => p ? { ...p, friendshipStatus: 'pending_sent' } : p);
  };

  const cancelOrUnfriend = async () => {
    if (!id) return;
    await friendsApi.removeByUserId(id);
    setProfile(p => p ? { ...p, friendshipStatus: 'none', friendCount: p.friendCount - (p.friendshipStatus === 'accepted' ? 1 : 0) } : p);
  };

  const onSave = async (d: any) => {
    // Strip empty strings for optional image fields — sending "" would clear existing images
    const payload: any = { ...d };
    if (!payload.avatarUrl) delete payload.avatarUrl;
    if (!payload.coverUrl)  delete payload.coverUrl;
    await usersApi.update(payload);
    setProfile(p => p ? { ...p, ...payload } : p);
    setEditing(false);
  };

  const handleFileUpload = async (file: File, field: 'avatarUrl' | 'coverUrl') => {
    const which = field === 'avatarUrl' ? 'avatar' : 'cover';
    setUploading(which);
    try {
      const res = await uploadApi.upload(file);
      if (res.success) setValue(field, res.url);
    } finally { setUploading(null); }
  };

  if (!profile) return (
    <div className="flex justify-center items-center h-64">
      <div className="w-8 h-8 border-4 border-[#1877f2] border-t-transparent rounded-full animate-spin" />
    </div>
  );

  return (
    <div className="max-w-[860px] mx-auto">
      {/* Cover */}
      <div className="relative">
        <div className="h-52 sm:h-72 bg-gradient-to-r from-blue-400 to-indigo-500 rounded-b-xl overflow-hidden">
          {profile.coverUrl && <img src={profile.coverUrl} className="w-full h-full object-cover" alt="cover" />}
        </div>
        <div className="absolute -bottom-16 left-6">
          <Avatar name={profile.fullName} src={profile.avatarUrl} size={120} className="border-4 border-white shadow-lg" />
        </div>
      </div>

      {/* Info bar */}
      <div className="bg-white shadow-sm px-6 pt-20 pb-4 border-b border-gray-200">
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-3">
          <div>
            <h1 className="text-2xl font-bold">{profile.fullName}</h1>
            <p className="text-gray-500 text-sm">@{profile.userName}</p>
            {profile.bio && <p className="text-gray-600 text-sm mt-1">{profile.bio}</p>}
            <div className="flex gap-4 mt-2 text-sm text-gray-600">
              <span><strong>{profile.postCount}</strong> bài viết</span>
              <span><strong>{profile.friendCount}</strong> bạn bè</span>
            </div>
          </div>

          {isMe ? (
            <button onClick={() => setEditing(v => !v)}
              className="flex items-center gap-2 bg-gray-100 hover:bg-gray-200 px-4 py-2 rounded-lg text-sm font-medium transition">
              <Pencil size={15} /> Chỉnh sửa trang cá nhân
            </button>
          ) : (
            <div className="flex gap-2 flex-wrap">
              {profile.friendshipStatus === 'none' && (
                <button onClick={sendRequest}
                  className="flex items-center gap-2 bg-[#1877f2] text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-600 transition">
                  <UserPlus size={15} /> Thêm bạn bè
                </button>
              )}
              {profile.friendshipStatus === 'pending_sent' && (
                <>
                  <button disabled className="flex items-center gap-2 bg-gray-200 text-gray-600 px-4 py-2 rounded-lg text-sm font-medium">
                    <Clock size={15} /> Đã gửi lời mời
                  </button>
                  <button onClick={cancelOrUnfriend}
                    className="flex items-center gap-2 bg-red-50 text-red-600 px-4 py-2 rounded-lg text-sm font-medium hover:bg-red-100 transition">
                    <X size={15} /> Hủy lời mời
                  </button>
                </>
              )}
              {profile.friendshipStatus === 'pending_received' && (
                <div className="flex gap-2">
                  <button onClick={sendRequest}
                    className="flex items-center gap-2 bg-[#1877f2] text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-600 transition">
                    <UserCheck size={15} /> Xác nhận
                  </button>
                  <button onClick={cancelOrUnfriend}
                    className="flex items-center gap-2 bg-gray-200 text-gray-700 px-4 py-2 rounded-lg text-sm font-medium hover:bg-gray-300 transition">
                    <X size={15} /> Xóa
                  </button>
                </div>
              )}
              {profile.friendshipStatus === 'accepted' && (
                <div className="flex gap-2">
                  <button disabled className="flex items-center gap-2 bg-gray-100 text-gray-700 px-4 py-2 rounded-lg text-sm font-medium">
                    <UserCheck size={15} /> Bạn bè
                  </button>
                  <button onClick={cancelOrUnfriend}
                    className="flex items-center gap-2 bg-red-50 text-red-600 px-4 py-2 rounded-lg text-sm font-medium hover:bg-red-100 transition">
                    <UserMinus size={15} /> Hủy kết bạn
                  </button>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Edit form */}
        {editing && (
          <form onSubmit={handleSubmit(onSave)} className="mt-4 p-4 bg-gray-50 rounded-xl flex flex-col gap-3">
            <input {...register('fullName')} defaultValue={profile.fullName}
              placeholder="Họ và tên" className="border rounded-lg px-3 py-2 text-sm outline-none focus:border-[#1877f2]" />
            <input {...register('bio')} defaultValue={profile.bio ?? ''}
              placeholder="Tiểu sử" className="border rounded-lg px-3 py-2 text-sm outline-none focus:border-[#1877f2]" />

            {/* Avatar URL + upload */}
            <div className="flex gap-2 items-center">
              <input {...register('avatarUrl')} defaultValue={profile.avatarUrl ?? ''}
                placeholder="Link ảnh đại diện" className="flex-1 border rounded-lg px-3 py-2 text-sm outline-none focus:border-[#1877f2]" />
              <input ref={avatarInputRef} type="file" accept="image/*" className="hidden"
                onChange={e => e.target.files?.[0] && handleFileUpload(e.target.files[0], 'avatarUrl')} />
              <button type="button" onClick={() => avatarInputRef.current?.click()}
                disabled={uploading === 'avatar'}
                className="shrink-0 flex items-center gap-1 bg-gray-200 hover:bg-gray-300 px-3 py-2 rounded-lg text-xs font-medium transition disabled:opacity-50">
                <Upload size={13} /> {uploading === 'avatar' ? '...' : 'Tải lên'}
              </button>
            </div>

            {/* Cover URL + upload */}
            <div className="flex gap-2 items-center">
              <input {...register('coverUrl')} defaultValue={profile.coverUrl ?? ''}
                placeholder="Link ảnh bìa" className="flex-1 border rounded-lg px-3 py-2 text-sm outline-none focus:border-[#1877f2]" />
              <input ref={coverInputRef} type="file" accept="image/*" className="hidden"
                onChange={e => e.target.files?.[0] && handleFileUpload(e.target.files[0], 'coverUrl')} />
              <button type="button" onClick={() => coverInputRef.current?.click()}
                disabled={uploading === 'cover'}
                className="shrink-0 flex items-center gap-1 bg-gray-200 hover:bg-gray-300 px-3 py-2 rounded-lg text-xs font-medium transition disabled:opacity-50">
                <Upload size={13} /> {uploading === 'cover' ? '...' : 'Tải lên'}
              </button>
            </div>

            <div className="flex gap-2">
              <button type="submit" disabled={isSubmitting}
                className="flex items-center gap-1 bg-[#1877f2] text-white px-4 py-2 rounded-lg text-sm font-medium disabled:opacity-60">
                <Check size={14} /> Lưu
              </button>
              <button type="button" onClick={() => setEditing(false)}
                className="flex items-center gap-1 bg-gray-200 px-4 py-2 rounded-lg text-sm font-medium">
                <X size={14} /> Hủy
              </button>
            </div>
          </form>
        )}
      </div>

      {/* Posts */}
      <div className="max-w-[600px] mx-auto px-4 py-4 space-y-4">
        {posts.length === 0 ? (
          <div className="text-center py-16 text-gray-400">Chưa có bài viết nào.</div>
        ) : posts.map(p => (
          <div key={p.id} className="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
            <p className="text-sm leading-relaxed whitespace-pre-wrap">{p.content}</p>
            {p.imageUrl && (() => {
              const t = getMediaType(p.imageUrl!);
              if (t === 'image')   return <img src={p.imageUrl} className="mt-3 rounded-lg w-full max-h-80 object-cover" alt="" />;
              if (t === 'video')   return <video src={p.imageUrl} controls className="mt-3 rounded-lg w-full max-h-80" />;
              if (t === 'youtube') return <iframe src={`https://www.youtube.com/embed/${getYouTubeId(p.imageUrl!)}`} className="mt-3 rounded-lg w-full h-48" allowFullScreen />;
            })()}
            <div className="flex items-center gap-2 mt-2">
              <p className="text-xs text-gray-400">{timeAgo(p.createdAt)}</p>
              <Globe size={10} className="text-gray-400" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

// ─── FRIENDS PAGE ─────────────────────────────────────────────────────────────
export const FriendsPage: React.FC = () => {
  const [friends, setFriends] = useState<Friendship[]>([]);
  const [pending, setPending] = useState<Friendship[]>([]);
  const [tab, setTab]         = useState<'friends' | 'requests'>('friends');

  useEffect(() => {
    friendsApi.list().then(setFriends);
    friendsApi.pending().then(setPending);
  }, []);

  const respond = async (id: number, accept: boolean) => {
    await friendsApi.respond(id, accept);
    setPending(p => p.filter(r => r.id !== id));
    if (accept) {
      const req = pending.find(r => r.id === id);
      if (req) setFriends(f => [...f, { ...req, status: 'accepted' }]);
    }
  };

  const handleUnfriend = async (friendshipId: number) => {
    await friendsApi.remove(friendshipId);
    setFriends(f => f.filter(x => x.id !== friendshipId));
  };

  return (
    <div className="max-w-2xl mx-auto px-4 py-4">
      <h1 className="text-2xl font-bold mb-4">Bạn bè</h1>
      <div className="flex gap-2 mb-4">
        {(['friends', 'requests'] as const).map(t => (
          <button key={t} onClick={() => setTab(t)}
            className={`px-4 py-2 rounded-lg font-medium text-sm transition ${tab === t ? 'bg-[#e7f3ff] text-[#1877f2]' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'}`}>
            {t === 'friends' ? `Bạn bè (${friends.length})` : `Lời mời (${pending.length})`}
          </button>
        ))}
      </div>

      {tab === 'requests' && (
        <div className="space-y-3">
          {pending.length === 0 && <p className="text-gray-400 text-center py-12">Không có lời mời kết bạn.</p>}
          {pending.map(req => (
            <div key={req.id} className="bg-white rounded-xl shadow-sm border border-gray-200 flex items-center gap-4 p-4">
              <Avatar name={req.otherUser.fullName} src={req.otherUser.avatarUrl} size={56} />
              <div className="flex-1">
                <Link to={`/profile/${req.otherUser.id}`} className="font-semibold hover:underline">{req.otherUser.fullName}</Link>
                <p className="text-xs text-gray-400">@{req.otherUser.userName}</p>
                <div className="flex gap-2 mt-2">
                  <button onClick={() => respond(req.id, true)}
                    className="bg-[#1877f2] text-white px-4 py-1.5 rounded-lg text-sm font-medium hover:bg-blue-600 transition">
                    Xác nhận
                  </button>
                  <button onClick={() => respond(req.id, false)}
                    className="bg-gray-200 text-gray-700 px-4 py-1.5 rounded-lg text-sm font-medium hover:bg-gray-300 transition">
                    Xóa
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {tab === 'friends' && (
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
          {friends.length === 0 && <p className="col-span-3 text-gray-400 text-center py-12">Chưa có bạn bè.</p>}
          {friends.map(f => (
            <div key={f.id} className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
              <Link to={`/profile/${f.otherUser.id}`}>
                <div className="h-28 bg-gradient-to-b from-gray-100 to-gray-200 flex items-center justify-center hover:opacity-90 transition">
                  <Avatar name={f.otherUser.fullName} src={f.otherUser.avatarUrl} size={64} />
                </div>
              </Link>
              <div className="p-3">
                <Link to={`/profile/${f.otherUser.id}`} className="font-semibold text-sm truncate block hover:underline">{f.otherUser.fullName}</Link>
                <p className="text-xs text-gray-400 truncate">@{f.otherUser.userName}</p>
                <button
                  onClick={() => handleUnfriend(f.id)}
                  className="mt-2 w-full flex items-center justify-center gap-1 bg-gray-100 hover:bg-red-50 hover:text-red-600 text-gray-600 px-2 py-1.5 rounded-lg text-xs font-medium transition">
                  <UserMinus size={12} /> Hủy kết bạn
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
