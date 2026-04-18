import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Globe, ThumbsUp, MessageCircle, Share2, Flag, X } from 'lucide-react';
import { postsApi, reportsApi } from '../services/api';
import type { Post } from '../types';
import { useAuth } from '../contexts/AuthContext';
import { Avatar } from '../components/ui/Avatar';

function timeAgo(d: string) {
  const utc = /Z|[+-]\d{2}:?\d{2}$/.test(d) ? d : d + 'Z';
  const diff = Math.floor((Date.now() - new Date(utc).getTime()) / 60000);
  if (diff < 1)  return 'Vừa xong';
  if (diff < 60) return `${diff} phút`;
  const h = Math.floor(diff / 60);
  if (h < 24)    return `${h} giờ`;
  return `${Math.floor(h / 24)} ngày`;
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

const HashtagPage: React.FC = () => {
  const { tag }   = useParams<{ tag: string }>();
  const { user }  = useAuth();
  const [posts, setPosts]     = useState<Post[]>([]);
  const [page, setPage]       = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [loading, setLoading]         = useState(false);
  const [reportPostId, setReportPostId] = useState<number | null>(null);
  const [reportReason, setReportReason] = useState('');
  const [reportSending, setReportSending] = useState(false);
  const [toast, setToast]               = useState('');
  const loaderRef                       = useRef<HTMLDivElement>(null);

  const loadPosts = useCallback(async (p: number, reset = false) => {
    if (!tag || loading) return;
    setLoading(true);
    try {
      const data = await postsApi.getByHashtag(tag, p);
      if (reset) setPosts(data.items);
      else setPosts(prev => [...prev, ...data.items]);
      setHasMore(data.hasNext);
    } finally { setLoading(false); }
  }, [tag, loading]);

  useEffect(() => {
    setPosts([]);
    setPage(1);
    setHasMore(true);
    loadPosts(1, true);
  }, [tag]);

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

  const handleReport = async () => {
    if (!reportPostId || !reportReason.trim()) return;
    setReportSending(true);
    try {
      await reportsApi.report(reportPostId, reportReason);
      setToast('Đã gửi báo cáo thành công!');
      setReportPostId(null);
      setReportReason('');
    } catch {
      setToast('Gửi báo cáo thất bại.');
    } finally { setReportSending(false); }
  };

  const handleLike = async (postId: number) => {
    setPosts(prev => prev.map(p => p.id === postId
      ? { ...p, isLikedByCurrentUser: !p.isLikedByCurrentUser, likeCount: p.isLikedByCurrentUser ? p.likeCount - 1 : p.likeCount + 1 }
      : p));
    await postsApi.toggleLike(postId);
  };

  return (
    <div className="max-w-[680px] mx-auto px-2 sm:px-0 py-4">
      {/* Header */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-5 mb-4">
        <div className="flex items-center gap-3">
          <div className="w-14 h-14 bg-[#1877f2] rounded-full flex items-center justify-center text-white text-2xl font-bold">#</div>
          <div>
            <h1 className="text-2xl font-bold">#{tag}</h1>
            <p className="text-sm text-gray-500">{posts.length > 0 ? `${posts.length}+ bài viết` : 'Đang tải...'}</p>
          </div>
        </div>
      </div>

      {/* Posts */}
      {posts.length === 0 && !loading && (
        <div className="text-center py-16 text-gray-400">
          <p className="text-lg font-medium mb-1">Chưa có bài viết nào</p>
          <p className="text-sm">với hashtag #{tag}</p>
        </div>
      )}

      {posts.map(post => (
        <div key={post.id} className="bg-white rounded-xl shadow-sm border border-gray-200 mb-4">
          <div className="flex items-start justify-between px-4 pt-4 pb-2">
            <div className="flex items-center gap-3">
              <Link to={`/profile/${post.author.id}`}>
                <Avatar name={post.author.fullName} src={post.author.avatarUrl} size={40} />
              </Link>
              <div>
                <Link to={`/profile/${post.author.id}`} className="font-semibold text-sm hover:underline">
                  {post.author.fullName}
                </Link>
                <p className="text-xs text-gray-500 flex items-center gap-1">
                  {timeAgo(post.createdAt)} · <Globe size={10} />
                </p>
              </div>
            </div>
          </div>

          <div className="px-4 pb-3">
            <p className="text-sm leading-relaxed whitespace-pre-wrap">
              {post.content.split(/(#[\wÀ-ỹ]+)/g).map((part, i) =>
                /^#[\wÀ-ỹ]+$/.test(part)
                  ? <Link key={i} to={`/hashtag/${part.slice(1).toLowerCase()}`}
                      className="text-[#1877f2] font-medium hover:underline">{part}</Link>
                  : <span key={i}>{part}</span>
              )}
            </p>
          </div>

          {post.imageUrl && (() => {
            const t = getMediaType(post.imageUrl!);
            if (t === 'image')   return <img src={post.imageUrl} alt="" className="w-full max-h-[400px] object-cover" />;
            if (t === 'video')   return <video src={post.imageUrl} controls className="w-full max-h-[400px] bg-black" />;
            if (t === 'youtube') return <iframe src={`https://www.youtube.com/embed/${getYouTubeId(post.imageUrl!)}`} className="w-full h-64" allowFullScreen />;
          })()}

          <div className="flex border-t border-gray-100 mx-4">
            <button onClick={() => handleLike(post.id)}
              className="flex-1 flex items-center justify-center gap-2 py-1.5 hover:bg-gray-100 rounded-lg text-sm font-medium transition"
              style={{ color: post.isLikedByCurrentUser ? '#1877f2' : '#606770' }}>
              <ThumbsUp size={18} fill={post.isLikedByCurrentUser ? '#1877f2' : 'none'} />
              <span className="hidden sm:inline">Thích</span>
              {post.likeCount > 0 && <span className="text-xs">({post.likeCount})</span>}
            </button>
            <button className="flex-1 flex items-center justify-center gap-2 py-1.5 hover:bg-gray-100 rounded-lg text-sm font-medium text-gray-500 transition">
              <MessageCircle size={18} />
              <span className="hidden sm:inline">Bình luận</span>
              {post.commentCount > 0 && <span className="text-xs">({post.commentCount})</span>}
            </button>
            <button className="flex-1 flex items-center justify-center gap-2 py-1.5 hover:bg-gray-100 rounded-lg text-sm font-medium text-gray-500 transition">
              <Share2 size={18} />
              <span className="hidden sm:inline">Chia sẻ</span>
            </button>
            {post.author.id !== user?.userId && (
              <button onClick={() => { setReportPostId(post.id); setReportReason(''); }}
                title="Báo cáo bài viết"
                className="flex items-center justify-center gap-1 px-3 py-1.5 hover:bg-orange-50 rounded-lg text-sm font-medium text-gray-400 hover:text-orange-500 transition">
                <Flag size={16} />
                <span className="hidden sm:inline">Báo cáo</span>
              </button>
            )}
          </div>
        </div>
      ))}

      <div ref={loaderRef} className="h-10 flex items-center justify-center">
        {loading && <div className="w-6 h-6 border-2 border-[#1877f2] border-t-transparent rounded-full animate-spin" />}
        {!hasMore && posts.length > 0 && <p className="text-xs text-gray-400">Đã hiển thị tất cả bài viết</p>}
      </div>

      {/* Report modal */}
      {reportPostId !== null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white rounded-2xl w-full max-w-sm mx-4 shadow-2xl p-5">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-bold text-lg flex items-center gap-2">
                <Flag size={18} className="text-orange-500" /> Báo cáo bài viết
              </h2>
              <button onClick={() => setReportPostId(null)}
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
              <button onClick={() => setReportPostId(null)}
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

      {/* Toast */}
      {toast && (
        <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-[100] bg-gray-800 text-white px-5 py-2.5 rounded-full text-sm shadow-lg">
          {toast}
        </div>
      )}
    </div>
  );
};

export default HashtagPage;
