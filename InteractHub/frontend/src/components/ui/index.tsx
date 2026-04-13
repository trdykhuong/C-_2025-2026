import React from 'react';
import { Loader2 } from 'lucide-react';

// ─── BUTTON ──────────────────────────────────────────────────────────────────
interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  loading?: boolean;
}

export const Button: React.FC<ButtonProps> = ({
  children, variant = 'primary', size = 'md', loading, disabled, className = '', ...props
}) => {
  const base = 'inline-flex items-center justify-center gap-2 font-medium rounded-lg transition-all focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed';
  const variants = {
    primary: 'bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-500',
    secondary: 'bg-gray-100 text-gray-800 hover:bg-gray-200 focus:ring-gray-400',
    ghost: 'text-gray-600 hover:bg-gray-100 focus:ring-gray-300',
    danger: 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500',
  };
  const sizes = { sm: 'px-3 py-1.5 text-sm', md: 'px-4 py-2 text-sm', lg: 'px-6 py-3 text-base' };

  return (
    <button
      className={`${base} ${variants[variant]} ${sizes[size]} ${className}`}
      disabled={disabled || loading}
      {...props}
    >
      {loading && <Loader2 size={16} className="animate-spin" />}
      {children}
    </button>
  );
};

// ─── INPUT ───────────────────────────────────────────────────────────────────
interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  icon?: React.ReactNode;
}

export const Input: React.FC<InputProps> = ({ label, error, icon, className = '', ...props }) => (
  <div className="flex flex-col gap-1">
    {label && <label className="text-sm font-medium text-gray-700">{label}</label>}
    <div className="relative">
      {icon && <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400">{icon}</span>}
      <input
        className={`w-full rounded-lg border px-3 py-2 text-sm outline-none transition
          ${icon ? 'pl-10' : ''}
          ${error ? 'border-red-400 focus:ring-red-300' : 'border-gray-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-200'}
          ${className}`}
        {...props}
      />
    </div>
    {error && <p className="text-xs text-red-500">{error}</p>}
  </div>
);

// ─── TEXTAREA ─────────────────────────────────────────────────────────────────
interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
}

export const Textarea: React.FC<TextareaProps> = ({ label, error, className = '', ...props }) => (
  <div className="flex flex-col gap-1">
    {label && <label className="text-sm font-medium text-gray-700">{label}</label>}
    <textarea
      className={`w-full rounded-lg border px-3 py-2 text-sm outline-none transition resize-none
        ${error ? 'border-red-400 focus:ring-red-300' : 'border-gray-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-200'}
        ${className}`}
      {...props}
    />
    {error && <p className="text-xs text-red-500">{error}</p>}
  </div>
);

// ─── AVATAR ──────────────────────────────────────────────────────────────────
interface AvatarProps {
  src?: string;
  name: string;
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  className?: string;
}

export const Avatar: React.FC<AvatarProps> = ({ src, name, size = 'md', className = '' }) => {
  const sizes = { xs: 'w-6 h-6 text-xs', sm: 'w-8 h-8 text-sm', md: 'w-10 h-10 text-base', lg: 'w-14 h-14 text-lg', xl: 'w-20 h-20 text-2xl' };
  const initials = name.split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase();
  const colors = ['bg-blue-500', 'bg-purple-500', 'bg-green-500', 'bg-orange-500', 'bg-pink-500'];
  const color = colors[name.charCodeAt(0) % colors.length];

  return src
    ? <img src={src} alt={name} className={`rounded-full object-cover ${sizes[size]} ${className}`} />
    : <div className={`rounded-full flex items-center justify-center text-white font-semibold ${color} ${sizes[size]} ${className}`}>{initials}</div>;
};

// ─── SPINNER ─────────────────────────────────────────────────────────────────
export const Spinner: React.FC<{ className?: string }> = ({ className = '' }) => (
  <Loader2 className={`animate-spin text-blue-600 ${className}`} />
);

// ─── CARD ────────────────────────────────────────────────────────────────────
export const Card: React.FC<{ children: React.ReactNode; className?: string }> = ({ children, className = '' }) => (
  <div className={`bg-white rounded-xl shadow-sm border border-gray-100 ${className}`}>{children}</div>
);

// ─── POST SKELETON ────────────────────────────────────────────────────────────
export const PostSkeleton: React.FC = () => (
  <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-4 animate-pulse">
    <div className="flex items-center gap-3 mb-4">
      <div className="w-10 h-10 bg-gray-200 rounded-full" />
      <div className="flex-1">
        <div className="h-4 bg-gray-200 rounded w-32 mb-1" />
        <div className="h-3 bg-gray-200 rounded w-20" />
      </div>
    </div>
    <div className="space-y-2">
      <div className="h-4 bg-gray-200 rounded w-full" />
      <div className="h-4 bg-gray-200 rounded w-3/4" />
    </div>
    <div className="flex gap-4 mt-4 pt-4 border-t border-gray-100">
      <div className="h-8 bg-gray-200 rounded w-16" />
      <div className="h-8 bg-gray-200 rounded w-16" />
    </div>
  </div>
);

// ─── EMPTY STATE ──────────────────────────────────────────────────────────────
export const EmptyState: React.FC<{ title: string; description?: string; icon?: React.ReactNode }> = ({ title, description, icon }) => (
  <div className="flex flex-col items-center justify-center py-16 text-center">
    {icon && <div className="text-gray-300 mb-4">{icon}</div>}
    <h3 className="text-lg font-semibold text-gray-600">{title}</h3>
    {description && <p className="text-sm text-gray-400 mt-1">{description}</p>}
  </div>
);

// ─── MODAL ───────────────────────────────────────────────────────────────────
interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  children: React.ReactNode;
}

export const Modal: React.FC<ModalProps> = ({ isOpen, onClose, title, children }) => {
  if (!isOpen) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40" onClick={onClose} />
      <div className="relative bg-white rounded-2xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        {title && (
          <div className="flex items-center justify-between p-4 border-b">
            <h2 className="text-lg font-semibold">{title}</h2>
            <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl leading-none">&times;</button>
          </div>
        )}
        <div className="p-4">{children}</div>
      </div>
    </div>
  );
};

// ─── PASSWORD STRENGTH ────────────────────────────────────────────────────────
export const PasswordStrength: React.FC<{ password: string }> = ({ password }) => {
  const checks = [
    password.length >= 8,
    /[A-Z]/.test(password),
    /[0-9]/.test(password),
    /[^A-Za-z0-9]/.test(password),
  ];
  const score = checks.filter(Boolean).length;
  const labels = ['', 'Weak', 'Fair', 'Good', 'Strong'];
  const colors = ['', 'bg-red-400', 'bg-yellow-400', 'bg-blue-400', 'bg-green-500'];

  if (!password) return null;
  return (
    <div className="mt-1">
      <div className="flex gap-1 mb-1">
        {[1, 2, 3, 4].map(i => (
          <div key={i} className={`h-1 flex-1 rounded-full transition-all ${i <= score ? colors[score] : 'bg-gray-200'}`} />
        ))}
      </div>
      <p className={`text-xs ${score <= 1 ? 'text-red-500' : score === 2 ? 'text-yellow-600' : score === 3 ? 'text-blue-600' : 'text-green-600'}`}>
        {labels[score]}
      </p>
    </div>
  );
};
