import axios from 'axios';
import {
    clearTokens,
    getAccessToken,
    getRefreshToken,
    setAccessToken,
} from '../utils/tokenManager';
import { apiBaseUrl, toApiUrl } from './apiBaseUrl';

export const backendAccessPoint = axios.create({
    baseURL: apiBaseUrl || undefined,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 60000, // 60 second timeout for AI/LLM requests
});

// Request interceptor to add Authorization header
backendAccessPoint.interceptors.request.use(
    config => {
        const token = getAccessToken();
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    error => {
        return Promise.reject(error);
    }
);

// Response interceptor for token refresh
backendAccessPoint.interceptors.response.use(
    response => response,
    async error => {
        const originalRequest = error.config;

        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;

            const refreshToken = getRefreshToken();
            if (refreshToken) {
                try {
                    const response = await axios.post(toApiUrl('/api/auth/refresh-token'), {
                        RefreshToken: refreshToken,
                    });

                    if (response.data.accessToken) {
                        setAccessToken(response.data.accessToken);
                        originalRequest.headers.Authorization = `Bearer ${response.data.accessToken}`;
                        return backendAccessPoint(originalRequest);
                    }
                } catch (refreshError) {
                    // Refresh failed, clear tokens and redirect to login
                    clearTokens();
                    window.location.href = '/login';
                    return Promise.reject(refreshError);
                }
            }
        }

        return Promise.reject(error);
    }
);
