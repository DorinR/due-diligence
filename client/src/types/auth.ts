export interface RegisterRequest {
    email: string;
    password: string;
    firstName?: string;
    lastName?: string;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface AuthResponse {
    success: boolean;
    message: string;
    user?: {
        id: number;
        email: string;
        firstName?: string;
        lastName?: string;
    };
    accessToken?: string;
    refreshToken?: string;
}

export interface User {
    id: number;
    email: string;
    firstName?: string;
    lastName?: string;
}
