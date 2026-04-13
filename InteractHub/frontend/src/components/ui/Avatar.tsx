// ─── src/components/ui/Avatar.tsx ──────────────────────────────────────────
import React from 'react';

const COLORS = ['#1877f2','#42b72a','#f02849','#e67e22','#8e44ad','#16a085'];

export const Avatar: React.FC<{
  name: string;
  src?: string | null;
  size?: number;
  className?: string;
}> = ({ name, src, size = 40, className = '' }) => {
  const initials = name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase();
  const color    = COLORS[name.charCodeAt(0) % COLORS.length];

  if (src) return (
    <img src={src} alt={name}
      style={{ width: size, height: size }}
      className={`rounded-full object-cover shrink-0 ${className}`} />
  );

  return (
    <div
      style={{ width: size, height: size, background: color, fontSize: size * 0.38 }}
      className={`rounded-full flex items-center justify-center text-white font-bold shrink-0 ${className}`}
    >
      {initials}
    </div>
  );
};
