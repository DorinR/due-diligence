import { useQuery } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

export type DocumentSource = {
    documentId: number;
    documentTitle: string;
    documentLink: string;
    fileName: string | null;
    relevanceScore: number;
    chunksUsed: number;
};

export type ConversationCompany = {
    id: string;
    companyName: string;
};

/**
 * Represents the ingestion status of a conversation's documents.
 * Matches the BatchProcessingStatus enum from the backend.
 */
export type IngestionStatus =
    | "Pending"
    | "Downloading"
    | "Extracting"
    | "Chunking"
    | "GeneratingEmbeddings"
    | "PersistingEmbeddings"
    | "Completed"
    | "Failed";

export type ConversationDocument = {
    id: string;
    originalFileName: string;
    contentType: string;
    fileSize: number;
    uploadedAt: string;
    description: string;
    conversationId: string;
};

export type ConversationMessage = {
    id: string;
    text: string;
    role: "User" | "Assistant" | "System";
    timestamp: string;
    conversationId: string;
    sources?: DocumentSource[];
};

export type ConversationWithDetails = {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    ingestionStatus: IngestionStatus | null;
    companies: ConversationCompany[];
    documents: ConversationDocument[];
    messages: ConversationMessage[];
};

type ConversationWithDetailsFromServer = {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    ingestionStatus: IngestionStatus | null;
    companies: Array<{
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
        sources?: DocumentSource[];
    }>;
};

export const getConversationById = async (
    conversationId: string
): Promise<ConversationWithDetails> => {
    const response = await backendAccessPoint.get<ConversationWithDetailsFromServer>(
        `/api/conversation/${conversationId}`
    );

    const serverData = response.data;

    return {
        id: serverData.id,
        title: serverData.title,
        createdAt: serverData.createdAt,
        updatedAt: serverData.updatedAt,
        ingestionStatus: serverData.ingestionStatus,
        companies: serverData.companies.map((c) => ({
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
};

export const useGetConversationById = (conversationId: string) => {
    return useQuery({
        queryKey: ["conversation", conversationId],
        queryFn: () => getConversationById(conversationId),
        enabled: !!conversationId,
        refetchOnMount: true,
    });
};

