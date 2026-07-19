import { Flex, Modal, Select } from 'antd';
import { IGroupRationInfo } from '../../../../data/group-ratio-info';
import { InfoCard } from '../../../../../../global-components/info-card/InfoCard';
import styles from './DetailedRationInfo.module.css';
import { IRation } from '../../../../data/ration';
import { useEffect, useState } from 'react';
import { SelectDataType } from '../../../../../../utils/formatting-data';
import FeedingSettingsForm from './components/feeding-settings-form/FeedingSettingsForm';

export const DetailedRationInfo = ({
    groupInfo,
    allRations,
    rationOptions,
    handleRationChange,
    open,
    onCancel,
    loading,
    setRefresh,
}: {
    groupInfo: IGroupRationInfo;
    allRations: IRation[];
    rationOptions: SelectDataType[];
    handleRationChange: (groupId: string, newRationId: string) => Promise<void>;
    open: boolean;
    onCancel: () => void;
    loading: boolean;
    setRefresh: (val: boolean) => void;
}) => {
    const [currentRation, setCurrentRation] = useState<IRation | null>();

    useEffect(() => {
        if (groupInfo.rationId) {
            const ration = allRations.find((ration) => ration.rationId === groupInfo.rationId);
            setCurrentRation(ration);
        } else {
            setCurrentRation(null);
        }
    }, [allRations, groupInfo, groupInfo.rationId]);

    return (
        <Modal
            open={open}
            onCancel={onCancel}
            centered
            width='55%'
            style={{
                top: 10,
                maxWidth: '95vw',
                marginBottom: 20,
            }}
            footer={null}
        >
            <Flex vertical>
                <h2>Подробная информация</h2>
                <p style={{ color: ' var(--secondary)' }}>
                    Здесь отображается подробная информация о рационе и настройках кормления.
                </p>
                {currentRation?.rationDescription && <p>Описание: {currentRation.rationDescription}</p>}
                <p>Количество голов в группе: {groupInfo.activeAnimalsCount}</p>
                <Flex align='center'>
                    <p>Выбранный рацион: </p>
                    <Select
                        value={groupInfo.rationId || undefined}
                        onChange={(val) => handleRationChange(groupInfo.groupId, val)}
                        style={{ width: 200, marginLeft: 4 }}
                        options={rationOptions}
                        placeholder={'Выберите рацион из списка'}
                        loading={loading}
                        disabled={loading}
                    />
                </Flex>
                <p>Стоимость рациона на 1 голову: {currentRation?.totalCost ?? '—'} ₽</p>
                <p>
                    Общая стоимость рациона на {groupInfo.activeAnimalsCount} голов:{' '}
                    {(currentRation?.totalCost ?? 0) * groupInfo.activeAnimalsCount} ₽
                </p>
                <h3>Нутриенты на 1 голову:</h3>
                <div className={styles['ration-info__wrapper']}>
                    <div style={{ background: '#fafafa', padding: 4, borderRadius: 4 }}>
                        <InfoCard title='СВ' description={groupInfo.svPerHead ? groupInfo.svPerHead.toFixed(2) : '—'} />
                    </div>
                    <div style={{ background: '#fafafa', padding: 4, borderRadius: 4 }}>
                        <InfoCard title='СП' description={groupInfo.spPerHead ? groupInfo.spPerHead.toFixed(2) : '—'} />
                    </div>
                    <div style={{ background: '#fafafa', padding: 4, borderRadius: 4 }}>
                        <InfoCard
                            title='ЧЭП'
                            description={groupInfo.cepPerHead ? groupInfo.cepPerHead.toFixed(2) : '—'}
                        />
                    </div>
                    <div style={{ background: '#fafafa', padding: 4, borderRadius: 4 }}>
                        <InfoCard
                            title='НДК'
                            description={groupInfo.ndkPerHead ? groupInfo.ndkPerHead.toFixed(2) : '—'}
                        />
                    </div>
                </div>
                <h3>Нутриенты на {groupInfo.activeAnimalsCount} голов:</h3>
                <div className={styles['ration-info__wrapper']}>
                    <div style={{ background: '#fafafa', padding: 4, borderRadius: 4 }}>
                        <InfoCard
                            title='СВ'
                            description={
                                groupInfo.svPerHead
                                    ? (groupInfo.svPerHead * groupInfo.activeAnimalsCount).toFixed(2)
                                    : '—'
                            }
                        />
                    </div>
                    <div style={{ background: '#fafafa', padding: 4, borderRadius: 4 }}>
                        <InfoCard
                            title='СП'
                            description={
                                groupInfo.spPerHead
                                    ? (groupInfo.spPerHead * groupInfo.activeAnimalsCount).toFixed(2)
                                    : '—'
                            }
                        />
                    </div>
                    <div style={{ background: '#fafafa', padding: 4, borderRadius: 4 }}>
                        <InfoCard
                            title='ЧЭП'
                            description={
                                groupInfo.cepPerHead
                                    ? (groupInfo.cepPerHead * groupInfo.activeAnimalsCount).toFixed(2)
                                    : '—'
                            }
                        />
                    </div>
                    <div style={{ background: '#fafafa', padding: 4, borderRadius: 4 }}>
                        <InfoCard
                            title='НДК'
                            description={
                                groupInfo.ndkPerHead
                                    ? (groupInfo.ndkPerHead * groupInfo.activeAnimalsCount).toFixed(2)
                                    : '—'
                            }
                        />
                    </div>
                </div>
            </Flex>
            <FeedingSettingsForm groupInfo={groupInfo} disableBtn={loading} setRefresh={setRefresh} />
        </Modal>
    );
};
