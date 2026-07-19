import { useMemo } from 'react';
import Fuse, { FuseOptionKey, IFuseOptions } from 'fuse.js';

type UseFuseSearchOptions<T> = Omit<IFuseOptions<T>, 'keys'> & {
  keys: FuseOptionKey<T>[];
  minSearchLength?: number; // от какой длины искать (по умолчанию 2)
};

export function useFuseSearch<T>(
  data: T[],
  searchText: string,
  {
    keys,
    minSearchLength = 2,
    ...fuseOptions
  }: UseFuseSearchOptions<T>,
): T[] {
  const fuse = useMemo(() => {
    if (!data.length) return null;

    return new Fuse<T>(data, {
      keys,
      threshold: 0.3,
      ignoreLocation: true,
      minMatchCharLength: 2,
      ...fuseOptions,
    });
  }, [data, keys, fuseOptions]);

  const filtered = useMemo(() => {
    const text = searchText.trim();

    if (!text || text.length < minSearchLength || !fuse) {
      return data;
    }

    return fuse.search(text).map(r => r.item);
  }, [searchText, fuse, data, minSearchLength]);

  return filtered;
}