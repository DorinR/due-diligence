import { useQuery } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

type ConversationFromServer = {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    companies: Array<{
        id: number;
        companyName: string;
    }>;
};

export type ConversationCompany = {
    id: string;
    companyName: string;
};

export type Conversation = {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    companies: ConversationCompany[];
};

export const getConversationList = async (): Promise<Conversation[]> => {
    const response = await backendAccessPoint.get<Array<ConversationFromServer>>("/api/conversation");
    return response.data.map((conv) => ({
        id: conv.id,
        title: conv.title,
        createdAt: conv.createdAt,
        updatedAt: conv.updatedAt,
        companies: conv.companies.map<ConversationCompany>((c) => ({
            id: c.id.toString(),
            companyName: c.companyName,
        })),
    }));
};

export const useGetConversationList = () => {
    return useQuery({
        queryKey: ["conversations"],
        queryFn: getConversationList,
        refetchOnMount: true,
    });
};

