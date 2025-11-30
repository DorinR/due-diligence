import { BrowserRouter } from 'react-router-dom';
import { Toaster } from 'sonner';
import { AuthProvider } from './contexts/AuthContext';
import { ThemeProvider } from './providers/ThemeProvider';
import { AppRoutes } from './routes';

function App() {
    return (
        <BrowserRouter>
            <AuthProvider>
                <ThemeProvider defaultTheme="light">
                    <AppRoutes />
                    <Toaster position="bottom-center" />
                </ThemeProvider>
            </AuthProvider>
        </BrowserRouter>
    );
}

export default App;
