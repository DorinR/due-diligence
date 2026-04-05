const rawBackendUrl = import.meta.env.VITE_BACKEND_URL?.trim();

export const apiBaseUrl = rawBackendUrl
    ? rawBackendUrl.replace(/\/+$/, "")
    : "";

export const toApiUrl = (path: string) => {
    if (!path.startsWith("/")) {
        path = `/${path}`;
    }

    return `${apiBaseUrl}${path}`;
};
