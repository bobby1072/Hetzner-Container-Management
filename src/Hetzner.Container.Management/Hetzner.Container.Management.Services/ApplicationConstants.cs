namespace Hetzner.Container.Management.Services;

public static class ApplicationConstants
{
    public static class ExceptionConstants
    {
        public const string InternalError = "An internal sever error occured during request.";
    }
    
    public static class ServiceKeys
    {
        public const string ApiKeyServiceKey = "ApiKey";
    }

    public static class CustomHeaders
    {
        public const string ApiKeyHeaderName = "x-api-key";
    }
}