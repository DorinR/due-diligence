import { useMutation } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

export type DocumentSource = {
    documentId: number;
    documentTitle: string;
    documentLink: string;
    fileName: string | null;
    relevanceScore: number;
    chunksUsed: number;
};

export type ConversationQueryRequest = {
    query: string;
    conversationId: string;
    limit?: number;
};

export type QueryIntent =
    | "Factual"
    | "Comprehensive"
    | "Exploratory"
    | "Comparative";

export type RetrievalConfig = {
    maxK: number;
    minSimilarity: number;
    description: string;
};

export type RetrievedChunk = {
    fullDocumentText: string;
    documentId: string;
    documentTitle: string;
    similarity: number;
};

export type ConversationQueryResponse = {
    originalQuery: string;
    processedQuery: string;
    conversationId: number;
    llmResponse: string;
    retrievedChunks: RetrievedChunk[];
    intent: QueryIntent;
    intentReasoning: string;
    retrievalConfig: RetrievalConfig;
    sources: DocumentSource[];
    totalChunks: number;
    uniqueDocuments: number;
};

export const sendConversationQuery = async (
    request: ConversationQueryRequest
): Promise<ConversationQueryResponse> => {
    const response = await backendAccessPoint.post<ConversationQueryResponse>(
        "/api/query/query",
        request
    );
    return response.data;
};

export const useSendConversationQuery = () => {
    return useMutation({
        mutationFn: sendConversationQuery,
        onError: (error) => {
            console.error("Error sending chat message:", error);
        },
    });
};
