using Microsoft.AspNetCore.Mvc;

namespace etymo.ApiService.Postgres.Filters
{
    public class ValidateCustomAntiForgeryTokenAttribute : TypeFilterAttribute
    {
        public ValidateCustomAntiForgeryTokenAttribute() : base(typeof(ValidateCustomAntiForgeryTokenFilter))
        {
        }
    }
}
