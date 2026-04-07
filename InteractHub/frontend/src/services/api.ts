import axios from 'axios';
import type {
  ApiResponse, AuthResponse, LoginDto, RegisterDto,
  Post, Comment, CreatePostDto, UserProfile, UserSummary,
  Friendship, Story, Notification, PagedResult
} from '../types';

// Default to HTTPS dev URL (dotnet runs HTTPS on 5001). Can be overridden with VITE_API_URL.
const API_BASE = import.meta.env.VITE_API_URL ?? 'https://localhost:5001';

export const api = axios.create({
  baseURL: `${API_BASE}/api`,
  headers: { 'Content-Type': 'application/json' },
});

// Inject token into every request
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Handle 401 globally
api.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// ─── AUTH ────────────────────────────────────────────────────────────────────
export const authApi = {
  register: (dto: RegisterDto) =>
    api.post<ApiResponse<AuthResponse>>('/auth/register', dto),
  login: (dto: LoginDto) =>
    api.post<ApiResponse<AuthResponse>>('/auth/login', dto),
  me: () =>
    api.get<ApiResponse<{ userId: string; userName: string; fullName: string }>>('/auth/me'),
};

// ─── POSTS ───────────────────────────────────────────────────────────────────
export const postsApi = {
  getFeed: (page = 1, pageSize = 10) =>
    api.get<PagedResult<Post>>(`/posts/feed?page=${page}&pageSize=${pageSize}`),
  getById: (id: number) =>
    api.get<ApiResponse<Post>>(`/posts/${id}`),
  getUserPosts: (userId: string, page = 1) =>
    api.get<PagedResult<Post>>(`/posts/user/${userId}?page=${page}`),
  search: (q: string, page = 1) =>
    api.get<PagedResult<Post>>(`/posts/search?q=${encodeURIComponent(q)}&page=${page}`),
  create: (dto: CreatePostDto) =>
    api.post<ApiResponse<Post>>('/posts', dto),
  update: (id: number, content: string) =>
    api.put<ApiResponse<Post>>(`/posts/${id}`, { content }),
  delete: (id: number) =>
    api.delete<ApiResponse<boolean>>(`/posts/${id}`),
  toggleLike: (id: number) =>
    api.post<ApiResponse<boolean>>(`/posts/${id}/like`),
};

// ─── COMMENTS ────────────────────────────────────────────────────────────────
export const commentsApi = {
  getByPost: (postId: number) =>
    api.get<ApiResponse<Comment[]>>(`/posts/${postId}/comments`),
  create: (postId: number, content: string) =>
    api.post<ApiResponse<Comment>>(`/posts/${postId}/comments`, { content }),
  delete: (postId: number, commentId: number) =>
    api.delete<ApiResponse<boolean>>(`/posts/${postId}/comments/${commentId}`),
};

// ─── USERS ───────────────────────────────────────────────────────────────────
export const usersApi = {
  getProfile: (id: string) =>
    api.get<ApiResponse<UserProfile>>(`/users/${id}`),
  updateProfile: (data: { fullName?: string; bio?: string }) =>
    api.put<ApiResponse<boolean>>('/users/me', data),
  search: (q: string) =>
    api.get<ApiResponse<UserSummary[]>>(`/users/search?q=${encodeURIComponent(q)}`),
};

// ─── FRIENDS ─────────────────────────────────────────────────────────────────
export const friendsApi = {
  getFriends: () =>
    api.get<ApiResponse<Friendship[]>>('/friends'),
  getPendingRequests: () =>
    api.get<ApiResponse<Friendship[]>>('/friends/requests'),
  sendRequest: (receiverId: string) =>
    api.post<ApiResponse<Friendship>>('/friends/request', { receiverId }),
  respondToRequest: (id: number, accept: boolean) =>
    api.put<ApiResponse<Friendship>>(`/friends/request/${id}?accept=${accept}`),
  removeFriend: (id: number) =>
    api.delete<ApiResponse<boolean>>(`/friends/${id}`),
};

// ─── STORIES ─────────────────────────────────────────────────────────────────
export const storiesApi = {
  getFeed: () =>
    api.get<ApiResponse<Story[]>>('/stories'),
  create: (data: { imageUrl?: string; caption?: string }) =>
    api.post<ApiResponse<Story>>('/stories', data),
  delete: (id: number) =>
    api.delete<ApiResponse<boolean>>(`/stories/${id}`),
};

// ─── NOTIFICATIONS ────────────────────────────────────────────────────────────
export const notificationsApi = {
  getAll: () =>
    api.get<ApiResponse<Notification[]>>('/notifications'),
  markAsRead: (id: number) =>
    api.put<ApiResponse<boolean>>(`/notifications/${id}/read`),
  markAllAsRead: () =>
    api.put<ApiResponse<boolean>>('/notifications/read-all'),
};
