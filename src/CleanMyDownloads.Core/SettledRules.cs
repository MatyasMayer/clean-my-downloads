namespace CleanMyDownloads.Core;

/// <summary>How long a file must rest before it moves. Defaults per SPEC section 4.</summary>
public sealed record SettleWait(TimeSpan Normal, TimeSpan Opp)
{
    public static SettleWait Default { get; } = new(TimeSpan.FromHours(3), TimeSpan.FromHours(24));
}

/// <summary>Seam over "is another process using this file".</summary>
public interface IInUseCheck
{
    bool IsInUse(string path);
}

/// <summary>Treats a file as in use when it cannot be opened exclusively.</summary>
public sealed class DiskInUseCheck : IInUseCheck
{
    public bool IsInUse(string path)
    {
        try
        {
            using var _ = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
    }
}

/// <summary>SPEC section 4: a file is settled once enough time has passed since the later of created and modified.</summary>
public static class SettledRules
{
    /// <summary>When the file becomes settled, or null if it already is.</summary>
    public static DateTime? WaitingUntil(FileEntry file, SettleWait wait, DateTime now)
    {
        if (file.Created is null && file.Modified is null) return null;
        var last = new[] { file.Created, file.Modified }.Max()!.Value;
        var needed = Path.GetExtension(file.Name).Equals(".opp", StringComparison.OrdinalIgnoreCase)
            ? wait.Opp : wait.Normal;
        var until = last + needed;
        return now >= until ? null : until;
    }
}
