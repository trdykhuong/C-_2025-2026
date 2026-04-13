import React, { useEffect, useState } from 'react';
import { TrendingUp, Users, Search } from 'lucide-react';
import { Link } from 'react-router-dom';
import { api } from '../../services/api';
import { usersApi } from '../../services/api';
import type { UserSummary } from '../../types';
import { Avatar, Card } from '../ui';
import { useDebounce } from '../../hooks';

interface HashtagTrend {
  name: string;
  postCount: number;
}

// ─── TRENDING HASHTAGS ────────────────────────────────────────────────────────
export const TrendingHashtags: React.FC = () => {
  const [trends, setTrends] = useState<HashtagTrend[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get<{ success: boolean; data: HashtagTrend[] }>('/hashtags/trending')
      .then(({ data }) => { if (data.success) setTrends(data.data ?? []); })
      .finally(() => setLoading(false));
  }, []);

  if (loading) return (
    <Card className="p-4">
      <div className="space-y-2 animate-pulse">
        {[1,2,3].map(i => <div key={i} className="h-6 bg-gray-100 rounded" />)}
      </div>
    </Card>
  );

  return (
    <Card className="p-4">
      <div className="flex items-center gap-2 mb-3">
        <TrendingUp size={16} className="text-blue-500" />
        <h3 className="font-semibold text-sm">Trending</h3>
      </div>
      {trends.length === 0 ? (
        <p className="text-xs text-gray-400">No trends yet.</p>
      ) : (
        <div className="space-y-2">
          {trends.map(t => (
            <div key={t.name} className="flex items-center justify-between">
              <span className="text-sm text-blue-600 font-medium">#{t.name}</span>
              <span className="text-xs text-gray-400">{t.postCount} posts</span>
            </div>
          ))}
        </div>
      )}
    </Card>
  );
};

// ─── PEOPLE YOU MAY KNOW ─────────────────────────────────────────────────────
export const PeopleSearch: React.FC = () => {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<UserSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const debounced = useDebounce(query, 400);

  useEffect(() => {
    if (!debounced.trim()) { setResults([]); return; }
    setLoading(true);
    usersApi.search(debounced)
      .then(({ data }) => { if (data.success) setResults(data.data ?? []); })
      .catch(() => setResults([]))
      .finally(() => setLoading(false));
  }, [debounced]);

  return (
    <Card className="p-4">
      <div className="flex items-center gap-2 mb-3">
        <Users size={16} className="text-blue-500" />
        <h3 className="font-semibold text-sm">Find People</h3>
      </div>
      <div className="relative">
        <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
        <input
          value={query}
          onChange={e => setQuery(e.target.value)}
          placeholder="Search people..."
          className="w-full pl-8 pr-3 py-1.5 text-xs rounded-lg bg-gray-50 border border-gray-200 outline-none focus:border-blue-400"
        />
      </div>
      {loading && <p className="text-xs text-gray-400 mt-2">Searching...</p>}
      {results.length > 0 && (
        <div className="mt-3 space-y-3">
          {results.slice(0, 5).map(user => (
            <Link key={user.id} to={`/profile/${user.id}`} className="flex items-center gap-2 group">
              <Avatar src={user.avatarUrl} name={user.fullName} size="sm" />
              <div>
                <p className="text-xs font-semibold group-hover:text-blue-600">{user.fullName}</p>
                <p className="text-[11px] text-gray-400">@{user.userName}</p>
              </div>
            </Link>
          ))}
        </div>
      )}
    </Card>
  );
};
