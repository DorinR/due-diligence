import { Navigate, Route, Routes } from 'react-router-dom';
import { useGetConversationList } from '../api/conversation/getConversationList';
import { LoginForm } from '../components/auth/LoginForm';
import { RegisterForm } from '../components/auth/RegisterForm';
import { ChatApp } from '../components/ChatApp';
import { ConversationSidebar } from '../components/ConversationSidebar';
import { Layout } from '../components/layout/Layout';
import { useAuth } from '../contexts/AuthContext';
import { ConversationPage } from '../pages/ConversationPage';
import { DashboardPage } from '../pages/DashboardPage';

const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
    const { isAuthenticated, loading } = useAuth();

    if (loading) {
        return <div>Loading...</div>; // You might want to replace this with a proper loading component
    }

    if (!isAuthenticated) {
        return <Navigate to="/login" />;
    }

    return <>{children}</>;
};

const ConversationLayout = ({ children }: { children: React.ReactNode }) => {
    return (
        <div className="flex h-full">
            <ConversationSidebar />
            <div className="flex-1">{children}</div>
        </div>
    );
};

const DashboardRedirect = () => {
    const { data: conversations, isLoading } = useGetConversationList();

    if (isLoading) {
        return (
            <div className="flex h-full items-center justify-center">
                <div className="text-center">
                    <div className="mx-auto mb-4 h-8 w-8 animate-spin rounded-full border-b-2 border-blue-600"></div>
                    <p className="text-gray-600">Loading conversations...</p>
                </div>
            </div>
        );
    }

    // If user has conversations, redirect to the most recent one
    if (conversations && conversations.length > 0) {
        const mostRecentConversation = [...conversations].sort(
            (a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
        )[0];
        return <Navigate to={`/conversations/${mostRecentConversation.id}`} replace />;
    }

    // If no conversations, show the dashboard page
    return <DashboardPage />;
};

export const AppRoutes = () => {
    const { isAuthenticated } = useAuth();

    return (
        <Layout>
            <Routes>
                {/* Public Routes */}
                <Route
                    path="/login"
                    element={isAuthenticated ? <Navigate to="/" /> : <LoginForm />}
                />
                <Route
                    path="/register"
                    element={isAuthenticated ? <Navigate to="/" /> : <RegisterForm />}
                />

                {/* Protected Routes */}
                <Route
                    path="/"
                    element={
                        <ProtectedRoute>
                            <DashboardRedirect />
                        </ProtectedRoute>
                    }
                />

                <Route
                    path="/conversations/:conversationId"
                    element={
                        <ProtectedRoute>
                            <ConversationLayout>
                                <ConversationPage />
                            </ConversationLayout>
                        </ProtectedRoute>
                    }
                />

                {/* Legacy route for backward compatibility */}
                <Route
                    path="/chat"
                    element={
                        <ProtectedRoute>
                            <ChatApp />
                        </ProtectedRoute>
                    }
                />

                {/* Catch all route */}
                <Route path="*" element={<Navigate to="/" />} />
            </Routes>
        </Layout>
    );
};
