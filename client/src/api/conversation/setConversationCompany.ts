import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

export type SetConversationCompanyRequest = {
    conversationId: string;
    companyName: string;
};

export type SetConversationCompanyResponse = {
    id: string;
    title: string;
    companies: Array<{
        id: string;
        companyName: string;
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
    const response = await backendAccessPoint.post<SetConversationCompanyResponse>(
        "/api/conversation/company",
        {
            conversationId: Number(data.conversationId),
            companyName: data.companyName,
        }
    );

    return response.data;
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

