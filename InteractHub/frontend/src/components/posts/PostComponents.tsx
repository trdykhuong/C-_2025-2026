import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Heart, MessageCircle, Trash2, MoreHorizontal, Flag } from 'lucide-react';
import type { Post, Comment } from '../../types';
import { useAuth } from '../../contexts/AuthContext';
import { useComments } from '../../hooks';
import { Avatar, Button, Card } from '../ui';
import { postsApi, api } from '../../services/api';

// ─── POST CARD ────────────────────────────────────────────────────────────────
interface PostCardProps {
  post: Post;
  onLike: (id: number) => void;
  onDelete: (id: number) => void;
}

export const PostCard: React.FC<PostCardProps> = ({ post, onLike, onDelete }) => {
  const { user } = useAuth();
  const [showComments, setShowComments] = useState(false);
  const [showMenu, setShowMenu] = useState(false);
  const isOwner = user?.userId === post.author.id;

  const timeAgo = (date: string) => {
    const diff = Date.now() - new Date(date).getTime();
    const m = Math.floor(diff / 60000);
    if (m < 1) return 'just now';
    if (m < 60) return `${m}m ago`;
    const h = Math.floor(m / 60);
    if (h < 24) return `${h}h ago`;
    return `${Math.floor(h / 24)}d ago`;
  };

  return (
    <Card className="p-4">
      {/* Header */}
      <div className="flex items-start justify-between mb-3">
        <Link to={`/profile/${post.author.id}`} className="flex items-center gap-3 group">
          <Avatar src={post.author.avatarUrl} name={post.author.fullName} size="md" />
          <div>
            <p className="font-semibold text-sm group-hover:text-blue-600 transition">{post.author.fullName}</p>
            <p className="text-xs text-gray-400">@{post.author.userName} · {timeAgo(post.createdAt)}</p>
          </div>
        </Link>

        {isOwner && (
          <div className="relative">
            <button onClick={() => setShowMenu(v => !v)} className="p-1 rounded hover:bg-gray-100 text-gray-400">
              <MoreHorizontal size={18} />
            </button>
            {showMenu && (
              <div className="absolute right-0 mt-1 bg-white rounded-lg shadow-lg border border-gray-100 z-10 min-w-[120px]">
                <button
                  onClick={() => { onDelete(post.id); setShowMenu(false); }}
                  className="w-full flex items-center gap-2 px-3 py-2 text-sm text-red-600 hover:bg-red-50"
                >
                  <Trash2 size={14} /> Delete
                </button>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Content */}
      <p className="text-sm text-gray-800 leading-relaxed mb-3">{post.content}</p>

      {/* Image */}
      {post.imageUrl && (
        <img src={post.imageUrl} alt="post" className="w-full rounded-lg mb-3 max-h-80 object-cover" />
      )}

      {/* Hashtags */}
      {post.hashtags.length > 0 && (
        <div className="flex flex-wrap gap-1 mb-3">
          {post.hashtags.map(tag => (
            <span key={tag} className="text-xs text-blue-600 bg-blue-50 px-2 py-0.5 rounded-full">#{tag}</span>
          ))}
        </div>
      )}

      {/* Actions */}
      <div className="flex items-center gap-4 pt-3 border-t border-gray-100">
        <button
          onClick={() => onLike(post.id)}
          className={`flex items-center gap-1.5 text-sm transition ${post.isLikedByCurrentUser ? 'text-red-500' : 'text-gray-500 hover:text-red-400'}`}
        >
          <Heart size={18} fill={post.isLikedByCurrentUser ? 'currentColor' : 'none'} />
          <span>{post.likeCount}</span>
        </button>

        <button
          onClick={() => setShowComments(v => !v)}
          className="flex items-center gap-1.5 text-sm text-gray-500 hover:text-blue-500 transition"
        >
          <MessageCircle size={18} />
          <span>{post.commentCount}</span>
        </button>
      </div>

      {/* Comments section */}
      {showComments && <CommentsSection postId={post.id} />}
    </Card>
  );
};

// ─── COMMENTS SECTION ─────────────────────────────────────────────────────────
const CommentsSection: React.FC<{ postId: number }> = ({ postId }) => {
  const { user } = useAuth();
  const { comments, loading, addComment, removeComment } = useComments(postId);
  const [text, setText] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!text.trim()) return;
    setSubmitting(true);
    await addComment(text.trim());
    setText('');
    setSubmitting(false);
  };

  return (
    <div className="mt-3 pt-3 border-t border-gray-100">
      {loading ? (
        <p className="text-xs text-gray-400">Loading comments...</p>
      ) : (
        <div className="space-y-3 mb-3">
          {comments.map(c => (
            <CommentItem key={c.id} comment={c} currentUserId={user?.userId ?? ''} onDelete={removeComment} />
          ))}
        </div>
      )}

      <form onSubmit={handleSubmit} className="flex gap-2">
        <Avatar src={user?.avatarUrl} name={user?.fullName ?? 'U'} size="xs" />
        <input
          value={text}
          onChange={e => setText(e.target.value)}
          placeholder="Write a comment..."
          className="flex-1 text-sm bg-gray-100 rounded-full px-4 py-1.5 outline-none focus:ring-2 focus:ring-blue-200"
        />
        <Button type="submit" size="sm" loading={submitting} disabled={!text.trim()}>Post</Button>
      </form>
    </div>
  );
};

const CommentItem: React.FC<{ comment: Comment; currentUserId: string; onDelete: (id: number) => void }> = ({ comment, currentUserId, onDelete }) => (
  <div className="flex items-start gap-2">
    <Avatar src={comment.author.avatarUrl} name={comment.author.fullName} size="xs" />
    <div className="flex-1 bg-gray-50 rounded-xl px-3 py-2">
      <p className="text-xs font-semibold">{comment.author.fullName}</p>
      <p className="text-sm text-gray-700">{comment.content}</p>
    </div>
    {comment.author.id === currentUserId && (
      <button onClick={() => onDelete(comment.id)} className="text-gray-300 hover:text-red-400 mt-1">
        <Trash2 size={12} />
      </button>
    )}
  </div>
);

// ─── CREATE POST FORM ─────────────────────────────────────────────────────────
interface CreatePostFormProps {
  onCreated: (post: Post) => void;
}

export const CreatePostForm: React.FC<CreatePostFormProps> = ({ onCreated }) => {
  const { user } = useAuth();
  const [content, setContent] = useState('');
  const [imageUrl, setImageUrl] = useState('');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [hashtags, setHashtags] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim()) { setError('Post content is required.'); return; }
    setLoading(true);
    setError('');
    try {
      const tags = hashtags.split(/[\s,#]+/).filter(t => t.trim().length > 0);
      const { data } = await postsApi.create({ content: content.trim(), imageUrl: imageUrl || undefined, hashtags: tags });
      if (data.success && data.data) {
        onCreated(data.data);
        setContent('');
        setImageUrl('');
        setHashtags('');
      }
    } catch {
      setError('Failed to create post.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card className="p-4">
      <form onSubmit={handleSubmit}>
        <div className="flex gap-3">
          <Avatar src={user?.avatarUrl} name={user?.fullName ?? 'U'} />
          <div className="flex-1">
            <textarea
              value={content}
              onChange={e => setContent(e.target.value)}
              placeholder="What's on your mind?"
              rows={3}
              className="w-full text-sm resize-none outline-none placeholder-gray-400"
            />
            {imageUrl && (
              <img src={imageUrl} alt="preview" className="mt-2 w-full max-h-48 object-cover rounded-lg" />
            )}
            {selectedFile && previewUrl && (
              (selectedFile.type.startsWith('image/') ? (
                <img src={previewUrl} alt="preview" className="mt-2 w-full max-h-48 object-cover rounded-lg" />
              ) : (
                <video src={previewUrl} className="mt-2 w-full max-h-48 object-cover rounded-lg" controls />
              ))
            )}
          </div>
        </div>

        {error && <p className="text-xs text-red-500 mt-2">{error}</p>}

        <div className="flex items-center gap-2 mt-3 pt-3 border-t border-gray-100">
          <input
            value={imageUrl}
            onChange={e => setImageUrl(e.target.value)}
            placeholder="Image URL (optional)"
            className="flex-1 text-xs bg-gray-50 border border-gray-200 rounded-lg px-3 py-1.5 outline-none"
          />
          <div className="flex items-center gap-2">
            <input
              type="file"
              accept="image/*,video/*"
              onChange={e => setSelectedFile(e.target.files ? e.target.files[0] : null)}
              className="text-xs"
            />
            {selectedFile && (
              <button type="button" onClick={() => setSelectedFile(null)} className="text-xs text-red-500 hover:underline">Remove</button>
            )}
          </div>
          <input
            value={hashtags}
            onChange={e => setHashtags(e.target.value)}
            placeholder="#tags"
            className="w-32 text-xs bg-gray-50 border border-gray-200 rounded-lg px-3 py-1.5 outline-none"
          />
          <Button type="submit" size="sm" loading={loading} disabled={!content.trim()}>Post</Button>
        </div>
      </form>
    </Card>
  );
};
