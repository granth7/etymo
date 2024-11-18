using Microsoft.AspNetCore.Components;

namespace etymo.Web.Components.Extensions
{
    public static class NavigationManagerExtensions
    {
        public static string GetQueryParameters(this NavigationManager navManager)
        {
            var uri = new Uri(navManager.Uri);
            var gameType = "";

            // Extract the query component
            string queryString = uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped);

            // Parse the query string into a dictionary
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString);

            // Read the parameters
            if (queryParams.TryGetValue("gameType", out var gameTypeValues))
            {
                gameType = gameTypeValues.First();
            }

            if (gameType != null)
            {
                return gameType;
            }
            else return "";
        }

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