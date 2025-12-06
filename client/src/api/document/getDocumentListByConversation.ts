import { useQuery } from "@tanstack/react-query";
import { DocumentResponse } from "./types";
import { backendAccessPoint } from "../backendAccessPoint";

export const getDocumentListByConversation = async (
    conversationId: string
): Promise<DocumentResponse[]> => {
    const response = await backendAccessPoint.get<DocumentResponse[]>(
        `/api/Document/conversation/${conversationId}`
    );
    return response.data;
};

export const useGetDocumentListByConversation = (conversationId: string) => {
    return useQuery({
        queryKey: ["documents", conversationId],
        queryFn: () => getDocumentListByConversation(conversationId),
        enabled: !!conversationId,
        refetchOnMount: true,
    });
};

