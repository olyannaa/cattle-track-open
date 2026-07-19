import { Flex } from 'antd';
import { Events } from './components/events/Events';
import { EventChart } from './components/event-chart/EventChart';
import styles from './HistoryEventWrapper.module.css';

export const HistoryEventWrapper = () => {
    return (
        <Flex vertical className={styles['history-event__section']}>
            <h2 className='form-title'>История событий</h2>
            <EventChart />
            <Events />
        </Flex>
    );
};
