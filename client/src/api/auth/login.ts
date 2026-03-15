import { useMutation } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

type AuthUserDto = {
    id: number;
    email: string;
    firstName?: string;
    lastName?: string;
};

type LoginResponseDto = {
    success: boolean;
    message: string;
    user?: AuthUserDto;
    accessToken?: string;
    refreshToken?: string;
};

type LoginRequestDto = {
    email: string;
    password: string;
};

export type AuthUser = {
    id: number;
    email: string;
    firstName?: string;
    lastName?: string;
};

export type LoginResponse = {
    success: boolean;
    message: string;
    user?: AuthUser;
    accessToken?: string;
    refreshToken?: string;
};

export type LoginRequest = {
    email: string;
    password: string;
};

export const login = async (data: LoginRequest): Promise<LoginResponse> => {
    const payload: LoginRequestDto = {
        email: data.email,
        password: data.password,
    };
    const response = await backendAccessPoint.post<LoginResponseDto>("/api/auth/login", payload);
    return {
        success: response.data.success,
        message: response.data.message,
        user: response.data.user
            ? {
                  id: response.data.user.id,
                  email: response.data.user.email,
                  firstName: response.data.user.firstName,
                  lastName: response.data.user.lastName,
              }
            : undefined,
        accessToken: response.data.accessToken,
        refreshToken: response.data.refreshToken,
    };
};

export const useLogin = () => {
    return useMutation({
        mutationFn: login,
        onError: (error) => {
            console.error("Error logging in:", error);
        },
    });
};
