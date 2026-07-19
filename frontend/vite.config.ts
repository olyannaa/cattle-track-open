import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd());

    return {
        base: '/app/',
        plugins: [react()],
        define: {
            __API_URL__: JSON.stringify(env.VITE_API_URL),
            
        },
        server:
            {
                host: true,
                port: 4173,
                allowedHosts: ['localhost', '127.0.0.1'],
                watch:{
                    usePolling: true
                }
            },
        preview: {
            host: true,
            port: 4173,
            allowedHosts: ['localhost', '127.0.0.1']
        }
    };
});
