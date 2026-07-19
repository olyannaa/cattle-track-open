/* eslint-disable @typescript-eslint/no-explicit-any */
import { useState, useCallback } from 'react';
import dayjs from 'dayjs';

interface UseAnimalFiltersProps {
  onApplyFilters?: (filters: any) => void;
}

export const useAnimalFilters = ({ onApplyFilters }: UseAnimalFiltersProps = {}) => {
  const [filters, setFilters] = useState({
    tagNumber: '',
    types: [] as string[],
    birthDateFrom: '',
    birthDateTo: '',
    breeds: [] as string[],
    groupNames: [] as string[],
    statuses: [] as string[],
    origins: [] as string[],
    originLocations: [] as string[],
    motherTagNumber: '',
    fatherTagNumber: '',
    identificationSearch: '',
  });

  const [search, setSearch] = useState('');
  const [sortInfo, setSortInfo] = useState({
    active: false,
    column: '',
    descending: false,
  });

  const updateFilters = useCallback((newFilters: Partial<typeof filters>) => {
    setFilters(prev => ({ ...prev, ...newFilters }));
  }, []);

  const resetFilters = useCallback(() => {
    setFilters({
      tagNumber: '',
      types: [],
      birthDateFrom: '',
      birthDateTo: '',
      breeds: [],
      groupNames: [],
      statuses: [],
      origins: [],
      originLocations: [],
      motherTagNumber: '',
      fatherTagNumber: '',
      identificationSearch: '',
    });
    setSearch('');
    setSortInfo({
      active: false,
      column: '',
      descending: false,
    });
  }, []);

  const prepareFiltersForApi = useCallback(() => {
    // Очищаем пустые значения
    const cleanedFilters: any = {};
    
    Object.entries(filters).forEach(([key, value]) => {
      if (Array.isArray(value)) {
        if (value.length > 0) {
          cleanedFilters[key] = value;
        }
      } else if (value !== '' && value != null) {
        cleanedFilters[key] = value;
      }
    });

    if (cleanedFilters.birthDateFrom) {
      cleanedFilters.birthDateFrom = dayjs(cleanedFilters.birthDateFrom).format('YYYY-MM-DD');
    }
    
    if (cleanedFilters.birthDateTo) {
      cleanedFilters.birthDateTo = dayjs(cleanedFilters.birthDateTo).format('YYYY-MM-DD');
    }

    return cleanedFilters;
  }, [filters]);

  const applyFilters = useCallback(() => {
    const apiFilters = prepareFiltersForApi();
    const params = {
      page: 1,
      pageSize: 10,
      search,
      filters: apiFilters,
      sortInfo,
    };

    if (onApplyFilters) {
      onApplyFilters(params);
    }

    return params;
  }, [prepareFiltersForApi, search, sortInfo, onApplyFilters]);

  return {
    filters,
    search,
    sortInfo,
    updateFilters,
    setSearch,
    setSortInfo,
    resetFilters,
    applyFilters,
    prepareFiltersForApi,
  };
};