import { useLogin } from "@/api/auth/login";
import { useLogout } from "@/api/auth/logout";
import { useRefreshToken } from "@/api/auth/refreshToken";
import { useRegister } from "@/api/auth/register";
import React, { createContext, useContext, useEffect, useState } from "react";
import { LoginRequest, RegisterRequest, User } from "../types/auth";
import { clearTokens, hasValidToken, setTokens } from "../utils/tokenManager";

type AuthContextType = {
    isAuthenticated: boolean;
    user: User | null;
    login: (data: LoginRequest) => Promise<void>;
    register: (data: RegisterRequest) => Promise<void>;
    logout: () => Promise<void>;
    loading: boolean;
};

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error("useAuth must be used within an AuthProvider");
    }
    return context;
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
    children,
}) => {
    const [user, setUser] = useState<User | null>(null);
    const [loading, setLoading] = useState(true);

    const loginMutation = useLogin();
    const registerMutation = useRegister();
    const logoutMutation = useLogout();
    const {
        data: refreshData,
        isLoading: isRefreshing,
        error: refreshError,
    } = useRefreshToken();

    useEffect(() => {
        // If we have tokens but refresh failed, clear everything
        if (refreshError && hasValidToken()) {
            clearTokens();
            setUser(null);
        }

        // If refresh succeeded, set user
        if (refreshData?.success && refreshData.user) {
            setUser(refreshData.user);
            // Update tokens if new ones were provided
            if (refreshData.accessToken && refreshData.refreshToken) {
                setTokens({
                    accessToken: refreshData.accessToken,
                    refreshToken: refreshData.refreshToken,
                });
            }
        }

        // If no tokens and no refresh happening, we're done loading
        if (!hasValidToken() && !isRefreshing) {
            setLoading(false);
        }

        // If refresh is complete (success or failure), we're done loading
        if (!isRefreshing) {
            setLoading(false);
        }
    }, [refreshData, isRefreshing, refreshError]);

    const login = async (data: LoginRequest) => {
        const response = await loginMutation.mutateAsync(data);
        if (
            response.success &&
            response.user &&
            response.accessToken &&
            response.refreshToken
        ) {
            setUser(response.user);
            setTokens({
                accessToken: response.accessToken,
                refreshToken: response.refreshToken,
            });
        } else {
            throw new Error(response.message || "Login failed");
        }
    };

    const register = async (data: RegisterRequest) => {
        const response = await registerMutation.mutateAsync(data);
        if (
            response.success &&
            response.user &&
            response.accessToken &&
            response.refreshToken
        ) {
            setUser(response.user);
            setTokens({
                accessToken: response.accessToken,
                refreshToken: response.refreshToken,
            });
        } else {
            throw new Error(response.message || "Registration failed");
        }
    };

    const logout = async () => {
        await logoutMutation.mutateAsync();
        setUser(null);
    };

    const value = {
        isAuthenticated: !!user,
        user,
        login,
        register,
        logout,
        loading: loading || isRefreshing,
    };

    return (
        <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
    );
};
