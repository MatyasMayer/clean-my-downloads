using System.Windows.Forms;

namespace CleanMyDownloads.Sorter;

/// <summary>
/// Native Windows notification through the shell's NotifyIcon balloon (rendered as a toast on Windows 10/11).
/// No window is shown, and no NuGet dependency is needed.
/// </summary>
public sealed class WindowsNotifier : INotifier
{
    public void Notify(string title, string message)
    {
        using var icon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Information,
            Visible = true,
            BalloonTipTitle = title,
            BalloonTipText = message,
            BalloonTipIcon = ToolTipIcon.Info,
        };
        icon.ShowBalloonTip(5000);
        // The shell needs the icon alive briefly for the notification to be handed off.
        Thread.Sleep(3000);
    }
}
