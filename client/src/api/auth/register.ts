import { useMutation } from "@tanstack/react-query";
import { AuthResponse, RegisterRequest } from "../../types/auth";
import { backendAccessPoint } from "../backendAccessPoint";

export const register = async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await backendAccessPoint.post<AuthResponse>("/api/auth/register", data);
    return response.data;
};

export const useRegister = () => {
    return useMutation({
        mutationFn: register,
        onError: (error) => {
            console.error("Error registering user:", error);
        },
    });
};

