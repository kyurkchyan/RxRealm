using ShellProgressBar;

namespace RxRealm.CLI;

public class ConsoleProgress : IProgress<int>, IDisposable
{
    private readonly int _total;
    private readonly string _initialMessage;
    private ProgressBar? _progressBar;

    public ConsoleProgress(int total, string initialMessage = "")
    {
        _total = total;
        _initialMessage = initialMessage;
    }


    private void InitializeProgressBar()
    {
        if (_progressBar != null) return;
        var options = new ProgressBarOptions
        {
            ForegroundColor = ConsoleColor.Yellow,
            ForegroundColorDone = ConsoleColor.DarkGreen,
            BackgroundColor = ConsoleColor.DarkGray,
            ProgressCharacter = '\u2593',
            ProgressBarOnBottom = true
        };
        _progressBar = new ProgressBar(_total, _initialMessage, options);
    }

    public void Dispose()
    {
        _progressBar?.Dispose();
    }

    public void Report(int value)
    {
        InitializeProgressBar();
        _progressBar!.Tick(value);
    }
}