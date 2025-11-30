import { useCallback, useEffect, useState } from 'react';
import { Navigate, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { useConversation } from '../api/conversation/conversationApi';
import { MessageRole, useSendMessage } from '../api/message/messageApi';
import { ChatInterface, Message } from '../components/ChatInterface';
import { ConversationMessage } from '../types/conversation';

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
    } = useConversation(conversationId!);

    // Hooks for mutations
    const { mutate: sendMessage } = useSendMessage();

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
                />
            </div>
        </div>
    );
}
