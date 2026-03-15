# AGENTS.md — Frontend Style Guide

This file defines conventions for AI coding agents (Cursor, Codex, etc.) working in this codebase.

## Project Overview

Single-page application built with **React 19**, **Vite 6**, and **TypeScript 5.9** (strict mode). Styling is handled by **Tailwind CSS 4**. The app is served as a client-side SPA with React Router 7 for routing.

## Tech Stack

| Concern      | Library                                    |
| ------------ | ------------------------------------------ |
| UI           | React 19                                   |
| Build        | Vite 6                                     |
| Language     | TypeScript 5.9 (`strict: true`)            |
| Styling      | Tailwind CSS 4 + `clsx` + `tailwind-merge` |
| Server state | TanStack React Query                       |
| Client state | Jotai (sparse, only when needed)           |
| Forms        | React Hook Form                            |
| Routing      | React Router 7                             |
| HTTP         | Axios                                      |
| Tables       | @tanstack/react-table                      |
| Icons        | Lucide React                               |

Always use the latest stable version of these libraries.

## Component Conventions

- **Functional components only.** No class components.
- **PascalCase** for component file names and component names (`ConversationPage.tsx`, `DashboardPage.tsx`).
- **camelCase** for hooks (`useGetConversationList.ts`), utilities, and helper files.
- Props are typed **inline** at the function signature, not as separate exported interfaces:

```tsx
export function ConversationsList({
    conversations,
    activeConversationId,
}: {
    conversations: Conversation[];
    activeConversationId?: string;
}) {
    // ...
}
```

- Use `<Fragment>` from React, not the `<>` shorthand.
- Named imports from React (`import { useState, useEffect } from "react"`), not `import React`.

### Styling

- Use Tailwind utility classes directly in JSX.
- Use `clsx` for conditional classes and `twMerge` (from `tailwind-merge`) when merging class lists that may conflict.
- Do not use CSS modules, styled-components, or inline `style` objects unless there is no Tailwind equivalent.

## State Management

**React Query is the primary state management tool.** It handles server state, caching, background refetching, and optimistic updates. Most application state is server-derived and should live in React Query's cache.

**Jotai** is used sparingly for client-only UI state that doesn't belong in React Query (e.g., which tab is active, modal open/closed). Prefer `atomWithImmer` from `jotai-immer` when the state shape involves nested updates.

**Do not reach for global state by default.** Prefer local component state (`useState`) or React Query. Only promote to Jotai when state must be shared across distant components and prop-drilling or context becomes unwieldy.

## API Layer (Endpoint Colocation Pattern)

The data-fetching architecture is organized by endpoint. Each endpoint gets a single file in `api/` that contains the full client contract for that endpoint:

1. Endpoint-specific types.
2. The raw API function that calls the backend via the shared Axios instance (`backendAccessPoint`).
3. The React Query hook that wraps the raw API function.

This is a colocated design, not a physically separate `api/` and `apiHooks/` split. The separation is by responsibility within the file, not by folder:

- Types define the request/response contract for the endpoint.
- The raw API function performs the network call and maps backend data into clean frontend data.
- The hook owns caching behavior: query keys, stale times, invalidation, optimistic updates.

The goal is to keep all endpoint-specific logic in one place so a change to a backend contract usually requires editing one file instead of several.

### Endpoint File Structure

Each endpoint file in `api/` is responsible for:

1. Defining the raw backend wire types and the cleaned frontend types.
2. **Mapping** from the backend response to the frontend type.
3. Exporting the plain async API function.
4. Exporting the React Query hook for that endpoint.

### Type Naming Convention

Use explicit suffixes so the backend boundary is obvious at a glance:

- `Dto` suffix for the exact wire format that matches the backend model.
- `Request` suffix for the input your app passes to the exported API function.
- `Response` suffix for the value your app gets back from the exported API function.

### `type` vs `interface`

In the API layer, prefer `type` aliases by default. DTOs, request shapes, response shapes, and mapped frontend contracts should all use `type`, not `interface`.

Use `interface` only when you specifically need interface behavior such as declaration merging or intentional extension across module boundaries. That is uncommon in this codebase's API layer.

Preferred:

