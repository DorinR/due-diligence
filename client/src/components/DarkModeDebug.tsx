import React, { useEffect, useState } from 'react';
import { useTheme } from '../providers/ThemeProvider';

export const DarkModeDebug: React.FC = () => {
  const { theme, setTheme } = useTheme();
  const [debug, setDebug] = useState({
    theme: theme,
    darkClass: false,
    systemDark: false,
    htmlClasses: '',
  });

  useEffect(() => {
    const updateDebug = () => {
      const html = document.documentElement;
      setDebug({
        theme: theme,
        darkClass: html.classList.contains('dark'),
        systemDark: window.matchMedia('(prefers-color-scheme: dark)').matches,
        htmlClasses: html.className,
      });
    };

    updateDebug();

    // Listen for system color scheme changes
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    mediaQuery.addEventListener('change', updateDebug);

    // Listen for class changes on html element
    const observer = new MutationObserver(updateDebug);
    observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ['class'],
    });

    return () => {
      mediaQuery.removeEventListener('change', updateDebug);
      observer.disconnect();
    };
  }, [theme]);

  const toggleTheme = () => {
    if (theme === 'dark') {
      setTheme('light');
    } else if (theme === 'light') {
      setTheme('system');
    } else {
      setTheme('dark');
    }
  };

  return (
    <div className="fixed right-4 bottom-4 z-50 rounded-lg bg-white p-4 shadow-lg dark:bg-gray-800">
      <div className="flex items-center justify-between gap-4">
        <h3 className="font-bold dark:text-white">Dark Mode Debug</h3>
        <button
          onClick={toggleTheme}
          className="rounded bg-blue-500 px-3 py-1 text-xs text-white hover:bg-blue-600"
        >
          Toggle Theme
        </button>
      </div>
      <ul className="mt-2 space-y-1 text-sm dark:text-gray-300">
        <li>Theme Setting: {debug.theme}</li>
        <li>Dark Class Present: {debug.darkClass.toString()}</li>
        <li>System Dark Mode: {debug.systemDark.toString()}</li>
        <li>HTML Classes: {debug.htmlClasses}</li>
      </ul>
    </div>
  );
};
