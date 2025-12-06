import { FileTextIcon, PlusIcon, TrashIcon } from '@radix-ui/react-icons';
import { useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { useDeleteDocument } from '../api/document/deleteDocument';

export type Document = {
    id: string;
    name: string;
    size: string;
    date: string;
};

type DocumentListProps = {
    documents: Document[];
    onSelectDocument: (documentId: string) => void;
    onAddNewDocument: () => void;
    isLoading?: boolean;
    conversationId?: string;
};

export function DocumentList({
    documents,
    onSelectDocument,
    onAddNewDocument,
    isLoading = false,
    conversationId,
}: DocumentListProps) {
    const queryClient = useQueryClient();
    const { mutate: deleteDocument } = useDeleteDocument(conversationId);
    const [showDeleteDialog, setShowDeleteDialog] = useState<string | null>(null);

    const handleDelete = (documentId: string) => {
        deleteDocument(documentId, {
            onSuccess: () => {
                // Additional query invalidation if needed
                if (conversationId) {
                    queryClient.invalidateQueries({
                        queryKey: ['documents', conversationId],
                    });
                } else {
                    queryClient.invalidateQueries({
                        queryKey: ['documents'],
                        exact: true,
                        refetchType: 'all',
                    });
                }
                setShowDeleteDialog(null);
            },
            onError: error => {
                console.error('Failed to delete document:', error);
                setShowDeleteDialog(null);
            },
        });
    };

    if (isLoading) {
        return (
            <div className="flex h-full flex-col border-r border-gray-200 p-4">
                <h2 className="mb-4 text-lg font-semibold">Documents</h2>
                <div className="flex-1 space-y-3">
                    {[...Array(3)].map((_, i) => (
                        <div key={i} className="animate-pulse rounded-md bg-gray-100 p-3">
                            <div className="flex items-center gap-2">
                                <div className="h-5 w-5 rounded bg-gray-200"></div>
                                <div className="flex-1">
                                    <div className="mb-1 h-4 rounded bg-gray-200"></div>
                                    <div className="h-3 w-2/3 rounded bg-gray-200"></div>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
                <div className="my-4 border-t border-gray-200"></div>
                <div className="h-10 animate-pulse rounded-md bg-gray-100"></div>
            </div>
        );
    }

    return (
        <div className="flex h-full flex-col border-r border-gray-200 p-4">
            <h2 className="mb-4 text-lg font-semibold">Documents</h2>

            <div
                className="scrollbar-thin scrollbar-thumb-gray-300 scrollbar-track-gray-100 flex-1 overflow-y-auto"
                style={{ height: 'calc(100% - 100px)' }}
            >
                <div className="flex flex-col gap-2">
                    {documents.map(doc => (
                        <div
                            key={doc.id}
                            onClick={() => onSelectDocument(doc.id)}
                            className="rounded-md border border-transparent bg-slate-50 p-3 transition-all duration-200 hover:bg-slate-100"
                        >
                            <div className="flex items-center gap-2">
                                <FileTextIcon width={20} height={20} className="text-gray-600" />
                                <div className="min-w-0 flex-1">
                                    <div className="text-sm font-medium break-words text-gray-900">
                                        {doc.name}
                                    </div>
                                    <div className="flex justify-between text-xs text-gray-500">
                                        <span>{doc.size}</span>
                                        <span>{doc.date}</span>
                                    </div>
                                </div>
                                <button
                                    onClick={e => {
                                        e.stopPropagation();
                                        setShowDeleteDialog(doc.id);
                                    }}
                                    className="rounded p-1 text-gray-400 transition-colors hover:bg-red-50 hover:text-red-500"
                                    title="Delete document"
                                >
                                    <TrashIcon width={14} height={14} />
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            </div>

            <div className="my-4 border-t border-gray-200"></div>

            <button
                onClick={onAddNewDocument}
                className="flex w-full items-center justify-center gap-2 rounded-md bg-gray-100 px-4 py-2 text-gray-700 transition-colors hover:bg-gray-200"
            >
                <PlusIcon width={16} height={16} />
                Add Document
            </button>

            {/* Delete confirmation dialog */}
            {showDeleteDialog && (
                <div className="bg-opacity-50 fixed inset-0 z-50 flex items-center justify-center bg-black">
                    <div className="mx-4 w-full max-w-md rounded-lg bg-white p-6 shadow-xl">
                        <h3 className="mb-2 text-lg font-semibold text-gray-900">
                            Delete Document
                        </h3>
                        <p className="mb-4 text-sm text-gray-600">
                            Are you sure you want to delete "
                            {documents.find(d => d.id === showDeleteDialog)?.name}"? This action
                            cannot be undone.
                        </p>
                        <div className="flex justify-end gap-3">
                            <button
                                onClick={() => setShowDeleteDialog(null)}
                                className="rounded-md bg-gray-100 px-4 py-2 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-200"
                            >
                                Cancel
                            </button>
                            <button
                                onClick={() => handleDelete(showDeleteDialog)}
                                className="rounded-md bg-red-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-red-700"
                            >
                                Delete
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
