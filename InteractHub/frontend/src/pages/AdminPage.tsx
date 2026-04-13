import React, { useState, useEffect, useCallback } from 'react';
import { adminApi } from '../services/api';
import { Avatar } from '../components/ui/Avatar';
import { Trash2, UserCheck, UserX, RefreshCw, Flag, FileText, Users } from 'lucide-react';

function timeAgo(d: string) {
  const utc = /Z|[+-]\d{2}:?\d{2}$/.test(d) ? d : d + 'Z';
  const diff = Math.floor((Date.now() - new Date(utc).getTime()) / 60000);
  if (diff < 1)  return 'Vừa xong';
  if (diff < 60) return `${diff} phút trước`;
  const h = Math.floor(diff / 60);
  if (h < 24)    return `${h} giờ trước`;
  const days = Math.floor(h / 24);
  return `${days} ngày trước`;
}

// ─── STATS CARDS ─────────────────────────────────────────────────────────────
const StatsCards: React.FC = () => {
  const [stats, setStats] = useState<any>(null);

  useEffect(() => {
    adminApi.getStats().then(setStats).catch(() => {});
  }, []);

  if (!stats) return (
    <div className="flex justify-center py-8">
      <div className="w-6 h-6 border-4 border-[#1877f2] border-t-transparent rounded-full animate-spin" />
    </div>
  );

  const cards = [
    { label: 'Người dùng', value: stats.totalUsers,   color: 'bg-blue-50 text-blue-600',  Icon: Users },
    { label: 'Bài viết',   value: stats.totalPosts,   color: 'bg-green-50 text-green-600', Icon: FileText },
    { label: 'Báo cáo chờ', value: stats.totalReports, color: 'bg-red-50 text-red-600',   Icon: Flag },
  ];

  return (
    <div className="grid grid-cols-3 gap-4 mb-6">
      {cards.map(({ label, value, color, Icon }) => (
        <div key={label} className={`rounded-xl p-4 flex items-center gap-3 ${color} bg-opacity-60 border border-current/10`}>
          <Icon size={28} />
          <div>
            <p className="text-2xl font-bold">{value ?? 0}</p>
            <p className="text-sm font-medium opacity-80">{label}</p>
          </div>
        </div>
      ))}
    </div>
  );
};

// ─── USERS TAB ───────────────────────────────────────────────────────────────
const UsersTab: React.FC = () => {
  const [users, setUsers]   = useState<any[]>([]);
  const [page, setPage]     = useState(1);
  const [keyword, setKeyword] = useState('');
  const [hasNext, setHasNext] = useState(false);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (p: number, kw: string) => {
    setLoading(true);
    try {
      const res = await adminApi.getUsers(p, kw);
      setUsers(res.items ?? []);
      setHasNext(res.hasNext ?? false);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(1, ''); }, [load]);

  const search = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    load(1, keyword);
  };

  const toggle = async (id: string) => {
    await adminApi.toggleActive(id);
    setUsers(u => u.map(x => x.id === id ? { ...x, isActive: !x.isActive } : x));
  };

  return (
    <div>
      <form onSubmit={search} className="flex gap-2 mb-4">
        <input value={keyword} onChange={e => setKeyword(e.target.value)}
          placeholder="Tìm kiếm người dùng..."
          className="flex-1 border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:border-[#1877f2]" />
        <button type="submit" className="px-4 py-2 bg-[#1877f2] text-white rounded-lg text-sm font-medium hover:bg-blue-600 transition">
          Tìm
        </button>
      </form>

      {loading ? (
        <div className="flex justify-center py-8"><div className="w-6 h-6 border-4 border-[#1877f2] border-t-transparent rounded-full animate-spin" /></div>
      ) : (
        <div className="space-y-2">
          {users.map(u => (
            <div key={u.id} className="bg-white rounded-xl border border-gray-200 flex items-center gap-3 p-3">
              <Avatar name={u.fullName} src={u.avatarUrl} size={44} />
              <div className="flex-1 min-w-0">
                <p className="font-semibold text-sm truncate">{u.fullName}</p>
                <p className="text-xs text-gray-400 truncate">@{u.userName} · {u.email}</p>
                <div className="flex gap-1 mt-0.5 flex-wrap">
                  {u.roles?.map((r: string) => (
                    <span key={r} className="text-[10px] bg-blue-50 text-blue-600 px-1.5 py-0.5 rounded">{r}</span>
                  ))}
                  <span className={`text-[10px] px-1.5 py-0.5 rounded ${u.isActive ? 'bg-green-50 text-green-600' : 'bg-red-50 text-red-500'}`}>
                    {u.isActive ? 'Hoạt động' : 'Bị khóa'}
                  </span>
                </div>
              </div>
              <button onClick={() => toggle(u.id)}
                title={u.isActive ? 'Khóa tài khoản' : 'Mở khóa tài khoản'}
                className={`flex items-center gap-1 px-3 py-1.5 rounded-lg text-xs font-medium transition
                  ${u.isActive ? 'bg-red-50 text-red-600 hover:bg-red-100' : 'bg-green-50 text-green-600 hover:bg-green-100'}`}>
                {u.isActive ? <><UserX size={13} /> Khóa</> : <><UserCheck size={13} /> Mở khóa</>}
              </button>
            </div>
          ))}
          {users.length === 0 && <p className="text-center text-gray-400 py-12">Không có người dùng nào.</p>}
        </div>
      )}

      <div className="flex justify-between items-center mt-4">
        <button onClick={() => { const np = Math.max(1, page - 1); setPage(np); load(np, keyword); }}
          disabled={page === 1}
          className="px-3 py-1.5 rounded-lg text-sm border border-gray-200 disabled:opacity-40 hover:bg-gray-100">
          Trang trước
        </button>
        <span className="text-sm text-gray-500">Trang {page}</span>
        <button onClick={() => { const np = page + 1; setPage(np); load(np, keyword); }}
          disabled={!hasNext}
          className="px-3 py-1.5 rounded-lg text-sm border border-gray-200 disabled:opacity-40 hover:bg-gray-100">
          Trang sau
        </button>
      </div>
    </div>
  );
};

