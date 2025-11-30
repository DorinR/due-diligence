import { useMutation, useQuery } from '@tanstack/react-query';
import { AuthResponse, LoginRequest, RegisterRequest } from '../../types/auth';
import { clearTokens, getRefreshToken } from '../../utils/tokenManager';
import { backendAccessPoint } from '../backendAccessPoint';

/**
 * Registers a new user
 */
export const register = async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await backendAccessPoint.post<AuthResponse>('/api/auth/register', data);
    return response.data;
};

/**
 * Hook that uses TanStack Query's useMutation to handle user registration
 */
export const useRegister = () => {
    return useMutation({
        mutationFn: register,
        onError: error => {
            console.error('Error registering user:', error);
        },
    });
};

/**
 * Logs in a user
 */
export const login = async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await backendAccessPoint.post<AuthResponse>('/api/auth/login', data);
    return response.data;
};

/**
 * Hook that uses TanStack Query's useMutation to handle user login
 */
export const useLogin = () => {
    return useMutation({
        mutationFn: login,
        onError: error => {
            console.error('Error logging in:', error);
        },
    });
};

/**
 * Logs out the current user
 */
export const logout = async (): Promise<void> => {
    const refreshToken = getRefreshToken();
    if (refreshToken) {
        await backendAccessPoint.post('/api/auth/revoke-token', { RefreshToken: refreshToken });
    }
    clearTokens();
};

/**
 * Hook that uses TanStack Query's useMutation to handle user logout
 */
export const useLogout = () => {
    return useMutation({
        mutationFn: logout,
        onError: error => {
            console.error('Error logging out:', error);
        },
    });
};

/**
 * Refreshes the authentication token
 */
export const refreshToken = async (): Promise<AuthResponse> => {
    const refreshTokenValue = getRefreshToken();
    if (!refreshTokenValue) {
        throw new Error('No refresh token available');
    }

    const response = await backendAccessPoint.post<AuthResponse>('/api/auth/refresh-token', {
        RefreshToken: refreshTokenValue,
    });
    return response.data;
};

/**
 * Hook that uses TanStack Query to handle token refresh
 * Note: This is now mainly used for initial authentication check
 */
export const useRefreshToken = () => {
    return useQuery({
        queryKey: ['auth', 'refresh'],
        queryFn: refreshToken,
        retry: 1,
        refetchOnWindowFocus: false, // Disabled since we now use interceptors
        refetchOnMount: true,
        refetchInterval: false, // Disabled since we now use interceptors
        refetchIntervalInBackground: false,
        staleTime: Infinity, // Never consider stale since we use interceptors
        enabled: !!getRefreshToken(), // Only run if we have a refresh token
    });
};
