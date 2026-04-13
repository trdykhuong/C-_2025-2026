import React, { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate, Outlet } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import Navbar from './components/layout/Navbar';
import { Spinner } from './components/ui';
import { TrendingHashtags, PeopleSearch } from './components/sidebar/SidebarWidgets';
import { LoginPage, RegisterPage } from './pages/AuthPages';

// Lazy-loaded pages (code splitting per route)
const HomePage        = lazy(() => import('./pages/HomePage'));
const ProfilePage     = lazy(() => import('./pages/ProfilePage'));
const FriendsPage     = lazy(() => import('./pages/FriendsPage'));
const NotificationsPage = lazy(() => import('./pages/NotificationsPage'));
const PostDetailPage  = lazy(() => import('./pages/PostDetailPage'));

// ─── SPINNERS ────────────────────────────────────────────────────────────────
const FullPageSpinner = () => (
  <div className="flex h-screen items-center justify-center">
    <Spinner className="w-8 h-8" />
  </div>
);
const PageSpinner = () => (
  <div className="flex justify-center pt-20">
    <Spinner className="w-8 h-8" />
  </div>
);

// ─── ROUTE GUARDS ─────────────────────────────────────────────────────────────
const ProtectedRoute: React.FC = () => {
  const { isAuthenticated, isLoading } = useAuth();
  if (isLoading) return <FullPageSpinner />;
  return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace />;
};

const PublicOnlyRoute: React.FC = () => {
  const { isAuthenticated, isLoading } = useAuth();
  if (isLoading) return <FullPageSpinner />;
  return !isAuthenticated ? <Outlet /> : <Navigate to="/" replace />;
};

// ─── MAIN LAYOUT ─────────────────────────────────────────────────────────────
const MainLayout: React.FC = () => (
  <div className="min-h-screen bg-gray-50">
    <Navbar />
    <div className="pt-14 max-w-6xl mx-auto px-4">
      <div className="flex gap-6 py-4">
        {/* Main content */}
        <div className="flex-1 min-w-0">
          <Outlet />
        </div>
        {/* Right sidebar – hidden on small screens */}
        <aside
          className="hidden lg:block w-72 shrink-0 space-y-4"
          style={{ position: 'sticky', top: '4.5rem', alignSelf: 'flex-start' }}
        >
          <PeopleSearch />
          <TrendingHashtags />
        </aside>
      </div>
    </div>
  </div>
);

// ─── PAGE SUSPENSE WRAPPER ────────────────────────────────────────────────────
const Page = ({ el }: { el: React.ReactNode }) => (
  <Suspense fallback={<PageSpinner />}>{el}</Suspense>
);

// ─── APP ─────────────────────────────────────────────────────────────────────
const App: React.FC = () => (
  <AuthProvider>
    <BrowserRouter>
      <Routes>
        {/* Public-only routes */}
        <Route element={<PublicOnlyRoute />}>
          <Route path="/login"    element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
        </Route>

        {/* Protected routes */}
        <Route element={<ProtectedRoute />}>
          <Route element={<MainLayout />}>
            <Route path="/"                element={<Page el={<HomePage />} />} />
            <Route path="/post/:id"        element={<Page el={<PostDetailPage />} />} />
            <Route path="/profile/:id"     element={<Page el={<ProfilePage />} />} />
            <Route path="/friends"         element={<Page el={<FriendsPage />} />} />
            <Route path="/notifications"   element={<Page el={<NotificationsPage />} />} />
          </Route>
        </Route>

        {/* 404 fallback */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  </AuthProvider>
);

export default App;
