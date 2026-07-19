import { SelectResponseType } from '../app/register-animal/services/registration-animal';

export type SelectDataType = {
    value: string;
    label: string;
};

export function formatDataForSelectInput(data: SelectResponseType[]): SelectDataType[] {
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
