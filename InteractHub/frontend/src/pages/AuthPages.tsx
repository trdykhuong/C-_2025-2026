import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { authApi } from '../services/api';
import { useAuth } from '../contexts/AuthContext';

// ─── PASSWORD STRENGTH ────────────────────────────────────────────────────────
const PasswordStrength: React.FC<{ pwd: string }> = ({ pwd }) => {
  const score = [pwd.length >= 8, /[A-Z]/.test(pwd), /[0-9]/.test(pwd), /[^a-zA-Z0-9]/.test(pwd)].filter(Boolean).length;
  const labels = ['', 'Yếu', 'Trung bình', 'Khá', 'Mạnh'];
  const colors = ['', 'bg-red-400', 'bg-yellow-400', 'bg-blue-400', 'bg-green-500'];
  if (!pwd) return null;
  return (
    <div className="mt-1">
      <div className="flex gap-1">
        {[1,2,3,4].map(i => <div key={i} className={`h-1 flex-1 rounded-full ${i <= score ? colors[score] : 'bg-gray-200'}`} />)}
      </div>
      <p className="text-xs text-gray-500 mt-0.5">{labels[score]}</p>
    </div>
  );
};

// ─── LOGIN PAGE ───────────────────────────────────────────────────────────────
export const LoginPage: React.FC = () => {
  const { setAuth } = useAuth();
  const navigate    = useNavigate();
  const [err, setErr] = useState('');
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<{ email: string; password: string }>();

  const onSubmit = async (d: any) => {
    setErr('');
    const res = await authApi.login(d);
    if (res.success && res.data) { setAuth(res.data); navigate('/'); }
    else setErr(res.message ?? 'Đăng nhập thất bại.');
  };

  return (
    <div className="min-h-screen bg-[#f0f2f5] flex flex-col items-center justify-center px-4">
      <div className="text-center mb-6">
        <h1 className="text-[#1877f2] text-5xl font-bold italic">interacthub</h1>
        <p className="text-xl text-gray-600 mt-2">Kết nối với bạn bè và thế giới xung quanh.</p>
      </div>
      <div className="bg-white rounded-2xl shadow-lg p-6 w-full max-w-sm">
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
          <input {...register('email', { required: 'Bắt buộc', pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Email không hợp lệ' } })}
            type="email" placeholder="Email hoặc số điện thoại"
            className="border border-gray-300 rounded-lg px-4 py-3 text-base outline-none focus:border-[#1877f2] focus:ring-2 focus:ring-blue-100" />
          {errors.email && <p className="text-red-500 text-xs -mt-2">{errors.email.message}</p>}

          <input {...register('password', { required: 'Bắt buộc', minLength: { value: 8, message: 'Tối thiểu 8 ký tự' } })}
            type="password" placeholder="Mật khẩu"
            className="border border-gray-300 rounded-lg px-4 py-3 text-base outline-none focus:border-[#1877f2] focus:ring-2 focus:ring-blue-100" />
          {errors.password && <p className="text-red-500 text-xs -mt-2">{errors.password.message}</p>}

          {err && <p className="bg-red-50 text-red-600 text-sm p-3 rounded-lg">{err}</p>}

          <button type="submit" disabled={isSubmitting}
            className="bg-[#1877f2] text-white font-bold py-3 rounded-lg text-lg hover:bg-blue-600 transition disabled:opacity-60">
            {isSubmitting ? 'Đang đăng nhập...' : 'Đăng nhập'}
          </button>

          <div className="text-center">
            <a href="#" className="text-[#1877f2] text-sm hover:underline">Quên mật khẩu?</a>
          </div>
          <div className="border-t border-gray-200 pt-4 text-center">
            <Link to="/register"
              className="bg-[#42b72a] text-white font-bold px-6 py-2.5 rounded-lg hover:bg-green-600 transition inline-block">
              Tạo tài khoản mới
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
};

// ─── REGISTER PAGE ────────────────────────────────────────────────────────────
export const RegisterPage: React.FC = () => {
  const { setAuth } = useAuth();
  const navigate    = useNavigate();
  const [err, setErr] = useState('');
  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<any>();
  const pwd = watch('password', '');

  const onSubmit = async (d: any) => {
    if (d.password !== d.confirmPassword) { setErr('Mật khẩu xác nhận không khớp.'); return; }
    setErr('');
    const { confirmPassword, ...dto } = d;
    const res = await authApi.register(dto);
    if (res.success && res.data) { setAuth(res.data); navigate('/'); }
    else setErr(res.message ?? 'Đăng ký thất bại.');
  };

  const inputClass = "border border-gray-300 rounded-lg px-3 py-2.5 text-sm outline-none focus:border-[#1877f2] focus:ring-2 focus:ring-blue-100";

  return (
    <div className="min-h-screen bg-[#f0f2f5] flex items-center justify-center px-4">
      <div className="bg-white rounded-2xl shadow-lg p-6 w-full max-w-md">
        <div className="text-center mb-6">
          <h1 className="text-2xl font-bold">Tạo tài khoản mới</h1>
          <p className="text-gray-500 text-sm">Nhanh chóng và dễ dàng.</p>
        </div>
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
          <input {...register('fullName', { required: 'Bắt buộc', minLength: { value: 2, message: 'Tối thiểu 2 ký tự' } })}
            placeholder="Họ và tên" className={inputClass} />
          {errors.fullName && <p className="text-red-500 text-xs -mt-2">{String(errors.fullName.message)}</p>}

          <input {...register('userName', { required: 'Bắt buộc', minLength: { value: 3, message: 'Tối thiểu 3 ký tự' }, pattern: { value: /^[a-zA-Z0-9_]+$/, message: 'Chỉ chữ, số và _' } })}
            placeholder="Tên người dùng (username)" className={inputClass} />
          {errors.userName && <p className="text-red-500 text-xs -mt-2">{String(errors.userName.message)}</p>}

          <input {...register('email', { required: 'Bắt buộc', pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Email không hợp lệ' } })}
            type="email" placeholder="Email" className={inputClass} />
          {errors.email && <p className="text-red-500 text-xs -mt-2">{String(errors.email.message)}</p>}

          <div>
            <input {...register('password', { required: 'Bắt buộc', minLength: { value: 8, message: 'Tối thiểu 8 ký tự' }, validate: v => /[0-9]/.test(v) || 'Mật khẩu phải chứa ít nhất 1 chữ số' })}
              type="password" placeholder="Mật khẩu mới" className={`${inputClass} w-full`} />
            {errors.password && <p className="text-red-500 text-xs mt-1">{String(errors.password.message)}</p>}
            <PasswordStrength pwd={pwd} />
          </div>

          <input {...register('confirmPassword', { required: 'Bắt buộc' })}
            type="password" placeholder="Xác nhận mật khẩu" className={inputClass} />

          {err && <p className="bg-red-50 text-red-600 text-sm p-3 rounded-lg">{err}</p>}

          <p className="text-xs text-gray-500 text-center">
            Bằng cách nhấp vào Đăng ký, bạn đồng ý với <a href="#" className="text-[#1877f2]">Điều khoản</a> của chúng tôi.
          </p>

          <button type="submit" disabled={isSubmitting}
            className="bg-[#42b72a] text-white font-bold py-2.5 rounded-lg hover:bg-green-600 transition disabled:opacity-60">
            {isSubmitting ? 'Đang đăng ký...' : 'Đăng ký'}
          </button>

          <div className="text-center border-t border-gray-200 pt-3">
            <Link to="/login" className="text-[#1877f2] font-medium text-sm hover:underline">
              Bạn đã có tài khoản?
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
};
