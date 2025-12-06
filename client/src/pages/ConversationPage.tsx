import { useCallback, useEffect, useMemo, useState } from 'react';
import { Navigate, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { useGetConversationById } from '../api/conversation/getConversationById';
import { MessageRole } from '../api/message/types';
import { useSendMessage } from '../api/message/sendMessage';
import { ChatInterface, Message } from '../components/ChatInterface';
import { ConversationMessage } from '../api/message/types';
import { useSetConversationCompany } from '../api/conversation/setConversationCompany';
import { Button } from '../components/ui/button/Button';

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

    // Hooks for data fetching
    const {
        data: conversation,
        isLoading: isLoadingConversation,
        error: conversationError,
        refetch: refetchConversation,
    } = useGetConversationById(conversationId!);

    // Hooks for mutations
    const { mutate: sendMessage } = useSendMessage();
    const {
        mutate: setConversationCompany,
        isPending: isSettingCompany,
    } = useSetConversationCompany();

    const [selectedCompany, setSelectedCompany] = useState('');

    // Convert conversation messages to chat interface format
    const convertMessagesToChat = useCallback(
        (conversationMessages: ConversationMessage[]): Message[] => {
            if (!conversationMessages) return [];
            return conversationMessages.map(msg => ({
                id: msg.id,
                text: msg.text,
                sender: msg.role,
                timestamp: new Date(msg.timestamp).toLocaleTimeString([], {
                    hour: '2-digit',
                    minute: '2-digit',
                }),
                sources: msg.sources || undefined, // Convert null to undefined for optional property
            }));
        },
        []
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
                sender: 'User',
                timestamp: new Date().toLocaleTimeString([], {
                    hour: '2-digit',
                    minute: '2-digit',
                }),
            };

            setMessages(prev => [...prev, newUserMessage]);
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
                                toast.error('Failed to load updated messages');
                                setIsLoading(false);
                            });
                    },
                    onError: () => {
                        toast.error('Failed to send your message. Please try again.');
                        setIsLoading(false);
                        // Remove optimistic update on error
                        setMessages(prev => prev.filter(msg => msg.id !== newUserMessage.id));
                    },
                }
            );
        },
        [conversationId, sendMessage, refetchConversation]
    );

    const hasCompanies = useMemo(
        () => (conversation?.companies?.length ?? 0) > 0,
        [conversation?.companies?.length]
    );

    const companyOptions = useMemo(
        () => ['Apple', 'Microsoft', 'Amazon', 'Alphabet', 'Berkshire Hathaway'],
        []
    );

    const handleResearch = () => {
        if (!conversationId || !selectedCompany) return;

        setConversationCompany(
            { conversationId, companyName: selectedCompany },
            {
                onSuccess: () => {
                    toast.success('Company set for research.');
                    setSelectedCompany('');
                    refetchConversation();
                },
                onError: () => {
                    toast.error('Failed to set company. Please try again.');
                },
            }
        );
    };

    const renderCompanySelector = (
        <div className="flex h-full items-center justify-center">
            <div className="flex flex-col items-center gap-4 rounded-lg border border-gray-200 bg-white p-6 shadow-sm">
                <div className="text-lg font-semibold text-gray-900">Choose a company</div>
                <select
                    className="w-64 rounded border border-gray-300 px-3 py-2 text-gray-900 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
                    value={selectedCompany}
                    onChange={e => setSelectedCompany(e.target.value)}
                >
                    <option value="" disabled>
                        Choose a company
                    </option>
                    {companyOptions.map(option => (
                        <option key={option} value={option}>
                            {option}
                        </option>
                    ))}
                </select>
                {selectedCompany && (
                    <Button
                        variant="primary"
                        onClick={handleResearch}
                        disabled={isSettingCompany}
                    >
                        {isSettingCompany ? 'Loading...' : 'Research'}
                    </Button>
                )}
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
                    <p className="mb-4 text-red-600">Failed to load conversation</p>
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
        <div className="flex h-full">
            {/* Chat interface */}
            <div className="flex-1">
                <ChatInterface
                    messages={messages}
                    onSendMessage={handleSendMessage}
                    isLoading={isLoading}
                    conversationType={conversation?.type}
                    showInput={hasCompanies}
                    emptyStateContent={!hasCompanies ? renderCompanySelector : undefined}
                />
            </div>
        </div>
    );
}
