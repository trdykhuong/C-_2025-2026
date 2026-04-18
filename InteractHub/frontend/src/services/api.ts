import axios from 'axios';
import type { ApiResult, PagedResult, Post, Comment, UserProfile, UserSummary, Friendship, Story, Notification, AuthUser } from '../types';

const BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

export const http = axios.create({ baseURL: `${BASE}/api` });

// Tự động gắn JWT vào header
http.interceptors.request.use(cfg => {
  const token = localStorage.getItem('token');
  if (token) cfg.headers.Authorization = `Bearer ${token}`;
  return cfg;
});

// 401 → về trang login
http.interceptors.response.use(
  r => r,
  err => {
    if (err.response?.status === 401) {
      localStorage.clear();
      window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);

// ── Auth ─────────────────────────────────────────────────────────────────────
export const authApi = {
  register: (d: any) => http.post<ApiResult<AuthUser>>('/auth/register', d).then(r => r.data),
  login:    (d: any) => http.post<ApiResult<AuthUser>>('/auth/login',    d).then(r => r.data),
};

// ── Posts ─────────────────────────────────────────────────────────────────────
export const postsApi = {
  getFeed:       (page = 1)              => http.get<PagedResult<Post>>(`/posts/feed?page=${page}`).then(r => r.data),
  getByUser:     (uid: string, page = 1) => http.get<PagedResult<Post>>(`/posts/user/${uid}?page=${page}`).then(r => r.data),
  search:        (kw: string)            => http.get<PagedResult<Post>>(`/posts/search?keyword=${encodeURIComponent(kw)}`).then(r => r.data),
  getByHashtag:  (tag: string, page = 1) => http.get<PagedResult<Post>>(`/posts/hashtag/${encodeURIComponent(tag)}?page=${page}`).then(r => r.data),
  create:        (d: any)                => http.post<ApiResult<Post>>('/posts', d),
  delete:        (id: number)            => http.delete<ApiResult<boolean>>(`/posts/${id}`),
  toggleLike:    (id: number)            => http.post<ApiResult<boolean>>(`/posts/${id}/like`),
};

// ── Comments ──────────────────────────────────────────────────────────────────
export const commentsApi = {
  getAll: (postId: number)                              => http.get<Comment[]>(`/posts/${postId}/comments`).then(r => ({ data: r.data })),
  create: (postId: number, content: string)             => http.post<ApiResult<Comment>>(`/posts/${postId}/comments`, { content }),
  update: (postId: number, cid: number, content: string) => http.put<ApiResult<Comment>>(`/posts/${postId}/comments/${cid}`, { content }),
  delete: (postId: number, cid: number)                 => http.delete(`/posts/${postId}/comments/${cid}`),
};

// ── Users ─────────────────────────────────────────────────────────────────────
export const usersApi = {
  getProfile: (id: string) => http.get<ApiResult<UserProfile>>(`/users/${id}`).then(r => r.data),
  update:     (d: any)     => http.put<ApiResult<boolean>>('/users/me', d),
  search:     (kw: string) => http.get<UserSummary[]>(`/users/search?keyword=${encodeURIComponent(kw)}`).then(r => ({ data: r.data })),
};

// ── Friends ───────────────────────────────────────────────────────────────────
export const friendsApi = {
  list:           ()                            => http.get<Friendship[]>('/friends').then(r => r.data),
  pending:        ()                            => http.get<Friendship[]>('/friends/requests').then(r => r.data),
  send:           (receiverId: string)          => http.post('/friends/request', { receiverId }),
  respond:        (id: number, accept: boolean) => http.put(`/friends/request/${id}?accept=${accept}`),
  remove:         (id: number)                  => http.delete(`/friends/${id}`),
  removeByUserId: (userId: string)              => http.delete(`/friends/with/${userId}`),
};

// ── Stories ───────────────────────────────────────────────────────────────────
export const storiesApi = {
  list:        ()            => http.get<Story[]>('/stories').then(r => r.data),
  create:      (d: any)      => http.post<ApiResult<Story>>('/stories', d).then(r => r.data),
  recordView:  (id: number)  => http.post(`/stories/${id}/view`),
  delete:      (id: number)  => http.delete(`/stories/${id}`),
};

// ── Upload ────────────────────────────────────────────────────────────────────
export const uploadApi = {
  upload: (file: File) => {
    const form = new FormData();
    form.append('file', file);
    return http.post<{ success: boolean; url: string }>('/upload', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then(r => r.data);
  },
};

// ── Hashtags ──────────────────────────────────────────────────────────────────
export const hashtagsApi = {
  getTrending: (top = 10) => http.get<{ name: string; count: number }[]>(`/hashtags/trending?top=${top}`).then(r => r.data),
};

// ── Reports ───────────────────────────────────────────────────────────────────
export const reportsApi = {
  report: (postId: number, reason: string) =>
    http.post<any>(`/reports/${postId}`, { reason }).then(r => r.data),
};

// ── Admin ─────────────────────────────────────────────────────────────────────
export const adminApi = {
  getStats:       ()                                          => http.get<any>('/admin/stats').then(r => r.data),
  getUsers:       (page = 1, keyword = '')                   => http.get<any>(`/admin/users?page=${page}&keyword=${encodeURIComponent(keyword)}`).then(r => r.data),
  toggleActive:   (id: string)                               => http.put<any>(`/admin/users/${id}/toggle-active`).then(r => r.data),
  getPosts:       (page = 1, keyword = '', deleted = false)  => http.get<any>(`/admin/posts?page=${page}&keyword=${encodeURIComponent(keyword)}&deleted=${deleted}`).then(r => r.data),
  deletePost:     (id: number)                               => http.delete<any>(`/admin/posts/${id}`).then(r => r.data),
  getReports:     (status = 'pending')                       => http.get<any[]>(`/admin/reports?status=${status}`).then(r => r.data),
  updateReport:   (id: number, status: string)               => http.put<any>(`/admin/reports/${id}?status=${status}`).then(r => r.data),
};

// ── Notifications ─────────────────────────────────────────────────────────────
export const notificationsApi = {
  getAll:      ()           => http.get<Notification[]>('/notifications').then(r => ({ data: r.data })),
  markRead:    (id: number) => http.put(`/notifications/${id}/read`),
  markAllRead: ()           => http.put('/notifications/read-all'),
};
