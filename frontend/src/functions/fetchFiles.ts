import { AnimalFilterParams } from '../app/animal-accounting/data/interfaces/animal-filters-params';
import { getFilterParameters } from '../utils/create-parameters';
import { IUser } from '../utils/userType';

export const downloadXlsxAnimals = async (data: AnimalFilterParams) => {
    const user: IUser = JSON.parse(localStorage.getItem('user') || '');
    try {
        const queryParams = getFilterParameters(data);

        // Формируем URL с query параметрами
        const queryString = queryParams.toString();
        const response = await fetch(`${import.meta.env.VITE_API_URL}files/csv/animals${queryString ? `?${queryString}` : ''}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                organizationId: user.organizationId,
            },
            credentials: 'include',
        });
        if (response.status === 401) {
            window.location.href = '/';
            localStorage.removeItem('user');
        }
        if (!response.ok) {
            throw new Error('Ошибка при получении данных.');
        }
        const blob = await response.blob();
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = getName(response);
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    } catch (error) {
        console.error('Ошибка при получении данных:', error);
        throw error;
    }
};

export const getName = (response: Response) => {
    const result = response.headers.get('content-disposition')?.split('; ')[1].slice(10, -1);
    return result || `file.xlsx`;
};
