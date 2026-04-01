import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { postsApi } from '../services/api';
import type { Post } from '../types';
import { PostCard } from '../components/posts/PostComponents';
import { PostSkeleton, Button } from '../components/ui';

const PostDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [post, setPost] = useState<Post | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    postsApi.getById(Number(id))
      .then(({ data }) => {
        if (data.success && data.data) setPost(data.data);
        else setError('Post not found.');
      })
      .catch(() => setError('Failed to load post.'))
      .finally(() => setLoading(false));
  }, [id]);

  const toggleLike = async (postId: number) => {
    setPost(prev => prev ? {
      ...prev,
      isLikedByCurrentUser: !prev.isLikedByCurrentUser,
      likeCount: prev.isLikedByCurrentUser ? prev.likeCount - 1 : prev.likeCount + 1
    } : prev);
    await postsApi.toggleLike(postId);
  };

  const deletePost = async (postId: number) => {
    await postsApi.delete(postId);
    navigate('/');
  };

  return (
    <div className="max-w-2xl mx-auto px-4 py-4 space-y-4">
      <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
        <ArrowLeft size={16} /> Back
      </Button>

      {loading ? (
        <PostSkeleton />
      ) : error ? (
        <div className="text-center py-16 text-gray-400">{error}</div>
      ) : post ? (
        <PostCard post={post} onLike={toggleLike} onDelete={deletePost} />
      ) : null}
    </div>
  );
};

export default PostDetailPage;
