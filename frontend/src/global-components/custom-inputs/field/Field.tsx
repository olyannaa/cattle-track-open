import { Flex } from 'antd';
import { InputLabel } from '../input-label/InputLabel';

type Props = {
    label: string;
    value: string;
    styles?: React.CSSProperties;
};

export const FieldCustom = ({ label, styles, value }: Props) => {
    return (
        <div
            style={{ maxWidth: '475px', width: '100%', marginBottom: '24px', ...styles }}
        >
            <InputLabel label={label} required={false} />
            <Flex
                style={{
                    padding: '0 11px',
                    background: '#FFFFFF',
                    border: '1px solid #D9D9D9',
                    height: '40px',
                    fontSize: '16px',
                    color: '#00000040',
                }}
                align='center'
            >
                {value}
            </Flex>
        </div>
    );
};
