import { useSubscribeToConversation } from "@/hooks/realtime/useDocumentProcessingUpdates";
import * as signalR from "@microsoft/signalr";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Navigate, useParams } from "react-router-dom";
import { toast } from "sonner";
import { useGetCompanyFilings } from "../api/company/getCompanyFilings";
import { useGetCompanyList } from "../api/company/getCompanyList";
import { useGetConversationById } from "../api/conversation/getConversationById";
import { useSetConversationCompany } from "../api/conversation/setConversationCompany";
import { ConversationMessage } from "../api/message/getMessageListByConversation";
import { MessageRole, useSendMessage } from "../api/message/sendMessage";
import { ChatInterface, Message } from "../components/ChatInterface";
import { Button } from "../components/ui/button/Button";

const MAX_SELECTED_FILING_TYPES = 5;

// Helper function to generate a unique ID
const generateId = () => Math.random().toString(36).substr(2, 9);

/**
 * ConversationPage component - handles knowledge base queries only
 * Displays a chat interface for querying the knowledge base with conversation history
 */
export function ConversationPage() {
    const { conversationId } = useParams<{ conversationId: string }>();
    const [messages, setMessages] = useState<Message[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [completionVisibility, setCompletionVisibility] = useState<
        "hidden" | "visible" | "fading"
    >("hidden");
    const {
        connectionState,
        processingComplete,
        processingError,
        processingUpdate,
    } = useSubscribeToConversation({
        conversationId,
    });

    // Hooks for data fetching
    const {
        data: conversation,
        isLoading: isLoadingConversation,
        error: conversationError,
        refetch: refetchConversation,
    } = useGetConversationById(conversationId!);

    // Hooks for mutations
    const { mutate: sendMessage } = useSendMessage();
    const { mutate: setConversationCompany, isPending: isSettingCompany } =
        useSetConversationCompany();
    const {
        data: availableCompanies = [],
        isLoading: isLoadingCompanies,
        error: companyListError,
    } = useGetCompanyList();

    const [selectedCompany, setSelectedCompany] = useState("");
    const [selectedFilingTypes, setSelectedFilingTypes] = useState<string[]>([]);
    const {
        data: companyFilings,
        isLoading: isLoadingCompanyFilings,
        error: companyFilingsError,
    } = useGetCompanyFilings(selectedCompany);

    // Convert conversation messages to chat interface format
    const convertMessagesToChat = useCallback(
        (conversationMessages: ConversationMessage[]): Message[] => {
            if (!conversationMessages) return [];
            return conversationMessages.map((msg) => ({
                id: msg.id,
                text: msg.text,
                sender: msg.role,
                timestamp: new Date(msg.timestamp).toLocaleTimeString([], {
                    hour: "2-digit",
                    minute: "2-digit",
                }),
                sources: msg.sources || undefined, // Convert null to undefined for optional property
            }));
        },
        [],
    );

    // Update messages when conversation data changes
    useEffect(() => {
        if (conversation?.messages) {
            setMessages(convertMessagesToChat(conversation.messages));
        }
    }, [conversation?.messages, convertMessagesToChat]);

    /**
     * Handles sending a message to query the knowledge base
     * Backend handles: saving user message, querying knowledge base, saving assistant response
     * @param text - The user's query text
     */
    const handleSendMessage = useCallback(
        (text: string) => {
            if (!conversationId) return;

            // Optimistic update: immediately show user message
            const newUserMessage: Message = {
                id: generateId(),
                text,
                sender: "User",
                timestamp: new Date().toLocaleTimeString([], {
                    hour: "2-digit",
                    minute: "2-digit",
                }),
            };

            setMessages((prev) => [...prev, newUserMessage]);
            setIsLoading(true);

            // Send message to backend (backend handles LLM query and saving assistant response)
            sendMessage(
                {
                    conversationId,
                    data: { content: text, role: MessageRole.User },
                },
                {
                    onSuccess: () => {
                        // Refetch conversation messages to get updated messages including assistant response
                        refetchConversation()
                            .then(() => {
                                setIsLoading(false);
                            })
                            .catch(() => {
                                toast.error("Failed to load updated messages");
                                setIsLoading(false);
                            });
                    },
                    onError: () => {
                        toast.error(
                            "Failed to send your message. Please try again.",
                        );
                        setIsLoading(false);
                        // Remove optimistic update on error
                        setMessages((prev) =>
                            prev.filter((msg) => msg.id !== newUserMessage.id),
                        );
                    },
                },
            );
        },
        [conversationId, sendMessage, refetchConversation],
    );

    const hasCompanies = useMemo(
        () => (conversation?.companies?.length ?? 0) > 0,
        [conversation?.companies?.length],
    );

    const connectionBadge = useMemo(() => {
        switch (connectionState) {
            case signalR.HubConnectionState.Connected:
                return {
                    label: "Connected",
                    className:
                        "border-emerald-200 bg-emerald-50 text-emerald-700",
                };
            case signalR.HubConnectionState.Reconnecting:
                return {
                    label: "Reconnecting",
                    className: "border-amber-200 bg-amber-50 text-amber-700",
                };
            case signalR.HubConnectionState.Connecting:
                return {
                    label: "Connecting",
                    className: "border-blue-200 bg-blue-50 text-blue-700",
                };
            default:
                return {
                    label: "Disconnected",
                    className: "border-gray-200 bg-gray-50 text-gray-600",
                };
        }
    }, [connectionState]);

    const processingStatus = useMemo(() => {
        if (processingError) {
            return {
                title: "Processing failed",
                detail: processingError.errorMessage,
                tone: "text-red-700",
            };
        }

        if (processingComplete) {
            return {
                title: "Processing complete",
                detail: `${processingComplete.successfulDocuments} succeeded, ${processingComplete.failedDocuments} failed`,
                tone: "text-emerald-700",
            };
        }

        if (processingUpdate) {
            return {
                title: `Processing: ${processingUpdate.stage}`,
                detail: processingUpdate.message,
                tone: "text-blue-700",
            };
        }

        return {
            title: "Waiting for document processing",
            detail: "Updates appear here as documents are ingested.",
            tone: "text-gray-700",
        };
    }, [processingComplete, processingError, processingUpdate]);

    const progressPercent = processingError
        ? null
        : processingComplete
          ? 100
          : (processingUpdate?.progressPercent ?? null);
    const progressDetail = processingUpdate?.totalDocuments
        ? `${processingUpdate.documentsProcessed ?? 0} / ${
              processingUpdate.totalDocuments
          } documents`
        : processingComplete
          ? `${processingComplete.successfulDocuments} / ${processingComplete.totalDocuments} documents`
          : null;
    const isProcessing =
        !!processingUpdate && !processingComplete && !processingError;
    const companyName = conversation?.companies?.[0]?.companyName;

    // Check if ingestion is already completed (persisted in DB) or just completed (via SignalR)
    const ingestionAlreadyCompleted =
        conversation?.ingestionStatus === "Completed";
    const justCompletedAndFaded =
        !!processingComplete && completionVisibility === "hidden";

    // Show status card only if we have companies, ingestion isn't complete, and there's no error
    // Also show it temporarily when processing just completed (during fade animation)
    const showStatusCard =
        hasCompanies &&
        !ingestionAlreadyCompleted &&
        (!processingComplete || completionVisibility !== "hidden") &&
        !processingError;

    // Ready for chat when companies exist AND ingestion is complete (either from DB or just finished)
    const isReadyForChat =
        hasCompanies && (ingestionAlreadyCompleted || justCompletedAndFaded);

    const showEmptyState =
        !hasCompanies || showStatusCard || isReadyForChat || !!processingError;

    useEffect(() => {
        if (!processingComplete || processingError) {
            setCompletionVisibility("hidden");
            return;
        }

        setCompletionVisibility("visible");
        const fadeTimer = window.setTimeout(
            () => setCompletionVisibility("fading"),
            4000,
        );
        const hideTimer = window.setTimeout(
            () => setCompletionVisibility("hidden"),
            5200,
        );

        return () => {
            window.clearTimeout(fadeTimer);
            window.clearTimeout(hideTimer);
        };
    }, [processingComplete, processingError]);

    const companyOptions = useMemo(
        () =>
            availableCompanies.filter(
                (company) => !!company.ticker && company.ticker.trim().length > 0,
            ),
        [availableCompanies],
    );

    const selectedCompanyInfo = useMemo(
        () =>
            companyOptions.find((option) => option.ticker === selectedCompany) ?? null,
        [companyOptions, selectedCompany],
    );

    useEffect(() => {
        if (!selectedCompany) {
            setSelectedFilingTypes([]);
            return;
        }

        if (companyFilings) {
            setSelectedFilingTypes((prev) =>
                prev.filter((formType) =>
                    companyFilings.availableFilingTypes.some(
                        (filing) => filing.formType === formType,
                    ),
                ),
            );
        }
    }, [companyFilings, selectedCompany]);

    const toggleFilingType = useCallback((formType: string) => {
        setSelectedFilingTypes((prev) => {
            if (prev.includes(formType)) {
                return prev.filter((value) => value !== formType);
            }

            if (prev.length >= MAX_SELECTED_FILING_TYPES) {
                return prev;
            }

            return [...prev, formType];
        });
    }, []);

    const handleResearch = () => {
        if (!conversationId || !selectedCompanyInfo?.ticker) return;
        if (selectedFilingTypes.length === 0) {
            toast.error("Select at least one filing type.");
            return;
        }

        setConversationCompany(
            {
                conversationId,
                companyName: selectedCompanyInfo.name,
                companyTicker: selectedCompanyInfo.ticker,
                filingTypes: selectedFilingTypes,
            },
            {
                onSuccess: () => {
                    setSelectedCompany("");
                    setSelectedFilingTypes([]);
                    refetchConversation();
                },
                onError: () => {
                    toast.error("Failed to start ingestion. Please try again.");
                },
            },
        );
    };

    const availableFilingTypes = companyFilings?.availableFilingTypes ?? [];

    const renderCompanySelector = (
        <div className="flex h-full items-center justify-center">
            <div className="flex w-full max-w-xl flex-col gap-4 rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
                <div>
                    <div className="text-lg font-semibold text-gray-900">
                        Choose a company
                    </div>
                    <div className="mt-1 text-sm text-slate-600">
                        Pick a Fortune 500 company, then choose which filings to ingest.
                    </div>
                </div>
                <select
                    className="w-full rounded border border-gray-300 px-3 py-2 text-gray-900 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                    value={selectedCompany}
                    onChange={(e) => {
                        setSelectedCompany(e.target.value);
                        setSelectedFilingTypes([]);
                    }}
                    disabled={isLoadingCompanies || isSettingCompany}
                >
                    <option value="" disabled>
                        {isLoadingCompanies
                            ? "Loading companies..."
                            : companyListError
                              ? "Failed to load companies"
                              : "Choose a company"}
                    </option>
                    {companyOptions.map((option) => (
                        <option
                            key={`${option.cik}:${option.ticker ?? "no-ticker"}:${option.exchange ?? "no-exchange"}`}
                            value={option.ticker ?? ""}
                        >
                            {option.name}
                            {option.ticker ? ` (${option.ticker})` : ""}
                            {option.exchange ? ` - ${option.exchange}` : ""}
                        </option>
                    ))}
                </select>
                {companyListError && (
                    <div className="text-sm text-red-600">
                        Failed to load companies. Refresh and try again.
                    </div>
                )}
                {selectedCompanyInfo && (
                    <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
                        <div className="text-sm font-semibold text-slate-900">
                            Available filings for {selectedCompanyInfo.name}
                        </div>
                        <div className="mt-1 text-xs text-slate-600">
                            {selectedCompanyInfo.ticker}
                            {selectedCompanyInfo.exchange
                                ? ` • ${selectedCompanyInfo.exchange}`
                                : ""}
                        </div>
                        {isLoadingCompanyFilings && (
                            <div className="mt-3 text-sm text-slate-600">
                                Loading available filings...
                            </div>
                        )}
                        {companyFilingsError && (
                            <div className="mt-3 text-sm text-red-600">
                                Failed to load filing types for this company.
                            </div>
                        )}
                        {!isLoadingCompanyFilings &&
                            !companyFilingsError &&
                            availableFilingTypes.length === 0 && (
                                <div className="mt-3 text-sm text-slate-600">
                                    No recent filings were found for this company.
                                </div>
                            )}
                        {availableFilingTypes.length > 0 && (
                            <>
                                <div className="mt-3 flex items-center justify-between gap-3">
                                    <div className="text-xs text-slate-500">
                                        Select up to {MAX_SELECTED_FILING_TYPES} filing types.
                                    </div>
                                    <button
                                        type="button"
                                        className="text-sm font-medium text-slate-600 transition hover:text-slate-900 disabled:cursor-not-allowed disabled:text-slate-400"
                                        onClick={() => setSelectedFilingTypes([])}
                                        disabled={selectedFilingTypes.length === 0}
                                    >
                                        Deselect all
                                    </button>
                                </div>
                                <div className="mt-2 grid gap-2">
                                    {availableFilingTypes.map((filing) => (
                                        <label
                                            key={filing.formType}
                                            className="flex items-start gap-3 rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700"
                                        >
                                            <input
                                                type="checkbox"
                                                className="mt-0.5"
                                                checked={selectedFilingTypes.includes(
                                                    filing.formType,
                                                )}
                                                disabled={
                                                    selectedFilingTypes.length >=
                                                        MAX_SELECTED_FILING_TYPES &&
                                                    !selectedFilingTypes.includes(
                                                        filing.formType,
                                                    )
                                                }
                                                onChange={() =>
                                                    toggleFilingType(filing.formType)
                                                }
                                            />
                                            <span className="flex-1">
                                                <span className="font-medium text-slate-900">
                                                    {filing.formType}
                                                </span>
                                                <span className="ml-2 text-xs text-slate-500">
                                                    {filing.filingCount} filings
                                                    {filing.latestFilingDate
                                                        ? ` • latest ${filing.latestFilingDate}`
                                                        : ""}
                                                </span>
                                            </span>
                                        </label>
                                    ))}
                                </div>
                            </>
                        )}
                    </div>
                )}
                <Button
                    variant="primary"
                    onClick={handleResearch}
                    disabled={
                        isSettingCompany ||
                        isLoadingCompanies ||
                        isLoadingCompanyFilings ||
                        !selectedCompanyInfo ||
                        selectedFilingTypes.length === 0
                    }
                >
                    {isSettingCompany ? "Starting..." : "Start research"}
                </Button>
            </div>
        </div>
    );

    const renderProcessingStatus = (
        <div
            className={`w-full max-w-xl rounded-lg border border-slate-200 bg-white p-4 shadow-sm transition-opacity duration-700 ${
                completionVisibility === "fading" ? "opacity-0" : "opacity-100"
            }`}
        >
            <div className="flex items-start justify-between gap-4">
                <div>
                    <div className="text-sm font-semibold text-slate-900">
                        Document processing status
                    </div>
                    <div
                        className={`mt-1 text-sm font-medium ${processingStatus.tone}`}
                    >
                        {processingStatus.title}
                    </div>
                    <div className="mt-1 text-xs text-slate-600">
                        {processingStatus.detail}
                    </div>
                </div>
                <span
                    className={`inline-flex items-center rounded-full border px-2 py-0.5 text-xs font-medium ${connectionBadge.className}`}
                >
                    {connectionBadge.label}
                </span>
            </div>
            {progressPercent !== null && (
                <div className="mt-3">
                    <div className="h-2 w-full rounded-full bg-slate-100">
                        <div
                            className={`h-2 rounded-full bg-blue-500 ${
                                isProcessing
                                    ? "animate-pulse shadow-[0_0_12px_rgba(59,130,246,0.65)]"
                                    : ""
                            }`}
                            style={{
                                width: `${Math.min(
                                    100,
                                    Math.max(0, progressPercent),
                                )}%`,
                            }}
                        />
                    </div>
                    <div className="mt-2 flex items-center justify-between text-xs text-slate-600">
                        <span>{progressPercent}%</span>
                        {progressDetail && <span>{progressDetail}</span>}
                    </div>
                </div>
            )}
        </div>
    );

    const renderProcessingError = (
        <div className="w-full max-w-xl rounded-lg border border-red-200 bg-red-50 p-4 text-left shadow-sm">
            <div className="text-sm font-semibold text-red-700">
                Processing failed
            </div>
            <div className="mt-1 text-xs text-red-600">
                {processingError?.errorMessage ??
                    "We ran into an issue while ingesting the documents."}
            </div>
        </div>
    );

    const renderCallToAction = (
        <div className="max-w-xl text-center">
            <div className="text-xl font-semibold text-slate-900">
                Ask about the company&apos;s financials
            </div>
            <div className="mt-2 text-sm text-slate-600">
                Start with revenue trends, guidance changes, balance sheet
                strength, or any red flags you want to investigate.
            </div>
        </div>
    );

    // Redirect if conversation doesn't exist
    if (!conversationId) {
        return <Navigate to="/" replace />;
    }

    // Loading state
    if (isLoadingConversation) {
        return (
            <div className="flex h-full items-center justify-center">
                <div className="text-center">
                    <div className="mx-auto mb-4 h-8 w-8 animate-spin rounded-full border-b-2 border-blue-600"></div>
                    <p className="text-gray-600">Loading conversation...</p>
                </div>
            </div>
        );
    }

    // Error state
    if (conversationError) {
        return (
            <div className="flex h-full items-center justify-center">
                <div className="text-center">
                    <p className="mb-4 text-red-600">
                        Failed to load conversation
                    </p>
                    <Navigate to="/" replace />
                </div>
            </div>
        );
    }

    // No conversation found
    if (!conversation) {
        return <Navigate to="/" replace />;
    }

    return (
        <div className="flex h-full min-h-0">
            {/* Chat interface */}
            <div className="flex-1 min-h-0">
                <div className="flex h-full min-h-0 flex-col">
                    <div className="px-4 pt-4">
                        {isReadyForChat && companyName ? (
                            <div className="flex items-center justify-center">
                                <div className="rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-slate-600">
                                    Training data: {companyName}
                                </div>
                            </div>
                        ) : null}
                    </div>
                    <div className="flex-1 min-h-0">
                        <ChatInterface
                            messages={messages}
                            onSendMessage={handleSendMessage}
                            isLoading={isLoading}
                            inputDisabled={isProcessing}
                            showInput={isReadyForChat}
                            showEmptyState={showEmptyState}
                            emptyStateContent={
                                !hasCompanies
                                    ? renderCompanySelector
                                    : processingError
                                      ? renderProcessingError
                                      : showStatusCard
                                        ? renderProcessingStatus
                                        : renderCallToAction
                            }
                        />
                    </div>
                </div>
            </div>
        </div>
    );
}
