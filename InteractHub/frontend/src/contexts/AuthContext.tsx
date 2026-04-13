// ─── AuthContext ─────────────────────────────────────────────────────────────
import React, { createContext, useContext, useState, useEffect } from 'react';
import type { AuthUser } from '../types';

interface AuthCtx {
  user: AuthUser | null;
  isAuth: boolean;
  setAuth: (u: AuthUser) => void;
  logout: () => void;
}

const Ctx = createContext<AuthCtx | null>(null);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<AuthUser | null>(null);

  useEffect(() => {
    const raw = localStorage.getItem('user');
    if (raw) {
      try {
        const u: AuthUser = JSON.parse(raw);
        if (new Date(u.expiredAt) > new Date()) setUser(u);
        else localStorage.clear();
      } catch { localStorage.clear(); }
    }
  }, []);

  const setAuth = (u: AuthUser) => {
    localStorage.setItem('token', u.token);
    localStorage.setItem('user', JSON.stringify(u));
    setUser(u);
  };

  const logout = () => {
    localStorage.clear();
    setUser(null);
  };

  return <Ctx.Provider value={{ user, isAuth: !!user, setAuth, logout }}>{children}</Ctx.Provider>;
};

export const useAuth = () => {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error('useAuth: no AuthProvider');
  return ctx;
};
