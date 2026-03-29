import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

type SetConversationCompanyRequestDto = {
    conversationId: number;
    companyName: string;
    companyTicker?: string;
    filingTypes: string[];
};

type SetConversationCompanyResponseDto = {
    id: string;
    title: string;
    companies: Array<{
        id: number | string;
        companyName: string;
        ticker: string;
    }>;
    createdAt: string;
    updatedAt: string;
};

export type SetConversationCompanyRequest = {
    conversationId: string;
    companyName: string;
    companyTicker?: string;
    filingTypes: string[];
};

export type SetConversationCompanyResponse = {
    id: string;
    title: string;
    companies: Array<{
        id: string;
        companyName: string;
        ticker: string;
    }>;
    createdAt: string;
    updatedAt: string;
};

/**
 * Sets (or replaces) the company associated with a conversation.
 */
export const setConversationCompany = async (
    data: SetConversationCompanyRequest
): Promise<SetConversationCompanyResponse> => {
    const payload: SetConversationCompanyRequestDto = {
        conversationId: Number(data.conversationId),
        companyName: data.companyName,
        companyTicker: data.companyTicker,
        filingTypes: data.filingTypes,
    };
    const response = await backendAccessPoint.post<SetConversationCompanyResponseDto>(
        "/api/conversation/company",
        payload
    );

    return {
        id: response.data.id,
        title: response.data.title,
        companies: response.data.companies.map((company) => ({
            id: company.id.toString(),
            companyName: company.companyName,
            ticker: company.ticker,
        })),
        createdAt: response.data.createdAt,
        updatedAt: response.data.updatedAt,
    };
};

export const useSetConversationCompany = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (data: SetConversationCompanyRequest) => setConversationCompany(data),
        onSuccess: (updatedConversation) => {
            queryClient.invalidateQueries({ queryKey: ["conversations"] });
            queryClient.invalidateQueries({
                queryKey: ["conversation", updatedConversation.id],
            });
        },
        onError: (error) => {
            console.error("Error setting conversation company:", error);
        },
    });
};
