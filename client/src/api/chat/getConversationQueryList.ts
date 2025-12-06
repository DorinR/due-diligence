import { useMutation } from "@tanstack/react-query";
import { ConversationQueryResponse } from "./sendConversationQuery";
import { backendAccessPoint } from "../backendAccessPoint";

export type ConversationQueryListRequest = {
    query: string;
    limit?: number;
};

export const getConversationQueryList = async (
    request: ConversationQueryListRequest
): Promise<ConversationQueryResponse> => {
    const response = await backendAccessPoint.post<ConversationQueryResponse>(
        "/api/query/query-all-conversations",
        request
    );
    return response.data;
};

export const useGetConversationQueryList = () => {
    return useMutation({
        mutationFn: getConversationQueryList,
        onError: (error) => {
            console.error("Error querying all conversations:", error);
        },
    });
};

