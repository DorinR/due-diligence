export enum MessageRole {
    User = 0,
    Assistant = 1,
    System = 2,
}

export type DocumentSource = {
    documentId: number;
    documentTitle: string;
    documentLink: string;
    fileName: string | null;
    relevanceScore: number;
    chunksUsed: number;
};

export type ConversationMessage = {
    id: string;
    text: string;
    role: "User" | "Assistant" | "System";
    timestamp: string;
    conversationId: string;
    sources?: DocumentSource[];
};

export type SendMessageRequest = {
    content: string;
    role: MessageRole;
};

export type SendMessageResponse = {
    id: string;
    text: string;
    role: "User" | "Assistant" | "System";
    timestamp: string;
    conversationId: string;
};

export type MessageFromServer = {
    id: string;
    content: string;
    role: "User" | "Assistant" | "System";
    timestamp: string;
    metadata: any | null;
    sources?: DocumentSource[];
};

export const convertServerMessageToConversationMessage = (
    serverMessage: MessageFromServer,
    conversationId: string
): ConversationMessage => {
    return {
        id: serverMessage.id.toString(),
        text: serverMessage.content,
        role: serverMessage.role,
        timestamp: serverMessage.timestamp,
        conversationId,
        sources: serverMessage.sources,
    };
};

export const getRoleEnumValue = (role: "User" | "Assistant" | "System"): MessageRole => {
    switch (role) {
        case "User":
            return MessageRole.User;
        case "Assistant":
            return MessageRole.Assistant;
        case "System":
            return MessageRole.System;
        default:
            return MessageRole.User;
    }
};

