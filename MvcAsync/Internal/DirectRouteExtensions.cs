using System.Web.Routing;

namespace System.Web.Mvc.Routing
{
    internal static class DirectRouteExtensions
    {
        public static bool HasDirectRouteMatch(this RouteData routeData)
        {
            if (routeData == null)
            {
                throw new ArgumentNullException(nameof(routeData));
            }
            return routeData.Values.ContainsKey(RouteDataTokenKeys.DirectRouteMatches);
        }
    }
}
