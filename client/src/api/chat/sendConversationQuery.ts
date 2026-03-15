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

type ConversationQueryRequestDto = {
    query: string;
    conversationId: string;
    limit?: number;
};

type ConversationQueryResponseDto = {
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

export type SendConversationQueryRequest = {
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

export type SendConversationQueryResponse = {
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
    request: SendConversationQueryRequest
): Promise<SendConversationQueryResponse> => {
    const payload: ConversationQueryRequestDto = {
        query: request.query,
        conversationId: request.conversationId,
        limit: request.limit,
    };

    const response = await backendAccessPoint.post<ConversationQueryResponseDto>(
        "/api/query/query",
        payload
    );
    return {
        originalQuery: response.data.originalQuery,
        processedQuery: response.data.processedQuery,
        conversationId: response.data.conversationId,
        llmResponse: response.data.llmResponse,
        retrievedChunks: response.data.retrievedChunks,
        intent: response.data.intent,
        intentReasoning: response.data.intentReasoning,
        retrievalConfig: response.data.retrievalConfig,
        sources: response.data.sources,
        totalChunks: response.data.totalChunks,
        uniqueDocuments: response.data.uniqueDocuments,
    };
};

export const useSendConversationQuery = () => {
    return useMutation({
        mutationFn: sendConversationQuery,
        onError: (error) => {
            console.error("Error sending chat message:", error);
        },
    });
};
