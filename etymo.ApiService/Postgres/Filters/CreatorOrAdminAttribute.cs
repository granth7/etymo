using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace etymo.ApiService.Postgres.Filters
{
    public class CreatorOrAdminAttribute : TypeFilterAttribute
    {
        public CreatorOrAdminAttribute() : base(typeof(CreatorOrAdminActionFilter))
        {
        }
    }
}
