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
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Collections.Generic;
using HtmlAgilityPack;

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

        private Uri? _currentWebUri;

        private readonly Stack<Uri> _webHistory =
            new Stack<Uri>();

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

            // CLEAR TERMINAL OUTPUT

            if (
                command.Equals(
                    "cls",
                    StringComparison.OrdinalIgnoreCase)

                ||

                command.Equals(
                    "clear",
                    StringComparison.OrdinalIgnoreCase))
            {
                TerminalOutput.Clear();

                CommandInput.Focus();

                return;
            }
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


            // =====================================================
            // TREASURE WEB NAVIGATION
            // =====================================================

            if (command.Equals(
                    "back",
                    StringComparison.OrdinalIgnoreCase))
            {
                await GoBackWeb();

                CommandInput.Focus();

                return;
            }

            if (command.Equals(
                    "reload",
                    StringComparison.OrdinalIgnoreCase))
            {
                await ReloadWeb();

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
            string url,
            bool addToHistory = true)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

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

            await OpenWebPage(
                new Uri(url),
                addToHistory);
        }

        private async Task OpenWebPage(
            Uri uri,
            bool addToHistory = true)
        {
            try
            {
                if (
                    addToHistory &&
                    _currentWebUri != null &&
                    _currentWebUri != uri)
                {
                    _webHistory.Push(
                        _currentWebUri);
                }

                string html =
                    await _httpClient
                        .GetStringAsync(uri);

                _currentWebUri =
                    uri;

                OutputScroller.Visibility =
                    Visibility.Collapsed;

                WelcomePanel.Visibility =
                    Visibility.Collapsed;

                WebViewer.Visibility =
                    Visibility.Visible;

                WebDocument.Blocks.Clear();

                RenderWebPage(
                    html,
                    uri);
            }
            catch (Exception ex)
            {
                ShowTerminalView();

                AppendOutput(
                    $"Web error: {ex.Message}");
            }
        }

        private async Task GoBackWeb()
        {
            if (_webHistory.Count == 0)
                return;

            Uri previous =
                _webHistory.Pop();

            await OpenWebPage(
                previous,
                false);
        }

        private async Task ReloadWeb()
        {
            if (_currentWebUri == null)
                return;

            await OpenWebPage(
                _currentWebUri,
                false);
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

        private void RenderWebPage(
            string html,
            Uri baseUri)
        {
            var document =
                new HtmlAgilityPack.HtmlDocument();

            document.LoadHtml(html);

            WebDocument.Blocks.Clear();

            HtmlNode? titleNode =
                document.DocumentNode
                    .SelectSingleNode("//title");

            if (titleNode != null)
            {
                string title =
                    CleanWebText(
                        titleNode.InnerText);

                if (!string.IsNullOrWhiteSpace(title))
                {
                    WebDocument.Blocks.Add(
                        CreateHeading(
                            title,
                            26));
                }
            }

            HtmlNode? body =
                document.DocumentNode
                    .SelectSingleNode("//body");

            if (body == null)
                return;

            foreach (
                HtmlNode node
                in body.ChildNodes)
            {
                RenderBlockNode(
                    node,
                    baseUri);
            }
        }

        private void RenderBlockNode(
            HtmlNode node,
            Uri baseUri)
        {
            string name =
                node.Name
                    .ToLowerInvariant();

            if (
                name == "script" ||
                name == "style" ||
                name == "noscript" ||
                name == "svg" ||
                name == "path" ||
                name == "meta" ||
                name == "link")
            {
                return;
            }

            switch (name)
            {
                case "h1":
                    AddHeadingFromNode(
                        node,
                        28);

                    return;

                case "h2":
                    AddHeadingFromNode(
                        node,
                        23);

                    return;

                case "h3":
                    AddHeadingFromNode(
                        node,
                        19);

                    return;

                case "p":
                    AddParagraphFromNode(
                        node,
                        baseUri);

                    return;

                case "img":
                    AddImageFromNode(
                        node,
                        baseUri);

                    return;

                case "ul":
                case "ol":
                    AddListFromNode(
                        node,
                        baseUri);

                    return;
            }

            if (
                name == "div" ||
                name == "section" ||
                name == "article" ||
                name == "main" ||
                name == "header" ||
                name == "footer" ||
                name == "nav" ||
                name == "body")
            {
                foreach (
                    HtmlNode child
                    in node.ChildNodes)
                {
                    RenderBlockNode(
                        child,
                        baseUri);
                }

                return;
            }

            if (node.NodeType ==
                HtmlNodeType.Text)
            {
                string text =
                    CleanWebText(
                        node.InnerText);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    WebDocument.Blocks.Add(
                        new Paragraph(
                            new Run(text))
                        {
                            Margin =
                                new Thickness(
                                    0,
                                    2,
                                    0,
                                    6),

                            LineHeight =
                                22
                        });
                }

                return;
            }

            foreach (
                HtmlNode child
                in node.ChildNodes)
            {
                RenderBlockNode(
                    child,
                    baseUri);
            }
        }

        private void AddHeadingFromNode(
            HtmlNode node,
            double fontSize)
        {
            string text =
                CleanWebText(
                    node.InnerText);

            if (string.IsNullOrWhiteSpace(text))
                return;

            WebDocument.Blocks.Add(
                CreateHeading(
                    text,
                    fontSize));
        }

        private Paragraph CreateHeading(
            string text,
            double fontSize)
        {
            return new Paragraph(
                new Run(text))
            {
                FontSize =
                    fontSize,

                FontWeight =
                    FontWeights.Bold,

                Foreground =
                    new SolidColorBrush(
                        System.Windows.Media.Color
                            .FromRgb(
                                244,
                                205,
                                113)),

                Margin =
                    new Thickness(
                        0,
                        10,
                        0,
                        10)
            };
        }

        private void AddParagraphFromNode(
            HtmlNode node,
            Uri baseUri)
        {
            var paragraph =
                new Paragraph
                {
                    Margin =
                        new Thickness(
                            0,
                            2,
                            0,
                            10),

                    LineHeight =
                        22
                };

            AddInlineNodes(
                paragraph.Inlines,
                node,
                baseUri);

            if (paragraph.Inlines.Count > 0)
            {
                WebDocument.Blocks.Add(
                    paragraph);
            }
        }

        private void AddInlineNodes(
            InlineCollection target,
            HtmlNode parent,
            Uri baseUri)
        {
            foreach (
                HtmlNode node
                in parent.ChildNodes)
            {
                if (
                    node.NodeType ==
                    HtmlNodeType.Text)
                {
                    string text =
                        CleanWebText(
                            node.InnerText);

                    if (!string.IsNullOrEmpty(text))
                    {
                        target.Add(
                            new Run(text));
                    }

                    continue;
                }

                string name =
                    node.Name
                        .ToLowerInvariant();

                if (name == "br")
                {
                    target.Add(
                        new LineBreak());

                    continue;
                }

                if (
                    name == "strong" ||
                    name == "b")
                {
                    var bold =
                        new Bold();

                    AddInlineNodes(
                        bold.Inlines,
                        node,
                        baseUri);

                    target.Add(
                        bold);

                    continue;
                }

                if (
                    name == "em" ||
                    name == "i")
                {
                    var italic =
                        new Italic();

                    AddInlineNodes(
                        italic.Inlines,
                        node,
                        baseUri);

                    target.Add(
                        italic);

                    continue;
                }

                if (name == "a")
                {
                    string href =
                        node.GetAttributeValue(
                            "href",
                            "");

                    Uri? resolved =
                        ResolveWebUri(
                            baseUri,
                            href);

                    string linkText =
                        CleanWebText(
                            node.InnerText);

                    if (
                        resolved != null &&
                        !string.IsNullOrWhiteSpace(
                            linkText))
                    {
                        var hyperlink =
                            new Hyperlink(
                                new Run(linkText))
                            {
                                Foreground =
                                    new SolidColorBrush(
                                        System.Windows.Media.Color
                                            .FromRgb(
                                                255,
                                                216,
                                                121)),

                                TextDecorations =
                                    TextDecorations.Underline,

                                Cursor =
                                    System.Windows.Input.Cursors.Hand
                            };

                        hyperlink.Click +=
                            async (_, _) =>
                            {
                                await OpenWebPage(
                                    resolved);
                            };

                        target.Add(
                            hyperlink);
                    }
                    else
                    {
                        AddInlineNodes(
                            target,
                            node,
                            baseUri);
                    }

                    continue;
                }

                AddInlineNodes(
                    target,
                    node,
                    baseUri);
            }
        }

        private void AddListFromNode(
            HtmlNode node,
            Uri baseUri)
        {
            foreach (
                HtmlNode item
                in node.ChildNodes
                    .Where(
                        x =>
                            x.Name.Equals(
                                "li",
                                StringComparison.OrdinalIgnoreCase)))
            {
                var paragraph =
                    new Paragraph
                    {
                        Margin =
                            new Thickness(
                                12,
                                1,
                                0,
                                4),

                        LineHeight =
                            22
                    };

                paragraph.Inlines.Add(
                    new Run("â€¢ "));

                AddInlineNodes(
                    paragraph.Inlines,
                    item,
                    baseUri);

                WebDocument.Blocks.Add(
                    paragraph);
            }
        }

        private void AddImageFromNode(
            HtmlNode node,
            Uri baseUri)
        {
            string source =
                node.GetAttributeValue(
                    "src",
                    "");

            if (string.IsNullOrWhiteSpace(source))
            {
                source =
                    node.GetAttributeValue(
                        "data-src",
                        "");
            }

            Uri? imageUri =
                ResolveWebUri(
                    baseUri,
                    source);

            if (imageUri == null)
                return;

            try
            {
                var bitmap =
                    new BitmapImage();

                bitmap.BeginInit();

                bitmap.UriSource =
                    imageUri;

                bitmap.CacheOption =
                    BitmapCacheOption.OnLoad;

                bitmap.CreateOptions =
                    BitmapCreateOptions.IgnoreImageCache;

                bitmap.EndInit();

                var image =
                    new System.Windows.Controls.Image
                    {
                        Source =
                            bitmap,

                        Stretch =
                            Stretch.Uniform,

                        MaxWidth =
                            600,

                        MaxHeight =
                            420,

                        HorizontalAlignment =
                            System.Windows
                                .HorizontalAlignment.Left,

                        Margin =
                            new Thickness(
                                0,
                                8,
                                0,
                                14)
                    };

                RenderOptions.SetBitmapScalingMode(
                    image,
                    BitmapScalingMode.HighQuality);

                var container =
                    new BlockUIContainer(
                        image)
                    {
                        Margin =
                            new Thickness(
                                0)
                    };

                WebDocument.Blocks.Add(
                    container);
            }
            catch
            {
                // Image could not be loaded.
                // Ignore it and continue rendering.
            }
        }

        private Uri? ResolveWebUri(
            Uri baseUri,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (
                value.StartsWith(
                    "#"))
            {
                return null;
            }

            if (
                value.StartsWith(
                    "javascript:",
                    StringComparison.OrdinalIgnoreCase)

                ||

                value.StartsWith(
                    "mailto:",
                    StringComparison.OrdinalIgnoreCase)

                ||

                value.StartsWith(
                    "tel:",
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (
                Uri.TryCreate(
                    value,
                    UriKind.Absolute,
                    out Uri? absolute))
            {
                return absolute;
            }

            if (
                Uri.TryCreate(
                    baseUri,
                    value,
                    out Uri? relative))
            {
                return relative;
            }

            return null;
        }

        private string CleanWebText(
            string text)
        {
            text =
                HtmlEntity.DeEntitize(
                    text);

            text =
                text.Replace(
                    '\u00A0',
                    ' ');

            text =
                System.Text.RegularExpressions
                    .Regex.Replace(
                        text,
                        @"\s+",
                        " ");

            return text.Trim();
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


