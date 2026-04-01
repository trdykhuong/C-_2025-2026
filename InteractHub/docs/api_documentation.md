# InteractHub API Documentation

**Base URL:** `https://your-app.azurewebsites.net/api`  
**Auth:** Bearer JWT token in `Authorization` header  
**Swagger UI:** `/swagger`

---

## Authentication

### POST `/auth/register`
Register a new account.
```json
// Request
{ "fullName": "John Doe", "email": "john@example.com", "userName": "johndoe", "password": "Pass1234" }

// Response 200
{ "success": true, "data": { "token": "eyJ...", "userId": "...", "userName": "johndoe", "fullName": "John Doe", "roles": ["User"], "expiresAt": "..." } }
```

### POST `/auth/login`
Login and receive JWT.
```json
// Request
{ "email": "john@example.com", "password": "Pass1234" }

// Response 200
{ "success": true, "data": { "token": "eyJ...", ... } }
```

### GET `/auth/me` 🔒
Get current user info.

---

## Posts

### GET `/posts/feed?page=1&pageSize=10` 🔒
Get paginated news feed from friends.

### GET `/posts/search?q=hello&page=1` 🔒
Search posts by content or hashtag.

### GET `/posts/{id}` 🔒
Get single post.

### GET `/posts/user/{userId}?page=1` 🔒
Get all posts by a user.

### POST `/posts` 🔒
Create a post.
```json
// Request
{ "content": "Hello world!", "imageUrl": "https://...", "hashtags": ["tech", "news"] }
```

### PUT `/posts/{id}` 🔒
Update own post.
```json
{ "content": "Updated content" }
```

### DELETE `/posts/{id}` 🔒
Soft-delete own post.

### POST `/posts/{id}/like` 🔒
Toggle like (returns `data: true` = liked, `data: false` = unliked).

---

## Comments

### GET `/posts/{postId}/comments` 🔒
Get all comments for a post.

### POST `/posts/{postId}/comments` 🔒
Add comment.
```json
{ "content": "Great post!" }
```

### DELETE `/posts/{postId}/comments/{commentId}` 🔒
Delete own comment.

---

## Friends

### GET `/friends` 🔒
Get accepted friends list.

### GET `/friends/requests` 🔒
Get pending incoming requests.

### POST `/friends/request` 🔒
Send friend request.
```json
{ "receiverId": "user-id-here" }
```

### PUT `/friends/request/{id}?accept=true` 🔒
Accept or reject a request.

### DELETE `/friends/{id}` 🔒
Remove a friend.

---

## Stories

### GET `/stories` 🔒
Get active stories from friends (not expired).

### POST `/stories` 🔒
Create a story (expires in 24h).
```json
{ "imageUrl": "https://...", "caption": "Good morning!" }
```

### DELETE `/stories/{id}` 🔒
Delete own story.

---

## Notifications

### GET `/notifications` 🔒
Get all notifications (latest 50).

### PUT `/notifications/{id}/read` 🔒
Mark one notification as read.

### PUT `/notifications/read-all` 🔒
Mark all notifications as read.

---

## Users

### GET `/users/{id}` 🔒
Get a user's public profile.

### PUT `/users/me` 🔒
Update own profile.
```json
{ "fullName": "New Name", "bio": "Updated bio" }
```

### GET `/users/search?q=john` 🔒
Search users.

---

## Hashtags

### GET `/hashtags/trending?count=10` 🔒
Get trending hashtags.

---

## Upload

### POST `/upload/image` 🔒
Upload image file (multipart/form-data, max 5MB).  
Returns `{ "data": "https://blob-url..." }`

---

## Reports (Admin)

### POST `/reports/{postId}` 🔒
Report a post.
```json
{ "reason": "Inappropriate content" }
```

### GET `/reports` 🔒 👑 Admin only
Get pending reports.

### PUT `/reports/{id}?status=reviewed` 🔒 👑 Admin only
Update report status.

---

## Standard Response Format

```json
{
  "success": true | false,
  "data": <T> | null,
  "message": "Optional message",
  "errors": ["Error 1", "Error 2"]
}
```

## Paginated Response Format

```json
{
  "items": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 10,
  "totalPages": 10,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```
