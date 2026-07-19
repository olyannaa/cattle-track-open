/* eslint-disable @typescript-eslint/no-explicit-any */
export type ErrorType = {
    statusCode: string;
    data?: {
        errorText?: string;
    };
};

export function isErrorType(error: unknown): error is ErrorType {
    return typeof error === 'object' && error !== null && 'data' in error && typeof (error as any).data === 'object';
}
