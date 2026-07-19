import { DynamicObject } from '../../../../utils/dynamicType';

export interface AnimalForm {
    /** Номер бирки */
    TagNumber: string;
    BirthDate: Date;
    /** Тип животного */
    Type: string;
    /** Порода */
    Breed?: string;
    /** Статус */
    Status?: string;
    /** Айди матери */
    MotherTag?: string;
    OrganizationId?: string;
    /** Айди отца */
    FatherTag?: string;
    /** Группа содержания */
    GroupId?: string;
    /** Происхождение */
    Origin?: string;
    /** Локация */
    OriginLocation?: string;
    Photo?: Blob;
    /** Дата осеменения */
    InseminationDate?: string;
    /** Ожидаемая дата отела */
    ExpectedCalvingDate?: Date;
    /** Партия спермы */
    SpermBatch?: string;
    /** Тип осменения */
    InseminationType?: string;
    /** Техник осеменения */
    Technician?: string;
    /** Примечания */
    Notes?: string;
    /** Дополнительные поля */
    AdditionalFields?: DynamicObject;
}