// ─── POSTS TAB ───────────────────────────────────────────────────────────────
const PostsTab: React.FC = () => {
  const [posts, setPosts]   = useState<any[]>([]);
  const [page, setPage]     = useState(1);
  const [keyword, setKeyword] = useState('');
  const [hasNext, setHasNext] = useState(false);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (p: number, kw: string) => {
    setLoading(true);
    try {
      const res = await adminApi.getPosts(p, kw);
      setPosts(res.items ?? []);
      setHasNext(res.hasNext ?? false);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(1, ''); }, [load]);

  const search = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    load(1, keyword);
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Xóa bài viết này?')) return;
    await adminApi.deletePost(id);
    setPosts(p => p.filter(x => x.id !== id));
  };

  return (
    <div>
      <form onSubmit={search} className="flex gap-2 mb-4">
        <input value={keyword} onChange={e => setKeyword(e.target.value)}
          placeholder="Tìm kiếm bài viết..."
          className="flex-1 border border-gray-200 rounded-lg px-3 py-2 text-sm outline-none focus:border-[#1877f2]" />
        <button type="submit" className="px-4 py-2 bg-[#1877f2] text-white rounded-lg text-sm font-medium hover:bg-blue-600 transition">
          Tìm
        </button>
      </form>

      {loading ? (
        <div className="flex justify-center py-8"><div className="w-6 h-6 border-4 border-[#1877f2] border-t-transparent rounded-full animate-spin" /></div>
      ) : (
        <div className="space-y-2">
          {posts.map(p => (
            <div key={p.id} className="bg-white rounded-xl border border-gray-200 p-3">
              <div className="flex items-start gap-3">
                <Avatar name={p.author?.fullName ?? 'U'} src={p.author?.avatarUrl} size={36} />
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between gap-2">
                    <p className="font-semibold text-sm">{p.author?.fullName}</p>
                    <p className="text-xs text-gray-400 shrink-0">{timeAgo(p.createdAt)}</p>
                  </div>
                  <p className="text-sm text-gray-700 mt-1 line-clamp-3">{p.content}</p>
                  {p.imageUrl && <img src={p.imageUrl} className="mt-2 h-24 rounded-lg object-cover" alt="" />}
                  <p className="text-xs text-gray-400 mt-1">{p.likeCount} thích · {p.commentCount} bình luận · {p.visibility}</p>
                </div>
                <button onClick={() => handleDelete(p.id)}
                  className="shrink-0 flex items-center gap-1 bg-red-50 text-red-600 hover:bg-red-100 px-2 py-1.5 rounded-lg text-xs font-medium transition">
                  <Trash2 size={12} /> Xóa
                </button>
              </div>
            </div>
          ))}
          {posts.length === 0 && <p className="text-center text-gray-400 py-12">Không có bài viết nào.</p>}
        </div>
      )}

      <div className="flex justify-between items-center mt-4">
        <button onClick={() => { const np = Math.max(1, page - 1); setPage(np); load(np, keyword); }}
          disabled={page === 1}
          className="px-3 py-1.5 rounded-lg text-sm border border-gray-200 disabled:opacity-40 hover:bg-gray-100">
          Trang trước
        </button>
        <span className="text-sm text-gray-500">Trang {page}</span>
        <button onClick={() => { const np = page + 1; setPage(np); load(np, keyword); }}
          disabled={!hasNext}
          className="px-3 py-1.5 rounded-lg text-sm border border-gray-200 disabled:opacity-40 hover:bg-gray-100">
          Trang sau
        </button>
      </div>
    </div>
  );
};

