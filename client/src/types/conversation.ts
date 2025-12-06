export interface Conversation {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    companies: ConversationCompany[];
}

export interface ConversationWithDetails extends Conversation {
    documents: ConversationDocument[];
    messages: ConversationMessage[];
}

export interface ConversationCompany {
    id: string;
    companyName: string;
}

export interface ConversationDocument {
    id: string;
    originalFileName: string;
    contentType: string;
    fileSize: number;
    uploadedAt: string;
    description: string;
    conversationId: string;
}

export interface CreateConversationRequest {
    title?: string;
}

export interface UpdateConversationRequest {
    title: string;
}

/**
 * Represents a document source cited in an assistant's response
 */
export interface DocumentSource {
    documentId: number;
    documentTitle: string;
    documentLink: string;
    fileName: string | null;
    relevanceScore: number;
    chunksUsed: number;
}

export interface ConversationMessage {
    id: string;
    text: string;
    role: "User" | "Assistant" | "System";
    timestamp: string;
    conversationId: string;
    sources?: DocumentSource[]; // Only populated for Assistant messages
}
