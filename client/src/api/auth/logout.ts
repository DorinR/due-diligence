import { useMutation } from "@tanstack/react-query";
import { clearTokens, getRefreshToken } from "../../utils/tokenManager";
import { backendAccessPoint } from "../backendAccessPoint";

type LogoutRequestDto = {
    RefreshToken: string;
};

export const logout = async (): Promise<void> => {
    const refreshToken = getRefreshToken();
    if (refreshToken) {
        const payload: LogoutRequestDto = {
            RefreshToken: refreshToken,
        };
        await backendAccessPoint.post("/api/auth/revoke-token", payload);
    }
    clearTokens();
};

export const useLogout = () => {
    return useMutation({
        mutationFn: logout,
        onError: (error) => {
            console.error("Error logging out:", error);
        },
    });
};
