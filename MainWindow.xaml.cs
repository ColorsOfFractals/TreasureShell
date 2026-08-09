using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Documents;

namespace TreasureShell
{
    public partial class MainWindow : Window
    {
        // =========================================================
        // RESIZE HIT TESTING
        // =========================================================

        private const int WM_NCHITTEST = 0x0084;

        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        private const double ResizeBorder = 8;

        // =========================================================
        // STATE
        // =========================================================

        private bool _opened = false;

        private Process? _powerShell;

        private double _restoreLeft;
        private double _restoreTop;
        private double _restoreWidth;
        private double _restoreHeight;

        private bool _hasRestoreBounds = false;

        private readonly string _homeDirectory =
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);

        // =========================================================
        // WEB
        // =========================================================

        private readonly HttpClient _httpClient =
            new HttpClient();

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public MainWindow()
        {
            InitializeComponent();

            _httpClient
                .DefaultRequestHeaders
                .UserAgent
                .ParseAdd(
                    "TreasureShell/1.0");

            SourceInitialized +=
                MainWindow_SourceInitialized;

            Closed +=
                MainWindow_Closed;
        }

        // =========================================================
        // NATIVE RESIZE HOOK
        // =========================================================

        private void MainWindow_SourceInitialized(
            object? sender,
            EventArgs e)
        {
            IntPtr hwnd =
                new WindowInteropHelper(this).Handle;

            HwndSource? source =
                HwndSource.FromHwnd(hwnd);

            source?.AddHook(WindowProc);
        }

        private IntPtr WindowProc(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (msg != WM_NCHITTEST)
                return IntPtr.Zero;

            int screenX =
                unchecked(
                    (short)(long)lParam);

            int screenY =
                unchecked(
                    (short)((long)lParam >> 16));

            System.Windows.Point mousePoint =
                PointFromScreen(
                    new System.Windows.Point(
                        screenX,
                        screenY));

            bool left =
                mousePoint.X <=
                ResizeBorder;

            bool right =
                mousePoint.X >=
                ActualWidth - ResizeBorder;

            bool top =
                mousePoint.Y <=
                ResizeBorder;

            bool bottom =
                mousePoint.Y >=
                ActualHeight - ResizeBorder;

            if (left && top)
            {
                handled = true;

                return new IntPtr(
                    HTTOPLEFT);
            }

            if (right && top)
            {
                handled = true;

                return new IntPtr(
                    HTTOPRIGHT);
            }

            if (left && bottom)
            {
                handled = true;

                return new IntPtr(
                    HTBOTTOMLEFT);
            }

            if (right && bottom)
            {
                handled = true;

                return new IntPtr(
                    HTBOTTOMRIGHT);
            }

            if (left)
            {
                handled = true;

                return new IntPtr(
                    HTLEFT);
            }

            if (right)
            {
                handled = true;

                return new IntPtr(
                    HTRIGHT);
            }

            if (top)
            {
                handled = true;

                return new IntPtr(
                    HTTOP);
            }

            if (bottom)
            {
                handled = true;

                return new IntPtr(
                    HTBOTTOM);
            }

            return IntPtr.Zero;
        }

        // =========================================================
        // CHEST
        // =========================================================

        private async void Chest_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (_opened)
                return;

            _opened = true;

            HintText.Opacity = 0;

            OpenChest();

            await Task.Delay(1750);

            StartPowerShell();

