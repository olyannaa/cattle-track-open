import { useState } from 'react';
import { getUser } from '../utils/userInfo';
import { getName } from '../functions/fetchFiles';

export const useDownload = () => {
    const [isLoading, setIsLoading] = useState(false);

    const downloadFile = async (url: string, params: Record<string, string> = {}) => {
        setIsLoading(true);
        try {
            const queryParams = new URLSearchParams(params).toString();
            const fullUrl = `${import.meta.env.VITE_API_URL}${url}?${queryParams}`;

            const response = await fetch(fullUrl, {
                method: 'GET',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                    organizationId: getUser().organizationId,
                },
            });

            if (!response.ok) {
                return false;
            }

            if (response.status === 401) {
                window.location.href = '/';
                localStorage.removeItem('user');
            }
            if (!response.ok) {
                throw new Error('Ошибка при получении данных.');

                return false;
            }
            const blob = await response.blob();
            const fileUrl = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = fileUrl;
            link.download = getName(response);
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            return true;
        } catch (err: unknown) {
            console.log(err);
            return false;
        } finally {
            setIsLoading(false);
        }
    };

    return { downloadFile, isLoading };
};
