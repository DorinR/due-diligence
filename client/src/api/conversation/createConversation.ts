import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";
import { ConversationSummary } from "./getConversationList";

type CreateConversationRequestDto = {
    title?: string;
};

type CreateConversationResponseDto = {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    ingestionStatus: import("./getConversationById").IngestionStatus | null;
    companies: Array<{
        id: number | string;
        companyName: string;
    }>;
};

export type CreateConversationRequest = {
    title?: string;
};

export type CreateConversationResponse = ConversationSummary;

export const createConversation = async (
    data: CreateConversationRequest = {}
): Promise<CreateConversationResponse> => {
    const payload: CreateConversationRequestDto = {
        title: data.title,
    };
    const response = await backendAccessPoint.post<CreateConversationResponseDto>(
        "/api/conversation",
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
        })),
    };
};

export const useCreateConversation = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: createConversation,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["conversations"] });
        },
        onError: (error) => {
            console.error("Error creating conversation:", error);
        },
    });
};
