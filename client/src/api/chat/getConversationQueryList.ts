import { useMutation } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";
import { SendConversationQueryResponse } from "./sendConversationQuery";

type ConversationQueryListRequestDto = {
    query: string;
    limit?: number;
};

type ConversationQueryListResponseDto = {
    originalQuery: string;
    processedQuery: string;
    conversationId: number;
    llmResponse: string;
    retrievedChunks: Array<{
        fullDocumentText: string;
        documentId: string;
        documentTitle: string;
        similarity: number;
    }>;
    intent: "Factual" | "Comprehensive" | "Exploratory" | "Comparative";
    intentReasoning: string;
    retrievalConfig: {
        maxK: number;
        minSimilarity: number;
        description: string;
    };
    sources: Array<{
        documentId: number;
        documentTitle: string;
        documentLink: string;
        fileName: string | null;
        relevanceScore: number;
        chunksUsed: number;
    }>;
    totalChunks: number;
    uniqueDocuments: number;
};

export type GetConversationQueryListRequest = {
    query: string;
    limit?: number;
};

export type GetConversationQueryListResponse = SendConversationQueryResponse;

export const getConversationQueryList = async (
    request: GetConversationQueryListRequest
): Promise<GetConversationQueryListResponse> => {
    const payload: ConversationQueryListRequestDto = {
        query: request.query,
        limit: request.limit,
    };

    const response = await backendAccessPoint.post<ConversationQueryListResponseDto>(
        "/api/query/query-all-conversations",
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

export const useGetConversationQueryList = () => {
    return useMutation({
        mutationFn: getConversationQueryList,
        onError: (error) => {
            console.error("Error querying all conversations:", error);
        },
    });
};
