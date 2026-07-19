import { FiltersAnimalsType } from '../utils/filtersAnimals';
import { IRequestGetIdentificationValues } from '../utils/requestType';
import { SortersAnimalsType } from '../utils/sortersAnimals';

export const getUrlFilterAnimals = (
    filters: FiltersAnimalsType,
    sorters: SortersAnimalsType = {
        column: '',
        descending: false,
        page: 0,
    },
    isId: boolean = false
) => {
    let url = isId ? 'DailyActions/animals/ids?' : 'DailyActions/animals?';
    Object.keys(filters).forEach((filter) => {
        if (filter === 'isActive') {
            url += `Filter.IsActive=${filters[filter] || false}&`;
        }
        if (filters[filter]) {
            if (filter === 'identificationFieldId' && filters.identificationFieldValue) {
                url += `Filter.IdentificationField.Id=${filters[filter]}&`;
            } else if (
                filter === 'identificationFieldValue' &&
                filters.identificationFieldId
            ) {
                url += `Filter.IdentificationField.Value=${filters[filter]}&`;
            } else if (filter === 'groupId') {
                url += `Filter.GroupId=${filters[filter]}&`;
            } else if (filter === 'type') {
                url += `Filter.Type=${filters[filter]}&`;
            } else if (filter === 'tagNumber') {
                url += `Filter.TagNumber=${filters[filter]}&`;
            }
        }
    });

    Object.keys(sorters).forEach((sorter) => {
        if (sorter === 'column') {
            url += `SortInfo.Column=${sorters[sorter] || 'TagNumber'}&`;
        }
        if (sorter === 'descending') {
            url += `SortInfo.Descending=${sorters[sorter]}&`;
        }
        if (sorter === 'page' && sorters[sorter]) {
            url += `Page=${sorters[sorter]}&`;
        }
    });

    return url.slice(0, -1);
};

export const getUrlIdentificationValues = (data: IRequestGetIdentificationValues) => {
    let url = 'groups/identification/values?';
    Object.keys(data).forEach((key) => {
        if (key === 'isActive') {
            url += `Filter.IsActive=${data[key] || false}&`;
        }
        if (key === 'identificationId') {
            url += `IdentificationId=${data[key]}&`;
        }
        if (data[key]) {
            if (key === 'groupId') {
                url += `Filter.GroupId=${data[key]}&`;
            } else if (key === 'type') {
                url += `Filter.Type=${data[key]}&`;
            }
        }
    });
    return url.slice(0, -1);
};

export const getUrlPaginationInfoFilterAnimals = (filters: FiltersAnimalsType) => {
    let url = 'DailyActions/animals/pagination-info?';
    Object.keys(filters).forEach((filter) => {
        if (filter === 'isActive') {
            url += `IsActive=${filters[filter] || false}&`;
        }
        if (filters[filter]) {
            if (filter === 'groupId') {
                url += `GroupId=${filters[filter]}&`;
            } else if (filter === 'type') {
                url += `Type=${filters[filter]}&`;
            }
        }
    });
    return url.slice(0, -1);
};
