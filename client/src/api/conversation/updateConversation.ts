import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";
import { Conversation } from "./getConversationList";

export type UpdateConversationRequest = {
    title: string;
};

export const updateConversation = async (
    conversationId: string,
    data: UpdateConversationRequest
): Promise<Conversation> => {
    const response = await backendAccessPoint.put<Conversation>(
        `/api/conversation/${conversationId}`,
        data
    );
    return response.data;
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

