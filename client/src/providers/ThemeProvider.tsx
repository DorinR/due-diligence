import { Theme as RadixTheme } from '@radix-ui/themes';
import '@radix-ui/themes/styles.css';
import { createContext, useContext, useEffect, useState } from 'react';

type Theme = 'dark' | 'light' | 'system';

type ThemeProviderProps = {
    children: React.ReactNode;
    defaultTheme?: Theme;
    storageKey?: string;
    accentColor?: 'indigo' | 'blue' | 'green' | 'orange' | 'tomato' | 'red' | 'purple';
    grayColor?: 'gray' | 'mauve' | 'slate' | 'sage' | 'olive' | 'sand';
    scaling?: '90%' | '95%' | '100%' | '105%' | '110%';
    radius?: 'none' | 'small' | 'medium' | 'large' | 'full';
};

type ThemeProviderState = {
    theme: Theme;
    setTheme: (theme: Theme) => void;
};

const initialState: ThemeProviderState = {
    theme: 'system',
    setTheme: () => null,
};

const ThemeProviderContext = createContext<ThemeProviderState>(initialState);

export const ThemeProvider = ({
    children,
    defaultTheme = 'light',
    storageKey = 'theme',
    accentColor = 'indigo',
    grayColor = 'mauve',
    scaling = '100%',
    radius = 'medium',
    ...props
}: ThemeProviderProps) => {
    const [theme, setTheme] = useState<Theme>(
        () => (localStorage.getItem(storageKey) as Theme) || defaultTheme
    );

    useEffect(() => {
        const root = window.document.documentElement;

        root.classList.remove('light', 'dark');

        if (theme === 'system') {
            const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches
                ? 'dark'
                : 'light';

            root.classList.add(systemTheme);
            return;
        }

        root.classList.add(theme);
    }, [theme]);

    const value = {
        theme,
        setTheme: (theme: Theme) => {
            localStorage.setItem(storageKey, theme);
            setTheme(theme);
        },
    };

    // Convert our theme state to Radix Theme's expected format
    const resolvedTheme =
        theme === 'system'
            ? window.matchMedia('(prefers-color-scheme: dark)').matches
                ? 'dark'
                : 'light'
            : theme;

    console.log('resolvedTheme', resolvedTheme);

    return (
        <ThemeProviderContext.Provider {...props} value={value}>
            <RadixTheme
                appearance={resolvedTheme}
                accentColor={accentColor}
                grayColor={grayColor}
                scaling={scaling}
                radius={radius}
            >
                {children}
            </RadixTheme>
        </ThemeProviderContext.Provider>
    );
};

export const useTheme = () => {
    const context = useContext(ThemeProviderContext);

    if (context === undefined) throw new Error('useTheme must be used within a ThemeProvider');

    return context;
};
