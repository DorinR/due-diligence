import { useMutation } from "@tanstack/react-query";
import { clearTokens, getRefreshToken } from "../../utils/tokenManager";
import { backendAccessPoint } from "../backendAccessPoint";

export const logout = async (): Promise<void> => {
    const refreshToken = getRefreshToken();
    if (refreshToken) {
        await backendAccessPoint.post("/api/auth/revoke-token", { RefreshToken: refreshToken });
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

