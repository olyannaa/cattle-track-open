import { Button, Empty, Flex, message, Table } from 'antd';
import { useGetDrugsQuery } from '../../services/drugs';
import { DrugSearch } from './components/DrugsSearch';
import { ModalCreateAndEditDrug } from './components/modal-create-and-edit-drug/ModalCreateAndEditDrug';
import { useState } from 'react';
import { IDrugsTable } from '../../data/interface/IDrugsTable';
import { IDrug } from '../../services/drugs';
import { getColumnsTableDrugs } from '../../data/const/columnsTableDrugs';
import { useFuseSearch } from '../../../../hooks/useFuseSearch';
import styles from './Drugs.module.css';

export const Drugs = () => {
    const { data: drugs = [], isLoading } = useGetDrugsQuery();
    const [openModal, setOpenModal] = useState(false);
    const [messageApi, contextHolder] = message.useMessage();
    const [searchText, setSearchText] = useState('');

    const filteredDrugs: IDrug[] = useFuseSearch<IDrug>(drugs, searchText, {
        keys: ['name', 'substance', 'factory'],
        threshold: 0.3,
        ignoreLocation: true,
        minMatchCharLength: 2,
        minSearchLength: 2,
    });

    return (
        <>
            {contextHolder}
            <Flex vertical style={{ width: '100%' }} gap={24}>
                <Flex justify='space-between' align='center' className={styles['header']}>
                    <h2>Библиотека препаратов</h2>
                    <Button
                        variant='solid'
                        type='primary'
                        size='large'
                        onClick={() => setOpenModal(true)}
                    >
                        <div style={{ fontSize: '18px' }}>+</div>
                        <div>Добавить препарат</div>
                    </Button>
                </Flex>

                {/* Только поле поиска, без выпадающих подсказок */}
                <DrugSearch value={searchText} onChange={setSearchText} />

                <Table<IDrugsTable>
                    dataSource={filteredDrugs.map((drug) => ({ ...drug, key: drug.id }))}
                    columns={getColumnsTableDrugs(messageApi) || []}
                    rowKey='id'
                    locale={{
                        emptyText: <Empty description='Ничего не найдено' />,
                    }}
                    pagination={false}
                    loading={isLoading}
                    style={{ width: '100%', overflowX: 'auto' }}
                />
            </Flex>
            {openModal && (
                <ModalCreateAndEditDrug
                    open={openModal}
                    setOpen={setOpenModal}
                    messageApi={messageApi}
                    drugs={drugs}
                />
            )}
        </>
    );
};
