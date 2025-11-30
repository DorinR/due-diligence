import { useMutation, useQuery } from '@tanstack/react-query';
import { backendAccessPoint } from '../backendAccessPoint';

export interface DocumentResponse {
    id: string;
    originalFileName: string;
    contentType: string;
    fileSize: number;
    uploadedAt: string;
    description: string;
    conversationId: string;
}

export interface UploadDocumentResponse {
    id: string;
    originalFileName: string;
    contentType: string;
    fileSize: number;
    uploadedAt: string;
    description: string;
    conversationId: string;
}

/**
 * Uploads a document to a specific conversation
 */
export const uploadDocument = async (
    conversationId: string,
    file: File,
    description: string = ''
): Promise<UploadDocumentResponse> => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('description', description);
    formData.append('conversationId', conversationId);

    // Override the default content-type to let the browser set it with the boundary
    const response = await backendAccessPoint.post<UploadDocumentResponse>(
        '/api/Document/upload',
        formData,
        {
            headers: {
                'Content-Type': 'multipart/form-data',
            },
        }
    );

    return response.data;
};

/**
 * Hook that uses TanStack Query's useMutation to handle document uploads
 */
export const useUploadDocument = () => {
    return useMutation({
        mutationFn: (params: { conversationId: string; file: File; description?: string }) =>
            uploadDocument(params.conversationId, params.file, params.description),
        onError: error => {
            console.error('Error uploading document:', error);
        },
    });
};

/**
 * Fetches all documents for a specific conversation
 */
export const fetchConversationDocuments = async (
    conversationId: string
): Promise<DocumentResponse[]> => {
    const response = await backendAccessPoint.get<DocumentResponse[]>(
        `/api/Document/conversation/${conversationId}`
    );
    return response.data;
};

/**
 * Hook that uses TanStack Query to fetch documents for a specific conversation
 */
export const useConversationDocuments = (conversationId: string) => {
    return useQuery({
        queryKey: ['documents', conversationId],
        queryFn: () => fetchConversationDocuments(conversationId),
        enabled: !!conversationId,
        refetchOnMount: true,
    });
};

/**
 * Fetches all documents from the backend (legacy - for backward compatibility)
 */
export const fetchDocuments = async (): Promise<DocumentResponse[]> => {
    const response = await backendAccessPoint.get<DocumentResponse[]>('/api/Document');
    return response.data;
};

/**
 * Hook that uses TanStack Query to fetch all documents (legacy - for backward compatibility)
 */
export const useDocuments = () => {
    return useQuery({
        queryKey: ['documents'],
        queryFn: fetchDocuments,
        refetchOnMount: true,
    });
};
