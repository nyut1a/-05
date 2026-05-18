using AptekaIS.Models;

namespace AptekaIS.Services;

public static class Session
{
    public static User? CurrentUser { get; set; }

    public static bool IsAdmin => CurrentUser?.RoleName == "admin";
    public static bool CanEdit => CurrentUser?.RoleName is "admin" or "operator";
}
