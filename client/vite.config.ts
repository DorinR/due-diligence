import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import path from 'path';
import { defineConfig, loadEnv } from 'vite';

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), '');
    const devProxyTarget = env.VITE_DEV_PROXY_TARGET || 'http://localhost:5104';

    return {
        plugins: [react(), tailwindcss()],
        resolve: {
            alias: {
                '@': path.resolve(__dirname, './src'),
            },
        },
        server: {
            proxy: {
                '/api': {
                    target: devProxyTarget,
                    changeOrigin: true,
                },
                '/hubs': {
                    target: devProxyTarget,
                    changeOrigin: true,
                    ws: true,
                },
                '/health': {
                    target: devProxyTarget,
                    changeOrigin: true,
                },
                '/hangfire': {
                    target: devProxyTarget,
                    changeOrigin: true,
                },
            },
        },
    };
});
