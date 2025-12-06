import { useQuery } from "@tanstack/react-query";
import { DocumentResponse } from "./types";
import { backendAccessPoint } from "../backendAccessPoint";

export const getDocumentList = async (): Promise<DocumentResponse[]> => {
    const response = await backendAccessPoint.get<DocumentResponse[]>("/api/Document");
    return response.data;
};

export const useGetDocumentList = () => {
    return useQuery({
        queryKey: ["documents"],
        queryFn: getDocumentList,
        refetchOnMount: true,
    });
};

