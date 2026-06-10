namespace Flit.Identity.Shared.Errors;

public static class ApiErrorCodes
{
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string SessionRevoked = "SESSION_REVOKED";
    public const string Forbidden = "FORBIDDEN";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string RateLimited = "RATE_LIMITED";
    public const string RoleHasUsers = "ROLE_HAS_USERS";
    public const string NotFound = "NOT_FOUND";
    public const string InvitationTokenInvalid = "INVITATION_TOKEN_INVALID";
    public const string ResetTokenInvalid = "RESET_TOKEN_INVALID";
}
