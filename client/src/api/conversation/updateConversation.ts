import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";
import { ConversationSummary } from "./getConversationList";

type UpdateConversationRequestDto = {
    title: string;
};

type UpdateConversationResponseDto = {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    ingestionStatus: import("./getConversationById").IngestionStatus | null;
    companies: Array<{
        id: number | string;
        companyName: string;
        ticker: string;
    }>;
};

export type UpdateConversationRequest = {
    title: string;
};

export type UpdateConversationResponse = ConversationSummary;

export const updateConversation = async (
    conversationId: string,
    data: UpdateConversationRequest
): Promise<UpdateConversationResponse> => {
    const payload: UpdateConversationRequestDto = {
        title: data.title,
    };
    const response = await backendAccessPoint.put<UpdateConversationResponseDto>(
        `/api/conversation/${conversationId}`,
        payload
    );
    return {
        id: response.data.id,
        title: response.data.title,
        createdAt: response.data.createdAt,
        updatedAt: response.data.updatedAt,
        ingestionStatus: response.data.ingestionStatus,
        companies: response.data.companies.map((company) => ({
            id: company.id.toString(),
            companyName: company.companyName,
            ticker: company.ticker,
        })),
    };
};

export const useUpdateConversation = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({
            conversationId,
            data,
        }: {
            conversationId: string;
            data: UpdateConversationRequest;
        }) => updateConversation(conversationId, data),
        onSuccess: (updatedConversation) => {
            queryClient.invalidateQueries({ queryKey: ["conversations"] });
            queryClient.invalidateQueries({
                queryKey: ["conversation", updatedConversation.id],
            });
        },
        onError: (error) => {
            console.error("Error updating conversation:", error);
        },
    });
};
