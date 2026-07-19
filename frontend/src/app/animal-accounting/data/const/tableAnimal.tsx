/* eslint-disable @typescript-eslint/no-unused-vars */
import { TableProps, TabsProps } from 'antd';
import { FieldTable } from '../../components/FieldTable/FieldTable';
import { IAnimalTable } from '../interfaces/animalTable';
import { IdentificationFieldName } from '../types/animal';
import { IdentificationFieldTable } from '../../components/IdentificationFieldTable/IdentificationFieldTable';
import { Link } from 'react-router-dom';

export enum animalTableColumns {}

interface AnimalTableSortInfo {
    column: string | null;
    descending: boolean;
}

export const items: TabsProps['items'] = [
    {
        key: 'Корова',
        label: 'Коровы',
    },
    {
        key: 'Нетель',
        label: 'Нетели',
    },
    {
        key: 'Бык',
        label: 'Быки',
    },
    {
        key: 'Телка',
        label: 'Телки',
    },
    {
        key: 'Бычок',
        label: 'Бычки',
    },
    {
        key: 'Яловые',
        label: 'Яловые',
    },
];

export const getColumns = (
    isEditTable: boolean,
    fieldsName: IdentificationFieldName[],
    sortInfo?: AnimalTableSortInfo,
): TableProps<IAnimalTable>['columns'] => {
    const getSortOrder = (column: string) => {
        if (sortInfo?.column !== column) {
            return null;
        }

        return sortInfo.descending ? 'descend' : 'ascend';
    };

    return [
              {
                  title: '№ бирки',
                  dataIndex: 'tagNumber',
                  key: 'tagNumber',
                  render: (_, { identificationFields, ...animal }) => (
                      <Link className='link' to={`/animal-card/${animal.id}`}>
                          <FieldTable
                              animal={animal}
                              dataIndex={'tagNumber'}
                          />
                      </Link>
                  ),
                  minWidth: 89,
                  sorter: true,
                  sortOrder: getSortOrder('TagNumber'),
              },
              {
                  title: 'Тип',
                  dataIndex: 'type',
                  key: 'type',
                  render: (_, { identificationFields, ...animal }) => (
                      <FieldTable
                          animal={animal}
                          dataIndex={'type'}
                      />
                  ),
                  minWidth: 89,
                  sorter: true,
                  sortOrder: getSortOrder('Type'),
              },
              {
                  title: 'Дата рождения',
                  dataIndex: 'birthDate',
                  key: 'birthDate',
                  render: (_, { identificationFields, ...animal }) => (
                      <FieldTable
                          animal={animal}
                          dataIndex={'birthDate'}
                      />
                  ),
                  minWidth: 116,
                  sorter: true,
                  sortOrder: getSortOrder('BirthDate'),
              },
              {
                  title: 'Порода',
                  dataIndex: 'breed',
                  key: 'breed',
                  minWidth: 130,
                  render: (_, { identificationFields, ...animal }) => (
                      <FieldTable
                          animal={animal}
                          dataIndex={'breed'}
                      />
                  ),
                  sorter: true,
                  sortOrder: getSortOrder('Breed'),
              },
              {
                  title: 'Группа',
                  dataIndex: 'groupName',
                  key: 'groupName',
                  sorter: true,
                  sortOrder: getSortOrder('GroupName'),
                  render: (_, { identificationFields, ...animal }) => (
                      <FieldTable
                          animal={animal}
                          dataIndex={'groupName'}
                      />
                  ),
                  minWidth: 225,
              },
              {
                  title: 'Статус',
                  dataIndex: 'status',
                  key: 'status',
                  render: (_, { identificationFields, ...animal }) => (
                      <FieldTable
                          animal={animal}
                          dataIndex={'status'}
                      />
                  ),
                  minWidth: 128,
                  sorter: true,
                  sortOrder: getSortOrder('Status'),
              },
              {
                  title: 'Происхождение',
                  dataIndex: 'origin',
                  key: 'origin',
                  minWidth: 157,
                  render: (_, { identificationFields, ...animal }) => (
                      <FieldTable
                          animal={animal}
                          dataIndex={'origin'}
                      />
                  ),
                  sorter: true,
                  sortOrder: getSortOrder('Origin'),
              },
              {
                  title: 'Место происхождения',
                  dataIndex: 'originLocation',
                  key: 'originLocation',
                  minWidth: 159,
                  render: (_, { identificationFields, ...animal }) => (
                      <FieldTable
                          animal={animal}
                          dataIndex={'originLocation'}
                      />
                  ),
                  sorter: true,
                  sortOrder: getSortOrder('OriginLocation'),
              },
              {
                  title: '№ матери',
                  dataIndex: 'motherTagNumber',
                  key: 'motherTagNumber',
                  minWidth: 78,
                  render: (_, { identificationFields, ...animal }) => (
                      <FieldTable
                          animal={animal}
                          dataIndex={'motherTagNumber'}
                      />
                  ),
                  sorter: true,
                  sortOrder: getSortOrder('MotherTagNumber'),
              },
              {
                  title: '№ отца',
                  dataIndex: 'fatherTagNumbers',
                  key: 'fatherTagNumbers',
                  minWidth: 78,
                  render: (_, { identificationFields, ...animal }) => (
                      <FieldTable
                          animal={animal}
                          dataIndex={'fatherTagNumbers'}
                      />
                  ),
              },
              ...fieldsName.map(({ name }) => ({
                  title: name,
                  dataIndex: name,
                  key: name,
                  minWidth: 100,
                  render: (_: unknown, animal: IAnimalTable) => {
                      const fieldValue =
                          animal.identificationFields?.find((f) => f.name === name)
                              ?.value ?? '';
                      return (
                          <IdentificationFieldTable
                              isEditTable={isEditTable}
                              nameField={name}
                              value={fieldValue}
                              id={animal.id}
                          />
                      );
                  },
              })),
              {
                  title: 'Дата последней вакцинации',
                  dataIndex: 'lastVaccinationDate',
                  key: 'lastVaccinationDate',
                  minWidth: 78,
                  render: (_, { identificationFields, ...animal }) => (
                      <FieldTable
                          animal={animal}
                          dataIndex={'lastVaccinationDate'}
                      />
                  ),
              },
          ];
};
