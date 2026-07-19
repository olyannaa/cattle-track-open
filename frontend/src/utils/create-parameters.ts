import { AnimalFilterParams } from '../app/animal-accounting/data/interfaces/animal-filters-params';

export function getFilterParameters(params: AnimalFilterParams) {
    const queryParams = new URLSearchParams();

    if (params.page) {
        queryParams.append('Page', params.page.toString());
    }

    if (params.filters?.tagNumber) {
        queryParams.append('Filters.TagNumber', params.filters.tagNumber);
    }

    if (params.filters?.types?.length) {
        params.filters.types.forEach((type) => {
            queryParams.append('Filters.Types', type);
        });
    }

    if (params.filters?.birthDateFrom) {
        queryParams.append('Filters.BirthDateFrom', params.filters.birthDateFrom);
    }

    if (params.filters?.birthDateTo) {
        queryParams.append('Filters.BirthDateTo', params.filters.birthDateTo);
    }

    if (params.filters?.breeds?.length) {
        params.filters?.breeds.forEach((breed) => {
            queryParams.append('Filters.Breeds', breed);
        });
    }

    if (params.filters?.groupNames?.length) {
        params.filters?.groupNames.forEach((group) => {
            queryParams.append('Filters.GroupNames', group);
        });
    }

    if (params.filters?.statuses?.length) {
        params.filters.statuses.forEach((status) => {
            queryParams.append('Filters.Statuses', status);
        });
    }

    if (params.filters?.origins?.length) {
        params.filters.origins.forEach((origin) => {
            queryParams.append('Filters.Origins', origin);
        });
    }

    if (params.filters?.originLocations?.length) {
        params.filters.originLocations.forEach((location) => {
            queryParams.append('Filters.OriginLocations', location);
        });
    }

    if (params.filters?.motherTagNumber) {
        queryParams.append('Filters.MotherTagNumber', params.filters.motherTagNumber);
    }

    if (params.filters?.fatherTagNumber) {
        queryParams.append('Filters.FatherTagNumber', params.filters.fatherTagNumber);
    }

    // Сортировка
    if (params.sortInfo?.active !== undefined) {
        queryParams.append('SortInfo.Active', params.sortInfo.active.toString());
    }

    if (params.sortInfo?.column) {
        queryParams.append('SortInfo.Column', params.sortInfo.column);
    }

    if (params.sortInfo?.descending !== undefined) {
        queryParams.append('SortInfo.Descending', params.sortInfo.descending.toString());
    }

    return queryParams.toString();
}