- `type LoginRequestDto = { ... }`
- `type LoginResponse = { ... }`
- `type DocumentSource = { ... }`

Avoid:

- `interface LoginRequestDto { ... }`
- `interface LoginResponse { ... }`

The goal is to keep the API layer consistent and optimized for data-shape modeling rather than extensible object contracts.

Examples:

- `LoginRequestDto` and `LoginResponseDto` are backend-facing wire types.
- `LoginRequest` and `LoginResponse` are frontend-facing API contract types.

If the backend shape and frontend shape happen to be identical today, still keep the `Dto` type separate when the boundary matters. That gives you a stable place to absorb backend drift later without changing components.

Domain helper types that are nested inside a response can use normal descriptive names without a suffix (`ConversationSummary`, `AuthUser`, `DocumentSource`), but the exported function boundary should use `Request` and `Response` names.

### Mapping Style

Keep endpoint DTO mapping inline inside the API function. Do not extract a separate `mapFooDto` helper unless the mapping is genuinely complex and reused.

Preferred:

- Build the request DTO inline before the network call.
- Always assign it to a `const payload: SomeRequestDto = { ... }` variable before calling Axios.
- Return the mapped frontend shape inline from the API function.

Avoid:

- Small one-off mapping helpers like `mapLoginResponseDto` or `mapDocumentDto`.
- Passing app-facing request objects directly into Axios when the endpoint has a request body.

```tsx
// api/conversation/getConversationList.ts

type ConversationDto = {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    companies: Array<{
        id: number;
        companyName: string;
    }>;
};

export type ConversationSummary = {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    companies: Array<{
        id: string;
        companyName: string;
    }>;
};

export type GetConversationListResponse = ConversationSummary[];

export const getConversationList = async (): Promise<GetConversationListResponse> => {
    const response = await backendAccessPoint.get<ConversationDto[]>(
        "/api/conversation",
    );

    return response.data.map((conversation) => ({
        id: conversation.id,
        title: conversation.title,
        createdAt: conversation.createdAt,
        updatedAt: conversation.updatedAt,
        companies: conversation.companies.map((company) => ({
            id: company.id.toString(),
            companyName: company.companyName,
        })),
    }));
};
```

DTO types are implementation details and should usually stay unexported. Export the app-facing `Request` and `Response` types, plus any small domain helper types the UI needs.

The React Query hook for the same endpoint lives in the same file and is responsible for:

1. Wrapping the raw API function with React Query (`useQuery`, `useMutation`).
2. Managing **caching logic**: query keys, stale times, invalidation.
3. Exposing a clean hook API to components.

For **mutations**, the hook returns a named function ending in `Async`:

```tsx
// api/message/sendMessage.ts

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";
 
type SendMessageRequestDto = {
    content: string;
    role: number;
};

type SendMessageResponseDto = {
    id: string;
    text: string;
    role: "User" | "Assistant" | "System";
    timestamp: string;
    conversationId: string;
};

export type SendMessageRequest = {
    content: string;
    role: MessageRole;
};

export type SendMessageResponse = {
    id: string;
    text: string;
    role: "User" | "Assistant" | "System";
    timestamp: string;
    conversationId: string;
};

export const sendMessage = async (
    conversationId: string,
    data: SendMessageRequest
): Promise<SendMessageResponse> => {
    const payload: SendMessageRequestDto = {
        content: data.content,
        role: data.role,
    };

    const response = await backendAccessPoint.post<SendMessageResponseDto>(
        `/api/conversations/${conversationId}/message`,
        payload
    );

    return {
        id: response.data.id,
        text: response.data.text,
        role: response.data.role,
        timestamp: response.data.timestamp,
        conversationId: response.data.conversationId,
    };
};

export const useSendMessage = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({
            conversationId,
            data,
        }: {
            conversationId: string;
            data: SendMessageRequest;
        }) => sendMessage(conversationId, data),
        onSuccess: (_, { conversationId }) => {
            queryClient.invalidateQueries({
                queryKey: ["messages", conversationId],
            });
            queryClient.invalidateQueries({
                queryKey: ["conversation", conversationId],
            });
        },
    });
};
```

For **queries**, the hook returns the standard React Query result:

