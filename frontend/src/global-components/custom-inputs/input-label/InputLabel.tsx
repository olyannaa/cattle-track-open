export const InputLabel = ({
    label,
    required = false,
    marginSize = '8px',
}: {
    label: string;
    required?: boolean;
    marginSize?: string;
}) => {
    return (
        <div
            style={{
                fontSize: '16px',
                fontWeight: '500',
                color: '#333',
                lineHeight: '24px',
                marginBottom: marginSize,
            }}
        >
            {label} {required && <span style={{ color: '#FF4218' }}>*</span>}
        </div>
    );
};
