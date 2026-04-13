import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Globe, ThumbsUp, MessageCircle, Share2 } from 'lucide-react';
import { postsApi } from '../services/api';
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

const HashtagPage: React.FC = () => {
  const { tag }   = useParams<{ tag: string }>();
  const { user }  = useAuth();
  const [posts, setPosts]     = useState<Post[]>([]);
  const [page, setPage]       = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [loading, setLoading] = useState(false);
  const loaderRef             = useRef<HTMLDivElement>(null);

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

          {post.imageUrl && (
            <img src={post.imageUrl} alt="" className="w-full max-h-[400px] object-cover" />
          )}

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
          </div>
        </div>
      ))}

      <div ref={loaderRef} className="h-10 flex items-center justify-center">
        {loading && <div className="w-6 h-6 border-2 border-[#1877f2] border-t-transparent rounded-full animate-spin" />}
        {!hasMore && posts.length > 0 && <p className="text-xs text-gray-400">Đã hiển thị tất cả bài viết</p>}
      </div>
    </div>
  );
};

export default HashtagPage;
