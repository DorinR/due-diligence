/**
 * Token management utilities for localStorage
 * Handles access and refresh token storage/retrieval
 */

const ACCESS_TOKEN_KEY = 'accessToken';
const REFRESH_TOKEN_KEY = 'refreshToken';

export type TokenPair = {
    accessToken: string;
    refreshToken: string;
};

/**
 * Gets the access token from localStorage
 * @returns {string | null} The access token or null if not found
 */
export const getAccessToken = (): string | null => {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
};

/**
 * Gets the refresh token from localStorage
 * @returns {string | null} The refresh token or null if not found
 */
export const getRefreshToken = (): string | null => {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
};

/**
 * Stores both access and refresh tokens in localStorage
 * @param {TokenPair} tokens - Object containing access and refresh tokens
 */
export const setTokens = ({ accessToken, refreshToken }: TokenPair): void => {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
};

/**
 * Stores only the access token in localStorage
 * @param {string} accessToken - The access token to store
 */
export const setAccessToken = (accessToken: string): void => {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
};

/**
 * Removes both access and refresh tokens from localStorage
 */
export const clearTokens = (): void => {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
};

/**
 * Checks if user has a valid access token
 * @returns {boolean} True if access token exists, false otherwise
 */
export const hasValidToken = (): boolean => {
    return !!getAccessToken();
};
