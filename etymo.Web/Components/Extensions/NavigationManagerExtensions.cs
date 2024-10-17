using Microsoft.AspNetCore.Components;

namespace etymo.Web.Components.Extensions
{
    public static class NavigationManagerExtensions
    {
        public static void RemoveQueryParameters(this NavigationManager navManager)
        {
            var uri = new Uri(navManager.Uri);
            var newUri = uri.GetLeftPart(UriPartial.Path); // Removes query parameters
            navManager.NavigateTo(newUri, forceLoad: false);
        }


        public static void RemoveFragment(this NavigationManager navManager)
        {
            var uri = new Uri(navManager.Uri);
            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                var newUri = uri.GetLeftPart(UriPartial.Path); // Remove fragment
                navManager.NavigateTo(newUri, forceLoad: false);
            }
        }
    }
}