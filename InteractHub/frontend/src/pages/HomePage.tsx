import React, { useState, useEffect, useCallback, useRef } from 'react';
import { Link } from 'react-router-dom';
import {
  Image, Smile, Video, ThumbsUp, MessageCircle, Share2,
  MoreHorizontal, Globe, Lock, Users, Trash2, X, Send,
  Plus, Pencil, Check, Copy, Eye, Upload, Flag,
} from 'lucide-react';
import { postsApi, commentsApi, storiesApi, uploadApi, reportsApi } from '../services/api';
import type { Post, Comment, Story } from '../types';
import { useAuth } from '../contexts/AuthContext';
import { Avatar } from '../components/ui/Avatar';

// ─── HELPERS ──────────────────────────────────────────────────────────────────
function timeAgo(d: string) {
  // Append Z if no timezone info — C# DateTime serialized without suffix defaults to local parse
  const utc = /Z|[+-]\d{2}:?\d{2}$/.test(d) ? d : d + 'Z';
  const diff = Math.floor((Date.now() - new Date(utc).getTime()) / 60000);
  if (diff < 1)  return 'Vừa xong';
  if (diff < 60) return `${diff} phút trước`;
  const h = Math.floor(diff / 60);
  if (h < 24)    return `${h} giờ trước`;
  const days = Math.floor(h / 24);
  if (days < 7)  return `${days} ngày trước`;
  return new Date(utc).toLocaleDateString('vi-VN');
}

const SEVEN_HOURS = 7 * 60 * 60 * 1000;
function canEdit(createdAt: string) {
  return Date.now() - new Date(createdAt).getTime() < SEVEN_HOURS;
}

function getMediaType(url: string): 'video' | 'youtube' | 'image' {
  if (/\.(mp4|webm|ogg|mov)(\?|$)/i.test(url)) return 'video';
  if (/youtube\.com|youtu\.be/i.test(url))       return 'youtube';
  return 'image';
}

function getYouTubeId(url: string) {
  const m = url.match(/(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))([^&?/\s]+)/);
  return m ? m[1] : null;
}

