import { useQuery } from "@tanstack/react-query";
import { AuthResponse } from "../../types/auth";
import { getRefreshToken } from "../../utils/tokenManager";
import { backendAccessPoint } from "../backendAccessPoint";

export const refreshToken = async (): Promise<AuthResponse> => {
    const refreshTokenValue = getRefreshToken();
    if (!refreshTokenValue) {
        throw new Error("No refresh token available");
    }

    const response = await backendAccessPoint.post<AuthResponse>("/api/auth/refresh-token", {
        RefreshToken: refreshTokenValue,
    });
    return response.data;
};

export const useRefreshToken = () => {
    return useQuery({
        queryKey: ["auth", "refresh"],
        queryFn: refreshToken,
        retry: 1,
        refetchOnWindowFocus: false,
        refetchOnMount: true,
        refetchInterval: false,
        refetchIntervalInBackground: false,
        staleTime: Infinity,
        enabled: !!getRefreshToken(),
    });
};

