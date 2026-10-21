namespace CleanMyDownloads.Sorter;

/// <summary>Seam for showing a native notification, so tests never pop real ones.</summary>
public interface INotifier
{
    void Notify(string title, string message);
}
