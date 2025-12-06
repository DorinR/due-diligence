import { useMutation } from "@tanstack/react-query";
import { AuthResponse, LoginRequest } from "../../types/auth";
import { backendAccessPoint } from "../backendAccessPoint";

export const login = async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await backendAccessPoint.post<AuthResponse>("/api/auth/login", data);
    return response.data;
};

export const useLogin = () => {
    return useMutation({
        mutationFn: login,
        onError: (error) => {
            console.error("Error logging in:", error);
        },
    });
};

