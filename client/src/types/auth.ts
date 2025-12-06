export type RegisterRequest = {
    email: string;
    password: string;
    firstName?: string;
    lastName?: string;
};

export type LoginRequest = {
    email: string;
    password: string;
};

export type AuthResponse = {
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
};

export type User = {
    id: number;
    email: string;
    firstName?: string;
    lastName?: string;
};
