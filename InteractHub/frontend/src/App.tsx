import React, { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate, Outlet } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import Navbar from './components/Navbar';
import { LoginPage, RegisterPage } from './pages/AuthPages';

const HomePage    = lazy(() => import('./pages/HomePage'));
const ProfilePage = lazy(() => import('./pages/ProfileAndFriends').then(m => ({ default: m.ProfilePage })));
const FriendsPage = lazy(() => import('./pages/ProfileAndFriends').then(m => ({ default: m.FriendsPage })));
const HashtagPage = lazy(() => import('./pages/HashtagPage'));
const AdminPage   = lazy(() => import('./pages/AdminPage'));

const Loader = () => (
  <div className="flex justify-center items-center h-64">
    <div className="w-8 h-8 border-4 border-[#1877f2] border-t-transparent rounded-full animate-spin" />
  </div>
);

// Chỉ cho phép khi đã đăng nhập
const PrivateRoute: React.FC = () => {
  const { isAuth } = useAuth();
  return isAuth ? <Outlet /> : <Navigate to="/login" replace />;
};

// Chỉ cho phép khi chưa đăng nhập
const GuestRoute: React.FC = () => {
  const { isAuth } = useAuth();
  return !isAuth ? <Outlet /> : <Navigate to="/" replace />;
};

// Layout chính có Navbar
const MainLayout: React.FC = () => (
  <div className="min-h-screen bg-[#f0f2f5]">
    <Navbar />
    <main className="pt-14">
      <Outlet />
    </main>
  </div>
);

const App: React.FC = () => (
  <AuthProvider>
    <BrowserRouter>
      <Routes>
        {/* Guest routes */}
        <Route element={<GuestRoute />}>
          <Route path="/login"    element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
        </Route>

        {/* Private routes */}
        <Route element={<PrivateRoute />}>
          <Route element={<MainLayout />}>
            <Route path="/" element={
              <Suspense fallback={<Loader />}><HomePage /></Suspense>
            } />
            <Route path="/profile/:id" element={
              <Suspense fallback={<Loader />}><ProfilePage /></Suspense>
            } />
            <Route path="/friends" element={
              <Suspense fallback={<Loader />}><FriendsPage /></Suspense>
            } />
            <Route path="/hashtag/:tag" element={
              <Suspense fallback={<Loader />}><HashtagPage /></Suspense>
            } />
          </Route>
          {/* Admin routes */}
          <Route element={<MainLayout />}>
            <Route path="/admin" element={
              <Suspense fallback={<Loader />}><AdminPage /></Suspense>
            } />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  </AuthProvider>
);

export default App;
