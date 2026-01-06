import * as signalR from "@microsoft/signalr";
import { useEffect, useRef, useState } from "react";
import { getAccessToken } from "@/utils/tokenManager";

type DocumentProcessingUpdate = {
    stage: string;
    message: string;
    progressPercent: number;
    documentsProcessed?: number;
    totalDocuments?: number;
    timestamp: string;
};

type ProcessingCompleteResult = {
    totalDocuments: number;
    successfulDocuments: number;
    failedDocuments: number;
    duration?: string;
    completedAt: string;
};

type ProcessingErrorResult = {
    errorMessage: string;
    stage: string;
    documentsProcessed?: number;
    timestamp: string;
};

/**
 * Calling this hook connects the component to the hub for a given conversation.
 */
export const useSubscribeToConversation = ({
    conversationId,
}: {
    conversationId: string | undefined;
}) => {
    const [connectionState, setConnectionState] =
        useState<signalR.HubConnectionState>(
            signalR.HubConnectionState.Disconnected
        );
    const [processingUpdate, setProcessingUpdate] =
        useState<DocumentProcessingUpdate | null>(null);
    const [processingComplete, setProcessingComplete] =
        useState<ProcessingCompleteResult | null>(null);
    const [processingError, setProcessingError] =
        useState<ProcessingErrorResult | null>(null);
    const connectionRef = useRef<signalR.HubConnection | undefined>(undefined);

    useEffect(() => {
        if (conversationId === undefined) return;

        setProcessingUpdate(null);
        setProcessingComplete(null);
        setProcessingError(null);

        const backendUrl =
            import.meta.env.VITE_BACKEND_URL ?? "http://localhost:5104";
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${backendUrl}/hubs/document-processing`, {
                accessTokenFactory: () => getAccessToken() ?? "",
            })
            .withAutomaticReconnect()
            .build();

        connectionRef.current = connection;

        connection.on(
            "ReceiveProcessingUpdate",
            (update: DocumentProcessingUpdate) => {
                setProcessingUpdate(update);
                setProcessingComplete(null);
                setProcessingError(null);
            }
        );

        connection.on(
            "ReceiveProcessingComplete",
            (result: ProcessingCompleteResult) => {
                setProcessingComplete(result);
                setProcessingError(null);
            }
        );

        connection.on(
            "ReceiveProcessingError",
            (error: ProcessingErrorResult) => {
                setProcessingError(error);
                setProcessingComplete(null);
            }
        );

        // Track connection lifecycle
        connection.onreconnected(() =>
            setConnectionState(signalR.HubConnectionState.Connected)
        );
        connection.onreconnecting(() =>
            setConnectionState(signalR.HubConnectionState.Reconnecting)
        );
        connection.onclose(() =>
            setConnectionState(signalR.HubConnectionState.Disconnected)
        );

        connection
            .start()
            .then(() =>
                setConnectionState(signalR.HubConnectionState.Connected)
            )
            .catch(console.error);

        return () => {
            // remove handlers (optional but recommended)
            connection.off("ReceiveProcessingUpdate");
            connection.off("ReceiveProcessingComplete");
            connection.off("ReceiveProcessingError");

            // actual cleanup/close
            connection.stop().catch(() => {
                /* ignore */
            });
        };
    }, [conversationId]);

    useEffect(() => {
        if (connectionState !== signalR.HubConnectionState.Connected) return;
        if (!connectionRef.current || !conversationId) return;

        connectionRef.current
            .invoke("SubscribeToConversation", conversationId)
            .catch(console.error);

        return () => {
            if (connectionRef.current) {
                connectionRef.current
                    .invoke("UnsubscribeFromConversation", conversationId)
                    .catch(console.error);
            }
        };
    }, [connectionState, conversationId]);

    return {
        connectionState,
        processingComplete,
        processingError,
        processingUpdate,
    };
};
