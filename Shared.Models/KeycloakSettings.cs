public class KeycloakSettings
{
    public const string SectionName = "KeycloakSettings";

    public string BaseDomain { get; set; } = string.Empty;
    public string RealmName { get; set; } = string.Empty;

    public string GetBaseUrl()
    {
        // Use http for localhost, https for everything else
        var protocol = BaseDomain.Contains("localhost") ? "http" : "https";
        return $"{protocol}://{BaseDomain}";
    }

    public string GetRealmUrl()
    {
        return $"{GetBaseUrl()}/realms/{RealmName}";
    }

    public string GetWellKnownConfigUrl()
    {
        return $"{GetRealmUrl()}/.well-known/openid-configuration";
    }
}