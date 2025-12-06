import { useQuery } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";
import { convertServerMessageToConversationMessage, MessageFromServer, ConversationMessage } from "./types";

export const getMessageListByConversation = async (
    conversationId: string
): Promise<ConversationMessage[]> => {
    const response = await backendAccessPoint.get<MessageFromServer[]>(
        `/api/conversations/${conversationId}/message`
    );

    return response.data.map((serverMessage) =>
        convertServerMessageToConversationMessage(serverMessage, conversationId)
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

