import { ExternalLink } from 'lucide-react';
import { DocumentSource } from '../api/message/types';
import { HoverCard, HoverCardContent, HoverCardTrigger } from './ui/hover-card';

type SourceCitationsProps = {
    sources?: DocumentSource[];
};

/**
 * Displays source citations for assistant messages with hover card previews.
 * Shows document titles as triggers, and displays detailed metadata in hover cards.
 *
 * @param sources - Optional array of document sources to display
 */
export function SourceCitations({ sources }: SourceCitationsProps) {
    if (!sources || sources.length === 0) {
        return null;
    }

    // Fallback PDF link for when documentLink is null
    const FALLBACK_PDF = 'https://mozilla.github.io/pdf.js/web/compressed.tracemonkey-pldi-09.pdf';

    const shouldTruncate = (title: string) => title.length > 50;

    return (
        <div className="mt-3 border-t border-gray-200 pt-3">
            <h4 className="mb-2 flex items-center gap-1.5 text-xs font-semibold text-gray-700">
                <span>📚</span>
                <span>Sources ({sources.length})</span>
            </h4>
            <div className="flex flex-wrap gap-2">
                {sources.map(source => {
                    const documentUrl = source.documentLink || FALLBACK_PDF;
                    const isTruncated = shouldTruncate(source.documentTitle);

                    return (
                        <HoverCard key={source.documentId}>
                            <HoverCardTrigger asChild>
                                <button
                                    className="group relative inline-flex items-center gap-1.5 overflow-hidden rounded-full border border-gray-300 bg-gray-50 px-3 py-1.5 text-sm text-gray-700 transition-colors hover:border-gray-400 hover:bg-gray-100"
                                    style={{ maxWidth: '250px' }}
                                >
                                    <span className="text-xs">📄</span>
                                    <span
                                        className={`${isTruncated ? 'relative pr-4' : ''}`}
                                        style={
                                            isTruncated
                                                ? {
                                                      display: 'block',
                                                      whiteSpace: 'nowrap',
                                                      overflow: 'hidden',
                                                      maskImage:
                                                          'linear-gradient(to right, black 80%, transparent 100%)',
                                                      WebkitMaskImage:
                                                          'linear-gradient(to right, black 80%, transparent 100%)',
                                                  }
                                                : {
                                                      whiteSpace: 'nowrap',
                                                  }
                                        }
                                    >
                                        {source.documentTitle}
                                    </span>
                                </button>
                            </HoverCardTrigger>
                            <HoverCardContent className="w-80">
                                <div className="space-y-3">
                                    <div>
                                        <h4 className="mb-2 font-semibold text-gray-900">
                                            {source.documentTitle}
                                        </h4>

                                        <div className="space-y-1.5 text-sm text-gray-600">
                                            <div>
                                                <span className="font-medium">
                                                    Relevance Score:
                                                </span>{' '}
                                                <span>
                                                    {(source.relevanceScore * 100).toFixed(1)}%
                                                </span>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="border-t pt-2">
                                        <a
                                            href={documentUrl}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            className="inline-flex items-center gap-1.5 text-sm font-medium text-blue-600 hover:text-blue-800 hover:underline"
                                        >
                                            <span>View Document</span>
                                            <ExternalLink className="h-3.5 w-3.5" />
                                        </a>
                                    </div>
                                </div>
                            </HoverCardContent>
                        </HoverCard>
                    );
                })}
            </div>
        </div>
    );
}
