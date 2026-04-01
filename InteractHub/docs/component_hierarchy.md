# InteractHub – Component Hierarchy

```
App (AuthProvider + BrowserRouter)
│
├── PublicOnlyRoute
│   ├── /login   → LoginPage
│   └── /register → RegisterPage
│
└── ProtectedRoute
    └── MainLayout (Navbar + Sidebar)
        ├── Navbar
        │   ├── Logo (Link)
        │   ├── SearchBar (debounced, dropdown results)
        │   ├── NavIcon (Home, Friends)
        │   ├── NotificationsDropdown
        │   └── ProfileDropdown (Avatar)
        │
        ├── [Main Content Area]
        │   ├── /              → HomePage
        │   │   ├── StoriesBar
        │   │   │   └── StoryViewer (Modal)
        │   │   ├── CreatePostForm
        │   │   └── PostFeed (infinite scroll)
        │   │       └── PostCard[]
        │   │           ├── PostHeader (Avatar, Author, Time)
        │   │           ├── PostContent (text, image, hashtags)
        │   │           ├── PostActions (Like, Comment buttons)
        │   │           └── CommentsSection (collapsible)
        │   │               ├── CommentItem[]
        │   │               └── CommentInput
        │   │
        │   ├── /profile/:id   → ProfilePage
        │   │   ├── CoverPhoto
        │   │   ├── ProfileHeader (Avatar, name, stats, friend button)
        │   │   ├── EditProfileForm (React Hook Form)
        │   │   └── UserPosts (PostCard[])
        │   │
        │   ├── /friends       → FriendsPage
        │   │   ├── PendingRequests (FriendCard[])
        │   │   └── FriendsList (FriendCard[])
        │   │
        │   └── /notifications → NotificationsPage
        │       └── NotificationItem[]
        │
        └── [Sidebar]
            ├── PeopleSearch (debounced, UserSummary[])
            └── TrendingHashtags (HashtagTrend[])
```

## Reusable UI Components (`/components/ui`)

| Component | Props | Description |
|---|---|---|
| `Button` | variant, size, loading | Primary/secondary/ghost/danger |
| `Input` | label, error, icon | Form input with validation display |
| `Textarea` | label, error | Multi-line text input |
| `Avatar` | src, name, size | Image or initial-based fallback |
| `Card` | className | White rounded container |
| `Modal` | isOpen, onClose, title | Overlay dialog |
| `Spinner` | className | Loading indicator |
| `PostSkeleton` | — | Loading placeholder for posts |
| `EmptyState` | title, description, icon | Zero-content placeholder |
| `PasswordStrength` | password | Visual strength indicator |

## Custom Hooks

| Hook | Returns | Description |
|---|---|---|
| `usePosts()` | posts, loading, toggleLike, deletePost, loadMore | Feed with optimistic updates + infinite scroll |
| `useComments(postId)` | comments, addComment, removeComment | Per-post comments |
| `useFriends()` | friends, pendingRequests, sendRequest, respond | Friend management |
| `useNotifications()` | notifications, unreadCount, markAllRead | Notification state |
| `useStories()` | stories, loading | Story feed |
| `useSearch()` | query, setQuery, results, loading | Debounced post search |
| `useDebounce(value, delay)` | debounced value | Generic debounce utility |
| `useIntersectionObserver(cb)` | ref | Infinite scroll trigger |
| `useSignalR(onNotification)` | connectionRef | Real-time notifications |
