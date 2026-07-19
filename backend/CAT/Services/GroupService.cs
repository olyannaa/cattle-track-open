using System.Security.Claims;
using System.Text.Json;
using CAT.Controllers.DTO;
using CAT.EF;
using CAT.EF.DAL;
using CAT.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CAT.Services
{
    public class GroupService : IGroupService
    {
        private readonly PostgresContext _db;
        private readonly IUserActionService _userActionService;
        private readonly UserActionQueue _actionQueue;
        private readonly IHttpContextAccessor _hc;

        public GroupService(PostgresContext postgresContext, UserActionQueue actionQueue, IHttpContextAccessor hc)
        {
            _db = postgresContext;
            _actionQueue = actionQueue;
            _hc = hc;
        }

        public List<GroupTypeDTO> GetGroupTypes(Guid organizationId)
        {
            var res = _db.GetGroupTypes(organizationId)
                .Select(x => new GroupTypeDTO { Id = x.Id, Name = x.Name })
                .ToList();
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupTypes))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod,
                recordId: organizationId
            ));
            
            return res;
        }

        public List<GroupInfrasctructureDTO> GetGroupsByOrganization(Guid organizationId)
        {
            var res = _db.Groups
                .Where(g => g.OrganizationId == organizationId)
                .Include(g => g.Type)
                .Select(g => new GroupInfrasctructureDTO
                {
                    Id = g.Id,
                    Name = g.Name,
                    TypeId = g.Type.Id,
                    TypeName = g.Type.Name,
                    Description = g.Description,
                    Location = g.Location
                }).ToList();
            
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                actionType: "search",
                table: "groups",
                recordId: organizationId
            ));
            
            return res;
        }

        public List<IdentificationInfoDTO> GetIdentificationsByOrganization(Guid organizationId)
        {
            var res = _db.GetOrgIdentifications(organizationId)
                .ToList();
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetOrgIdentifications))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod,
                recordId: organizationId
            ));
            
            return res;
        }

        public bool CreateGroup(CreateGroupDTO dto, Guid organizationId)
        {
            try
            {
                var existing = _db.Groups.FirstOrDefault(x => x.OrganizationId == organizationId && x.Name == dto.Name);
                if (existing != null)
                {
                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        actionType: "insert",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.AddGroup)),
                        recordId: existing.Id,
                        oldValues: null,
                        newValues: JsonSerializer.Serialize(dto),
                        status: "error",
                        errorMessage: "Группа с таким именем уже существует"
                    ));
                    return false;
                }

                _db.AddGroup(organizationId, dto.Name, dto.TypeId, dto.Description, dto.Location);

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.AddGroup)),
                    recordId: null,
                    oldValues: null,
                    newValues: JsonSerializer.Serialize(dto),
                    status: "success"
                ));

                return true;
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.AddGroup)),
                    recordId: organizationId,
                    oldValues: null,
                    newValues: JsonSerializer.Serialize(dto),
                    status: "error",
                    errorMessage: ex.Message
                ));
                return false;
            }
        }

        public bool CreateGroupType(CreateGroupTypeDTO dto, Guid organizationId)
        {
            try
            {
                var existing = _db.GroupTypes.FirstOrDefault(x => x.OrganizationId == organizationId && x.Name == dto.Name);
                if (existing != null)
                {
                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        actionType: "insert",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.AddGroupType)),
                        recordId: existing.Id,
                        oldValues: null,
                        newValues: JsonSerializer.Serialize(dto),
                        status: "error",
                        errorMessage: "Тип группы с таким именем уже существует"
                    ));
                    return false;
                }

                _db.AddGroupType(organizationId, dto.Name);

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.AddGroupType)),
                    recordId: null,
                    oldValues: null,
                    newValues: JsonSerializer.Serialize(dto),
                    status: "success"
                ));

                return true;
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "insert",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.AddGroupType)),
                    recordId: null,
                    oldValues: null,
                    newValues: JsonSerializer.Serialize(dto),
                    status: "error",
                    errorMessage: ex.Message
                ));
                return false;
            }
        }

        public bool CreateIdentification(CreateIdentificationDTO dto, Guid organizationId)
        {
            try
            {
                var existing = _db.IdentificationFields.FirstOrDefault(x => x.OrganizationId == organizationId && x.FieldName == dto.Name);
                if (existing != null)
                {
                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        actionType: "insert",
                        dbMethod: null,
                        recordId: existing.Id,
                        oldValues: null,
                        newValues: JsonSerializer.Serialize(dto),
                        status: "error",
                        errorMessage: "Поле идентификации с таким именем уже существует",
                        table:"identification_fields"
                    ));
                    return false;
                }

                _db.AddIdentificationField(dto.Name, organizationId);

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "insert",
                    dbMethod: null,
                    recordId: null,
                    oldValues: null,
                    newValues: JsonSerializer.Serialize(dto),
                    status: "success",
                    table:"identification_fields"
                ));

                return true;
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "insert",
                    dbMethod: null,
                    recordId: null,
                    oldValues: null,
                    newValues: JsonSerializer.Serialize(dto),
                    status: "error",
                    errorMessage: ex.Message,
                    table:"identification_fields"
                ));

                return false;
            }
        }


        public bool DeleteGroupType(Guid typeId, Guid organizationId)
        {
            try
            {
                var groupType = _db.GroupTypes.FirstOrDefault(x => x.Id == typeId && x.OrganizationId == organizationId);
                if (groupType is null)
                {
                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        actionType: "delete",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteGroupType)),
                        recordId: typeId,
                        status: "error",
                        errorMessage: "Тип группы не найден в организации",
                        table: "group_types"
                    ));
                    return false;
                }

                var dependentGroups = _db.Groups.Where(x => x.TypeId == typeId).ToList();
                if (dependentGroups.Count > 0)
                {
                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        actionType: "delete",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteGroupType)),
                        recordId: typeId,
                        oldValues: JsonSerializer.Serialize(dependentGroups),
                        status: "error",
                        errorMessage: "Невозможно удалить: есть связанные группы",
                        table: "group_types"
                    ));
                    return false;
                }

                var oldValuesJson = JsonSerializer.Serialize(new
                {
                    groupType.Id,
                    groupType.Name,
                    groupType.OrganizationId
                });
                _db.DeleteGroupType(typeId);

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "delete",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteGroupType)),
                    recordId: typeId,
                    oldValues: oldValuesJson,
                    status: "success",
                    table: "group_types"
                ));

                return true;
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "delete",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteGroupType)),
                    recordId: typeId,
                    oldValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "group_types"
                ));
                return false;
            }
        }

        public bool DeleteGroup(Guid groupId, Guid organizationId)
        {
            try
            {
                var group = _db.Groups.FirstOrDefault(x => x.Id == groupId && x.OrganizationId == organizationId);
                if (group is null)
                {
                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        actionType: "delete",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteGroup)),
                        recordId: groupId,
                        status: "error",
                        errorMessage: "Группа не найдена в организации",
                        table: "groups"
                    ));
                    return false;
                }

                var dependentAnimals = _db.Animals.Where(x => x.GroupId == groupId).ToList();
                if (dependentAnimals.Count > 0)
                {
                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        actionType: "delete",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteGroup)),
                        recordId: groupId,
                        oldValues: JsonSerializer.Serialize(dependentAnimals),
                        status: "error",
                        errorMessage: "Невозможно удалить: есть связанные животные",
                        table: "groups"
                    ));
                    return false;
                }

                var oldValuesJson = JsonSerializer.Serialize(new
                {
                    group.Id,
                    group.Name,
                    group.OrganizationId,
                    group.TypeId,
                    group.Description,
                    group.Location
                });
                _db.DeleteGroup(groupId);

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "delete",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteGroup)),
                    recordId: groupId,
                    oldValues: oldValuesJson,
                    status: "success",
                    table: "groups"
                ));

                return true;
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "delete",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteGroup)),
                    recordId: groupId,
                    oldValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "groups"
                ));
                return false;
            }
        }

        public bool DeleteIdentification(Guid identificationId, Guid organizationId)
        {
            try
            {
                var identification = _db.IdentificationFields.FirstOrDefault(x => x.Id == identificationId && x.OrganizationId == organizationId);
                if (identification is null)
                {
                   _actionQueue.Enqueue(UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        actionType: "delete",
                        dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteIdentification)),
                        recordId: identificationId,
                        status: "error",
                        errorMessage: "Поле идентификации не найдено в организации",
                        table: "identifications"
                    ));
                    return false;
                }

                var oldValuesJson = JsonSerializer.Serialize(new
                {
                    identification.Id,
                    identification.FieldName,
                    identification.OrganizationId
                });
                _db.DeleteIdentification(identificationId);

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "delete",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteIdentification)),
                    recordId: identificationId,
                    oldValues: oldValuesJson,
                    status: "success",
                    table: "identifications"
                ));
                return true;
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "delete",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.DeleteIdentification)),
                    recordId: identificationId,
                    oldValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "identifications"
                ));
                return false;
            }
        }

        public void EditGroup(EditGroupDTO dto, Guid organizationId)
        {
            try
            {
                var oldValuesJson = JsonSerializer.Serialize(
                    _db.Groups.FirstOrDefault(g => g.Id == dto.Id && g.OrganizationId == organizationId)
                );

                _db.EditGroup(dto.Id, organizationId, dto.Name, dto.TypeId, dto.Description, dto.Location);

               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "update",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.EditGroup)),
                    recordId: dto.Id,
                    oldValues: oldValuesJson,
                    status: "success",
                    table: "groups"
                ));
            }
            catch (Exception ex)
            {
               _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    actionType: "update",
                    dbMethod: typeof(PostgresContext).GetMethod(nameof(PostgresContext.EditGroup)),
                    recordId: dto.Id,
                    oldValues: null,
                    status: "error",
                    errorMessage: ex.Message,
                    table: "groups"
                ));
            }
        }

        public IEnumerable<string?> GetIdentificationValues(Guid identificationId, Guid orgId, IdentificationValuesFilterDTO? filter)
        {
            var res = _db.GetIdentificationValues(identificationId, orgId, filter);
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetIdentificationValues))!;
           _actionQueue.Enqueue(UserActionDtoFactory.Create(
                _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                "search",
                dbMethod,
                recordId: identificationId
            ));
            
            return res;
        }
    }
}
