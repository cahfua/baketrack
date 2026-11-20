namespace BakeTrack.Models;

/// <summary>
/// Application authorization roles for BakeTrack.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";

    public static readonly string[] All = [Admin, Staff];
}
