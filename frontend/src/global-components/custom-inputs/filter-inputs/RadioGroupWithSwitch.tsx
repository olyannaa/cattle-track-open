import { Flex, Radio, RadioChangeEvent, Switch } from 'antd';
import { IItemRadioGroupWithSwitch } from '../../data/interface/InputsFilter';
import { InputLabel } from '../input-label/InputLabel';

export const RadioGroupWithSwitch = ({
    label,
    options,
    styles,
    isGroup,
    onChange,
    onChangeAll,
    value,
}: IItemRadioGroupWithSwitch) => {
    const handlerSwitchChange = (checked: boolean) => {
        if (checked && onChangeAll) {
            onChangeAll();
        }
    };

    const handlerRadioChange = (e: RadioChangeEvent) => {
        onChange?.(e.target.value);
    };

    return (
        <div
            style={{ position: 'relative', width: '100%', maxWidth: '491px', ...styles }}
        >
            <InputLabel label={label} />
            <div style={{ maxWidth: '391px' }}>
                <Radio.Group
                    style={{
                        display: 'flex',
                        flexWrap: 'wrap',
                        ...styles,
                    }}
                    options={options}
                    optionType='button'
                    onChange={handlerRadioChange}
                    value={value}
                />
            </div>
            {isGroup && onChangeAll && (
                <Flex
                    style={{ position: 'absolute', top: '-5px', right: '0' }}
                    align='center'
                    gap={8}
                >
                    <div style={{ fontWeight: '500' }}>Выбрать всех</div>
                    <div style={{ marginBottom: '0' }}>
                        <Switch onChange={handlerSwitchChange} checked={!value} />
                    </div>
                </Flex>
            )}
        </div>
    );
};
