import React, { useState, useRef, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Bell, Home, Users, Search, LogOut, User, Menu, X, MessageCircle } from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { useNotifications, useSearch } from '../../hooks';
import { Avatar } from '../ui';

const Navbar: React.FC = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { unreadCount, notifications, markAllRead } = useNotifications();
  const { query, setQuery, results, loading } = useSearch();
  const [showNotifs, setShowNotifs] = useState(false);
  const [showProfile, setShowProfile] = useState(false);
  const [showMobileMenu, setShowMobileMenu] = useState(false);
  const searchRef = useRef<HTMLDivElement>(null);

  // Close dropdowns on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (searchRef.current && !searchRef.current.contains(e.target as Node))
        setQuery('');
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [setQuery]);

  const handleLogout = () => { logout(); navigate('/login'); };

  return (
    <nav className="fixed top-0 left-0 right-0 z-40 bg-white border-b border-gray-200 shadow-sm">
      <div className="max-w-6xl mx-auto px-4 h-14 flex items-center justify-between gap-4">

        {/* Logo */}
        <Link to="/" className="font-bold text-blue-600 text-lg shrink-0">InteractHub</Link>

        {/* Search */}
        <div ref={searchRef} className="relative flex-1 max-w-sm hidden md:block">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input
            value={query}
            onChange={e => setQuery(e.target.value)}
            placeholder="Search posts..."
            className="w-full pl-9 pr-4 py-1.5 rounded-full border border-gray-200 text-sm focus:outline-none focus:border-blue-400 bg-gray-50"
          />
          {query && (
            <div className="absolute top-full mt-1 w-full bg-white rounded-xl shadow-lg border border-gray-100 overflow-hidden z-50">
              {loading ? (
                <p className="text-sm text-gray-400 p-3">Searching...</p>
              ) : results.length === 0 ? (
                <p className="text-sm text-gray-400 p-3">No results found</p>
              ) : results.slice(0, 5).map(post => (
                <Link
                  key={post.id}
                  to={`/post/${post.id}`}
                  onClick={() => setQuery('')}
                  className="block px-4 py-2 hover:bg-gray-50 text-sm"
                >
                  <p className="font-medium truncate">{post.author.fullName}</p>
                  <p className="text-gray-500 truncate">{post.content}</p>
                </Link>
              ))}
            </div>
          )}
        </div>

        {/* Desktop Nav Icons */}
        <div className="hidden md:flex items-center gap-1">
          <NavIcon to="/" icon={<Home size={20} />} label="Home" />
          <NavIcon to="/friends" icon={<Users size={20} />} label="Friends" />

          {/* Notifications */}
          <div className="relative">
            <button
              onClick={() => { setShowNotifs(v => !v); setShowProfile(false); if (!showNotifs) markAllRead(); }}
              className="relative p-2 rounded-lg hover:bg-gray-100 text-gray-600"
            >
              <Bell size={20} />
              {unreadCount > 0 && (
                <span className="absolute top-1 right-1 w-4 h-4 bg-red-500 text-white text-[10px] rounded-full flex items-center justify-center">
                  {unreadCount > 9 ? '9+' : unreadCount}
                </span>
              )}
            </button>

            {showNotifs && (
              <div className="absolute right-0 top-full mt-1 w-80 bg-white rounded-xl shadow-lg border border-gray-100 overflow-hidden">
                <div className="p-3 border-b font-semibold text-sm">Notifications</div>
                <div className="max-h-80 overflow-y-auto">
                  {notifications.length === 0 ? (
                    <p className="text-sm text-gray-400 p-4 text-center">No notifications</p>
                  ) : notifications.slice(0, 10).map(n => (
                    <div key={n.id} className={`px-4 py-3 text-sm border-b last:border-0 ${!n.isRead ? 'bg-blue-50' : ''}`}>
                      <p className="text-gray-800">{n.message}</p>
                      <p className="text-xs text-gray-400 mt-0.5">{new Date(n.createdAt).toLocaleDateString()}</p>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Profile */}
          <div className="relative">
            <button
              onClick={() => { setShowProfile(v => !v); setShowNotifs(false); }}
              className="p-1 rounded-full hover:ring-2 hover:ring-blue-300 transition"
            >
              <Avatar src={user?.avatarUrl} name={user?.fullName ?? 'User'} size="sm" />
            </button>

            {showProfile && (
              <div className="absolute right-0 top-full mt-1 w-48 bg-white rounded-xl shadow-lg border border-gray-100 overflow-hidden">
                <Link
                  to={`/profile/${user?.userId}`}
                  onClick={() => setShowProfile(false)}
                  className="flex items-center gap-3 px-4 py-3 hover:bg-gray-50 text-sm"
                >
                  <User size={16} className="text-gray-500" />
                  Profile
                </Link>
                <button
                  onClick={handleLogout}
                  className="w-full flex items-center gap-3 px-4 py-3 hover:bg-red-50 text-sm text-red-600"
                >
                  <LogOut size={16} />
                  Logout
                </button>
              </div>
            )}
          </div>
        </div>

        {/* Mobile menu toggle */}
        <button className="md:hidden p-2" onClick={() => setShowMobileMenu(v => !v)}>
          {showMobileMenu ? <X size={20} /> : <Menu size={20} />}
        </button>
      </div>

      {/* Mobile dropdown */}
      {showMobileMenu && (
        <div className="md:hidden border-t border-gray-100 bg-white px-4 py-3 flex flex-col gap-2">
          <Link to="/" className="flex items-center gap-2 py-2 text-sm" onClick={() => setShowMobileMenu(false)}><Home size={18} /> Home</Link>
          <Link to="/friends" className="flex items-center gap-2 py-2 text-sm" onClick={() => setShowMobileMenu(false)}><Users size={18} /> Friends</Link>
          <Link to={`/profile/${user?.userId}`} className="flex items-center gap-2 py-2 text-sm" onClick={() => setShowMobileMenu(false)}><User size={18} /> Profile</Link>
          <button onClick={handleLogout} className="flex items-center gap-2 py-2 text-sm text-red-600"><LogOut size={18} /> Logout</button>
        </div>
      )}
    </nav>
  );
};

const NavIcon: React.FC<{ to: string; icon: React.ReactNode; label: string }> = ({ to, icon, label }) => (
  <Link to={to} className="p-2 rounded-lg hover:bg-gray-100 text-gray-600" title={label}>{icon}</Link>
);

export default Navbar;
