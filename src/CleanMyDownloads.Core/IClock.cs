namespace CleanMyDownloads.Core;

/// <summary>Seam over the system clock.</summary>
public interface IClock
{
    DateTime Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}
