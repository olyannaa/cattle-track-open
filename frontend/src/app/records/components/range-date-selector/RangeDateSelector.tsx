import React, { useState, useEffect } from 'react';
import { Segmented, DatePicker, Space, Flex } from 'antd';
import type { SegmentedValue } from 'antd/es/segmented';
import dayjs, { Dayjs } from 'dayjs';
import quarterOfYear from 'dayjs/plugin/quarterOfYear';
import { CalendarOutlined } from '@ant-design/icons';
import { useWindowSize } from '../../../../hooks/useWindowSize';
import { Range } from './rangeDateUtils';
dayjs.extend(quarterOfYear);

const MODE_OPTIONS = ['Неделя', 'Месяц', 'Квартал', 'Год'] as const;
type Mode = (typeof MODE_OPTIONS)[number];

const getRangeToToday = (mode: Mode, today: Dayjs = dayjs()): Range => {
    const end = today.endOf('day');
    switch (mode) {
        case 'Неделя': {
            // последние 7 дней, включая сегодня
            const start = today.subtract(6, 'day').startOf('day');
            return [start, end];
        }
        case 'Месяц': {
            const start = today.subtract(1, 'month').add(1, 'day').startOf('day');
            return [start, end];
        }
        case 'Квартал': {
            const start = today.subtract(3, 'month').add(1, 'day').startOf('day');
            return [start, end];
        }
        case 'Год': {
            const start = today.subtract(1, 'year').add(1, 'day').startOf('day');
            return [start, end];
        }
        default:
            return [today.startOf('day'), end];
    }
};

const detectModeByRange = (start: Dayjs | null, end: Dayjs | null): Mode | null => {
    if (!start || !end) return null;

    const today = dayjs();
    for (const option of MODE_OPTIONS) {
        const [optStart, optEnd] = getRangeToToday(option, today);
        if (start.isSame(optStart, 'day') && end.isSame(optEnd, 'day')) {
            return option;
        }
    }
    return null;
};

type RangeDateSelectorProps = {
    range: Range;
    onRangeChange: (range: Range) => void;
};

export const RangeDateSelector: React.FC<RangeDateSelectorProps> = ({
    range,
    onRangeChange,
}) => {
    const [mode, setMode] = useState<Mode | null>('Месяц');
    const width = useWindowSize();
    // При смене режима пересчитываем диапазон (если режим не снят)
    useEffect(() => {
        if (!mode) return;
        onRangeChange(getRangeToToday(mode));
    }, [mode]);

    const updateRange = (nextRange: Range) => {
        const [start, end] = nextRange;
        onRangeChange(nextRange);
        const detected = start && end ? detectModeByRange(start, end) : null;
        setMode(detected);
    };

    const onStartChange = (d: Dayjs | null) => {
        if (!d) return;

        const newStart = d.startOf('day');
        const [, end] = range;

        // если есть конец и новый старт > конца — НИЧЕГО не делаем
        if (end && newStart.isAfter(end, 'day')) {
            return;
        }

        updateRange([newStart, end]);
    };

    const onEndChange = (d: Dayjs | null) => {
        if (!d) return;

        const newEnd = d.endOf('day');
        const [start] = range;

        // если есть старт и новый конец < старта — НИЧЕГО не делаем
        if (start && newEnd.isBefore(start, 'day')) {
            return;
        }

        updateRange([start, newEnd]);
    };
    const onSegmentChange = (val: SegmentedValue) => {
        if (typeof val === 'string' && MODE_OPTIONS.includes(val as Mode)) {
            setMode(val as Mode);
        } else {
            setMode(null);
        }
    };

    return (
        <Flex vertical gap={16}>
            <Segmented
                options={MODE_OPTIONS.map((m) => ({
                    label: m,
                    value: m,
                }))}
                value={mode ?? undefined}
                onChange={onSegmentChange}
            />

            <Space align='center'>
                {width > 500 && <CalendarOutlined />}
                <DatePicker
                    value={range[0]}
                    onChange={onStartChange}
                    format='DD.MM.YYYY'
                    allowClear={false}
                />
                <span>—</span>
                <DatePicker
                    value={range[1]}
                    onChange={onEndChange}
                    format='DD.MM.YYYY'
                    allowClear={false}
                />
            </Space>
        </Flex>
    );
};
