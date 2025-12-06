import { UploadIcon } from '@radix-ui/react-icons';
import { useQueryClient } from '@tanstack/react-query';
import { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import { useUploadDocument } from '../api/document/uploadDocument';

type FileDropzoneProps = {
    onFilesDrop: (files: File[]) => void;
    conversationId?: string;
};

export function FileDropzone({ onFilesDrop, conversationId }: FileDropzoneProps) {
    const [uploading, setUploading] = useState(false);
    const [uploadError, setUploadError] = useState<string | null>(null);
    const queryClient = useQueryClient();

    // Use our mutation hook
    const { mutate: uploadDocument } = useUploadDocument();

    const onDrop = useCallback(
        async (acceptedFiles: File[]) => {
            if (acceptedFiles.length === 0) return;

            setUploading(true);
            setUploadError(null);

            try {
                // Upload files one by one
                const uploadPromises = acceptedFiles.map(
                    file =>
                        new Promise((resolve, reject) => {
                            const uploadParams = conversationId
                                ? { conversationId, file }
                                : ({ file } as any); // Backward compatibility

                            uploadDocument(uploadParams, {
                                onSuccess: () => resolve(file),
                                onError: error => reject(error),
                            });
                        })
                );

                await Promise.all(uploadPromises);

                // Invalidate relevant queries
                if (conversationId) {
                    queryClient.invalidateQueries({ queryKey: ['documents', conversationId] });
                    queryClient.invalidateQueries({ queryKey: ['conversation', conversationId] });
                } else {
                    queryClient.invalidateQueries({ queryKey: ['documents'] });
                }

                // Call the parent callback to update the UI
                onFilesDrop(acceptedFiles);
            } catch (error) {
                console.error('Upload failed:', error);
                setUploadError('Failed to upload one or more files. Please try again.');
            } finally {
                setUploading(false);
            }
        },
        [onFilesDrop, uploadDocument, queryClient, conversationId]
    );

    const { getRootProps, getInputProps, isDragActive } = useDropzone({
        onDrop,
        accept: {
            'application/pdf': ['.pdf'],
        },
        disabled: uploading,
    });

    return (
        <div className="flex h-screen flex-col items-center justify-center gap-6 py-9">
            <div
                {...getRootProps()}
                className={`mx-4 w-full max-w-2xl rounded-lg p-8 shadow-lg transition-all duration-200 ${
                    uploading
                        ? 'cursor-not-allowed border-2 border-dashed border-gray-300 bg-gray-50'
                        : isDragActive
                          ? 'cursor-pointer border-2 border-dashed border-blue-400 bg-blue-50'
                          : 'cursor-pointer border-2 border-dashed border-gray-300 bg-white hover:bg-gray-50'
                }`}
            >
                <input {...getInputProps()} />
                <div className="flex flex-col items-center gap-6 py-9">
                    <div className="flex h-20 w-20 items-center justify-center rounded-full bg-gray-100">
                        <UploadIcon width={36} height={36} className="text-gray-600" />
                    </div>
                    <h2 className="text-center text-2xl font-semibold text-gray-900">
                        {uploading ? 'Uploading...' : 'Drop your PDFs here'}
                    </h2>
                    <p className="text-center text-gray-600">
                        {uploading
                            ? 'Please wait while your files are being uploaded'
                            : 'Drag and drop your PDF documents, or click to select files'}
                    </p>
                    {uploadError && (
                        <p className="text-center font-medium text-red-600">{uploadError}</p>
                    )}
                    <p className="text-xs text-gray-500">Only PDF files are accepted</p>
                </div>
            </div>
        </div>
    );
}
