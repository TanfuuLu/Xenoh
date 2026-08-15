using Microsoft.AspNetCore.Authentication;

namespace Xenoh.API.Auth;

internal static class ExternalAuthReturnTargets
{
    internal const string ReturnTargetItemKey = "xenoh:return-target";
    private const string MobileReturnTarget = "mobile";
    private const string MobileCallbackConfigurationKey = "Authentication:MobileCallbackUrl";
    private const string FrontendConfigurationKey = "Authentication:FrontendUrl";
    private const string MobileCallbackScheme = "xenoh";
    private const string MobileCallbackHost = "auth";
    private const string MobileCallbackPath = "/social-callback";
    private const string ExternalLoginFailureCode = "external_login_failed";

    internal static bool TrySetReturnTarget(AuthenticationProperties properties, string? client)
    {
        if (client is null)
            return true;

        if (!string.Equals(client, MobileReturnTarget, StringComparison.OrdinalIgnoreCase))
            return false;

        properties.Items[ReturnTargetItemKey] = MobileReturnTarget;
        return true;
    }

    internal static string BuildSuccessRedirect(
        IConfiguration configuration,
        AuthenticationProperties? properties,
        string ticket) =>
        BuildRedirect(configuration, properties, "auth/social-callback", ("ticket", ticket));

    internal static string BuildFailureRedirect(
        IConfiguration configuration,
        AuthenticationProperties? properties)
    {
        if (IsMobile(properties))
            return BuildRedirect(configuration, properties, "login", ("error", ExternalLoginFailureCode));

        return BuildRedirect(
            configuration,
            properties,
            "login",
            ("externalError", "External login failed."));
    }

    internal static bool IsValidMobileCallback(string? callback)
    {
        if (!Uri.TryCreate(callback, UriKind.Absolute, out var uri))
            return false;

        return string.Equals(uri.Scheme, MobileCallbackScheme, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(uri.Host, MobileCallbackHost, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(uri.AbsolutePath, MobileCallbackPath, StringComparison.Ordinal) &&
               string.IsNullOrEmpty(uri.Query) &&
               string.IsNullOrEmpty(uri.Fragment) &&
               string.IsNullOrEmpty(uri.UserInfo);
    }

    private static string BuildRedirect(
        IConfiguration configuration,
        AuthenticationProperties? properties,
        string webPath,
        params (string Key, string Value)[] query)
    {
        string baseUrl;
        if (IsMobile(properties))
        {
            baseUrl = configuration[MobileCallbackConfigurationKey]
                ?? throw new InvalidOperationException($"Missing {MobileCallbackConfigurationKey} configuration.");
            if (!IsValidMobileCallback(baseUrl))
                throw new InvalidOperationException($"Invalid {MobileCallbackConfigurationKey} configuration.");
        }
        else
        {
            var frontendUrl = configuration[FrontendConfigurationKey] ?? "http://localhost:5173";
            baseUrl = $"{frontendUrl.TrimEnd('/')}/{webPath.TrimStart('/')}";
        }

        var queryString = string.Join(
            "&",
            query.Select(item =>
                $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));
        return query.Length == 0 ? baseUrl : $"{baseUrl}?{queryString}";
    }

    private static bool IsMobile(AuthenticationProperties? properties) =>
        properties?.Items.TryGetValue(ReturnTargetItemKey, out var returnTarget) == true &&
        string.Equals(returnTarget, MobileReturnTarget, StringComparison.Ordinal);
}
