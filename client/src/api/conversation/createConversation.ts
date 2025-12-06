import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";
import { Conversation } from "./getConversationList";

export type CreateConversationRequest = {
    title?: string;
};

export const createConversation = async (
    data: CreateConversationRequest = {}
): Promise<Conversation> => {
    const response = await backendAccessPoint.post<Conversation>(
        "/api/conversation",
        data
    );
    return response.data;
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
