import { ChatBubbleIcon, PlusIcon } from '@radix-ui/react-icons';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { useCreateConversation } from '../api/conversation/createConversation';
import { Button } from '../components/ui/button/Button';

export function DashboardPage() {
    const navigate = useNavigate();
    const { mutate: createConversation, isPending: isCreating } = useCreateConversation();

    const handleCreateFirstConversation = () => {
        createConversation(
            { title: 'My First Conversation' },
            {
                onSuccess: newConversation => {
                    navigate(`/conversations/${newConversation.id}`);
                    toast.success('Conversation created! Start by uploading some documents.');
                },
                onError: () => {
                    toast.error('Failed to create conversation');
                },
            }
        );
    };

    return (
        <div className="flex h-full items-center justify-center bg-gray-50">
            <div className="max-w-md text-center">
                <div className="mb-8">
                    <ChatBubbleIcon className="mx-auto h-16 w-16 text-blue-500" />
                </div>

                <h1 className="mb-4 text-3xl font-bold text-gray-900">
                    Welcome to Your RAG Assistant
                </h1>

                <p className="mb-8 text-gray-600">
                    Create your first conversation to start chatting with your documents. Upload
                    PDFs and ask questions to get intelligent responses.
                </p>

                <Button
                    onClick={handleCreateFirstConversation}
                    disabled={isCreating}
                    variant="primary"
                    icon={PlusIcon}
                    iconPosition="left"
                    className="px-8 py-3 text-lg"
                >
                    {isCreating ? 'Creating...' : 'Create First Conversation'}
                </Button>

                <div className="mt-12 space-y-4 text-sm text-gray-500">
                    <div className="flex items-center gap-3">
                        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-blue-100 text-sm font-semibold text-blue-600">
                            1
                        </div>
                        <span>Create a conversation</span>
                    </div>
                    <div className="flex items-center gap-3">
                        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-blue-100 text-sm font-semibold text-blue-600">
                            2
                        </div>
                        <span>Upload your PDF documents</span>
                    </div>
                    <div className="flex items-center gap-3">
                        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-blue-100 text-sm font-semibold text-blue-600">
                            3
                        </div>
                        <span>Ask questions about your documents</span>
                    </div>
                </div>
            </div>
        </div>
    );
}
