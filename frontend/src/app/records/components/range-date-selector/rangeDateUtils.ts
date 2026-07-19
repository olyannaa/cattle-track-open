import dayjs, { Dayjs } from 'dayjs';

export type Range = [Dayjs | null, Dayjs | null];

export const getRangeMonthToToday = (): Range => {
    const today = dayjs();
    const end = today.endOf('day');
    const start = today.subtract(1, 'month').add(1, 'day').startOf('day');
    return [start, end];
};
