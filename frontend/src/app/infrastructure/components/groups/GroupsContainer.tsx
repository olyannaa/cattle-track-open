import { InfrastructureTypes } from '../../data/enums/infrastructureTypes';
import {
    useDeleteGroupMutation,
    useGetGroupsQuery,
} from '../../services/infrastructure-service';
import { InfrastructureList } from '../infrastructure-list/InfrastructureList';
import { GroupsForm } from './components/group-form/GroupsForm';

export const GroupsContainer = () => {
    const {
        data: groups,
        isFetching,
        isLoading,
    } = useGetGroupsQuery(undefined, {
        refetchOnMountOrArgChange: true,
    });
    const [deleteG] = useDeleteGroupMutation();
    const showAlert: boolean =
        !isFetching && !isLoading && (!groups || groups.length === 0);

    return (
        <>
            <GroupsForm isEmpty={showAlert}></GroupsForm>
            {groups && (
                <InfrastructureList
                    list={groups}
                    partInfrastructure={InfrastructureTypes.group}
                    onDelete={deleteG}
                    title='Группы'
                />
            )}
        </>
    );
};
