import React, { useState } from 'react';
import { Plus } from 'lucide-react';
import { useStories } from '../../hooks';
import { useAuth } from '../../contexts/AuthContext';
import { Avatar, Modal } from '../ui';
import type { Story } from '../../types';

const StoriesBar: React.FC = () => {
  const { stories, loading } = useStories();
  const { user } = useAuth();
  const [selected, setSelected] = useState<Story | null>(null);

  if (loading) return null;

  return (
    <>
      <div className="flex gap-3 overflow-x-auto pb-1 scrollbar-hide">
        {/* Add story */}
        <div className="flex flex-col items-center gap-1 shrink-0">
          <button className="w-14 h-14 rounded-full bg-blue-600 flex items-center justify-center text-white shadow-md hover:bg-blue-700 transition">
            <Plus size={22} />
          </button>
          <p className="text-xs text-gray-500 w-14 text-center truncate">Your story</p>
        </div>

        {stories.map(story => (
          <div key={story.id} className="flex flex-col items-center gap-1 shrink-0 cursor-pointer" onClick={() => setSelected(story)}>
            <div className="w-14 h-14 rounded-full p-0.5 bg-gradient-to-tr from-blue-500 to-purple-500">
              <Avatar
                src={story.author.avatarUrl ?? story.imageUrl}
                name={story.author.fullName}
                size="lg"
                className="border-2 border-white"
              />
            </div>
            <p className="text-xs text-gray-600 w-14 text-center truncate">{story.author.fullName.split(' ')[0]}</p>
          </div>
        ))}
      </div>

      {/* Story viewer */}
      <Modal isOpen={!!selected} onClose={() => setSelected(null)}>
        {selected && (
          <div className="text-center">
            <div className="flex items-center gap-2 mb-3">
              <Avatar src={selected.author.avatarUrl} name={selected.author.fullName} size="sm" />
              <p className="font-semibold text-sm">{selected.author.fullName}</p>
            </div>
            {selected.imageUrl && (
              <img src={selected.imageUrl} alt="story" className="w-full rounded-lg max-h-96 object-cover" />
            )}
            {selected.caption && <p className="mt-3 text-sm text-gray-600">{selected.caption}</p>}
          </div>
        )}
      </Modal>
    </>
  );
};

export default StoriesBar;
