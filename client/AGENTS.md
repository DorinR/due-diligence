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

## API Layer (Two-Layer Pattern)

The data-fetching architecture is split into two layers with distinct responsibilities:

### Layer 1: `api/` — Raw API Functions

These files live in `api/` and are responsible for:

1. Calling the backend via the shared Axios instance (`backendAccessPoint`).
2. Defining **two types**: the raw backend response shape and the cleaned frontend shape.
3. **Mapping** from the backend response to the frontend type.

```tsx
// api/conversation/getConversationList.ts

type ConversationFromServer = {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    companies: Array<{
        id: number;
        companyName: string;
    }>;
}

export type Conversation = {
    id: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    companies: Array<{
        id: string;
        companyName: string;
    }>;
}

export const getConversationList = async (): Promise<Conversation[]> => {
    const response = await backendAccessPoint.get<ConversationFromServer[]>(
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

The backend response type is **not exported** — it is an implementation detail. Only the clean frontend type is exported.

### Layer 2: React Query Wrappers

These files live alongside the API functions in `api/` and are responsible for:

1. Wrapping the `api/` functions with React Query (`useQuery`, `useMutation`).
2. Managing **caching logic**: query keys, stale times, invalidation.
3. Exposing a clean hook API to components.

For **mutations**, the hook returns a named function ending in `Async`:

```tsx
// api/message/sendMessage.ts

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { sendMessage } from "./sendMessage";

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
import { getMessageListByConversation } from "./getMessageListByConversation";

export const useGetMessageListByConversation = (conversationId: string) => {
    return useQuery({
        queryKey: ["messages", conversationId],
        queryFn: () => getMessageListByConversation(conversationId),
        enabled: !!conversationId,
    });
};
```

**Key rules:**

- `api/` functions know nothing about React Query. They are plain async functions.
- React Query hooks live alongside the API functions and only orchestrate caching.
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
- Map backend responses to clean frontend types in the `api/` layer.
- Keep `apiHooks/` thin — caching logic only.
- Use Tailwind for all styling.
- Use `const` by default, `let` only when needed.
- Use strict TypeScript — no `any`, no `as` casts unless absolutely necessary.
- Name mutation return values as `verbNounAsync`.

**Don't:**

- Don't use class components.
- Don't export backend response types from `api/` files.
- Don't put Axios calls or response mapping in `apiHooks/`.
- Don't use global state (Jotai) when local state or React Query suffices.
- Don't use CSS modules, styled-components, or inline styles.
- Don't use `<>` shorthand — use `<Fragment>`.
- Don't add lint rules that slow down shipping without clear safety benefit.
