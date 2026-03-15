import { useQuery } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

export type DocumentSource = {
    documentId: number;
    documentTitle: string;
    documentLink: string;
    fileName: string | null;
    relevanceScore: number;
    chunksUsed: number;
};

export type ConversationMessage = {
    id: string;
    text: string;
    role: "User" | "Assistant" | "System";
    timestamp: string;
    conversationId: string;
    sources?: DocumentSource[];
};

type MessageDto = {
    id: string;
    content: string;
    role: "User" | "Assistant" | "System";
    timestamp: string;
    metadata: unknown | null;
    sources?: DocumentSource[];
};

export type GetMessageListByConversationResponse = ConversationMessage[];

export const getMessageListByConversation = async (
    conversationId: string
): Promise<GetMessageListByConversationResponse> => {
    const response = await backendAccessPoint.get<MessageDto[]>(
        `/api/conversations/${conversationId}/message`
    );

    return response.data.map((serverMessage) =>
        ({
            id: serverMessage.id.toString(),
            text: serverMessage.content,
            role: serverMessage.role,
            timestamp: serverMessage.timestamp,
            conversationId,
            sources: serverMessage.sources,
        })
    );
};

export const useGetMessageListByConversation = (conversationId: string) => {
    return useQuery({
        queryKey: ["messages", conversationId],
        queryFn: () => getMessageListByConversation(conversationId),
        enabled: !!conversationId,
        refetchOnMount: true,
    });
};
