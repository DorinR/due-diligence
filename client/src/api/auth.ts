import { AuthResponse, LoginRequest, RegisterRequest } from '../types/auth';
import { clearTokens, getAccessToken, getRefreshToken } from '../utils/tokenManager';

export const authApi = {
    register: async (data: RegisterRequest): Promise<AuthResponse> => {
        const response = await fetch('/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
        });
        return response.json();
    },

    login: async (data: LoginRequest): Promise<AuthResponse> => {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
        });
        return response.json();
    },

    logout: async (): Promise<void> => {
        const refreshToken = getRefreshToken();
        const headers: HeadersInit = { 'Content-Type': 'application/json' };

        const accessToken = getAccessToken();
        if (accessToken) {
            headers.Authorization = `Bearer ${accessToken}`;
        }

        if (refreshToken) {
            await fetch('/api/auth/revoke-token', {
                method: 'POST',
                headers,
                body: JSON.stringify({ RefreshToken: refreshToken }),
            });
        }
        clearTokens();
    },

    refreshToken: async (): Promise<AuthResponse> => {
        const refreshTokenValue = getRefreshToken();
        if (!refreshTokenValue) {
            throw new Error('No refresh token available');
        }

        const response = await fetch('/api/auth/refresh-token', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ RefreshToken: refreshTokenValue }),
        });
        return response.json();
    },
};
