import { useQuery } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";
import { Document } from "./getDocumentList";

type DocumentByConversationDto = {
    id: string;
    originalFileName: string;
    contentType: string;
    fileSize: number;
    uploadedAt: string;
    description: string;
    conversationId: string;
};

export type GetDocumentListByConversationResponse = Document[];

export const getDocumentListByConversation = async (
    conversationId: string
): Promise<GetDocumentListByConversationResponse> => {
    const response = await backendAccessPoint.get<DocumentByConversationDto[]>(
        `/api/Document/conversation/${conversationId}`
    );
    return response.data.map((document) => ({
        id: document.id,
        originalFileName: document.originalFileName,
        contentType: document.contentType,
        fileSize: document.fileSize,
        uploadedAt: document.uploadedAt,
        description: document.description,
        conversationId: document.conversationId,
    }));
};

export const useGetDocumentListByConversation = (conversationId: string) => {
    return useQuery({
        queryKey: ["documents", conversationId],
        queryFn: () => getDocumentListByConversation(conversationId),
        enabled: !!conversationId,
        refetchOnMount: true,
    });
};
