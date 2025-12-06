import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

export const deleteConversation = async (conversationId: string): Promise<void> => {
    await backendAccessPoint.delete(`/api/conversation/${conversationId}`);
};

export const useDeleteConversation = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: deleteConversation,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["conversations"] });
        },
        onError: (error) => {
            console.error("Error deleting conversation:", error);
        },
    });
};

