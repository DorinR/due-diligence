import { ExitIcon, MoonIcon, SunIcon } from '@radix-ui/react-icons';
import { Avatar, Text } from '@radix-ui/themes';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useTheme } from '../../providers/ThemeProvider';
import { Button } from '../ui/button/Button';

const ENABLE_THEME_TOGGLE = false;

export const Navbar = () => {
    const { isAuthenticated, user, logout } = useAuth();
    const { theme, setTheme } = useTheme();
    const navigate = useNavigate();

    const handleLogout = async () => {
        await logout();
        navigate('/login');
    };

    const toggleTheme = () => {
        setTheme(theme === 'dark' ? 'light' : 'dark');
    };

    return (
        <nav className="border-b-2 border-gray-300 bg-gray-100">
            <div className="mx-auto px-8">
                <div className="flex items-center justify-between py-4">
                    <Link to="/" className="flex items-center space-x-4 no-underline">
                        <div className="font-title text-2xl">Fincite</div>
                    </Link>
                    <div className="flex items-center space-x-4">
                        {isAuthenticated ? (
                            <div className="flex items-center space-x-4">
                                <div className="flex items-center space-x-2">
                                    <Avatar
                                        size="2"
                                        radius="full"
                                        fallback={user?.email?.[0].toUpperCase() || '?'}
                                        color="blue"
                                    />
                                    <Text size="2" weight="medium" className="text-foreground">
                                        {user?.email}
                                    </Text>
                                </div>
                                <Button
                                    onClick={handleLogout}
                                    variant="soft"
                                    icon={ExitIcon}
                                    iconPosition="left"
                                >
                                    Sign out
                                </Button>
                            </div>
                        ) : (
                            <div className="flex items-center space-x-3">
                                <Button variant="soft">
                                    <Link to="/login">Sign in</Link>
                                </Button>
                                <Button variant="soft">
                                    <Link to="/register">Sign up</Link>
                                </Button>
                            </div>
                        )}
                        {ENABLE_THEME_TOGGLE && (
                            <button
                                onClick={toggleTheme}
                                className="hover:bg-muted dark:hover:bg-muted rounded-lg p-2"
                                aria-label="Toggle theme"
                            >
                                {theme === 'dark' ? (
                                    <SunIcon className="text-foreground h-5 w-5" />
                                ) : (
                                    <MoonIcon className="text-foreground h-5 w-5" />
                                )}
                            </button>
                        )}
                    </div>
                </div>
            </div>
        </nav>
    );
};
