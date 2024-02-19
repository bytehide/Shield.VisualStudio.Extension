using System;
using System.Security.Cryptography;
using System.Text;
using VSExtension;

namespace ShieldVSExtension.Common.Helpers;

internal static class Utils
{
    public static Version GetVersionNumber()
    {
        // var assembly = Assembly.GetExecutingAssembly();
        // var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        var versions = Vsix.Version.Split('.');

        if (versions.Length < 3) return new Version();

        Version version = new()
        {
            Major = versions[0],
            Minor = versions[1],
            Patch = versions[2],
            Build = versions.Length > 3 ? versions[3] : "0"
        };

        return version;
    }


    public static string Truncate(this string value, int maxLength, string tail = "...") =>
        value.Length <= maxLength ? value : $"{value.Substring(0, maxLength)}{tail}";

    public static void GoToWebsite(string path) => System.Diagnostics.Process.Start(path);

    public static Guid Uuid(this string source)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(source);
        var hashBytes = md5.ComputeHash(inputBytes);
        var uuid = new Guid(hashBytes);

        return uuid;
    }
}