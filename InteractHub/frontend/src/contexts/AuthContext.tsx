import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import type { AuthResponse, LoginDto, RegisterDto } from '../types';
import { authApi, api } from '../services/api';

interface AuthState {
  user: AuthResponse | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

interface AuthContextValue extends AuthState {
  login: (dto: LoginDto) => Promise<{ success: boolean; error?: string }>;
  register: (dto: RegisterDto) => Promise<{ success: boolean; error?: string }>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [state, setState] = useState<AuthState>({
    user: null,
    isAuthenticated: false,
    isLoading: true,
  });

  useEffect(() => {
    const token = localStorage.getItem('token');
    const userStr = localStorage.getItem('user');
    if (token && userStr) {
      try {
        const user: AuthResponse = JSON.parse(userStr);
        const expires = new Date(user.expiresAt);
        if (expires > new Date()) {
          setState({ user, isAuthenticated: true, isLoading: false });
          return;
        }
      } catch {}
    }
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setState(s => ({ ...s, isLoading: false }));
  }, []);

  const login = useCallback(async (dto: LoginDto) => {
    try {
      const { data } = await authApi.login(dto);
      if (data.success && data.data) {
        localStorage.setItem('token', data.data.token);
        localStorage.setItem('user', JSON.stringify(data.data));
        setState({ user: data.data, isAuthenticated: true, isLoading: false });
        return { success: true };
      }
      return { success: false, error: data.errors?.[0] ?? 'Login failed.' };
    } catch (err: any) {
      return { success: false, error: err.response?.data?.errors?.[0] ?? 'Login failed.' };
    }
  }, []);

  const register = useCallback(async (dto: RegisterDto) => {
    try {
      const { data } = await authApi.register(dto);
      if (data.success && data.data) {
        localStorage.setItem('token', data.data.token);
        localStorage.setItem('user', JSON.stringify(data.data));
        setState({ user: data.data, isAuthenticated: true, isLoading: false });
        return { success: true };
      }
      return { success: false, error: data.errors?.[0] ?? 'Registration failed.' };
    } catch (err: any) {
      return { success: false, error: err.response?.data?.errors?.[0] ?? 'Registration failed.' };
    }
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setState({ user: null, isAuthenticated: false, isLoading: false });
  }, []);

  return (
    <AuthContext.Provider value={{ ...state, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextValue => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
};
