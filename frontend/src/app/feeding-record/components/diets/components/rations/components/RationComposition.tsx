import { Button, Flex, Form, InputNumber, Select, Table, Typography } from 'antd';
import { RationComponent } from '../../../../../data/ration';
import { DeleteOutlined, PlusOutlined } from '@ant-design/icons';
import { SelectDataType } from '../../../../../../../utils/selectDataType';
import { v4 as uuidv4 } from 'uuid';
const { Text } = Typography;

type RationCompositionProps = {
    groups: string[];
    components: RationComponent[];
    isEditing: boolean;
    handleComponentChange: (index: number, field: string, value: unknown) => void;
    removeComponent: (index: number) => void;
    addComponent: () => void;
    dietComponentsOptions: SelectDataType[];
};

export const RationComposition = ({
    groups,
    components,
    isEditing,
    handleComponentChange,
    removeComponent,
    addComponent,
    dietComponentsOptions,
}: RationCompositionProps) => {
    const columns = [
        {
            title: 'Компонент',
            dataIndex: 'componentName',
            key: 'componentId',
            render: (text: string, record: RationComponent, index: number) =>
                isEditing ? (
                    <Form.Item
                        name={['components', index, 'componentId']}
                        rules={[{ required: true }]}
                        style={{ marginBottom: 0 }}
                        required={true}
                        initialValue={record.componentId}
                    >
                        <Select
                            placeholder='Выберите компонент'
                            onChange={(value) => handleComponentChange(index, 'componentId', value)}
                            allowClear
                            options={dietComponentsOptions}
                        ></Select>
                    </Form.Item>
                ) : (
                    text
                ),
        },
        {
            title: 'Количество (кг)',
            dataIndex: 'kg',
            key: 'kg',
            render: (text: number, record: RationComponent, index: number) =>
                isEditing ? (
                    <InputNumber
                        type='number'
                        value={record.kg ?? 1}
                        min={0}
                        step={0.1}
                        onChange={(value) => handleComponentChange(index, 'kg', value)}
                    />
                ) : (
                    text
                ),
        },
        {
            title: 'Цена за ед.',
            dataIndex: 'cost',
            key: 'cost',
            render: (text: number, record: RationComponent, index: number) =>
                isEditing ? (
                    <InputNumber
                        type='number'
                        value={record.cost}
                        min={0}
                        step={0.01}
                        onChange={(value) => handleComponentChange(index, 'cost', value ?? 0)}
                    />
                ) : (
                    `${text?.toFixed(2) ?? '0.00'} ₽`
                ),
        },
        {
            title: 'Общая стоимость',
            key: 'totalCost',
            render: (_: unknown, record: RationComponent) => `${(record.kg * record.cost)?.toFixed(2) ?? '0.00'} ₽`,
        },
        {
            title: 'Нутриенты',
            render: (_: unknown, record: RationComponent) => {
                return (
                    <div style={{ fontSize: 12 }}>
                        <p>СВ: {(record.sv * record.kg).toFixed(2)}</p>
                        <p>СП: {(record.sp * record.kg).toFixed(2)}</p>
                        <p>ЧЭП: {(record.cep * record.kg).toFixed(2)}</p>
                        <p>НДК: {(record.ndk * record.kg).toFixed(2)}</p>
                    </div>
                );
            },
        },
        ...(isEditing
            ? [
                  {
                      title: 'Действия',
                      key: 'actions',
                      render: (_: unknown, __: unknown, index: number) => (
                          <Button icon={<DeleteOutlined />} danger onClick={() => removeComponent(index)} />
                      ),
                  },
              ]
            : []),
    ];

    const total = components?.reduce((sum, item) => sum + (item.kg || 0) * (item.cost || 0), 0);
    const totalCount = components?.reduce((sum, item) => sum + (item.kg || 0), 0);
    const totalNutrients = components?.reduce(
        (acc, comp) => {
            const kg = Number(comp.kg) || 0;
            acc.sv += (Number(comp.sv) || 0) * kg;
            acc.sp += (Number(comp.sp) || 0) * kg;
            acc.cep += (Number(comp.cep) || 0) * kg;
            acc.ndk += (Number(comp.ndk) || 0) * kg;
            return acc;
        },
        { sv: 0, sp: 0, cep: 0, ndk: 0 }
    );

    return (
        <div>
            <p>
                Используется группами:{' '}
                {groups.map((group, index) => {
                    if (groups.length - 1 === index) {
                        return <b>{group}</b>;
                    }

                    return <b>{group}, </b>;
                })}
            </p>
            <Flex className='content-align-center bottom-margin-xl'>
                <h3>Состав рациона</h3>
                {isEditing && (
                    <Button type='dashed' icon={<PlusOutlined />} onClick={addComponent}>
                        Добавить компонент
                    </Button>
                )}
            </Flex>
            <Form>
                <Table
                    columns={columns}
                    dataSource={components}
                    rowKey={(record) => `${record.componentId}-${uuidv4()}`}
                    pagination={false}
                    footer={() => (
                        <Flex justify='space-between'>
                            <div>
                                <Text strong>Количество кг веществ:</Text>
                                <p>{totalCount?.toFixed(2)} ₽</p>
                            </div>
                            <div>
                                <Text strong>Общая стоимость:</Text>
                                <p>{total?.toFixed(2)} ₽</p>
                            </div>
                            <div>
                                <Text strong>Итоговые нутриенты:</Text>
                                <div>СВ: {totalNutrients.sv.toFixed(1)} кг</div>
                                <div>СП: {totalNutrients.sp.toFixed(1)} кг</div>
                                <div>ЧЭП: {totalNutrients.cep.toFixed(1)} МДж</div>
                                <div>НДК: {totalNutrients.ndk.toFixed(1)} кг</div>
                            </div>
                        </Flex>
                    )}
                />
            </Form>
        </div>
    );
};
