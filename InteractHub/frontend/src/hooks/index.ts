import { useState, useEffect, useCallback, useRef } from 'react';
import type { Post, Comment, Friendship, Notification, Story, PagedResult } from '../types';
import { postsApi, commentsApi, friendsApi, notificationsApi, storiesApi } from '../services/api';

// ─── usePosts (paginated feed) ───────────────────────────────────────────────
export function usePosts() {
  const [posts, setPosts] = useState<Post[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);

  const fetchPosts = useCallback(async (p: number, reset = false) => {
    setLoading(true);
    setError(null);
    try {
      const { data } = await postsApi.getFeed(p);
      setPosts(prev => reset ? data.items : [...prev, ...data.items]);
      setHasMore(data.hasNextPage);
    } catch {
      setError('Failed to load posts.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchPosts(1, true); }, [fetchPosts]);

  const loadMore = () => {
    if (!loading && hasMore) {
      const next = page + 1;
      setPage(next);
      fetchPosts(next);
    }
  };

  const refresh = () => {
    setPage(1);
    fetchPosts(1, true);
  };

  const toggleLike = async (postId: number) => {
    setPosts(prev => prev.map(p =>
      p.id === postId
        ? { ...p, isLikedByCurrentUser: !p.isLikedByCurrentUser, likeCount: p.isLikedByCurrentUser ? p.likeCount - 1 : p.likeCount + 1 }
        : p
    ));
    try {
      await postsApi.toggleLike(postId);
    } catch {
      // rollback on failure
      setPosts(prev => prev.map(p =>
        p.id === postId
          ? { ...p, isLikedByCurrentUser: !p.isLikedByCurrentUser, likeCount: p.isLikedByCurrentUser ? p.likeCount - 1 : p.likeCount + 1 }
          : p
      ));
    }
  };

  const deletePost = async (postId: number) => {
    await postsApi.delete(postId);
    setPosts(prev => prev.filter(p => p.id !== postId));
  };

  return { posts, loading, error, hasMore, loadMore, refresh, toggleLike, deletePost, setPosts };
}

// ─── useComments ─────────────────────────────────────────────────────────────
export function useComments(postId: number) {
  const [comments, setComments] = useState<Comment[]>([]);
  const [loading, setLoading] = useState(false);

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await commentsApi.getByPost(postId);
      if (data.success) setComments(data.data ?? []);
    } finally {
      setLoading(false);
    }
  }, [postId]);

  useEffect(() => { fetch(); }, [fetch]);

  const addComment = async (content: string) => {
    const { data } = await commentsApi.create(postId, content);
    if (data.success && data.data) setComments(prev => [...prev, data.data!]);
  };

  const removeComment = async (commentId: number) => {
    await commentsApi.delete(postId, commentId);
    setComments(prev => prev.filter(c => c.id !== commentId));
  };

  return { comments, loading, addComment, removeComment };
}

// ─── useDebounce ──────────────────────────────────────────────────────────────
export function useDebounce<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);
  return debounced;
}

// ─── useSearch ────────────────────────────────────────────────────────────────
export function useSearch() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<Post[]>([]);
  const [loading, setLoading] = useState(false);
  const debouncedQuery = useDebounce(query, 400);

  useEffect(() => {
    if (!debouncedQuery.trim()) { setResults([]); return; }
    setLoading(true);
    postsApi.search(debouncedQuery)
      .then(({ data }) => setResults(data.items))
      .catch(() => setResults([]))
      .finally(() => setLoading(false));
  }, [debouncedQuery]);

  return { query, setQuery, results, loading };
}

// ─── useFriends ───────────────────────────────────────────────────────────────
export function useFriends() {
  const [friends, setFriends] = useState<Friendship[]>([]);
  const [pendingRequests, setPendingRequests] = useState<Friendship[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([friendsApi.getFriends(), friendsApi.getPendingRequests()])
      .then(([f, p]) => {
        if (f.data.success) setFriends(f.data.data ?? []);
        if (p.data.success) setPendingRequests(p.data.data ?? []);
      })
      .finally(() => setLoading(false));
  }, []);

  const sendRequest = async (receiverId: string) => {
    const { data } = await friendsApi.sendRequest(receiverId);
    return data.success;
  };

  const respond = async (id: number, accept: boolean) => {
    const { data } = await friendsApi.respondToRequest(id, accept);
    if (data.success) {
      setPendingRequests(prev => prev.filter(r => r.id !== id));
      if (accept && data.data) setFriends(prev => [...prev, data.data!]);
    }
  };

  return { friends, pendingRequests, loading, sendRequest, respond };
}

// ─── useNotifications ─────────────────────────────────────────────────────────
export function useNotifications() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);

  useEffect(() => {
    notificationsApi.getAll().then(({ data }) => {
      if (data.success) {
        setNotifications(data.data ?? []);
        setUnreadCount(data.data?.filter(n => !n.isRead).length ?? 0);
      }
    });
  }, []);

  const markAllRead = async () => {
    await notificationsApi.markAllAsRead();
    setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
    setUnreadCount(0);
  };

  const addNotification = (n: Notification) => {
    setNotifications(prev => [n, ...prev]);
    setUnreadCount(c => c + 1);
  };

  return { notifications, unreadCount, markAllRead, addNotification };
}

// ─── useStories ───────────────────────────────────────────────────────────────
export function useStories() {
  const [stories, setStories] = useState<Story[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    storiesApi.getFeed()
      .then(({ data }) => { if (data.success) setStories(data.data ?? []); })
      .finally(() => setLoading(false));
  }, []);

  return { stories, loading };
}

// ─── useIntersectionObserver (infinite scroll) ───────────────────────────────
export function useIntersectionObserver(callback: () => void) {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const observer = new IntersectionObserver(entries => {
      if (entries[0].isIntersecting) callback();
    }, { threshold: 0.1 });
    observer.observe(el);
    return () => observer.disconnect();
  }, [callback]);
  return ref;
}
