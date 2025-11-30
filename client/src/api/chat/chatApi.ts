import { useMutation } from '@tanstack/react-query';
import { DocumentSource } from '../../types/conversation';
import { backendAccessPoint } from '../backendAccessPoint';

// Types to match backend request/response
export interface QueryRequest {
    query: string;
    conversationId: string;
    limit?: number;
}

export interface QueryAllConversationsRequest {
    query: string;
    limit?: number;
}

export type QueryIntent = 'Factual' | 'Comprehensive' | 'Exploratory' | 'Comparative';

export interface RetrievalConfig {
    maxK: number;
    minSimilarity: number;
    description: string;
}

export interface RetrievedChunk {
    fullDocumentText: string;
    documentId: string;
    documentTitle: string;
    similarity: number;
}

/**
 * Response from the query knowledge base endpoint
 * Includes LLM response, retrieved chunks, source citations, and retrieval metadata
 */
export interface ChatResponse {
    originalQuery: string;
    processedQuery: string;
    conversationId: number;
    llmResponse: string;
    retrievedChunks: RetrievedChunk[];

    // Query classification (not displayed in UI)
    intent: QueryIntent;
    intentReasoning: string;

    // Retrieval configuration used (not displayed in UI)
    retrievalConfig: RetrievalConfig;

    // Source citations
    sources: DocumentSource[];

    // Retrieval statistics
    totalChunks: number;
    uniqueDocuments: number;
}

/**
 * Low-level function to make API call to the conversation-scoped query endpoint
 */
const sendChatMessage = async (request: QueryRequest): Promise<ChatResponse> => {
    const response = await backendAccessPoint.post<ChatResponse>('/api/query/query', request);
    return response.data;
};

/**
 * Low-level function to query across all user's conversations
 */
const queryAllConversations = async (
    request: QueryAllConversationsRequest
): Promise<ChatResponse> => {
    const response = await backendAccessPoint.post<ChatResponse>(
        '/api/query/query-all-conversations',
        request
    );
    return response.data;
};

/**
 * Hook that uses TanStack Query's useMutation to handle conversation-scoped chat API calls
 */
export const useSendChatMessage = () => {
    return useMutation({
        mutationFn: sendChatMessage,
        onError: error => {
            console.error('Error sending chat message:', error);
        },
    });
};

/**
 * Hook that uses TanStack Query's useMutation to query across all conversations
 */
export const useQueryAllConversations = () => {
    return useMutation({
        mutationFn: queryAllConversations,
        onError: error => {
            console.error('Error querying all conversations:', error);
        },
    });
};
