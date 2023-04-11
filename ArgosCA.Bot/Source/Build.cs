using System.Reflection;

namespace ArgosCA.Bot.Source;

internal static class Build
{
    public static DateTimeOffset? BuildTime { get => _buildTime; }
    private static readonly DateTimeOffset? _buildTime = InitializeBuildTime();

    public static TimeZoneInfo? BuildTimeZone { get => _buildTimeZone; }
    private static readonly TimeZoneInfo? _buildTimeZone = InitializeBuildTimeZone();

    public static string BuildTimestamp { get => _buildTimestamp; }
    private static readonly string _buildTimestamp = InitializeBuildTimestamp();

    private static DateTimeOffset? InitializeBuildTime()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream("ArgosCA.Bot.Source.BuildTimeResource.txt");
        if (stream == null)
        {
            return null;
        }
        using StreamReader reader = new(stream);
        string[] lines = reader.ReadToEnd().Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0 || !DateTimeOffset.TryParse(lines[0], out DateTimeOffset buildTime))
        {
            return null;
        }
        return buildTime;
    }

    private static TimeZoneInfo? InitializeBuildTimeZone()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream("ArgosCA.Bot.Source.BuildTimeResource.txt");
        if (stream == null)
        {
            return null;
        }
        using StreamReader reader = new(stream);
        string[] lines = reader.ReadToEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return null;
        }
        try
        {
            TimeZoneInfo buildTimeZone = TimeZoneInfo.FindSystemTimeZoneById(lines[1]);
            return buildTimeZone;
        }
        catch
        {
            return null;
        }
    }

    private static string InitializeBuildTimestamp()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream("ArgosCA.Bot.Source.BuildTimeResource.txt");
        if (stream == null)
        {
            return "Timestamp not set.";
        }
        using StreamReader reader = new(stream);
        string[] lines = reader.ReadToEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0 || !DateTimeOffset.TryParse(lines[0], out DateTimeOffset _))
        {
            return "Timestamp not set.";
        }
        return lines[0];
    }
}