function extractHashtags(text: string): string[] {
  const matches = text.match(/#[\wÀ-ỹ]+/g) ?? [];
  return [...new Set(matches.map(t => t.slice(1).toLowerCase()))];
}

function ContentWithHashtags({ text }: { text: string }) {
  const parts = text.split(/(#[\wÀ-ỹ]+)/g);
  return (
    <span>
      {parts.map((p, i) =>
        /^#[\wÀ-ỹ]+$/.test(p)
          ? <Link key={i} to={`/hashtag/${p.slice(1).toLowerCase()}`}
              className="text-[#1877f2] font-medium hover:underline">{p}</Link>
          : <span key={i}>{p}</span>
      )}
    </span>
  );
}

const VISIBILITY_OPTIONS = [
  { value: 'public',  label: 'Công khai',    icon: Globe,  color: 'text-gray-600' },
  { value: 'friends', label: 'Bạn bè',       icon: Users,  color: 'text-blue-600' },
  { value: 'private', label: 'Chỉ mình tôi', icon: Lock,   color: 'text-gray-500' },
] as const;

type Visibility = 'public' | 'friends' | 'private';

function VisibilityIcon({ v, size = 14 }: { v: Visibility; size?: number }) {
  const opt = VISIBILITY_OPTIONS.find(o => o.value === v)!;
  const Icon = opt.icon;
  return <Icon size={size} className={opt.color} />;
}

// ─── TOAST ────────────────────────────────────────────────────────────────────
const Toast: React.FC<{ msg: string; onDone: () => void }> = ({ msg, onDone }) => {
  useEffect(() => { const t = setTimeout(onDone, 2500); return () => clearTimeout(t); }, [onDone]);
  return (
    <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-[100] bg-gray-800 text-white px-5 py-2.5 rounded-full text-sm shadow-lg">
      {msg}
    </div>
  );
};

// ─── STORY BAR ────────────────────────────────────────────────────────────────
const StoryBar: React.FC = () => {
  const { user }  = useAuth();
  const [stories, setStories]     = useState<Story[]>([]);
  const [viewing, setViewing]     = useState<Story | null>(null);
  const [creating, setCreating]   = useState(false);
  const [imgUrl, setImgUrl]       = useState('');
  const [caption, setCaption]     = useState('');
  const [storyVisibility, setStoryVisibility] = useState<Visibility>('public');
  const [uploading, setUploading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [progress, setProgress]   = useState(0);
  const progressRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const storyFileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    storiesApi.list().then(setStories).catch(() => {});
  }, []);

  // Story auto-advance progress bar (5 seconds)
  useEffect(() => {
    if (!viewing) { setProgress(0); return; }
    // Record view (don't await — fire & forget)
    storiesApi.recordView(viewing.id).catch(() => {});
    setProgress(0);
    let p = 0;
    progressRef.current = setInterval(() => {
      p += 2;
      setProgress(p);
      if (p >= 100) { clearInterval(progressRef.current!); setViewing(null); }
    }, 100);
    return () => clearInterval(progressRef.current!);
  }, [viewing?.id]);

  const handleStoryFileUpload = async (file: File) => {
    setUploading(true);
    try {
      const res = await uploadApi.upload(file);
      if (res.success) setImgUrl(res.url);
    } finally { setUploading(false); }
  };

  const handleCreate = async () => {
    if (!imgUrl.trim() && !caption.trim()) return;
    setSubmitting(true);
    try {
      const res = await storiesApi.create({ imageUrl: imgUrl || undefined, caption: caption || undefined, visibility: storyVisibility });
      if (res.success && res.data) {
        setStories(s => [res.data!, ...s]);
        setCreating(false); setImgUrl(''); setCaption(''); setStoryVisibility('public');
      }
    } finally { setSubmitting(false); }
  };

  const handleDelete = async (id: number) => {
    await storiesApi.delete(id);
    setStories(s => s.filter(x => x.id !== id));
    setViewing(null);
  };

  // Own stories and others
  const myStories    = stories.filter(s => s.author.id === user?.userId);
  const otherStories = stories.filter(s => s.author.id !== user?.userId);
  // Deduplicate others by author
  const uniqueOthers = otherStories.reduce<Story[]>((acc, s) => {
    if (!acc.find(x => x.author.id === s.author.id)) acc.push(s);
    return acc;
  }, []);

  const openOwnStory = () => {
    if (myStories.length > 0) setViewing(myStories[0]);
    else setCreating(true);
  };

  return (
    <>
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 mb-4 p-3">
        <div className="flex gap-3 overflow-x-auto pb-1">
          {/* My story card */}
          <div
            onClick={openOwnStory}
            className="shrink-0 w-24 h-36 rounded-xl overflow-hidden cursor-pointer relative group select-none"
          >
            {myStories[0]?.imageUrl
              ? <img src={myStories[0].imageUrl} className="w-full h-full object-cover" alt="" />
              : <div className="w-full h-full bg-gradient-to-b from-gray-200 to-gray-100" />
            }
            <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent" />
            {myStories.length === 0 && (
              <div className="absolute top-3 left-1/2 -translate-x-1/2 w-9 h-9 bg-[#1877f2] border-4 border-white rounded-full flex items-center justify-center">
                <Plus size={14} className="text-white" />
              </div>
            )}
            {myStories.length > 0 && (
              <div className="absolute top-2 left-2 ring-2 ring-[#1877f2] rounded-full">
                <Avatar name={user?.fullName ?? 'U'} src={user?.avatarUrl} size={30} />
              </div>
            )}
            <p className="absolute bottom-2 left-0 right-0 text-white text-[10px] font-semibold text-center px-1 leading-tight">
              {myStories.length > 0 ? 'Story của bạn' : 'Tạo story'}
            </p>
          </div>

          {/* Others' stories */}
          {uniqueOthers.map(s => (
            <div
              key={s.id}
              onClick={() => setViewing(s)}
              className="shrink-0 w-24 h-36 rounded-xl overflow-hidden cursor-pointer relative select-none"
            >
              {s.imageUrl
                ? <img src={s.imageUrl} className="w-full h-full object-cover" alt="" />
                : <div className="w-full h-full bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center p-2">
                    <p className="text-white text-[11px] font-bold text-center">{s.caption}</p>
                  </div>
              }
              <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent" />
              <div className="absolute top-2 left-2 ring-2 ring-[#1877f2] rounded-full">
                <Avatar name={s.author.fullName} src={s.author.avatarUrl} size={30} />
              </div>
              <p className="absolute bottom-2 left-0 right-0 text-white text-[10px] font-semibold text-center px-1 truncate">
                {s.author.fullName}
              </p>
            </div>
          ))}
        </div>
      </div>

      {/* Create story modal */}
      {creating && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60">
          <div className="bg-white rounded-2xl w-full max-w-sm mx-4 shadow-2xl p-5">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-bold text-lg">Tạo story</h2>
              <button onClick={() => setCreating(false)} className="w-8 h-8 bg-gray-100 rounded-full flex items-center justify-center hover:bg-gray-200">
                <X size={16} />
              </button>
            </div>
            {/* Image URL + upload */}
            <div className="flex gap-2 items-center mb-3">
              <input
                value={imgUrl}
                onChange={e => setImgUrl(e.target.value)}
                placeholder="Link ảnh (tuỳ chọn)"
                className="flex-1 border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:border-[#1877f2]"
              />
              <input ref={storyFileRef} type="file" accept="image/*" className="hidden"
                onChange={e => e.target.files?.[0] && handleStoryFileUpload(e.target.files[0])} />
              <button type="button" onClick={() => storyFileRef.current?.click()}
                disabled={uploading}
                className="shrink-0 flex items-center gap-1 bg-gray-100 hover:bg-gray-200 px-3 py-2 rounded-lg text-xs font-medium transition disabled:opacity-50">
                <Upload size={13} /> {uploading ? '...' : 'Tải lên'}
              </button>
            </div>
            {imgUrl && <img src={imgUrl} className="w-full h-40 object-cover rounded-lg mb-3" alt="" />}
            {/* Visibility picker */}
            <div className="flex gap-2 mb-3">
              {VISIBILITY_OPTIONS.map(o => {
                const OIcon = o.icon;
                return (
                  <button key={o.value} onClick={() => setStoryVisibility(o.value)}
                    className={`flex-1 flex items-center justify-center gap-1 py-1.5 rounded-lg text-xs font-medium border transition
                      ${storyVisibility === o.value ? 'border-[#1877f2] bg-blue-50 text-[#1877f2]' : 'border-gray-200 text-gray-600 hover:bg-gray-50'}`}>
                    <OIcon size={12} /> {o.label}
                  </button>
                );
              })}
            </div>
            <textarea
              value={caption}
              onChange={e => setCaption(e.target.value)}
              placeholder="Caption..."
              rows={3}
              className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none resize-none mb-3 focus:border-[#1877f2]"
            />
            <button
              onClick={handleCreate}
              disabled={submitting || (!imgUrl.trim() && !caption.trim())}
              className="w-full bg-[#1877f2] text-white font-semibold py-2 rounded-lg hover:bg-blue-600 transition disabled:opacity-50"
            >
              {submitting ? 'Đang đăng...' : 'Đăng story'}
            </button>
          </div>
        </div>
      )}

      {/* View story modal */}
      {viewing && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/90" onClick={() => setViewing(null)}>
          <div className="relative w-full max-w-sm h-[85vh] flex flex-col" onClick={e => e.stopPropagation()}>
            {/* Progress bar */}
            <div className="absolute top-2 left-2 right-2 z-10 h-1 bg-white/30 rounded-full overflow-hidden">
              <div className="h-full bg-white rounded-full transition-none" style={{ width: `${progress}%` }} />
            </div>

            {/* Author + controls */}
            <div className="absolute top-6 left-3 right-3 z-10 flex items-center gap-2">
              <Avatar name={viewing.author.fullName} src={viewing.author.avatarUrl} size={34} />
              <div className="flex-1 min-w-0">
                <p className="text-white font-semibold text-sm drop-shadow truncate">{viewing.author.fullName}</p>
                <p className="text-white/60 text-[11px]">{timeAgo(viewing.createdAt)}</p>
              </div>
              {/* View count (only shown to story owner) */}
              {viewing.author.id === user?.userId && viewing.viewCount > 0 && (
                <div className="flex items-center gap-1 bg-black/40 rounded-full px-2 py-0.5">
                  <Eye size={12} className="text-white" />
                  <span className="text-white text-[11px]">{viewing.viewCount}</span>
                </div>
              )}
              {viewing.author.id === user?.userId && (
                <button
                  onClick={() => handleDelete(viewing.id)}
                  className="w-7 h-7 bg-red-500/80 rounded-full flex items-center justify-center hover:bg-red-600"
                >
                  <Trash2 size={13} className="text-white" />
                </button>
              )}
              <button onClick={() => setViewing(null)} className="w-7 h-7 bg-black/40 rounded-full flex items-center justify-center">
                <X size={14} className="text-white" />
              </button>
            </div>

            {/* Story content */}
            <div className="flex-1 rounded-2xl overflow-hidden bg-black flex items-center justify-center">
              {viewing.imageUrl
                ? <img src={viewing.imageUrl} className="w-full h-full object-contain" alt="" />
                : <div className="w-full h-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center p-8">
                    <p className="text-white text-2xl font-bold text-center">{viewing.caption}</p>
                  </div>
              }
            </div>
            {viewing.imageUrl && viewing.caption && (
              <div className="absolute bottom-4 left-4 right-4 bg-black/40 rounded-xl px-4 py-2">
                <p className="text-white text-sm text-center">{viewing.caption}</p>
              </div>
            )}
          </div>
        </div>
      )}
    </>
  );
};

// ─── CREATE POST BOX ─────────────────────────────────────────────────────────
const CreatePostBox: React.FC<{ onCreated: (p: Post) => void }> = ({ onCreated }) => {
  const { user } = useAuth();
  const [open, setOpen]             = useState(false);
  const [content, setContent]       = useState('');
  const [mediaUrl, setMediaUrl]     = useState('');
  const [visibility, setVisibility] = useState<Visibility>('public');
  const [loading, setLoading]       = useState(false);
  const [uploading, setUploading]   = useState(false);
  const [showVis, setShowVis]       = useState(false);
  const mediaFileRef = useRef<HTMLInputElement>(null);

  const handleMediaUpload = async (file: File) => {
    setUploading(true);
    try {
      const res = await uploadApi.upload(file);
      if (res.success) setMediaUrl(res.url);
    } finally { setUploading(false); }
  };

  const submit = async () => {
    if (!content.trim()) return;
    setLoading(true);
    try {
      const { data } = await postsApi.create({
        content,
        imageUrl: mediaUrl || undefined,
        visibility,
        hashtags: extractHashtags(content),
      });
      if (data.success && data.data) {
        onCreated(data.data);
        setOpen(false); setContent(''); setMediaUrl(''); setVisibility('public'); setUploading(false);
      }
    } finally { setLoading(false); }
  };

  const mediaType = mediaUrl ? getMediaType(mediaUrl) : null;
  const visOpt    = VISIBILITY_OPTIONS.find(o => o.value === visibility)!;
  const VisIcon   = visOpt.icon;

  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-4 mb-4">
      <div className="flex gap-3 items-center">
        <Avatar name={user?.fullName ?? 'U'} src={user?.avatarUrl} size={40} />
        <button onClick={() => setOpen(true)}
          className="flex-1 bg-gray-100 hover:bg-gray-200 text-gray-500 text-left px-4 py-2.5 rounded-full text-sm transition">
          {user?.fullName} ơi, bạn đang nghĩ gì thế?
        </button>
      </div>
      <div className="flex gap-1 mt-3 pt-3 border-t border-gray-100">
        {[['Video trực tiếp', '#f02849', Video], ['Ảnh/video', '#45bd62', Image], ['Cảm xúc', '#f7b928', Smile]].map(([label, color, Icon]: any) => (
          <button key={label} onClick={() => setOpen(true)}
            className="flex-1 flex items-center justify-center gap-2 py-1.5 hover:bg-gray-100 rounded-lg text-sm font-medium text-gray-600 transition">
            <Icon size={18} style={{ color }} />
            <span className="hidden sm:inline">{label}</span>
          </button>
        ))}
      </div>

      {open && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white rounded-2xl w-full max-w-lg mx-4 shadow-2xl">
            <div className="flex items-center justify-between px-4 py-3 border-b">
              <div className="w-8" />
              <h2 className="font-semibold text-lg">Tạo bài viết</h2>
              <button onClick={() => setOpen(false)} className="w-8 h-8 bg-gray-100 rounded-full flex items-center justify-center hover:bg-gray-200">
                <X size={16} />
              </button>
            </div>
            <div className="p-4">
              <div className="flex items-center gap-3 mb-3">
                <Avatar name={user?.fullName ?? 'U'} src={user?.avatarUrl} size={40} />
                <div>
                  <p className="font-semibold text-sm">{user?.fullName}</p>
                  {/* Visibility picker */}
                  <div className="relative mt-0.5">
                    <button
                      onClick={() => setShowVis(v => !v)}
                      className="flex items-center gap-1 bg-gray-100 hover:bg-gray-200 px-2 py-0.5 rounded text-xs font-medium transition"
                    >
                      <VisIcon size={11} className={visOpt.color} />
                      <span>{visOpt.label}</span>
                    </button>
                    {showVis && (
                      <div className="absolute top-7 left-0 bg-white border border-gray-200 rounded-xl shadow-lg z-10 py-1 min-w-[160px]">
                        {VISIBILITY_OPTIONS.map(o => {
                          const OIcon = o.icon;
                          return (
                            <button key={o.value} onClick={() => { setVisibility(o.value); setShowVis(false); }}
                              className={`w-full flex items-center gap-3 px-3 py-2 text-sm hover:bg-gray-50 ${visibility === o.value ? 'bg-blue-50' : ''}`}>
                              <OIcon size={15} className={o.color} />
                              {o.label}
                              {visibility === o.value && <Check size={12} className="ml-auto text-[#1877f2]" />}
                            </button>
                          );
                        })}
                      </div>
                    )}
                  </div>
                </div>
              </div>

              <textarea
                value={content}
                onChange={e => setContent(e.target.value)}
                placeholder={`${user?.fullName} ơi, bạn đang nghĩ gì thế?`}
                className="w-full resize-none outline-none text-lg min-h-[120px] placeholder-gray-400"
                autoFocus
              />
              {extractHashtags(content).length > 0 && (
                <div className="flex flex-wrap gap-1 mt-1 mb-2">
                  {extractHashtags(content).map(t => (
                    <span key={t} className="bg-blue-50 text-[#1877f2] text-xs px-2 py-0.5 rounded-full">#{t}</span>
                  ))}
                </div>
              )}
              {/* Media preview */}
              {mediaUrl && mediaType === 'image' && (
                <div className="relative mt-2">
                  <img src={mediaUrl} alt="" className="rounded-lg w-full max-h-60 object-cover" />
                  <button onClick={() => setMediaUrl('')} className="absolute top-1 right-1 w-6 h-6 bg-black/50 rounded-full flex items-center justify-center"><X size={12} className="text-white" /></button>
                </div>
              )}
              {mediaUrl && mediaType === 'video' && (
                <div className="relative mt-2">
                  <video src={mediaUrl} controls className="rounded-lg w-full max-h-60" />
                  <button onClick={() => setMediaUrl('')} className="absolute top-1 right-1 w-6 h-6 bg-black/50 rounded-full flex items-center justify-center"><X size={12} className="text-white" /></button>
                </div>
              )}
              {mediaUrl && mediaType === 'youtube' && (
                <div className="relative mt-2">
                  <iframe src={`https://www.youtube.com/embed/${getYouTubeId(mediaUrl)}`} className="rounded-lg w-full h-48" allowFullScreen />
                  <button onClick={() => setMediaUrl('')} className="absolute top-1 right-1 w-6 h-6 bg-black/50 rounded-full flex items-center justify-center"><X size={12} className="text-white" /></button>
                </div>
              )}
              <div className="mt-3 border border-gray-200 rounded-lg flex items-center gap-2 px-3 py-2">
                <Image size={16} className="text-gray-400 shrink-0" />
                <input
                  value={mediaUrl}
                  onChange={e => setMediaUrl(e.target.value)}
                  placeholder="Dán link ảnh hoặc video (YouTube, .mp4...)"
                  className="flex-1 text-sm outline-none"
                />
                {mediaUrl && (
                  <button onClick={() => setMediaUrl('')} className="text-gray-400 hover:text-gray-600"><X size={14} /></button>
                )}
              </div>
              <input ref={mediaFileRef} type="file" accept="image/*,video/*" className="hidden"
                onChange={e => e.target.files?.[0] && handleMediaUpload(e.target.files[0])} />
              <button type="button" onClick={() => mediaFileRef.current?.click()}
                disabled={uploading}
                className="mt-2 w-full flex items-center justify-center gap-2 border border-dashed border-gray-300 hover:border-[#1877f2] hover:bg-blue-50 text-gray-500 hover:text-[#1877f2] py-2 rounded-lg text-sm font-medium transition disabled:opacity-50">
                <Upload size={15} /> {uploading ? 'Đang tải lên...' : 'Tải ảnh/video từ máy tính'}
              </button>
              <p className="text-[11px] text-gray-400 mt-1 ml-1">Dùng # trong nội dung để thêm hashtag</p>
            </div>
            <div className="px-4 pb-4">
              <button onClick={submit} disabled={!content.trim() || loading}
                className="w-full bg-[#1877f2] text-white font-semibold py-2 rounded-lg disabled:opacity-50 hover:bg-blue-600 transition">
                {loading ? 'Đang đăng...' : 'Đăng'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

// ─── COMMENT SECTION ──────────────────────────────────────────────────────────
const CommentSection: React.FC<{ postId: number }> = ({ postId }) => {
  const { user } = useAuth();
  const [comments, setComments]   = useState<Comment[]>([]);
  const [text, setText]           = useState('');
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editText, setEditText]   = useState('');

  useEffect(() => {
    commentsApi.getAll(postId).then(r => { if (r.data) setComments(r.data); });
  }, [postId]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!text.trim()) return;
    const { data } = await commentsApi.create(postId, text);
    if (data.success && data.data) { setComments(p => [...p, data.data!]); setText(''); }
  };

  const saveEdit = async (cid: number) => {
    if (!editText.trim()) return;
    const { data } = await commentsApi.update(postId, cid, editText);
    if (data.success && data.data) {
      setComments(p => p.map(c => c.id === cid ? { ...c, content: data.data!.content } : c));
      setEditingId(null);
    }
  };

  const handleDelete = async (cid: number) => {
    await commentsApi.delete(postId, cid);
    setComments(p => p.filter(c => c.id !== cid));
  };

  return (
    <div className="px-4 pb-3 border-t border-gray-100 mt-2 pt-3">
      {comments.map(c => {
        const isOwner = c.author.id === user?.userId;
        return (
          <div key={c.id} className="flex gap-2 mb-3">
            <Link to={`/profile/${c.author.id}`} className="shrink-0">
              <Avatar name={c.author.fullName} src={c.author.avatarUrl} size={32} />
            </Link>
            <div className="flex-1">
              <div className="bg-gray-100 rounded-2xl px-3 py-2 inline-block max-w-[90%]">
                <Link to={`/profile/${c.author.id}`} className="font-semibold text-xs hover:underline">{c.author.fullName}</Link>
                {editingId === c.id ? (
                  <div className="mt-1 flex gap-1 items-center">
                    <input
                      value={editText}
                      onChange={e => setEditText(e.target.value)}
                      className="text-sm bg-white border border-gray-300 rounded px-2 py-0.5 outline-none flex-1"
                      autoFocus
                      onKeyDown={e => { if (e.key === 'Enter') saveEdit(c.id); if (e.key === 'Escape') setEditingId(null); }}
                    />
                    <button onClick={() => saveEdit(c.id)} className="text-[#1877f2]"><Check size={14} /></button>
                    <button onClick={() => setEditingId(null)} className="text-gray-400"><X size={14} /></button>
                  </div>
                ) : (
                  <p className="text-sm mt-0.5"><ContentWithHashtags text={c.content} /></p>
                )}
              </div>
              {/* Timestamp + actions */}
              <div className="flex items-center gap-3 mt-0.5 ml-1">
                <span className="text-[10px] text-gray-400">{timeAgo(c.createdAt)}</span>
                {isOwner && editingId !== c.id && (
                  <>
                    <button onClick={() => { setEditingId(c.id); setEditText(c.content); }}
                      className="text-[10px] text-gray-500 hover:text-[#1877f2] font-medium">Chỉnh sửa</button>
                    <button onClick={() => handleDelete(c.id)}
                      className="text-[10px] text-gray-500 hover:text-red-500 font-medium">Xóa</button>
                  </>
                )}
              </div>
            </div>
          </div>
        );
      })}
      <form onSubmit={submit} className="flex gap-2 mt-1 items-center">
        <Avatar name={user?.fullName ?? 'U'} src={user?.avatarUrl} size={32} />
        <div className="flex-1 flex items-center bg-gray-100 rounded-full px-3">
          <input value={text} onChange={e => setText(e.target.value)} placeholder="Viết bình luận..."
            className="flex-1 bg-transparent py-2 text-sm outline-none" />
          <button type="submit" className="text-[#1877f2]"><Send size={16} /></button>
        </div>
      </form>
    </div>
  );
};

// ─── POST CARD ────────────────────────────────────────────────────────────────
const PostCard: React.FC<{
  post: Post;
  currentUserId: string;
  onLike: (id: number) => void;
  onDelete: (id: number) => void;
  onUpdate: (id: number, content: string) => void;
  onToast: (msg: string) => void;
}> = ({ post, currentUserId, onLike, onDelete, onUpdate, onToast }) => {
  const [showComments, setShowComments]   = useState(false);
  const [showMenu, setShowMenu]           = useState(false);
  const [editing, setEditing]             = useState(false);
  const [editContent, setEditContent]     = useState(post.content);
  const [saving, setSaving]               = useState(false);
  const [showReport, setShowReport]       = useState(false);
  const [reportReason, setReportReason]   = useState('');
  const [reportSending, setReportSending] = useState(false);
  const isOwner  = post.author.id === currentUserId;
  const canEditPost = isOwner && canEdit(post.createdAt);

  const handleReport = async () => {
    if (!reportReason.trim()) return;
    setReportSending(true);
    try {
      await reportsApi.report(post.id, reportReason);
      onToast('Đã gửi báo cáo thành công!');
      setShowReport(false);
      setReportReason('');
    } catch {
      onToast('Gửi báo cáo thất bại, thử lại sau.');
    } finally { setReportSending(false); }
  };

  const mediaType = post.imageUrl ? getMediaType(post.imageUrl) : null;

  const handleShare = async () => {
    try {
      await navigator.clipboard.writeText(`${window.location.origin}/post/${post.id}`);
      onToast('Đã sao chép liên kết!');
    } catch { onToast('Không thể sao chép liên kết.'); }
    setShowMenu(false);
  };

  const handleSaveEdit = async () => {
    if (!editContent.trim()) return;
    setSaving(true);
    try {
      await postsApi.create; // dummy ref — actual call below
      // Use the update endpoint via http directly
      const { data } = await import('../services/api').then(m =>
        m.http.put<import('../types').ApiResult<Post>>(`/posts/${post.id}`, { content: editContent })
      );
      if (data.success) {
        onUpdate(post.id, editContent);
        setEditing(false);
      }
    } finally { setSaving(false); }
  };

  const visOpt = VISIBILITY_OPTIONS.find(o => o.value === (post.visibility ?? 'public'))!;
  const VisIcon = visOpt?.icon ?? Globe;

  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-200 mb-4">
      {/* Header */}
      <div className="flex items-start justify-between px-4 pt-4 pb-2">
        <div className="flex items-center gap-3">
          <Link to={`/profile/${post.author.id}`}>
            <Avatar name={post.author.fullName} src={post.author.avatarUrl} size={40} />
          </Link>
          <div>
            <Link to={`/profile/${post.author.id}`} className="font-semibold text-sm hover:underline leading-tight">
              {post.author.fullName}
            </Link>
            <p className="text-xs text-gray-500 flex items-center gap-1">
              {timeAgo(post.createdAt)}
              {post.updatedAt && <span className="italic">(đã sửa)</span>}
              · <VisIcon size={10} className={visOpt?.color} />
              {post.visibility === 'friends' && <span>Bạn bè</span>}
              {post.visibility === 'private' && <span>Riêng tư</span>}
            </p>
          </div>
        </div>
        <div className="relative">
          <button onClick={() => setShowMenu(v => !v)}
            className="w-9 h-9 rounded-full hover:bg-gray-100 flex items-center justify-center text-gray-500">
            <MoreHorizontal size={20} />
          </button>
          {showMenu && (
            <div className="absolute right-0 top-10 bg-white border border-gray-200 rounded-xl shadow-lg z-10 py-1 min-w-[180px]">
              <button onClick={handleShare}
                className="w-full flex items-center gap-3 px-4 py-2 text-sm hover:bg-gray-50">
                <Copy size={14} className="text-gray-500" /> Sao chép liên kết
              </button>
              {canEditPost && (
                <button onClick={() => { setEditing(true); setShowMenu(false); }}
                  className="w-full flex items-center gap-3 px-4 py-2 text-sm hover:bg-gray-50">
                  <Pencil size={14} className="text-gray-500" /> Chỉnh sửa bài viết
                </button>
              )}
              {isOwner && (
                <button onClick={() => { onDelete(post.id); setShowMenu(false); }}
                  className="w-full flex items-center gap-3 px-4 py-2 text-sm text-red-600 hover:bg-red-50">
                  <Trash2 size={14} /> Xóa bài viết
                </button>
              )}
              {!isOwner && (
                <button onClick={() => { setShowReport(true); setShowMenu(false); }}
                  className="w-full flex items-center gap-3 px-4 py-2 text-sm text-orange-600 hover:bg-orange-50">
                  <Flag size={14} /> Báo cáo bài viết
                </button>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Content */}
      <div className="px-4 pb-3">
        {editing ? (
          <div>
            <textarea
              value={editContent}
              onChange={e => setEditContent(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm outline-none resize-none focus:border-[#1877f2]"
              rows={4}
              autoFocus
            />
            <div className="flex gap-2 mt-2">
              <button onClick={handleSaveEdit} disabled={saving || !editContent.trim()}
                className="flex items-center gap-1 bg-[#1877f2] text-white px-4 py-1.5 rounded-lg text-sm font-medium disabled:opacity-50">
                <Check size={13} /> {saving ? 'Đang lưu...' : 'Lưu'}
              </button>
              <button onClick={() => { setEditing(false); setEditContent(post.content); }}
                className="flex items-center gap-1 bg-gray-200 px-4 py-1.5 rounded-lg text-sm font-medium">
                <X size={13} /> Hủy
              </button>
            </div>
          </div>
        ) : (
          <p className="text-sm leading-relaxed whitespace-pre-wrap">
            <ContentWithHashtags text={post.content} />
          </p>
        )}
      </div>

      {/* Media */}
      {post.imageUrl && mediaType === 'image' && (
        <img src={post.imageUrl} alt="" className="w-full max-h-[500px] object-cover" />
      )}
      {post.imageUrl && mediaType === 'video' && (
        <video src={post.imageUrl} controls className="w-full max-h-[500px] bg-black" />
      )}
      {post.imageUrl && mediaType === 'youtube' && (
        <iframe src={`https://www.youtube.com/embed/${getYouTubeId(post.imageUrl)}`} className="w-full h-64" allowFullScreen />
      )}

      {/* Counts */}
      {(post.likeCount > 0 || post.commentCount > 0) && (
        <div className="flex items-center justify-between px-4 py-2 text-xs text-gray-500">
          {post.likeCount > 0 && (
            <div className="flex items-center gap-1">
              <span className="w-4 h-4 bg-[#1877f2] rounded-full flex items-center justify-center">
                <ThumbsUp size={9} className="text-white" />
              </span>
              {post.likeCount}
            </div>
          )}
          {post.commentCount > 0 && (
            <button onClick={() => setShowComments(v => !v)} className="ml-auto hover:underline">
              {post.commentCount} bình luận
            </button>
          )}
        </div>
      )}

      {/* Actions */}
      <div className="flex border-t border-gray-100 mx-4">
        {[
          { icon: ThumbsUp,      label: 'Thích',     active: post.isLikedByCurrentUser, color: '#1877f2', action: () => onLike(post.id) },
          { icon: MessageCircle, label: 'Bình luận', active: false, color: '#606770',   action: () => setShowComments(v => !v) },
          { icon: Share2,        label: 'Chia sẻ',   active: false, color: '#606770',   action: handleShare },
        ].map(({ icon: Icon, label, active, color, action }) => (
          <button key={label} onClick={action}
            className="flex-1 flex items-center justify-center gap-2 py-1.5 hover:bg-gray-100 rounded-lg text-sm font-medium transition"
            style={{ color: active ? color : '#606770' }}>
            <Icon size={18} fill={active ? color : 'none'} />
            <span className="hidden sm:inline">{label}</span>
          </button>
        ))}
      </div>

      {showComments && <CommentSection postId={post.id} />}

      {showReport && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white rounded-2xl w-full max-w-sm mx-4 shadow-2xl p-5">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-bold text-lg flex items-center gap-2">
                <Flag size={18} className="text-orange-500" /> Báo cáo bài viết
              </h2>
              <button onClick={() => { setShowReport(false); setReportReason(''); }}
                className="w-8 h-8 bg-gray-100 rounded-full flex items-center justify-center hover:bg-gray-200">
                <X size={16} />
              </button>
            </div>
            <p className="text-sm text-gray-500 mb-3">Cho chúng tôi biết lý do bạn báo cáo bài viết này.</p>
            <textarea
              value={reportReason}
              onChange={e => setReportReason(e.target.value)}
              placeholder="Nhập lý do báo cáo..."
              rows={3}
              className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none resize-none mb-4 focus:border-[#1877f2]"
            />
            <div className="flex gap-2">
              <button onClick={() => { setShowReport(false); setReportReason(''); }}
                className="flex-1 py-2 rounded-lg border border-gray-200 text-sm font-medium hover:bg-gray-50">
                Hủy
              </button>
              <button onClick={handleReport} disabled={!reportReason.trim() || reportSending}
                className="flex-1 py-2 rounded-lg bg-orange-500 text-white text-sm font-medium hover:bg-orange-600 disabled:opacity-50 transition">
                {reportSending ? 'Đang gửi...' : 'Gửi báo cáo'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

// ─── HOME PAGE ────────────────────────────────────────────────────────────────
const HomePage: React.FC = () => {
  const { user }    = useAuth();
  const [posts, setPosts]       = useState<Post[]>([]);
  const [page, setPage]         = useState(1);
  const [hasMore, setHasMore]   = useState(true);
  const [loading, setLoading]   = useState(false);
  const [newCount, setNewCount] = useState(0);
  const [toast, setToast]       = useState('');
  const loaderRef               = useRef<HTMLDivElement>(null);
  const latestIdRef             = useRef<number>(0);

  const loadPosts = useCallback(async (p: number, reset = false) => {
    if (loading) return;
    setLoading(true);
    try {
      const data = await postsApi.getFeed(p);
      if (reset) {
        setPosts(data.items);
        if (data.items.length > 0) latestIdRef.current = data.items[0].id;
      } else {
        setPosts(prev => [...prev, ...data.items]);
      }
      setHasMore(data.hasNext);
    } finally { setLoading(false); }
  }, [loading]);

  useEffect(() => { loadPosts(1, true); }, []);

  // Infinite scroll
  useEffect(() => {
    const el = loaderRef.current;
    if (!el) return;
    const obs = new IntersectionObserver(entries => {
      if (entries[0].isIntersecting && hasMore && !loading) {
        const next = page + 1;
        setPage(next);
        loadPosts(next);
      }
    }, { threshold: 0.1 });
    obs.observe(el);
    return () => obs.disconnect();
  }, [hasMore, loading, page]);

  // Polling for new posts every 30s
  useEffect(() => {
    const id = setInterval(async () => {
      try {
        const data = await postsApi.getFeed(1);
        if (data.items.length > 0 && data.items[0].id > latestIdRef.current && latestIdRef.current > 0) {
          setNewCount(data.items.filter(p => p.id > latestIdRef.current).length);
        }
      } catch { /* silent */ }
    }, 30000);
    return () => clearInterval(id);
  }, []);

  const loadNewPosts = async () => {
    setNewCount(0);
    setPage(1);
    await loadPosts(1, true);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handleLike = async (postId: number) => {
    setPosts(prev => prev.map(p => p.id === postId
      ? { ...p, isLikedByCurrentUser: !p.isLikedByCurrentUser, likeCount: p.isLikedByCurrentUser ? p.likeCount - 1 : p.likeCount + 1 }
      : p));
    await postsApi.toggleLike(postId);
  };

  const handleDelete = async (postId: number) => {
    await postsApi.delete(postId);
    setPosts(prev => prev.filter(p => p.id !== postId));
  };

  const handleUpdate = (postId: number, content: string) => {
    setPosts(prev => prev.map(p => p.id === postId ? { ...p, content, updatedAt: new Date().toISOString() } : p));
  };

  return (
    <div className="max-w-[680px] mx-auto px-2 sm:px-0 py-4">
      {newCount > 0 && (
        <button onClick={loadNewPosts}
          className="w-full mb-3 bg-[#1877f2] text-white text-sm font-semibold py-2.5 rounded-xl shadow hover:bg-blue-600 transition">
          {newCount} bài viết mới — Nhấn để tải
        </button>
      )}

      <StoryBar />
      <CreatePostBox onCreated={p => { setPosts(prev => [p, ...prev]); latestIdRef.current = p.id; }} />

      {posts.map(p => (
        <PostCard
          key={p.id} post={p}
          currentUserId={user?.userId ?? ''}
          onLike={handleLike}
          onDelete={handleDelete}
          onUpdate={handleUpdate}
          onToast={setToast}
        />
      ))}

      <div ref={loaderRef} className="h-10 flex items-center justify-center">
        {loading && <div className="w-6 h-6 border-2 border-[#1877f2] border-t-transparent rounded-full animate-spin" />}
        {!hasMore && posts.length > 0 && <p className="text-xs text-gray-400">Bạn đã xem hết bài viết</p>}
      </div>

      {toast && <Toast msg={toast} onDone={() => setToast('')} />}
    </div>
  );
};

export default HomePage;
