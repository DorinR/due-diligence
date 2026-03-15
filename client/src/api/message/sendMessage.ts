import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

export enum MessageRole {
    User = 0,
    Assistant = 1,
    System = 2,
}

type SendMessageRequestDto = {
    content: string;
    role: MessageRole;
};

type SendMessageResponseDto = {
    id: string;
    text: string;
    role: "User" | "Assistant" | "System";
    timestamp: string;
    conversationId: string;
};

export type SendMessageRequest = {
    content: string;
    role: MessageRole;
};

export type SendMessageResponse = {
    id: string;
    text: string;
    role: "User" | "Assistant" | "System";
    timestamp: string;
    conversationId: string;
};

export const sendMessage = async (
    conversationId: string,
    data: SendMessageRequest
): Promise<SendMessageResponse> => {
    const payload: SendMessageRequestDto = {
        content: data.content,
        role: data.role,
    };

    const response = await backendAccessPoint.post<SendMessageResponseDto>(
        `/api/conversations/${conversationId}/message`,
        payload
    );
    return {
        id: response.data.id,
        text: response.data.text,
        role: response.data.role,
        timestamp: response.data.timestamp,
        conversationId: response.data.conversationId,
    };
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