            CommandInput.Focus();
        }

        // =========================================================
        // POWERSHELL
        // =========================================================

        private void StartPowerShell()
        {
            if (_powerShell is
                { HasExited: false })
            {
                return;
            }

            var psi =
                new ProcessStartInfo
                {
                    FileName =
                        "powershell.exe",

                    Arguments =
                        "-NoLogo -NoProfile -ExecutionPolicy Bypass",

                    WorkingDirectory =
                        _homeDirectory,

                    UseShellExecute =
                        false,

                    RedirectStandardInput =
                        true,

                    RedirectStandardOutput =
                        true,

                    RedirectStandardError =
                        true,

                    CreateNoWindow =
                        true,

                    StandardOutputEncoding =
                        Encoding.UTF8,

                    StandardErrorEncoding =
                        Encoding.UTF8
                };

            _powerShell =
                new Process
                {
                    StartInfo = psi,
                    EnableRaisingEvents = true
                };

            _powerShell.OutputDataReceived +=
                PowerShell_OutputDataReceived;

            _powerShell.ErrorDataReceived +=
                PowerShell_ErrorDataReceived;

            _powerShell.Exited +=
                (_, _) =>
                {
                    AppendOutput("");

                    AppendOutput(
                        "[PowerShell process exited]");
                };

            try
            {
                _powerShell.Start();

                _powerShell
                    .BeginOutputReadLine();

                _powerShell
                    .BeginErrorReadLine();

                ShowWelcomeBanner();

                UpdatePrompt(
                    _homeDirectory);
            }
            catch (Exception ex)
            {
                AppendOutput(
                    "Failed to start PowerShell:");

                AppendOutput(
                    ex.Message);
            }
        }

        // =========================================================
        // WELCOME BANNER
        // =========================================================

        private void ShowWelcomeBanner()
        {
            AppendOutput(
                "Current location:");

            AppendOutput(
                _homeDirectory);

            AppendOutput("");
        }

        // =========================================================
        // POWERSHELL OUTPUT
        // =========================================================

        private void PowerShell_OutputDataReceived(
            object sender,
            DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                AppendOutput(
                    e.Data);
            }
        }

        private void PowerShell_ErrorDataReceived(
            object sender,
            DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                AppendOutput(
                    e.Data);
            }
        }

        // =========================================================
        // INPUT
        // =========================================================

        private async void CommandInput_KeyDown(
            object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            // TAB COMPLETION

            if (e.Key == Key.Tab)
            {
                e.Handled = true;

                CompletePath();

                return;
            }

            if (e.Key != Key.Enter)
                return;

            e.Handled = true;

            string command =
                CommandInput.Text.Trim();

            CommandInput.Clear();

            if (string.IsNullOrWhiteSpace(
                    command))
            {
                return;
            }

            // =====================================================
            // TREASURESHELL NATIVE COMMANDS
            // =====================================================

            // WEB OFF
            //
            // Return to normal PowerShell terminal view.

            if (command.Equals(
                    "web off",
                    StringComparison.OrdinalIgnoreCase))
            {
                AppendOutput(
                    $"{PromptText.Text}{command}");

                ShowTerminalView();

                CommandInput.Focus();

                return;
            }

            // WEB <URL>
            //
            // TreasureShell handles this command itself.
            // It is NOT sent to powershell.exe.

            if (command.StartsWith(
                    "web ",
                    StringComparison.OrdinalIgnoreCase))
            {
                string url =
                    command
                        .Substring(4)
                        .Trim();

                AppendOutput(
                    $"{PromptText.Text}{command}");

                await OpenWebPage(
                    url);

                CommandInput.Focus();

                return;
            }

            // PowerShell already echoes the submitted command.
            // Enabling this caused TreasureShell to print every command twice.
            //
            // AppendOutput(
            //     $"{PromptText.Text}{command}");

            if (_powerShell == null ||
                _powerShell.HasExited)
            {
                AppendOutput(
                    "PowerShell backend is not currently running.");

                return;
            }

            try
            {
                await _powerShell
                    .StandardInput
                    .WriteLineAsync(
                        command);

                await _powerShell
                    .StandardInput
                    .FlushAsync();

                // Ask PowerShell for its location
                // after every command.

                await Task.Delay(100);

                await RequestCurrentLocation();
            }
            catch (Exception ex)
            {
                AppendOutput(
                    "Failed to send command:");

                AppendOutput(
                    ex.Message);
            }
        }

        // =========================================================
        // TAB COMPLETION
        // =========================================================

        private string GetPromptDirectory()
        {
            string prompt =
                PromptText.Text;

            if (
                prompt.StartsWith("PS ") &&
                prompt.EndsWith("> "))
            {
                return prompt.Substring(
                    3,
                    prompt.Length - 5);
            }

            return _homeDirectory;
        }

        private void CompletePath()
        {
            string text =
                CommandInput.Text;

            if (string.IsNullOrWhiteSpace(
                    text))
            {
                return;
            }

            int lastSpace =
                text.LastIndexOf(' ');

            string prefix =
                lastSpace >= 0
                    ? text.Substring(
                        0,
                        lastSpace + 1)
                    : "";

            string partial =
                lastSpace >= 0
                    ? text.Substring(
                        lastSpace + 1)
                    : text;

            string currentDirectory =
                GetPromptDirectory();

            string searchDirectory =
                currentDirectory;

            string searchText =
                partial;

            // Handle paths that already
            // contain folders.

            string? partialDirectory =
                Path.GetDirectoryName(
                    partial);

            if (!string.IsNullOrEmpty(
                    partialDirectory))
            {
                searchDirectory =
                    Path.IsPathRooted(
                        partialDirectory)
                        ? partialDirectory
                        : Path.Combine(
                            currentDirectory,
                            partialDirectory);

                searchText =
                    Path.GetFileName(
                        partial);
            }

            try
            {
                string? match =
                    Directory
                        .GetFileSystemEntries(
                            searchDirectory,
                            searchText + "*")
                        .FirstOrDefault();

                if (match == null)
                    return;

                string completedName =
                    Path.GetFileName(
                        match);

                if (!string.IsNullOrEmpty(
                        partialDirectory))
                {
                    completedName =
                        Path.Combine(
                            partialDirectory,
                            completedName);
                }

                // Add slash when completing
                // a folder.

                if (Directory.Exists(
                        match))
                {
                    completedName +=
                        Path.DirectorySeparatorChar;
                }

                CommandInput.Text =
                    prefix +
                    completedName;

                CommandInput.CaretIndex =
                    CommandInput.Text.Length;
            }
            catch
            {
                // No valid completion.
            }
        }

        // =========================================================
        // WEB BROWSER
        // =========================================================

        private async Task OpenWebPage(
            string url)
        {
            if (string.IsNullOrWhiteSpace(
                    url))
            {
                return;
            }

            // Allow:
            //
            // web github.com
            //
            // instead of requiring:
            //
            // web https://github.com

            if (
                !url.StartsWith(
                    "http://",
                    StringComparison.OrdinalIgnoreCase)

                &&

                !url.StartsWith(
                    "https://",
                    StringComparison.OrdinalIgnoreCase))
            {
                url =
                    "https://" + url;
            }

            try
            {
                string html =
                    await _httpClient
                        .GetStringAsync(
                            url);

                OutputScroller.Visibility =
                    Visibility.Collapsed;

                WelcomePanel.Visibility =
                    Visibility.Collapsed;

                WebViewer.Visibility =
                    Visibility.Visible;

                WebDocument.Blocks.Clear();

                RenderSimpleWebPage(
                    html,
                    new Uri(url));
            }
            catch (Exception ex)
            {
                ShowTerminalView();

                AppendOutput(
                    $"Web error: {ex.Message}");
            }
        }

        private void ShowTerminalView()
        {
            WebViewer.Visibility =
                Visibility.Collapsed;

            OutputScroller.Visibility =
                Visibility.Visible;

            WelcomePanel.Visibility =
                Visibility.Visible;
        }

        private void RenderSimpleWebPage(
            string html,
            Uri baseUri)
        {
            // =====================================================
            // REMOVE SCRIPT / STYLE CONTENT
            // =====================================================

            html =
                System.Text.RegularExpressions
                    .Regex.Replace(
                        html,
                        @"<(script|style)[^>]*>.*?</\1>",
                        "",
                        System.Text.RegularExpressions
                            .RegexOptions.Singleline |
                        System.Text.RegularExpressions
                            .RegexOptions.IgnoreCase);

            // =====================================================
            // PAGE TITLE
            // =====================================================

            var titleMatch =
                System.Text.RegularExpressions
                    .Regex.Match(
                        html,
                        @"<title[^>]*>(.*?)</title>",
                        System.Text.RegularExpressions
                            .RegexOptions.Singleline |
                        System.Text.RegularExpressions
                            .RegexOptions.IgnoreCase);

            if (titleMatch.Success)
            {
                string title =
                    System.Net.WebUtility
                        .HtmlDecode(
                            titleMatch
                                .Groups[1]
                                .Value
                                .Trim());

                var heading =
                    new Paragraph(
                        new Run(title))
                    {
                        FontSize = 25,

                        FontWeight =
                            FontWeights.Bold,

                        Foreground =
                            new SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(
                                    244,
                                    205,
                                    113)),

                        Margin =
                            new Thickness(
                                0,
                                0,
                                0,
                                18)
                    };

                WebDocument.Blocks.Add(
                    heading);
            }

            // =====================================================
            // TURN BLOCK ELEMENTS INTO LINE BREAKS
            // =====================================================

            html =
                System.Text.RegularExpressions
                    .Regex.Replace(
                        html,
                        @"</?(p|div|section|article|header|footer|main|nav|h1|h2|h3|h4|li|ul|ol)[^>]*>",
                        Environment.NewLine,
                        System.Text.RegularExpressions
                            .RegexOptions.IgnoreCase);

            html =
                System.Text.RegularExpressions
                    .Regex.Replace(
                        html,
                        @"<br\s*/?>",
                        Environment.NewLine,
                        System.Text.RegularExpressions
                            .RegexOptions.IgnoreCase);

            // =====================================================
            // REMOVE REMAINING HTML TAGS
            // =====================================================

            string text =
                System.Text.RegularExpressions
                    .Regex.Replace(
                        html,
                        "<[^>]+>",
                        "");

            // Decode:
            //
            // &amp;
            // &nbsp;
            // &quot;
            // etc.

            text =
                System.Net.WebUtility
                    .HtmlDecode(
                        text);

            // =====================================================
            // CLEAN WHITESPACE
            // =====================================================

            text =
                System.Text.RegularExpressions
                    .Regex.Replace(
                        text,
                        @"[ \t]+",
                        " ");

            text =
                System.Text.RegularExpressions
                    .Regex.Replace(
                        text,
                        @"(\r?\n\s*){3,}",
                        Environment.NewLine +
                        Environment.NewLine);

            text =
                text.Trim();

            // =====================================================
            // BODY
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                    text))
            {
                var body =
                    new Paragraph(
                        new Run(text))
                    {
                        Margin =
                            new Thickness(0),

                        LineHeight =
                            22
                    };

                WebDocument.Blocks.Add(
                    body);
            }
        }

        // =========================================================
        // LOCATION TRACKING
        // =========================================================

        private async Task RequestCurrentLocation()
        {
            if (_powerShell == null ||
                _powerShell.HasExited)
            {
                return;
            }

            const string marker =
                "__TREASURESHELL_LOCATION__";

            await _powerShell
                .StandardInput
                .WriteLineAsync(
                    $"Write-Output \"{marker}$((Get-Location).Path)\"");

            await _powerShell
                .StandardInput
                .FlushAsync();
        }

        private void UpdatePrompt(
            string path)
        {
            Dispatcher.Invoke(() =>
            {
                PromptText.Text =
                    $"PS {path}> ";
            });
        }

        // =========================================================
        // OUTPUT
        // =========================================================

        private void AppendOutput(
            string text)
        {
            const string marker =
                "__TREASURESHELL_LOCATION__";

            // Internal location response.
            // Update prompt instead of displaying it.

            if (text.StartsWith(
                    marker))
            {
                string path =
                    text.Substring(
                        marker.Length);

                UpdatePrompt(
                    path);

                return;
            }

            // Hide our internal location command
            // if PowerShell echoes it.

            if (
                text.Contains(
                    "Write-Output")

                &&

                text.Contains(
                    marker))
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                TerminalOutput.AppendText(
                    text +
                    Environment.NewLine);

                TerminalOutput
                    .ScrollToEnd();

                OutputScroller
                    .ScrollToEnd();
            });
        }

        // =========================================================
        // WINDOW MOVEMENT
        // =========================================================

        private void TitleBar_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (
                e.ChangedButton ==
                MouseButton.Left)
            {
                DragMove();
            }
        }

        // =========================================================
        // WINDOW CONTROLS
        // =========================================================

        private void MinimizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState =
                WindowState.Minimized;
        }

        private void MaximizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // RESTORE

            if (
                MaximizeButton
                    .Content?
                    .ToString() == "❐")
            {
                WindowState =
                    WindowState.Normal;

                if (_hasRestoreBounds)
                {
                    Left =
                        _restoreLeft;

                    Top =
                        _restoreTop;

                    Width =
                        _restoreWidth;

                    Height =
                        _restoreHeight;
                }

                MaximizeButton.Content =
                    "□";

                return;
            }

            // SAVE CURRENT WINDOW

            _restoreLeft =
                Left;

            _restoreTop =
                Top;

            _restoreWidth =
                ActualWidth;

            _restoreHeight =
                ActualHeight;

            _hasRestoreBounds =
                true;

            // CURRENT MONITOR

            IntPtr handle =
                new WindowInteropHelper(this)
                    .Handle;

            Screen screen =
                Screen.FromHandle(
                    handle);

            var workArea =
                screen.WorkingArea;

            // LARGE WINDOW SIZE

            double targetWidth =
                Math.Min(
                    2000,
                    workArea.Width - 80);

            double targetHeight =
                Math.Min(
                    1000,
                    workArea.Height - 80);

            // POSITION ON CURRENT MONITOR

            double offsetX =
                250;

            double offsetY =
                -140;

            Left =
                workArea.Left +
                (workArea.Width -
                 targetWidth) / 2 +
                offsetX;

            Top =
                workArea.Top +
                (workArea.Height -
                 targetHeight) / 2 +
                offsetY;

            Width =
                targetWidth;

            Height =
                targetHeight;

            MaximizeButton.Content =
                "❐";
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void MainWindow_Closed(
            object? sender,
            EventArgs e)
        {
            try
            {
                if (_powerShell is
                    { HasExited: false })
                {
                    _powerShell
                        .StandardInput
                        .Close();

                    _powerShell.Kill(
                        entireProcessTree:
                        true);
                }
            }
            catch
            {
            }

            _powerShell?.Dispose();

            _httpClient.Dispose();
        }

        // =========================================================
        // CHEST -> TERMINAL
        // =========================================================

        private void OpenChest()
        {
            AnimateTranslateY(
                LidTranslate,
                0,
                -105,
                0.70);

            AnimateScaleY(
                LidScale,
                1,
                0.42,
                0.70);

            var shrinkX =
                CreateAnimation(
                    1,
                    0.10,
                    0.70,
                    0.78);

            var shrinkY =
                CreateAnimation(
                    1,
                    0.10,
                    0.70,
                    0.78);

            shrinkX.Completed +=
                (_, _) =>
                {
                    Chest.Visibility =
                        Visibility.Collapsed;
                };

            ChestScale.BeginAnimation(
                System.Windows.Media
                    .ScaleTransform
                    .ScaleXProperty,
                shrinkX);

            ChestScale.BeginAnimation(
                System.Windows.Media
                    .ScaleTransform
                    .ScaleYProperty,
                shrinkY);

            Chest.BeginAnimation(
                OpacityProperty,
                CreateAnimation(
                    1,
                    0,
                    0.55,
                    0.90));

            TerminalPanel.BeginAnimation(
                OpacityProperty,
                CreateAnimation(
                    0,
                    1,
                    0.65,
                    1.00));

            AnimateScaleX(
                TerminalScale,
                0.20,
                1,
                0.85,
                0.95);

            AnimateScaleY(
                TerminalScale,
                0.20,
                1,
                0.85,
                0.95);
        }

        // =========================================================
        // ANIMATION HELPERS
        // =========================================================

        private void AnimateTranslateY(
            System.Windows.Media.TranslateTransform target,
            double from,
            double to,
            double duration,
            double delay = 0)
        {
            target.BeginAnimation(
                System.Windows.Media
                    .TranslateTransform
                    .YProperty,
                CreateAnimation(
                    from,
                    to,
                    duration,
                    delay));
        }

        private void AnimateScaleX(
            System.Windows.Media.ScaleTransform target,
            double from,
            double to,
            double duration,
            double delay = 0)
        {
            target.BeginAnimation(
                System.Windows.Media
                    .ScaleTransform
                    .ScaleXProperty,
                CreateAnimation(
                    from,
                    to,
                    duration,
                    delay));
        }

        private void AnimateScaleY(
            System.Windows.Media.ScaleTransform target,
            double from,
            double to,
            double duration,
            double delay = 0)
        {
            target.BeginAnimation(
                System.Windows.Media
                    .ScaleTransform
                    .ScaleYProperty,
                CreateAnimation(
                    from,
                    to,
                    duration,
                    delay));
        }

        private DoubleAnimation CreateAnimation(
            double from,
            double to,
            double duration,
            double delay = 0)
        {
            return new DoubleAnimation
            {
                From =
                    from,

                To =
                    to,

                Duration =
                    TimeSpan.FromSeconds(
                        duration),

                BeginTime =
                    TimeSpan.FromSeconds(
                        delay),

                EasingFunction =
                    new CubicEase
                    {
                        EasingMode =
                            EasingMode.EaseInOut
                    }
            };
        }
    }
}