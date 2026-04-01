import React from 'react';

const SkeletonLoader = ({ type = 'text', className = '' }) => {
  const baseClass = `skeleton ${className}`;

  if (type === 'circle') {
    return <div className={`${baseClass} rounded-full`} />;
  }

  if (type === 'card') {
    return (
      <div className={`${baseClass} min-h-[150px] p-8 space-y-4`}>
        <div className="w-1/3 h-4 skeleton bg-slate-300/30" />
        <div className="w-full h-8 skeleton bg-slate-300/30" />
        <div className="w-2/3 h-4 skeleton bg-slate-300/30" />
      </div>
    );
  }

  if (type === 'title') {
    return <div className={`${baseClass} h-10 w-2/3`} />;
  }

  return <div className={`${baseClass} h-4 w-full`} />;
};

export default SkeletonLoader;
