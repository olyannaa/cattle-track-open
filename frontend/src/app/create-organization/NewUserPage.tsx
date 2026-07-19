import { Button, Flex, Form } from "antd"
import { useRegistrOrganizationMutation } from "./services/organizationApi"
import { RegistrInput } from "./components/registr-input/RegistrInput";
import { IUser } from "../../utils/userType";
import { useNavigate } from "react-router-dom";
import { useLoginUpdateMutation } from "../../app-service/services/auth";

type FormData = {
    name: string;
    inn?: string;
    ogrn?: string;
}

export const NewUserPage = () => {
    const user: IUser = JSON.parse(localStorage.getItem('user') || '{}');
    const [registrOrganization] = useRegistrOrganizationMutation()
    const [loginUpdate] = useLoginUpdateMutation()
    const navigate = useNavigate();

    const onFinish = async(data: FormData) => {
        try{
            await registrOrganization(data).unwrap()
            const response = await loginUpdate().unwrap()
            const user: IUser = {
                ...response
            }
            localStorage.setItem('user', JSON.stringify(user));
            navigate('/accounting');
        }catch(error){
            console.error('Failed to register organization', error);
        }
    }

    return (
        <Flex vertical style={{maxWidth:'448px', margin:'0 auto', padding: '0 10px'}}>
            {
                user.roleId === '8d5716d0-4cde-45f6-a67f-96323236b0f8' ?
                <>
                    <Flex style={{fontSize:'24px', marginBottom: '20px', fontWeight: '500', textAlign:'center'}}>
                        Введите данные для создания организации
                    </Flex>
                    <Form onFinish={onFinish}>
                        <RegistrInput
                            name='name'
                            label='Название организации'
                            placeholder='Введите название организации'
                        />
                        <RegistrInput name='inn' label='ИНН' placeholder='Введите ИНН' />
                        <RegistrInput name='ogrn' label='ОГРН' placeholder='Введите ОГРН' />
                        <Button
                            type='primary'
                            size='large'
                            color='default'
                            variant='solid'
                            htmlType='submit'
                            style={{width:'100%'}}
                        >
                            Создать организацию
                        </Button>
                    </Form>
                </> : 
                <Flex style={{fontSize:'20px', margin: '20px 0', fontWeight: '500', textAlign:'center'}}>
                    Дождитесь ссылки-приглашения в организацию
                </Flex>
            }
        </Flex>
    )
}
