import React, { useState, useEffect, useRef } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { Home, Users, Bell, Search, ChevronDown, LogOut, User, Menu, X, ShieldCheck } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { notificationsApi, usersApi } from '../services/api';
import type { Notification, UserSummary } from '../types';
import { Avatar } from './ui/Avatar';

const Navbar: React.FC = () => {
  const { user, logout }  = useAuth();
  const navigate           = useNavigate();
  const location           = useLocation();
  const [searchVal, setSearchVal]       = useState('');
  const [searchResults, setSearchResults] = useState<UserSummary[]>([]);
  const [notifs, setNotifs]             = useState<Notification[]>([]);
  const [unread, setUnread]             = useState(0);
  const [showNotifs, setShowNotifs]     = useState(false);
  const [showProfile, setShowProfile]   = useState(false);
  const [showSearch, setShowSearch]     = useState(false);
  const [mobileMenu, setMobileMenu]     = useState(false);
  const searchTimer                     = useRef<number>();

  // Load notifications
  useEffect(() => {
    notificationsApi.getAll().then(r => {
      setNotifs(r.data ?? []);
      setUnread(r.data?.filter(n => !n.isRead).length ?? 0);
    });
  }, []);

  // Debounce search
  useEffect(() => {
    clearTimeout(searchTimer.current);
    if (!searchVal.trim()) { setSearchResults([]); return; }
    searchTimer.current = setTimeout(async () => {
      const { data } = await usersApi.search(searchVal);
      setSearchResults(data ?? []);
    }, 400);
  }, [searchVal]);

  const handleLogout = () => { logout(); navigate('/login'); };

  const navLinks = [
    { to: '/',        icon: Home,  label: 'Trang chủ' },
    { to: '/friends', icon: Users, label: 'Bạn bè' },
  ];

  return (
    <header className="fixed top-0 left-0 right-0 z-50 bg-white shadow-sm border-b border-gray-200">
      <div className="max-w-6xl mx-auto px-4 h-14 flex items-center gap-2">

        {/* Logo */}
        <Link to="/" className="shrink-0">
          <div className="w-10 h-10 bg-[#1877f2] rounded-full flex items-center justify-center">
            <span className="text-white font-bold text-lg italic">f</span>
          </div>
        </Link>

        {/* Search */}
        <div className="relative hidden md:block">
          <div className="flex items-center bg-gray-100 rounded-full px-3 gap-2 h-10 w-60">
            <Search size={16} className="text-gray-500 shrink-0" />
            <input
              value={searchVal}
              onChange={e => { setSearchVal(e.target.value); setShowSearch(true); }}
              onBlur={() => setTimeout(() => setShowSearch(false), 150)}
              onFocus={() => setShowSearch(true)}
              placeholder="Tìm kiếm trên InteractHub"
              className="bg-transparent text-sm outline-none w-full"
            />
          </div>
          {showSearch && searchResults.length > 0 && (
            <div
              className="absolute top-12 left-0 w-72 bg-white rounded-xl shadow-lg border border-gray-100 overflow-hidden"
              onMouseDown={e => e.preventDefault()}
            >
              {searchResults.map(u => (
                <Link key={u.id} to={`/profile/${u.id}`}
                  onClick={() => { setSearchVal(''); setShowSearch(false); }}
                  className="flex items-center gap-3 px-4 py-3 hover:bg-gray-50">
                  <Avatar name={u.fullName} src={u.avatarUrl} size={36} />
                  <div>
                    <p className="font-semibold text-sm">{u.fullName}</p>
                    <p className="text-xs text-gray-500">@{u.userName}</p>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>

        {/* Nav icons (desktop) */}
        <div className="hidden md:flex flex-1 justify-center gap-1">
          {navLinks.map(({ to, icon: Icon, label }) => (
            <Link key={to} to={to}
              className={`flex flex-col items-center justify-center w-28 h-12 rounded-xl transition text-sm gap-0.5
                ${location.pathname === to
                  ? 'text-[#1877f2] border-b-2 border-[#1877f2] rounded-none'
                  : 'text-gray-500 hover:bg-gray-100'}`}
            >
              <Icon size={22} />
            </Link>
          ))}
        </div>

        {/* Right section */}
        <div className="ml-auto flex items-center gap-2">
          {/* Notifications */}
          <div className="relative">
            <button
              onClick={() => { setShowNotifs(v => !v); setShowProfile(false); if (!showNotifs) { notificationsApi.markAllRead(); setUnread(0); } }}
              className="relative w-10 h-10 bg-gray-100 hover:bg-gray-200 rounded-full flex items-center justify-center text-gray-800"
            >
              <Bell size={20} />
              {unread > 0 && (
                <span className="absolute -top-0.5 -right-0.5 w-5 h-5 bg-red-500 text-white text-[10px] rounded-full flex items-center justify-center font-bold">
                  {unread > 9 ? '9+' : unread}
                </span>
              )}
            </button>
            {showNotifs && (
              <div className="absolute right-0 top-12 w-80 bg-white rounded-xl shadow-xl border border-gray-100 overflow-hidden">
                <div className="px-4 py-3 font-bold text-lg border-b">Thông báo</div>
                <div className="max-h-96 overflow-y-auto">
                  {notifs.length === 0
                    ? <p className="text-center text-gray-400 py-8 text-sm">Không có thông báo</p>
                    : notifs.map(n => (
                        <div key={n.id} className={`px-4 py-3 border-b hover:bg-gray-50 ${!n.isRead ? 'bg-blue-50' : ''}`}>
                          <p className="text-sm">{n.message}</p>
                          <p className="text-xs text-gray-400 mt-0.5">{new Date(n.createdAt).toLocaleString('vi-VN')}</p>
                        </div>
                      ))}
                </div>
              </div>
            )}
          </div>

          {/* Profile dropdown */}
          <div className="relative">
            <button onClick={() => { setShowProfile(v => !v); setShowNotifs(false); }}
              className="flex items-center gap-1">
              <Avatar name={user?.fullName ?? 'U'} src={user?.avatarUrl} size={36} />
              <ChevronDown size={14} className="text-gray-600" />
            </button>
            {showProfile && (
              <div className="absolute right-0 top-12 w-56 bg-white rounded-xl shadow-xl border border-gray-100 overflow-hidden">
                <Link to={`/profile/${user?.userId}`} onClick={() => setShowProfile(false)}
                  className="flex items-center gap-3 px-4 py-3 hover:bg-gray-100">
                  <Avatar name={user?.fullName ?? 'U'} src={user?.avatarUrl} size={36} />
                  <div>
                    <p className="font-semibold text-sm">{user?.fullName}</p>
                    <p className="text-xs text-gray-500">Xem trang cá nhân</p>
                  </div>
                </Link>
                <div className="border-t border-gray-100 m-2" />
                {user?.roles?.includes('Admin') && (
                  <Link to="/admin" onClick={() => setShowProfile(false)}
                    className="w-full flex items-center gap-3 px-4 py-2.5 hover:bg-gray-100 text-sm">
                    <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
                      <ShieldCheck size={16} className="text-[#1877f2]" />
                    </div>
                    Trang quản trị
                  </Link>
                )}
                <button onClick={handleLogout}
                  className="w-full flex items-center gap-3 px-4 py-2.5 hover:bg-gray-100 text-sm">
                  <div className="w-8 h-8 bg-gray-200 rounded-full flex items-center justify-center">
                    <LogOut size={16} />
                  </div>
                  Đăng xuất
                </button>
              </div>
            )}
          </div>

          {/* Mobile menu */}
          <button className="md:hidden w-10 h-10 bg-gray-100 rounded-full flex items-center justify-center"
            onClick={() => setMobileMenu(v => !v)}>
            {mobileMenu ? <X size={18} /> : <Menu size={18} />}
          </button>
        </div>
      </div>

      {/* Mobile menu */}
      {mobileMenu && (
        <div className="md:hidden border-t bg-white px-4 py-2 flex flex-col gap-1">
          {navLinks.map(({ to, icon: Icon, label }) => (
            <Link key={to} to={to} onClick={() => setMobileMenu(false)}
              className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-gray-100 text-sm">
              <Icon size={18} /> {label}
            </Link>
          ))}
          <button onClick={handleLogout}
            className="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-gray-100 text-sm text-red-600">
            <LogOut size={18} /> Đăng xuất
          </button>
        </div>
      )}
    </header>
  );
};

export default Navbar;
