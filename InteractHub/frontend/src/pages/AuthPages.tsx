import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { Mail, Lock, User, AtSign } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { Button, Input, PasswordStrength } from '../components/ui';
import type { LoginDto, RegisterDto } from '../types';

// ─── LOGIN PAGE ───────────────────────────────────────────────────────────────
export const LoginPage: React.FC = () => {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [apiError, setApiError] = useState('');

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<LoginDto>();

  const onSubmit = async (data: LoginDto) => {
    setApiError('');
    const result = await login(data);
    if (result.success) navigate('/');
    else setApiError(result.error ?? 'Login failed.');
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl p-8 w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-blue-600">InteractHub</h1>
          <p className="text-gray-500 mt-1">Welcome back! Sign in to continue.</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <Input
            label="Email"
            type="email"
            placeholder="you@example.com"
            icon={<Mail size={16} />}
            error={errors.email?.message}
            {...register('email', {
              required: 'Email is required.',
              pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Invalid email format.' }
            })}
          />

          <Input
            label="Password"
            type="password"
            placeholder="Your password"
            icon={<Lock size={16} />}
            error={errors.password?.message}
            {...register('password', {
              required: 'Password is required.',
              minLength: { value: 8, message: 'Password must be at least 8 characters.' }
            })}
          />

          {apiError && (
            <div className="bg-red-50 text-red-600 text-sm p-3 rounded-lg">{apiError}</div>
          )}

          <Button type="submit" className="w-full" loading={isSubmitting}>Sign In</Button>
        </form>

        <p className="text-center text-sm text-gray-500 mt-6">
          Don't have an account?{' '}
          <Link to="/register" className="text-blue-600 font-medium hover:underline">Sign up</Link>
        </p>
      </div>
    </div>
  );
};

// ─── REGISTER PAGE ────────────────────────────────────────────────────────────
export const RegisterPage: React.FC = () => {
  const { register: registerUser } = useAuth();
  const navigate = useNavigate();
  const [apiError, setApiError] = useState('');

  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<RegisterDto & { confirmPassword: string }>();
  const password = watch('password', '');

  const onSubmit = async (data: RegisterDto & { confirmPassword: string }) => {
    setApiError('');
    const { confirmPassword, ...dto } = data;
    const result = await registerUser(dto);
    if (result.success) navigate('/');
    else setApiError(result.error ?? 'Registration failed.');
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl p-8 w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-blue-600">InteractHub</h1>
          <p className="text-gray-500 mt-1">Create your account today</p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <Input
            label="Full Name"
            placeholder="John Doe"
            icon={<User size={16} />}
            error={errors.fullName?.message}
            {...register('fullName', {
              required: 'Full name is required.',
              minLength: { value: 2, message: 'Name too short.' },
              maxLength: { value: 100, message: 'Name too long.' }
            })}
          />

          <Input
            label="Username"
            placeholder="johndoe"
            icon={<AtSign size={16} />}
            error={errors.userName?.message}
            {...register('userName', {
              required: 'Username is required.',
              minLength: { value: 3, message: 'Username must be at least 3 characters.' },
              maxLength: { value: 50, message: 'Username too long.' },
              pattern: { value: /^[a-zA-Z0-9_]+$/, message: 'Only letters, numbers and underscores.' }
            })}
          />

          <Input
            label="Email"
            type="email"
            placeholder="you@example.com"
            icon={<Mail size={16} />}
            error={errors.email?.message}
            {...register('email', {
              required: 'Email is required.',
              pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Invalid email format.' }
            })}
          />

          <div>
            <Input
              label="Password"
              type="password"
              placeholder="Min. 8 characters"
              icon={<Lock size={16} />}
              error={errors.password?.message}
              {...register('password', {
                required: 'Password is required.',
                minLength: { value: 8, message: 'At least 8 characters required.' }
              })}
            />
            <PasswordStrength password={password} />
          </div>

          <Input
            label="Confirm Password"
            type="password"
            placeholder="Repeat your password"
            icon={<Lock size={16} />}
            error={errors.confirmPassword?.message}
            {...register('confirmPassword', {
              required: 'Please confirm your password.',
              validate: val => val === password || 'Passwords do not match.'
            })}
          />

          {apiError && (
            <div className="bg-red-50 text-red-600 text-sm p-3 rounded-lg">{apiError}</div>
          )}

          <Button type="submit" className="w-full" loading={isSubmitting}>Create Account</Button>
        </form>

        <p className="text-center text-sm text-gray-500 mt-6">
          Already have an account?{' '}
          <Link to="/login" className="text-blue-600 font-medium hover:underline">Sign in</Link>
        </p>
      </div>
    </div>
  );
};
