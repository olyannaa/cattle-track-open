using System.Security.Claims;
using CAT.Controllers.DTO;
using CAT.Controllers.DTO.Feeding;
using CAT.EF;
using CAT.EF.DAL;
using CAT.Services.Interfaces;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CAT.Services
{
    public class FeedingService : IFeedingService
    {
        private readonly PostgresContext _db;
        private readonly IHttpContextAccessor _hc;
        private readonly UserActionQueue _actionQueue;

        public FeedingService(PostgresContext postgresContext,  IHttpContextAccessor hc, UserActionQueue actionQueue)
        {
            _db = postgresContext;
            _hc = hc;
            _actionQueue = actionQueue;
        }

        public async Task<Guid> CreateComponent(CreateComponentDTO component)
        {
            var componentDAL = _db.Components.FirstOrDefault(x => x.Name == component.Name);
            
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "insert",
                    newValues: component,
                    table: "components"
                ));
            
            if (componentDAL != null) return Guid.Empty;
            return _db.CreateComponent(component);
        }
            

        public async Task<(Guid? RationId, string Error)> CreateRation(CreateRationRequestDTO ration)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var rationId = _db.CreateRationWithComponents(ration); 

                if (ration.GroupId != null)
                {
                    try
                    {
                        _db.CreateRationToGroup((Guid)ration.GroupId, rationId, "Ручной");
                    }
                    catch (PostgresException ex) when (ex.SqlState == "P0001") 
                    {
                        transaction.Rollback();
                        return (null, "Рацион не может быть добавлен, у группы уже есть рацион");
                    }
                }
                
                var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.CreateRationToGroup))!;
                _actionQueue.Enqueue(
                    UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        "insert",
                        dbMethod: dbMethod,
                        newValues: ration,
                        table: "rations"
                    ));
                
                transaction.Commit();
                return (rationId, null);
            }
            catch (Exception ex)
            {
                _actionQueue.Enqueue(
                    UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        "insert",
                        newValues: ration,
                        status: "error",
                        errorMessage: ex.Message,
                        table: "rations"
                    ));
                
                transaction.Rollback();
                throw;
            }
        }



        public async Task<(bool Success, string ErrorText)> DeleteComponent(Guid componentId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.CreateRationToGroup))!;
            try
            {
                var oldVal = _db.GetComponentsById(componentId);
                
                _db.DeleteComponent(componentId);
                
                _actionQueue.Enqueue(
                    UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        "delete",
                        dbMethod: dbMethod,
                        oldValues: oldVal
                    ));
                
                return (true, null);
            }
            catch (PostgresException ex) when (ex.SqlState == "P0001")
            {
                _actionQueue.Enqueue(
                    UserActionDtoFactory.Create(
                        _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                        "delete",
                        dbMethod: dbMethod,
                        status: "error",
                        errorMessage: "Компонент с таким Id не найден" 
                    ));
                
                return (false, "Компонент с таким Id не найден");
            }
        }


        public async Task<List<ComponentDTO>> GetComponents(Guid organizationId)
        {
            var components = _db.GetComponentsByOrganization(organizationId);
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetComponentsByOrganization))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod
                ));
            
            foreach(var component in components)
                component.InRation = _db.RationComponents.Any(x => x.ComponentId == component.Id);
            return components;
        }

        public async Task UpdateComponent(UpdateComponentDTO component)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.UpdateComponent))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "insert",
                    dbMethod: dbMethod,
                    newValues: component
                ));
            
            _db.UpdateComponent(component);
        }

        public async Task<List<GroupWithStatsDTO>> GetGroupWithStats(Guid organizationId)
        {
            var groups = _db.GetGroupsWithStats(organizationId);
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupsWithStats))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod
                ));
            
            return groups;
        }

        public async Task<List<GroupWithRationDTO>> GetGroupWithRations(Guid organizationId)
        {
            var groups = _db.GetGroupsWithRations(organizationId);
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupsWithRations))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod
                ));
            
            return groups;
        }

        public async Task<RationSummaryDTO> GetRationSummaryEnhanced(Guid rationId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetRationSummaryEnhanced))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod
                ));
            
            return _db.GetRationSummaryEnhanced(rationId);
        }

        public async Task<List<RationGroupedDTO>> GetRationWithComponents(Guid organizationId)
        {
            var rations = _db.GetRationsGroupedWithComponentsByOrganization(organizationId);
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetRationsGroupedWithComponentsByOrganization))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod
                ));
            
            foreach(var ration in rations)
            {
                var groupIds = _db.GroupRations.Where(x => x.RationId == ration.RationId)?.Select(x => x.GroupId);
                if (groupIds == null) continue;
                var groupNames = _db.Groups.Where(x => groupIds.Contains(x.Id)).Select(x => x.Name).ToArray();
                ration.GroupNames = groupNames;
            }
            return rations;
        }
            


        public async Task UpdateRationFull(
            Guid rationId, Guid? organizationId,
            UpdateRationRequestDTO dto)
        {
            var ration = _db.Rations.FirstOrDefault(x => x.Id == rationId);
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.UpdateRationFull))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "update",
                    dbMethod: dbMethod,
                    newValues: ration
                ));
            
            _db.UpdateRationFull(rationId, ration.Name, ration.Description, organizationId, dto.Components);
        }

        public async Task CreateRationToGroup(Guid groupId, Guid rationId, string rationType)
        {
            var ration = new
            {
                groupId = groupId,
                rationId = rationId,
                rationType = rationType
            };
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.CreateRationToGroup))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "update",
                    dbMethod: dbMethod,
                    newValues: ration
                ));
            
            _db.CreateRationToGroup(groupId, rationId, rationType);
        }

        public Task<Guid> AssignRationToGroup(AssignRationToGroupDTO dto)
        {
            var id = _db.AssignRationToGroup(
                dto.OrganizationId,
                dto.GroupId,
                dto.RationId,
                dto.MorningFeeding,
                dto.DayFeeding,
                dto.NightFeeding
            );

            return Task.FromResult(id);
        }

        public async Task<List<Ration>> GetRations(Guid organizationId)
        {
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    table: "rations"
                ));
            
            return await _db.Rations.Where(x => x.OrganizationId == organizationId).ToListAsync();
        }

        public async Task<List<GroupedFeedingByRationDTO>> GetFeedingDailyStats(Guid organizationId)
        {
            var records = _db.GetFeedingDailyRecords(organizationId);
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetFeedingDailyRecords))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod
                ));
            
            var grouped = records
                .SelectMany(r => r.FeedingDetails.Select(fd => new
                {
                    r.GroupName,
                    r.GroupId,
                    r.DailyFactKg,
                    r.EventDate,
                    fd.RationId,
                    fd.RationName,
                    FeedingTime = fd.FeedingTime,
                    FactKg = fd.FactKg
                }))
                .GroupBy(x => new { x.RationName, x.GroupName, x.GroupId })
                .Select(group => new GroupedFeedingByRationDTO
                {
                    RationName = group.Key.RationName,
                    GroupName = group.Key.GroupName,
                    GroupId = group.Key.GroupId,
                    DailyFactKg = group.Sum(x => x.FactKg),
                    Events = group
                        .GroupBy(e => e.EventDate)
                        .Select(ev => new FeedingEventDTO
                        {
                            EventDate = ev.Key,
                            TotalFactKg = ev.Sum(x => x.FactKg),
                            FeedingTimes = ev
                                .Where(x => !string.IsNullOrWhiteSpace(x.FeedingTime))
                                .Select(x => x.FeedingTime)
                                .Distinct()
                                .ToList()
                        })
                        .OrderBy(e => e.EventDate)
                        .ToList()
                })
                .ToList();

            return await Task.FromResult(grouped);
        }

        public async Task<List<GroupedFeedingRecordDTO>> GetGroupRationStats(Guid organizationId, Guid groupId)
        {
            var flatList = _db.GetGroupFeedingStats(organizationId, groupId);
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupFeedingStats))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod
                ));
            
            var grouped = flatList
                .GroupBy(x => new { x.GroupName, x.RationName })
                .Select(g => new GroupedFeedingRecordDTO
                {
                    GroupName = g.Key.GroupName,
                    GroupRationName = g.Key.RationName,
                    Records = g.Select(x => new DailyFeedingStatDTO
                    {
                        EventDate = x.EventDate,
                        DailyFactKg = x.DailyFactKg,
                        RationName = g.Key.RationName
                    })
                    .OrderBy(x => x.EventDate)
                    .ToList()
                })
                .ToList();

            return await Task.FromResult(grouped);
        }

        public async Task<List<GroupFeedingRecordCostDTO>> GetGroupRationStatsCost(Guid organizationId, Guid groupId)
        {
            var flatList = _db.GetGroupFeedingStatsCost(organizationId, groupId);

            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupFeedingStatsCost))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod
                ));
            
            var grouped = flatList
                .GroupBy(x => new { x.GroupName, x.GroupRationName })
                .Select(g => new GroupFeedingRecordCostDTO
                {
                    GroupName = g.Key.GroupName,
                    GroupRationName = g.Key.GroupRationName,
                    Records = g.Select(x => new DailyFeedingCostStatDTO
                    {
                        EventDate = x.EventDate,
                        RationCost = x.RationCost,
                        TotalRationCost = x.TotalRationCost,
                        RationName = x.GroupRationName
                    })
                    .OrderBy(x => x.EventDate)
                    .ToList()
                })
                .ToList();

            return await Task.FromResult(grouped);
        }

        public async Task<List<GroupFeedingRecordYearlyCostDTO>> GetGroupRationStatsCostYearly(Guid organizationId, Guid groupId)
        {
            var flatList = _db.GetGroupFeedingStatsCostYearly(organizationId, groupId);
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupFeedingStatsCostYearly))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod
                ));
            
            var grouped = flatList
                .GroupBy(x => new { x.GroupName, x.GroupRationName })
                .Select(g => new GroupFeedingRecordYearlyCostDTO
                {
                    GroupName = g.Key.GroupName,
                    GroupRationName = g.Key.GroupRationName,
                    Records = g.GroupBy(x => x.MonthYear)
                        .Select(m => new MonthlyFeedingCostStatDTO
                        {
                            MonthYear = m.Key,
                            RationCost = m.First().RationCost,
                            TotalRationCost = m.Sum(x => x.TotalRationCost),
                            RationName = g.Key.GroupRationName
                        })
                        .OrderBy(x => x.MonthYear)
                        .ToList()
                })
                .ToList();

            return await Task.FromResult(grouped);
        }

        public async Task<List<GroupFeedingNutritionDTO>> GetGroupRationNutritionStats(Guid organizationId, Guid groupId)
        {
            var flatList = _db.GetGroupFeedingStatsNutrition(organizationId, groupId);
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupFeedingStatsNutrition))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod
                ));
            
            var grouped = flatList
                .GroupBy(x => new { x.GroupName, x.GroupRationName })
                .Select(g => new GroupFeedingNutritionDTO
                {
                    GroupName = g.Key.GroupName,
                    GroupRationName = g.Key.GroupRationName,
                    Records = g.Select(x => new DailyFeedingNutritionDTO
                    {
                        EventDate = x.EventDate,
                        TotalSv = x.TotalSv,
                        TotalSp = x.TotalSp,
                        TotalCep = x.TotalCep,
                        TotalNdk = x.TotalNdk,
                        RationName = x.GroupRationName
                    }).OrderBy(x => x.EventDate).ToList()
                })
                .ToList();

            return await Task.FromResult(grouped);
        }

        public List<GroupFeedingStatsDTO> GetGroupFeedingStats(Guid organizationId)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetOrganizationGroupStats))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod
                ));
            
            return _db.GetOrganizationGroupStats(organizationId);
        }

        public List<GroupFeedingDailyDTO> GetGroupFeedingDailyStats(Guid organizationId, DateOnly date)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.GetGroupFeedingDailyStats))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "search",
                    dbMethod: dbMethod
                ));

            return _db.GetGroupFeedingDailyStats(organizationId, date);
        }

        public Guid RecordFeeding(RecordFeedingDTO dto)
        {
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.RecordFeeding))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "insert",
                    dbMethod: dbMethod,
                    newValues: dto
                ));
            
            return _db.RecordFeeding(dto);
        }
        
        public async Task RunDailyFeedingRecordFill(Guid organizationId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
            var dbMethod = typeof(PostgresContext).GetMethod(nameof(PostgresContext.FillDailyFeedingRecords))!;
            _actionQueue.Enqueue(
                UserActionDtoFactory.Create(
                    _hc.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "insert",
                    dbMethod: dbMethod
                ));
            
            await  _db.FillDailyFeedingRecords(organizationId, today);
        }
    }
}
