import { Button, Flex, Tabs, TabsProps } from 'antd';
import styles from './HeaderContent.module.css';

type Props = {
    title: string;
    items: TabsProps['items'];
    buttons?: {
        text: string;
        buttonClick: () => void;
    }[];
    onChange?: (activeKey: string) => void;
    activeKey?: string;
    handlerSaveAnimals?: () => void;
};

export const HeaderContent = ({
    title,
    items,
    buttons = [],
    onChange,
    activeKey,
    handlerSaveAnimals,
}: Props) => {
    return (
        <Flex vertical className={styles['content-header']}>
            <Flex className={styles['title']} justify='space-between'>
                <h1 className={styles['title__text']}>{title}</h1>
                <Flex gap={12} className={styles['title__buttons']}>
                    {buttons.length
                        ? buttons.map((button, i) => (
                              <Button
                                  onClick={
                                      button.text === 'Сохранить таблицу'
                                          ? handlerSaveAnimals
                                          : button.buttonClick
                                  }
                                  type={i === 0 ? 'text' : 'primary'}
                                  className={
                                      styles[
                                          `title__button_${i === 0 ? 'first' : 'second'}`
                                      ]
                                  }
                                  size='large'
                                  key={i}
                              >
                                  {button.text}
                              </Button>
                          ))
                        : ''}
                </Flex>
            </Flex>
            <Tabs
                items={items}
                onChange={onChange}
                className={styles['tabs']}
                activeKey={activeKey}
            />
        </Flex>
    );
};
