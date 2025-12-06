import { useMutation } from "@tanstack/react-query";
import { UploadDocumentResponse } from "./types";
import { backendAccessPoint } from "../backendAccessPoint";

export const uploadDocument = async (
    conversationId: string,
    file: File,
    description: string = ""
): Promise<UploadDocumentResponse> => {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("description", description);
    formData.append("conversationId", conversationId);

    const response = await backendAccessPoint.post<UploadDocumentResponse>(
        "/api/Document/upload",
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data",
            },
        }
    );

    return response.data;
};

export const useUploadDocument = () => {
    return useMutation({
        mutationFn: (params: { conversationId: string; file: File; description?: string }) =>
            uploadDocument(params.conversationId, params.file, params.description),
        onError: (error) => {
            console.error("Error uploading document:", error);
        },
    });
};

