import { useQuery } from "@tanstack/react-query";
import { getRefreshToken } from "../../utils/tokenManager";
import { backendAccessPoint } from "../backendAccessPoint";
import { LoginResponse } from "./login";

type RefreshTokenRequestDto = {
    RefreshToken: string;
};

type RefreshTokenResponseDto = {
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

export type RefreshTokenResponse = LoginResponse;

export const refreshToken = async (): Promise<RefreshTokenResponse> => {
    const refreshTokenValue = getRefreshToken();
    if (!refreshTokenValue) {
        throw new Error("No refresh token available");
    }

    const payload: RefreshTokenRequestDto = {
        RefreshToken: refreshTokenValue,
    };

    const response = await backendAccessPoint.post<RefreshTokenResponseDto>(
        "/api/auth/refresh-token",
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
