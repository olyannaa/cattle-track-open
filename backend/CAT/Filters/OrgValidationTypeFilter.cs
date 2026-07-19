using CAT.Filters;
using Microsoft.AspNetCore.Mvc;

public class OrgValidationTypeFilter: TypeFilterAttribute
{
    public OrgValidationTypeFilter(bool checkOrgAdmin = default, bool checkLocAdmin = default, bool checkOrg = default)
     : base(typeof(OrgValidationFilter))
    {
        Arguments = new object[] { checkOrgAdmin, checkLocAdmin, checkOrg };
    }
}