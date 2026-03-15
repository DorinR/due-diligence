import { useQuery } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

type DocumentDto = {
    id: string;
    originalFileName: string;
    contentType: string;
    fileSize: number;
    uploadedAt: string;
    description: string;
    conversationId: string;
};

export type Document = {
    id: string;
    originalFileName: string;
    contentType: string;
    fileSize: number;
    uploadedAt: string;
    description: string;
    conversationId: string;
};

export type GetDocumentListResponse = Document[];

export const getDocumentList = async (): Promise<GetDocumentListResponse> => {
    const response = await backendAccessPoint.get<DocumentDto[]>("/api/Document");
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

export const useGetDocumentList = () => {
    return useQuery({
        queryKey: ["documents"],
        queryFn: getDocumentList,
        refetchOnMount: true,
    });
};