// ─── REPORTS TAB ─────────────────────────────────────────────────────────────
const ReportsTab: React.FC = () => {
  const [reports, setReports]   = useState<any[]>([]);
  const [status, setStatus]     = useState<'pending' | 'resolved' | 'dismissed'>('pending');
  const [loading, setLoading]   = useState(false);

  const load = useCallback(async (s: string) => {
    setLoading(true);
    try {
      const res = await adminApi.getReports(s);
      setReports(res ?? []);
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { load(status); }, [load, status]);

  const updateReport = async (id: number, newStatus: string) => {
    await adminApi.updateReport(id, newStatus);
    setReports(r => r.filter(x => x.id !== id));
  };

  const deleteRelatedPost = async (reportId: number, postId: number | undefined) => {
    if (!postId) return;
    if (!confirm('Xóa bài viết bị báo cáo này?')) return;
    await adminApi.deletePost(postId);
    await adminApi.updateReport(reportId, 'resolved');
    setReports(r => r.filter(x => x.id !== reportId));
  };

  return (
    <div>
      <div className="flex gap-2 mb-4">
        {(['pending', 'resolved', 'dismissed'] as const).map(s => (
          <button key={s} onClick={() => { setStatus(s); }}
            className={`px-3 py-1.5 rounded-lg text-sm font-medium transition
              ${status === s ? 'bg-[#1877f2] text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'}`}>
            {s === 'pending' ? 'Chờ xử lý' : s === 'resolved' ? 'Đã xử lý' : 'Bỏ qua'}
          </button>
        ))}
        <button onClick={() => load(status)} className="ml-auto p-1.5 rounded-lg hover:bg-gray-100">
          <RefreshCw size={15} className="text-gray-500" />
        </button>
      </div>

      {loading ? (
        <div className="flex justify-center py-8"><div className="w-6 h-6 border-4 border-[#1877f2] border-t-transparent rounded-full animate-spin" /></div>
      ) : (
        <div className="space-y-3">
          {reports.map(r => (
            <div key={r.id} className="bg-white rounded-xl border border-gray-200 p-4">
              <div className="flex items-start justify-between gap-3">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <Flag size={14} className="text-red-500 shrink-0" />
                    <p className="font-semibold text-sm">{r.reporter?.fullName ?? 'Người dùng'}</p>
                    <span className="text-xs text-gray-400">{timeAgo(r.createdAt)}</span>
                  </div>
                  <p className="text-sm text-gray-700 bg-gray-50 rounded-lg p-2 mb-1">
                    <span className="font-medium">Lý do: </span>{r.reason}
                  </p>
                  {r.post?.content && (
                    <p className="text-xs text-gray-500 line-clamp-2 border-l-2 border-gray-300 pl-2">{r.post.content}</p>
                  )}
                </div>
              </div>
              {status === 'pending' && (
                <div className="flex gap-2 mt-3 flex-wrap">
                  <button onClick={() => updateReport(r.id, 'dismissed')}
                    className="flex items-center gap-1 bg-gray-100 text-gray-600 hover:bg-gray-200 px-3 py-1.5 rounded-lg text-xs font-medium transition">
                    Bỏ qua
                  </button>
                  <button onClick={() => updateReport(r.id, 'resolved')}
                    className="flex items-center gap-1 bg-green-50 text-green-600 hover:bg-green-100 px-3 py-1.5 rounded-lg text-xs font-medium transition">
                    Đánh dấu đã xử lý
                  </button>
                  {r.post?.id && !r.post?.isDeleted && (
                    <button onClick={() => deleteRelatedPost(r.id, r.post?.id)}
                      className="flex items-center gap-1 bg-red-50 text-red-600 hover:bg-red-100 px-3 py-1.5 rounded-lg text-xs font-medium transition">
                      <Trash2 size={12} /> Xóa bài viết
                    </button>
                  )}
                </div>
              )}
            </div>
          ))}
          {reports.length === 0 && (
            <p className="text-center text-gray-400 py-12">Không có báo cáo nào.</p>
          )}
        </div>
      )}
    </div>
  );
};

// ─── ADMIN PAGE ───────────────────────────────────────────────────────────────
const AdminPage: React.FC = () => {
  const [tab, setTab] = useState<'users' | 'posts' | 'reports'>('reports');

  const tabs = [
    { key: 'reports', label: 'Báo cáo',      Icon: Flag },
    { key: 'posts',   label: 'Bài viết',     Icon: FileText },
    { key: 'users',   label: 'Người dùng',  Icon: Users },
  ] as const;

  return (
    <div className="max-w-3xl mx-auto px-4 py-6">
      <h1 className="text-2xl font-bold mb-4">Quản trị viên</h1>
      <StatsCards />

      <div className="flex gap-2 mb-4 border-b border-gray-200 pb-0">
        {tabs.map(({ key, label, Icon }) => (
          <button key={key} onClick={() => setTab(key)}
            className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition -mb-px
              ${tab === key ? 'border-[#1877f2] text-[#1877f2]' : 'border-transparent text-gray-600 hover:text-gray-800'}`}>
            <Icon size={15} /> {label}
          </button>
        ))}
      </div>

      {tab === 'reports' && <ReportsTab />}
      {tab === 'posts'   && <PostsTab />}
      {tab === 'users'   && <UsersTab />}
    </div>
  );
};

export default AdminPage;
