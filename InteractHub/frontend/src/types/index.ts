// ─── AUTH ───────────────────────────────────────────────────────────────────
export interface RegisterDto {
  fullName: string;
  email: string;
  userName: string;
  password: string;
}

export interface LoginDto {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  userId: string;
  userName: string;
  fullName: string;
  avatarUrl?: string;
  roles: string[];
  expiresAt: string;
}

// ─── USERS ──────────────────────────────────────────────────────────────────
export interface UserSummary {
  id: string;
  userName: string;
  fullName: string;
  avatarUrl?: string;
}

export interface UserProfile extends UserSummary {
  bio?: string;
  coverUrl?: string;
  createdAt: string;
  postCount: number;
  friendCount: number;
  friendshipStatus: 'none' | 'pending' | 'accepted';
}

// ─── POSTS ──────────────────────────────────────────────────────────────────
export interface Post {
  id: number;
  content: string;
  imageUrl?: string;
  createdAt: string;
  updatedAt?: string;
  author: UserSummary;
  likeCount: number;
  commentCount: number;
  isLikedByCurrentUser: boolean;
  hashtags: string[];
}

export interface CreatePostDto {
  content: string;
  imageUrl?: string;
  hashtags: string[];
}

// ─── COMMENTS ───────────────────────────────────────────────────────────────
export interface Comment {
  id: number;
  content: string;
  createdAt: string;
  author: UserSummary;
}

// ─── FRIENDS ────────────────────────────────────────────────────────────────
export interface Friendship {
  id: number;
  status: string;
  createdAt: string;
  otherUser: UserSummary;
}

// ─── STORIES ────────────────────────────────────────────────────────────────
export interface Story {
  id: number;
  imageUrl?: string;
  caption?: string;
  createdAt: string;
  expiresAt: string;
  author: UserSummary;
}

// ─── NOTIFICATIONS ──────────────────────────────────────────────────────────
export interface Notification {
  id: number;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
  relatedPostId?: number;
  actor?: UserSummary;
}

// ─── API RESPONSE ───────────────────────────────────────────────────────────
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
