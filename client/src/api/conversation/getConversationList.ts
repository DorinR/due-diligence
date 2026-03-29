import { useQuery } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";
import { IngestionStatus } from "./getConversationById";

type ConversationSummaryDto = {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    ingestionStatus: IngestionStatus | null;
    companies: Array<{
        id: number;
        companyName: string;
        ticker: string;
    }>;
};

export type ConversationCompany = {
    id: string;
    companyName: string;
    ticker: string;
};

export type ConversationSummary = {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    ingestionStatus: IngestionStatus | null;
    companies: ConversationCompany[];
};

export type GetConversationListResponse = ConversationSummary[];

export const getConversationList = async (): Promise<GetConversationListResponse> => {
    const response =
        await backendAccessPoint.get<Array<ConversationSummaryDto>>(
            "/api/conversation",
        );
    return response.data.map((conversation) => ({
        id: conversation.id,
        title: conversation.title,
        createdAt: conversation.createdAt,
        updatedAt: conversation.updatedAt,
        ingestionStatus: conversation.ingestionStatus,
        companies: conversation.companies.map<ConversationCompany>((company) => ({
            id: company.id.toString(),
            companyName: company.companyName,
            ticker: company.ticker ?? "",
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
