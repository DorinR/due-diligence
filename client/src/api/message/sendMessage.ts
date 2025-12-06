import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";
import { SendMessageRequest, SendMessageResponse } from "./types";

export const sendMessage = async (
    conversationId: string,
    data: SendMessageRequest
): Promise<SendMessageResponse> => {
    const response = await backendAccessPoint.post<SendMessageResponse>(
        `/api/conversations/${conversationId}/message`,
        data
    );
    return response.data;
};

export const useSendMessage = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({
            conversationId,
            data,
        }: {
            conversationId: string;
            data: SendMessageRequest;
        }) => sendMessage(conversationId, data),
        onSuccess: (_, { conversationId }) => {
            queryClient.invalidateQueries({ queryKey: ["messages", conversationId] });
            queryClient.invalidateQueries({ queryKey: ["conversation", conversationId] });
        },
        onError: (error) => {
            console.error("Error sending message:", error);
        },
    });
};

