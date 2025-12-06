import { PaperPlaneIcon } from "@radix-ui/react-icons";
import { ReactNode, useState } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { DocumentSource } from "../api/message/types";
import { SourceCitations } from "./SourceCitations";
import { Button } from "./ui/button/Button";

export type Message = {
    id: string;
    text: string;
    sender: "User" | "Assistant" | "System";
    timestamp: string;
    sources?: DocumentSource[];
};

type ChatInterfaceProps = {
    messages: Message[];
    onSendMessage: (message: string) => void;
    isLoading?: boolean;
    showInput?: boolean;
    emptyStateContent?: ReactNode;
    conversationType?: string;
};

export function ChatInterface({
    messages,
    onSendMessage,
    isLoading = false,
    showInput = true,
    emptyStateContent,
}: ChatInterfaceProps) {
    const [inputValue, setInputValue] = useState("");

    const handleSendMessage = () => {
        if (inputValue.trim()) {
            onSendMessage(inputValue);
            setInputValue("");
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            handleSendMessage();
        }
    };

    const getEmptyStateText = () => {
        return "Ask questions about your documents";
    };

    const getPlaceholderText = () => {
        return "Ask a question about your documents...";
    };

    return (
        <div className="flex h-full flex-col p-4">
            <div
                className="scrollbar-thin scrollbar-thumb-gray-300 scrollbar-track-gray-100 flex-1 overflow-y-auto"
                style={{ height: "calc(100% - 130px)" }}
            >
                <div className="flex flex-col gap-4 pb-4">
                    {messages.length === 0
                        ? (emptyStateContent ?? (
                              <div className="py-9 text-center text-gray-500">
                                  {getEmptyStateText()}
                              </div>
                          ))
                        : messages.map((message) => (
                              <div
                                  key={message.id}
                                  className={`flex max-w-[80%] flex-col rounded-lg p-3 ${
                                      message.sender === "User"
                                          ? "ml-auto self-end bg-blue-100"
                                          : "self-start rounded-md border bg-slate-50"
                                  }`}
                              >
                                  <div className="mb-1 flex items-center gap-2">
                                      <div
                                          className={`flex h-6 w-6 items-center justify-center rounded-full text-xs font-medium ${
                                              message.sender === "User"
                                                  ? "bg-blue-600 text-white"
                                                  : "bg-cyan-600 text-white"
                                          }`}
                                      >
                                          {message.sender === "User"
                                              ? "U"
                                              : "AI"}
                                      </div>
                                      <span className="text-xs text-gray-600">
                                          {message.sender === "User"
                                              ? "You"
                                              : "AI Assistant"}
                                      </span>
                                      <span className="ml-auto text-xs text-gray-500">
                                          {message.timestamp}
                                      </span>
                                  </div>
                                  <div className="prose prose-sm max-w-none text-gray-900">
                                      <ReactMarkdown
                                          remarkPlugins={[remarkGfm]}
                                          components={{
                                              // Customize headings
                                              h1: (props) => (
                                                  <h1
                                                      className="mt-4 mb-2 text-xl font-bold"
                                                      {...props}
                                                  />
                                              ),
                                              h2: (props) => (
                                                  <h2
                                                      className="mt-3 mb-2 text-lg font-bold"
                                                      {...props}
                                                  />
                                              ),
                                              h3: (props) => (
                                                  <h3
                                                      className="mt-2 mb-1 text-base font-bold"
                                                      {...props}
                                                  />
                                              ),
                                              // Customize lists
                                              ul: (props) => (
                                                  <ul
                                                      className="mb-2 ml-4 list-disc space-y-1"
                                                      {...props}
                                                  />
                                              ),
                                              ol: (props) => (
                                                  <ol
                                                      className="mb-2 ml-4 list-decimal space-y-1"
                                                      {...props}
                                                  />
                                              ),
                                              li: (props) => (
                                                  <li
                                                      className="ml-1"
                                                      {...props}
                                                  />
                                              ),
                                              // Customize paragraphs
                                              p: (props) => (
                                                  <p
                                                      className="mb-2 last:mb-0"
                                                      {...props}
                                                  />
                                              ),
                                              // Customize code blocks
                                              code: ({
                                                  inline,
                                                  ...props
                                              }: any) =>
                                                  inline ? (
                                                      <code
                                                          className="rounded bg-gray-200 px-1.5 py-0.5 font-mono text-sm"
                                                          {...props}
                                                      />
                                                  ) : (
                                                      <code
                                                          className="mb-2 block overflow-x-auto rounded-md bg-gray-100 p-3 font-mono text-sm"
                                                          {...props}
                                                      />
                                                  ),
                                              // Customize blockquotes
                                              blockquote: (props) => (
                                                  <blockquote
                                                      className="my-2 border-l-4 border-gray-300 pl-4 italic"
                                                      {...props}
                                                  />
                                              ),
                                              // Customize links
                                              a: (props) => (
                                                  <a
                                                      className="text-blue-600 hover:underline"
                                                      target="_blank"
                                                      rel="noopener noreferrer"
                                                      {...props}
                                                  />
                                              ),
                                              // Customize tables
                                              table: (props) => (
                                                  <table
                                                      className="my-2 border-collapse border border-gray-300"
                                                      {...props}
                                                  />
                                              ),
                                              th: (props) => (
                                                  <th
                                                      className="border border-gray-300 bg-gray-100 px-3 py-2 font-semibold"
                                                      {...props}
                                                  />
                                              ),
                                              td: (props) => (
                                                  <td
                                                      className="border border-gray-300 px-3 py-2"
                                                      {...props}
                                                  />
                                              ),
                                              // Customize strong (bold)
                                              strong: (props) => (
                                                  <strong
                                                      className="font-bold"
                                                      {...props}
                                                  />
                                              ),
                                              // Customize emphasis (italic)
                                              em: (props) => (
                                                  <em
                                                      className="italic"
                                                      {...props}
                                                  />
                                              ),
                                          }}
                                      >
                                          {message.text}
                                      </ReactMarkdown>
                                  </div>
                                  {message.sender === "Assistant" &&
                                      message.sources && (
                                          <SourceCitations
                                              sources={message.sources}
                                          />
                                      )}
                              </div>
                          ))}
                    {isLoading && (
                        <div className="flex max-w-[80%] flex-col self-start rounded-lg bg-gray-100 p-3">
                            <div className="mb-1 flex items-center gap-2">
                                <div className="flex h-6 w-6 items-center justify-center rounded-full bg-cyan-600 text-xs font-medium text-white">
                                    AI
                                </div>
                                <span className="text-xs text-gray-600">
                                    AI Assistant
                                </span>
                                <span className="ml-auto text-xs text-gray-500">
                                    {new Date().toLocaleTimeString([], {
                                        hour: "2-digit",
                                        minute: "2-digit",
                                    })}
                                </span>
                            </div>
                            <div className="whitespace-pre-wrap text-gray-900">
                                Thinking...
                            </div>
                        </div>
                    )}
                </div>
            </div>

            {showInput && (
                <div className="sticky bottom-0 flex gap-2">
                    <textarea
                        placeholder={getPlaceholderText()}
                        value={inputValue}
                        onChange={(e) => setInputValue(e.target.value)}
                        onKeyDown={handleKeyDown}
                        disabled={isLoading}
                        className="flex-1 grow resize-none rounded-lg border border-gray-300 bg-white p-3 focus:border-transparent focus:ring-2 focus:ring-blue-500 focus:outline-none disabled:bg-gray-50 disabled:text-gray-500"
                        rows={1}
                    />
                    <Button
                        onClick={handleSendMessage}
                        disabled={!inputValue.trim() || isLoading}
                        variant="primary"
                        icon={PaperPlaneIcon}
                        iconPosition="left"
                    >
                        Send
                    </Button>
                </div>
            )}
        </div>
    );
}
