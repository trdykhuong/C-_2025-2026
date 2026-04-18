export interface UserSummary {
  id: string; userName: string; fullName: string; avatarUrl?: string;
}
export interface UserProfile extends UserSummary {
  bio?: string; coverUrl?: string; createdAt: string;
  postCount: number; friendCount: number;
  friendshipStatus: 'none' | 'pending_sent' | 'pending_received' | 'accepted';
  friendshipId?: number;
}
export interface AuthUser {
  token: string; userId: string; userName: string; fullName: string;
  avatarUrl?: string; roles: string[]; isAdmin: boolean; expiredAt: string;
}
export interface SharedPost {
  id: number; content: string; imageUrl?: string;
  createdAt: string; author: UserSummary;
}
export interface Post {
  id: number; content: string; imageUrl?: string;
  visibility: 'public' | 'friends' | 'private';
  createdAt: string; updatedAt?: string;
  author: UserSummary; likeCount: number; commentCount: number;
  isLikedByCurrentUser: boolean; hashtags: string[];
  sharedPostId?: number; sharedPost?: SharedPost;
}
export interface Comment {
  id: number; content: string; createdAt: string; author: UserSummary;
}
export interface Friendship {
  id: number; status: string; createdAt: string; otherUser: UserSummary;
}
export interface Story {
  id: number; imageUrl?: string; caption?: string;
  visibility: 'public' | 'friends' | 'private';
  createdAt: string; expiresAt: string;
  viewCount: number;
  author: UserSummary;
}
export interface Notification {
  id: number; message: string; type: string; isRead: boolean;
  createdAt: string; relatedPostId?: number;
}
export interface PagedResult<T> {
  items: T[]; totalCount: number; page: number; pageSize: number; hasNext: boolean;
}
export interface ApiResult<T> {
  success: boolean; data?: T; message: string;
}
