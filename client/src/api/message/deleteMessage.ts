import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

export const deleteMessage = async (conversationId: string, messageId: string): Promise<void> => {
    await backendAccessPoint.delete(`/api/conversations/${conversationId}/message/${messageId}`);
};

export const useDeleteMessage = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({
            conversationId,
            messageId,
        }: {
            conversationId: string;
            messageId: string;
        }) => deleteMessage(conversationId, messageId),
        onSuccess: (_, { conversationId }) => {
            queryClient.invalidateQueries({ queryKey: ["messages", conversationId] });
            queryClient.invalidateQueries({ queryKey: ["conversation", conversationId] });
        },
        onError: (error) => {
            console.error("Error deleting message:", error);
        },
    });
};

