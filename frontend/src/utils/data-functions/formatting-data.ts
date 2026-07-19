import { SelectDataType } from '../selectDataType';
import { SelectResponseType } from '../selectResponseType';

export function formatDataForSelectInput(
    data: SelectResponseType[]
): SelectDataType[] {
    return data
        ? data.map((key) => {
              const item = {
                  value: key.id,
                  label: key.name,
              };
              return item;
          })
        : [];
}
