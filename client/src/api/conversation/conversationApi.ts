import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Conversation,
    ConversationWithDetails,
    CreateConversationRequest,
    DocumentSource,
    UpdateConversationRequest,
} from "../../types/conversation";
import { backendAccessPoint } from "../backendAccessPoint";

// Server response format for conversation with details (matching your JSON example)
interface ConversationWithDetailsFromServer {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    companies?: Array<{
        id: number;
        companyName: string;
    }>;
    documents: Array<{
        id: number;
        originalFileName: string;
        contentType: string;
        fileSize: number;
        uploadedAt: string;
        description: string;
    }>;
    messages: Array<{
        id: number;
        role: "User" | "Assistant" | "System";
        content: string;
        timestamp: string;
        metadata: any | null;
        sources?: DocumentSource[]; // Source citations for Assistant messages
    }>;
}

/**
 * Creates a new conversation
 */
export const createConversation = async (
    data: CreateConversationRequest = {}
): Promise<Conversation> => {
    const response = await backendAccessPoint.post<Conversation>(
        "/api/conversation",
        data
    );
    return response.data;
};

/**
 * Hook that uses TanStack Query's useMutation to handle conversation creation
 */
export const useCreateConversation = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: createConversation,
        onSuccess: () => {
            // Invalidate conversations list to refetch
            queryClient.invalidateQueries({ queryKey: ["conversations"] });
        },
        onError: (error) => {
            console.error("Error creating conversation:", error);
        },
    });
};

/**
 * Fetches all conversations for the current user
 */
export const fetchConversations = async (): Promise<Conversation[]> => {
    const response =
        await backendAccessPoint.get<Array<ConversationWithDetailsFromServer>>(
            "/api/conversation"
        );
    return response.data.map((conv) => ({
        id: conv.id,
        title: conv.title,
        createdAt: conv.createdAt,
        updatedAt: conv.updatedAt,
        companies: conv.companies?.map((c) => ({
            id: c.id.toString(),
            companyName: c.companyName,
        })),
    }));
};

/**
 * Hook that uses TanStack Query to fetch all conversations
 */
export const useConversations = () => {
    return useQuery({
        queryKey: ["conversations"],
        queryFn: fetchConversations,
        refetchOnMount: true,
    });
};

/**
 * Fetches a specific conversation with its documents and messages
 */
export const fetchConversationById = async (
    conversationId: string
): Promise<ConversationWithDetails> => {
    const response =
        await backendAccessPoint.get<ConversationWithDetailsFromServer>(
            `/api/conversation/${conversationId}`
        );

    const serverData = response.data;

    // Convert server format to frontend format
    const convertedData: ConversationWithDetails = {
        id: serverData.id,
        title: serverData.title,
        createdAt: serverData.createdAt,
        updatedAt: serverData.updatedAt,
        companies: serverData.companies?.map((c) => ({
            id: c.id.toString(),
            companyName: c.companyName,
        })),
        documents: serverData.documents.map((doc) => ({
            id: doc.id.toString(),
            originalFileName: doc.originalFileName,
            contentType: doc.contentType,
            fileSize: doc.fileSize,
            uploadedAt: doc.uploadedAt,
            description: doc.description,
            conversationId: conversationId,
        })),
        messages: serverData.messages.map((msg) => ({
            id: msg.id.toString(),
            text: msg.content,
            role: msg.role,
            timestamp: msg.timestamp,
            conversationId: conversationId,
            sources: msg.sources,
        })),
    };

    return convertedData;
};

/**
 * Hook that uses TanStack Query to fetch a specific conversation
 */
export const useConversation = (conversationId: string) => {
    return useQuery({
        queryKey: ["conversation", conversationId],
        queryFn: () => fetchConversationById(conversationId),
        enabled: !!conversationId,
        refetchOnMount: true,
    });
};

/**
 * Updates a conversation's title
 */
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

/**
 * Hook that uses TanStack Query's useMutation to handle conversation updates
 */
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
            // Update the conversations list cache
            queryClient.invalidateQueries({ queryKey: ["conversations"] });
            // Update the specific conversation cache
            queryClient.invalidateQueries({
                queryKey: ["conversation", updatedConversation.id],
            });
        },
        onError: (error) => {
            console.error("Error updating conversation:", error);
        },
    });
};

/**
 * Deletes a conversation and all its associated data
 */
export const deleteConversation = async (
    conversationId: string
): Promise<void> => {
    await backendAccessPoint.delete(`/api/conversation/${conversationId}`);
};

/**
 * Hook that uses TanStack Query's useMutation to handle conversation deletion
 */
export const useDeleteConversation = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: deleteConversation,
        onSuccess: () => {
            // Invalidate conversations list to refetch
            queryClient.invalidateQueries({ queryKey: ["conversations"] });
        },
        onError: (error) => {
            console.error("Error deleting conversation:", error);
        },
    });
};
