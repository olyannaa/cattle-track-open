/* eslint-disable @typescript-eslint/no-explicit-any */
import { Flex } from 'antd';
import { InfrastructureTypes } from '../../data/enums/infrastructureTypes';
import { InfrastructureDataItem } from '../../services/infrastructure-service';
import { InfrastructureItem } from '../infrastructure-item/InfrastructureItem';
import styles from './InfrastructureList.module.css';

export const InfrastructureList = ({
    list,
    partInfrastructure,
    onDelete,
    title,
}: {
    list: InfrastructureDataItem[];
    partInfrastructure: InfrastructureTypes;
    onDelete: (id: string) => any;
    title?: string;
}) => {
    return (
        <Flex vertical className={`${styles['list']} content-container`}>
            <h2 style={{ marginBottom: 'var(--spacing-xl' }}>{title}</h2>
            {list.map((item: InfrastructureDataItem) => (
                <div key={item.id}>
                    <InfrastructureItem
                        item={item}
                        partInfrastructure={partInfrastructure}
                        onDelete={onDelete}
                    />
                </div>
            ))}
        </Flex>
    );
};
