using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CAT.Filters
{
    public class OrgValidationFilter : Attribute, IActionFilter
    {
        private readonly Guid _localAdminId;
        private readonly Guid _orgAdminId;
        private readonly bool _checkOrg;
        private readonly bool _checkLocAdmin;
        private readonly bool _checkOrgAdmin;

        public OrgValidationFilter(IConfiguration config, bool checkOrgAdmin = default, bool checkLocAdmin = default, bool checkOrg = default)
        {
            _checkOrgAdmin = checkOrgAdmin;
            _checkLocAdmin = checkLocAdmin;
            _checkOrg = checkOrg;
            _localAdminId = config.GetValue<Guid>("Enviroment:LocalAdminId");
            _orgAdminId = config.GetValue<Guid>("Enviroment:OrgAdminId");
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (_checkOrg)
            {
                var responseOrgId = context.HttpContext.Request.Headers["OrganizationId"].ToString();
                if (responseOrgId is null || responseOrgId == String.Empty)
                {
                    context.Result = GetOrgIdHeaderResult();
                    return;
                }

                var authOrgId = GetUserClaims(context).Find(x => x.Type == "Organization")?.Value;
                if (responseOrgId != authOrgId)
                {
                    context.Result = GetOrgIdNotEqualsResult();
                    return;
                }
            }

            if (_checkLocAdmin)
            {
                if (_localAdminId == Guid.Empty)
                {
                    context.Result = GetNotImplementedLocalResult();
                    return;
                }
                var userRoleId = GetUserClaims(context).Find(x => x.Type == ClaimTypes.Role)?.Value;
                if (!Guid.TryParse(userRoleId, out var roleId) || (roleId != _localAdminId && roleId != _orgAdminId))
                {
                    context.Result = GetNotAdminResult();
                    return;
                }
            }

            if (_checkOrgAdmin)
            {
                if (_orgAdminId == Guid.Empty)
                {
                    context.Result = GetNotImplementedOrgResult();
                    return;
                }
                var userRoleId = GetUserClaims(context).Find(x => x.Type == ClaimTypes.Role)?.Value;
                if (!Guid.TryParse(userRoleId, out var roleId) || roleId != _orgAdminId)
                {
                    context.Result = GetNotAdminResult();
                    return;
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }

        private List<Claim> GetUserClaims(ActionExecutingContext context)
        {
            return context.HttpContext.User.Claims.ToList();
        }

        private ContentResult GetOrgIdHeaderResult()
        {
            return new ContentResult
            {
                StatusCode = 403,
                Content = "Для выполнения запроса необходимо указать id организации в заголовке"
            };
        }

        private ContentResult GetOrgIdNotEqualsResult()
        {
            return new ContentResult
            {
                StatusCode = 403,
                Content = "Нет доступа к информации чужой организации"
            };
        }

        private ContentResult GetNotAdminResult()
        {
            return new ContentResult
            {
                StatusCode = 403,
                Content = "Не достаточно прав внутри организации"
            };
        }

        private ContentResult GetNotImplementedLocalResult()
        {
            return new ContentResult
            {
                StatusCode = 500,
                Content = "ID локального администратора не указан в конфигурационном файле"
            };
        }
        
        private ContentResult GetNotImplementedOrgResult()
        {
            return new ContentResult{ 
                StatusCode=500,
                Content="ID администратора организатора не указан в конфигурационном файле"
            };
        }
    }
}
