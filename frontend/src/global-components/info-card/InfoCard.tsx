import styles from './InfoCard.module.css';

export type InfoCardProps = {
    title: string;
    description: string;
};

export const InfoCard = (data: InfoCardProps) => {
    return (
        <div className={styles['main__stat-item']}>
            <p className={styles['main__card-title__default']}>{data.title}</p>
            <p className={`${styles['main__card-number']} ${styles['main__card-number__default']}`}>
                {data.description}
            </p>
        </div>
    );
};
