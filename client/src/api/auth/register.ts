import { useMutation } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";
import { LoginResponse } from "./login";

type RegisterRequestDto = {
    email: string;
    password: string;
    firstName?: string;
    lastName?: string;
};

type RegisterResponseDto = {
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

export type RegisterRequest = {
    email: string;
    password: string;
    firstName?: string;
    lastName?: string;
};

export type RegisterResponse = LoginResponse;

export const register = async (data: RegisterRequest): Promise<RegisterResponse> => {
    const payload: RegisterRequestDto = {
        email: data.email,
        password: data.password,
        firstName: data.firstName,
        lastName: data.lastName,
    };
    const response = await backendAccessPoint.post<RegisterResponseDto>(
        "/api/auth/register",
        payload
    );
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

export const useRegister = () => {
    return useMutation({
        mutationFn: register,
        onError: (error) => {
            console.error("Error registering user:", error);
        },
    });
};
