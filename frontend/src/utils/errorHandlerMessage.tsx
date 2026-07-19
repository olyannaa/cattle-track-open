import { isErrorType } from './errorType';

export const ErrorHandlerMessage = (err: unknown, defaultMessage?: string) => {
    if (isErrorType(err) && err?.data?.errorText) {
        return err.data.errorText;
    }
    return defaultMessage ?? 'Сервис временно не доступен. Попробуйте позже';
};
