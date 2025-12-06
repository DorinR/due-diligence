import { ChatBubbleIcon, PlusIcon, TrashIcon } from '@radix-ui/react-icons';
import { useNavigate, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import {
    useConversations,
    useCreateConversation,
    useDeleteConversation,
} from '../api/conversation/conversationApi';
import { Button } from './ui/button/Button';

export function ConversationSidebar() {
    const navigate = useNavigate();
    const { conversationId } = useParams<{ conversationId: string }>();

    const { data: conversations, isLoading } = useConversations();
    const { mutate: createConversation, isPending: isCreating } = useCreateConversation();
    const { mutate: deleteConversation, isPending: isDeleting } = useDeleteConversation();

    const handleNewConversation = () => {
        createConversation(
            {},
            {
                onSuccess: newConversation => {
                    navigate(`/conversations/${newConversation.id}`);
                    toast.success('New conversation created');
                },
                onError: () => {
                    toast.error('Failed to create conversation');
                },
            }
        );
    };

    const handleSelectConversation = (id: string) => {
        navigate(`/conversations/${id}`);
    };

    const handleDeleteConversation = (id: string, title: string, e: React.MouseEvent) => {
        e.stopPropagation();

        if (confirm(`Are you sure you want to delete "${title}"?`)) {
            deleteConversation(id, {
                onSuccess: () => {
                    toast.success('Conversation deleted');
                    // If we deleted the current conversation, navigate to the first available one
                    if (id === conversationId) {
                        const remainingConversations = conversations?.filter(c => c.id !== id);
                        if (remainingConversations && remainingConversations.length > 0) {
                            navigate(`/conversations/${remainingConversations[0].id}`);
                        } else {
                            navigate('/');
                        }
                    }
                },
                onError: () => {
                    toast.error('Failed to delete conversation');
                },
            });
        }
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        const now = new Date();
        const diffTime = Math.abs(now.getTime() - date.getTime());
        const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

        if (diffDays === 1) {
            return 'Today';
        } else if (diffDays === 2) {
            return 'Yesterday';
        } else if (diffDays <= 7) {
            return `${diffDays - 1} days ago`;
        } else {
            return date.toLocaleDateString();
        }
    };

    if (isLoading) {
        return (
            <div className="w-80 border-r border-gray-200 bg-white p-4">
                <div className="animate-pulse">
                    <div className="mb-4 h-10 rounded bg-gray-200"></div>
                    <div className="space-y-2">
                        {[...Array(5)].map((_, i) => (
                            <div key={i} className="h-16 rounded bg-gray-100"></div>
                        ))}
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="flex w-80 flex-col border-r border-gray-200 bg-white">
            {/* Header with conversation creation buttons */}
            <div className="border-b border-gray-200 p-4">
                <div className="space-y-2">
                    <Button
                        onClick={handleNewConversation}
                        disabled={isCreating}
                        variant="primary"
                        icon={PlusIcon}
                        iconPosition="left"
                        className="w-full"
                    >
                        {isCreating ? 'Creating...' : 'New Conversation'}
                    </Button>
                </div>
            </div>

            {/* Conversations List */}
            <div className="flex-1 overflow-y-auto">
                <div className="p-2">
                    {conversations && conversations.length > 0 ? (
                        <div className="space-y-1">
                            {conversations.map(conversation => (
                                <div
                                    key={conversation.id}
                                    className={`group flex cursor-pointer items-center justify-between rounded-lg p-3 transition-colors hover:bg-gray-50 ${
                                        conversation.id === conversationId
                                            ? 'border border-blue-200 bg-blue-50'
                                            : 'border border-transparent'
                                    }`}
                                    onClick={() => handleSelectConversation(conversation.id)}
                                >
                                    <div className="flex min-w-0 flex-1 items-center gap-3">
                                        <ChatBubbleIcon
                                            className={`h-5 w-5 flex-shrink-0 ${
                                                conversation.id === conversationId
                                                    ? 'text-blue-600'
                                                    : 'text-gray-400'
                                            }`}
                                        />
                                        <div className="min-w-0 flex-1">
                                            <p
                                                className={`truncate text-sm font-medium ${
                                                    conversation.id === conversationId
                                                        ? 'text-blue-900'
                                                        : 'text-gray-900'
                                                }`}
                                            >
                                                {conversation.title}
                                            </p>
                                            <p className="text-xs text-gray-500">
                                                {formatDate(conversation.updatedAt)}
                                            </p>
                                        </div>
                                    </div>
                                    <button
                                        onClick={e =>
                                            handleDeleteConversation(
                                                conversation.id,
                                                conversation.title,
                                                e
                                            )
                                        }
                                        disabled={isDeleting}
                                        className="rounded p-1 text-gray-400 opacity-0 transition-all group-hover:opacity-100 hover:bg-red-50 hover:text-red-600"
                                    >
                                        <TrashIcon className="h-4 w-4" />
                                    </button>
                                </div>
                            ))}
                        </div>
                    ) : (
                        <div className="py-8 text-center text-gray-500">
                            <ChatBubbleIcon className="mx-auto h-12 w-12 text-gray-300" />
                            <p className="mt-2 text-sm">No conversations yet</p>
                            <p className="text-xs">Create your first conversation to get started</p>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}
