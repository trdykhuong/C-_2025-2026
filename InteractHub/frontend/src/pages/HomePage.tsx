import React from 'react';
import { usePosts, useIntersectionObserver } from '../hooks';
import { CreatePostForm, PostCard } from '../components/posts/PostComponents';
import StoriesBar from '../components/stories/StoriesBar';
import { PostSkeleton, EmptyState, Spinner } from '../components/ui';
import type { Post } from '../types';
import { FileText } from 'lucide-react';

const HomePage: React.FC = () => {
  const { posts, loading, error, hasMore, loadMore, refresh, toggleLike, deletePost, setPosts } = usePosts();

  // Infinite scroll sentinel
  const sentinelRef = useIntersectionObserver(() => { if (hasMore && !loading) loadMore(); });

  const handlePostCreated = (post: Post) => {
    setPosts(prev => [post, ...prev]);
  };

  return (
    <div className="max-w-2xl mx-auto px-4 py-4 space-y-4">
      {/* Stories */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-4">
        <StoriesBar />
      </div>

      {/* Create Post */}
      <CreatePostForm onCreated={handlePostCreated} />

      {/* Posts Feed */}
      {loading && posts.length === 0 ? (
        <div className="space-y-4">
          {[1, 2, 3].map(i => <PostSkeleton key={i} />)}
        </div>
      ) : error ? (
        <div className="bg-red-50 text-red-600 text-sm p-4 rounded-xl text-center">
          {error}
          <button onClick={refresh} className="ml-2 underline">Retry</button>
        </div>
      ) : posts.length === 0 ? (
        <EmptyState
          title="No posts yet"
          description="Add friends or create your first post!"
          icon={<FileText size={48} />}
        />
      ) : (
        <div className="space-y-4">
          {posts.map(post => (
            <PostCard
              key={post.id}
              post={post}
              onLike={toggleLike}
              onDelete={deletePost}
            />
          ))}

          {/* Infinite scroll sentinel */}
          <div ref={sentinelRef} className="h-4 flex items-center justify-center">
            {loading && hasMore && <Spinner />}
            {!hasMore && posts.length > 0 && (
              <p className="text-xs text-gray-400">You've seen all posts</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default HomePage;