```tsx
// api/message/getMessageListByConversation.ts

import { useQuery } from "@tanstack/react-query";
import { backendAccessPoint } from "../backendAccessPoint";

type MessageFromServer = {
    id: string;
    text: string;
};

export type Message = {
    id: string;
    text: string;
};

export type GetMessageListByConversationResponse = Message[];

export const getMessageListByConversation = async (
    conversationId: string
): Promise<GetMessageListByConversationResponse> => {
    const response = await backendAccessPoint.get<MessageFromServer[]>(
        `/api/conversations/${conversationId}/message`
    );

    return response.data.map((message) => ({
        id: message.id,
        text: message.text,
    }));
};

export const useGetMessageListByConversation = (conversationId: string) => {
    return useQuery({
        queryKey: ["messages", conversationId],
        queryFn: () => getMessageListByConversation(conversationId),
        enabled: !!conversationId,
    });
};
```

**Key rules:**

- One endpoint file should contain the endpoint's exported frontend types, raw API function, and React Query hook.
- `api/` functions know nothing about React Query. They are plain async functions and must remain usable without hooks.
- React Query hooks live in the same file as the API function and only orchestrate caching.
- Backend wire types use the `Dto` suffix.
- Exported function boundary types use `Request` and `Response` suffixes.
- Use `type` aliases by default in the API layer.
- For request bodies, always create a typed `payload` variable using the endpoint's `RequestDto` type before the Axios call.
- Keep DTO-to-frontend mapping inline in the API function by default.
- Keep the file scoped to one endpoint or one endpoint action. Do not turn a domain folder into a dumping ground.
- Mutation hooks return `{ verbNounAsync: mutateAsync, isPending }`.
- Query hook names start with `useGet`.

## Import Rules

- **Relative imports only.** No path aliases (no `@/` or `~/`).
- All imports go at the **top of the file**. No inline or dynamic imports unless genuinely needed for code-splitting.

## Linting

The linting setup is intentionally lightweight to avoid slowing down development. We use ESLint + Prettier with a minimal rule set:

- **Prettier** handles all formatting (semicolons, quotes, trailing commas, line width). Do not fight with Prettier.
- **`no-explicit-any`** — Do not use `any`. Use `unknown` and narrow, or define a proper type.
- **`prefer-const`** — Use `const` unless reassignment is needed.
- **`eqeqeq`** — Always use `===` and `!==`.
- **`no-console`** — Warn. Use a proper logging utility or remove before committing.
- **`prefer-template`** — Use template literals over string concatenation.

Beyond these, lean toward shipping. If a lint rule is blocking you from delivering working code and the violation is cosmetic, note it and move on.

## Quick Reference

**Do:**

- Use React Query for all server state.
- Keep endpoint-specific types, raw API functions, and React Query hooks in the same `api/` file.
- Use `Dto` suffixes for backend wire shapes and `Request`/`Response` suffixes for the exported API contract.
- Use `type` aliases for DTOs, request/response types, and domain helper shapes in the API layer.
- Map backend responses to clean frontend types in the raw API function.
- Build request bodies through a typed `payload` object before calling Axios.
- Prefer inline mapping in the API function over tiny standalone mapper helpers.
- Use Tailwind for all styling.
- Use `const` by default, `let` only when needed.
- Use strict TypeScript — no `any`, no `as` casts unless absolutely necessary.
- Name mutation return values as `verbNounAsync`.

**Don't:**

- Don't use class components.
- Don't export backend DTO types from `api/` files unless another endpoint in the same feature genuinely needs them.
- Don't split one endpoint across separate `api/`, `types`, and `apiHooks` files unless the file has clearly outgrown endpoint-level colocation.
- Don't put Axios calls or response mapping inside components.
- Don't create one-off `mapXDto` helpers for simple endpoint mapping.
- Don't use `interface` in the API layer unless you specifically need declaration merging or cross-module extension.
- Don't pass request bodies directly to Axios without first assigning a typed `payload` DTO.
- Don't use global state (Jotai) when local state or React Query suffices.
- Don't use CSS modules, styled-components, or inline styles.
- Don't use `<>` shorthand — use `<Fragment>`.
- Don't add lint rules that slow down shipping without clear safety benefit.
