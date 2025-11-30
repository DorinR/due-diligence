import { useMutation, useQueryClient } from '@tanstack/react-query';
import { backendAccessPoint } from '../backendAccessPoint';

/**
 * Deletes a document from the backend by its ID
 */
export const deleteDocument = async (id: string): Promise<void> => {
    await backendAccessPoint.delete(`/api/Document/${id}`);
};

/**
 * Hook that uses TanStack Query's useMutation to handle document deletion
 */
export const useDeleteDocument = (conversationId?: string) => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: deleteDocument,
        onError: error => {
            console.error('Error deleting document:', error);
        },
        onSuccess: () => {
            // Invalidate relevant queries
            if (conversationId) {
                // Invalidate conversation-specific documents
                queryClient.invalidateQueries({
                    queryKey: ['documents', conversationId],
                });
                // Invalidate the conversation details to update document count
                queryClient.invalidateQueries({
                    queryKey: ['conversation', conversationId],
                });
            } else {
                // Fallback to invalidating all documents (for backward compatibility)
                queryClient.invalidateQueries({
                    queryKey: ['documents'],
                    exact: true,
                    refetchType: 'all',
                });
            }
        },
    });
};
