import { Cross2Icon, PlusIcon } from '@radix-ui/react-icons';

export interface Conversation {
    id: string;
    title: string;
    date: string;
    messageCount: number;
    isActive: boolean;
}

interface ConversationsListProps {
    conversations: Conversation[];
    onSelectConversation: (conversationId: string) => void;
    onNewConversation: () => void;
    onDeleteConversation: (conversationId: string) => void;
}

export function ConversationsList({
    conversations,
    onSelectConversation,
    onNewConversation,
    onDeleteConversation,
}: ConversationsListProps) {
    return (
        <div className="flex h-full flex-col border-l border-gray-200 p-4">
            <h2 className="mb-4 text-lg font-semibold">Conversations</h2>

            <button
                onClick={onNewConversation}
                className="mb-4 flex w-full items-center justify-center gap-2 rounded-md bg-gray-100 px-4 py-2 text-gray-700 transition-colors hover:bg-gray-200"
            >
                <PlusIcon width={16} height={16} />
                New Conversation
            </button>

            <div className="my-4 border-t border-gray-200"></div>

            <div className="scrollbar-thin scrollbar-thumb-gray-300 scrollbar-track-gray-100 flex-1 overflow-y-auto">
                <div className="flex flex-col gap-2">
                    {conversations.length === 0 ? (
                        <div className="py-6 text-center text-gray-500">No conversations yet</div>
                    ) : (
                        conversations.map(conversation => (
                            <div
                                key={conversation.id}
                                className={`flex cursor-pointer items-center rounded-md p-3 transition-all duration-200 hover:bg-gray-50 ${
                                    conversation.isActive
                                        ? 'border border-blue-200 bg-blue-50'
                                        : 'border border-transparent'
                                }`}
                                onClick={() => onSelectConversation(conversation.id)}
                            >
                                <div className="min-w-0 flex-1">
                                    <div className="mb-1 flex items-center justify-between">
                                        <span
                                            className={`text-sm break-words ${
                                                conversation.isActive
                                                    ? 'font-semibold'
                                                    : 'font-normal'
                                            } text-gray-900`}
                                        >
                                            {conversation.title}
                                        </span>
                                        <span className="inline-flex items-center rounded-full bg-gray-100 px-2 py-1 text-xs font-medium text-gray-800">
                                            {conversation.messageCount}
                                        </span>
                                    </div>
                                    <div className="text-xs text-gray-500">{conversation.date}</div>
                                </div>
                                <button
                                    onClick={e => {
                                        e.stopPropagation();
                                        onDeleteConversation(conversation.id);
                                    }}
                                    className="ml-2 rounded p-1 text-gray-400 transition-colors hover:bg-red-50 hover:text-red-500"
                                >
                                    <Cross2Icon width={14} height={14} />
                                </button>
                            </div>
                        ))
                    )}
                </div>
            </div>
        </div>
    );
}
