import { useMutation } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

type UploadDocumentResponseDto = {
    id: string;
    originalFileName: string;
    contentType: string;
    fileSize: number;
    uploadedAt: string;
    description: string;
    conversationId: string;
};

type UploadDocumentRequestDto = FormData;

export type UploadDocumentResponse = {
    id: string;
    originalFileName: string;
    contentType: string;
    fileSize: number;
    uploadedAt: string;
    description: string;
    conversationId: string;
};

export const uploadDocument = async (
    conversationId: string | undefined,
    file: File,
    description: string = ""
): Promise<UploadDocumentResponse> => {
    const payload: UploadDocumentRequestDto = new FormData();
    payload.append("file", file);
    payload.append("description", description);
    if (conversationId) {
        payload.append("conversationId", conversationId);
    }

    const response = await backendAccessPoint.post<UploadDocumentResponseDto>(
        "/api/Document/upload",
        payload,
        {
            headers: {
                "Content-Type": "multipart/form-data",
            },
        }
    );

    return {
        id: response.data.id,
        originalFileName: response.data.originalFileName,
        contentType: response.data.contentType,
        fileSize: response.data.fileSize,
        uploadedAt: response.data.uploadedAt,
        description: response.data.description,
        conversationId: response.data.conversationId,
    };
};

export const useUploadDocument = () => {
    return useMutation({
        mutationFn: (params: { conversationId?: string; file: File; description?: string }) =>
            uploadDocument(params.conversationId, params.file, params.description),
        onError: (error) => {
            console.error("Error uploading document:", error);
        },
    });
};
