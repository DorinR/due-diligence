import { useQuery } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

type CompanyFilingsDto = {
    cik: string;
    name: string | null;
    tickers: string[];
    exchanges: string[];
    availableFilingTypes: CompanyFilingTypeDto[];
};

type CompanyFilingTypeDto = {
    formType: string;
    filingCount: number;
    latestFilingDate: string | null;
};

export type CompanyFilings = {
    cik: string;
    name: string | null;
    tickers: string[];
    exchanges: string[];
    availableFilingTypes: CompanyFilingType[];
};

export type CompanyFilingType = {
    formType: string;
    filingCount: number;
    latestFilingDate: string | null;
};

export const getCompanyFilings = async (
    companyIdentifier: string,
): Promise<CompanyFilings> => {
    const response = await backendAccessPoint.get<CompanyFilingsDto>(
        `/api/companies/${encodeURIComponent(companyIdentifier)}/filings`,
    );

    return {
        cik: response.data.cik,
        name: response.data.name,
        tickers: response.data.tickers,
        exchanges: response.data.exchanges,
        availableFilingTypes: response.data.availableFilingTypes.map((filing) => ({
            formType: filing.formType,
            filingCount: filing.filingCount,
            latestFilingDate: filing.latestFilingDate,
        })),
    };
};

export const useGetCompanyFilings = (companyIdentifier: string) => {
    return useQuery({
        queryKey: ["company-filings", companyIdentifier],
        queryFn: () => getCompanyFilings(companyIdentifier),
        enabled: !!companyIdentifier,
        staleTime: 1000 * 60 * 15,
    });
};
