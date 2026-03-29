import { useQuery } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

type CompanyDto = {
    cik: string;
    cikNumber: number;
    name: string;
    ticker: string | null;
    exchange: string | null;
};

export type Company = {
    cik: string;
    cikNumber: number;
    name: string;
    ticker: string | null;
    exchange: string | null;
};

export const getCompanyList = async (): Promise<Company[]> => {
    const response = await backendAccessPoint.get<CompanyDto[]>("/api/companies");

    return response.data.map((company) => ({
        cik: company.cik,
        cikNumber: company.cikNumber,
        name: company.name,
        ticker: company.ticker,
        exchange: company.exchange,
    }));
};

export const useGetCompanyList = () => {
    return useQuery({
        queryKey: ["companies"],
        queryFn: getCompanyList,
        staleTime: 1000 * 60 * 60,
        gcTime: 1000 * 60 * 60 * 6,
    });
};
