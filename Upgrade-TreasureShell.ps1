$ErrorActionPreference = "Stop"

$ProjectRoot = $PSScriptRoot
$CsFile = Join-Path $ProjectRoot "MainWindow.xaml.cs"
$ProjectFile = Join-Path $ProjectRoot "TreasureShell.csproj"

Write-Host ""
Write-Host "TreasureShell Web v2 Upgrade" -ForegroundColor Yellow
Write-Host "============================" -ForegroundColor Yellow
Write-Host ""

# ============================================================
# VERIFY
# ============================================================

if (-not (Test-Path $CsFile)) {
    throw "MainWindow.xaml.cs not found."
}

if (-not (Test-Path $ProjectFile)) {
    throw "TreasureShell.csproj not found."
}

# ============================================================
# BACKUP
# ============================================================

$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$Backup = "$CsFile.$Timestamp.bak"

Copy-Item $CsFile $Backup

Write-Host "Backup created:" -ForegroundColor Green
Write-Host $Backup
Write-Host ""

# ============================================================
# ENSURE HTML AGILITY PACK
# ============================================================

$ProjectText = Get-Content $ProjectFile -Raw

if ($ProjectText -notmatch "HtmlAgilityPack") {
    Write-Host "Installing HtmlAgilityPack..." -ForegroundColor Cyan

    dotnet add $ProjectFile package HtmlAgilityPack

    if ($LASTEXITCODE -ne 0) {
        throw "HtmlAgilityPack installation failed."
    }
}
else {
    Write-Host "HtmlAgilityPack already installed." -ForegroundColor Green
}

Write-Host ""

# ============================================================
# LOAD C#
# ============================================================

$Code = Get-Content $CsFile -Raw

# ============================================================
# ADD USING STATEMENTS
# ============================================================

$Usings = @(
    "using HtmlAgilityPack;"
    "using System.Collections.Generic;"
    "using System.Windows.Controls;"
    "using System.Windows.Media.Imaging;"
)

foreach ($Using in $Usings) {

    if ($Code -notmatch [regex]::Escape($Using)) {

        $Code = $Code.Replace(
            "using System.Windows.Documents;",
            "using System.Windows.Documents;`r`n$Using"
        )

        Write-Host "Added: $Using" -ForegroundColor Green
    }
}

# ============================================================
# ADD WEB STATE
# ============================================================

if ($Code -notmatch "_currentWebUri") {

    $OldState = @'
        private readonly HttpClient _httpClient =
            new HttpClient();
'@

    $NewState = @'
        private readonly HttpClient _httpClient =
            new HttpClient();

        private Uri? _currentWebUri;

        private readonly Stack<Uri> _webHistory =
            new Stack<Uri>();
'@

    $Code = $Code.Replace(
        $OldState,
        $NewState
    )

    Write-Host "Added browser state." -ForegroundColor Green
}

# ============================================================
# ADD BACK / RELOAD COMMANDS
# ============================================================

if ($Code -notmatch "TREASURE WEB NAVIGATION") {

$NavigationCommands = @'

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

'@

    $Anchor = @'
            // WEB <URL>
'@

    $Code = $Code.Replace(
        $Anchor,
        $NavigationCommands + $Anchor
    )

    Write-Host "Added back/reload commands." -ForegroundColor Green
}

# ============================================================
# NEW WEB BROWSER BLOCK
# ============================================================

$NewWebBlock = @'
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

'@

# ============================================================
# REPLACE OLD WEB BLOCK
# ============================================================

$Pattern = '(?s)        // =========================================================\r?\n        // WEB BROWSER\r?\n        // =========================================================.*?(?=        // =========================================================\r?\n        // LOCATION TRACKING)'

if ($Code -notmatch $Pattern) {
    throw "Could not find the WEB BROWSER block."
}

$Code =
    [regex]::Replace(
        $Code,
        $Pattern,
        $NewWebBlock
    )

Write-Host "Web renderer replaced." -ForegroundColor Green

# ============================================================
# SAVE
# ============================================================

Set-Content `
    -Path $CsFile `
    -Value $Code `
    -Encoding utf8

Write-Host ""
Write-Host "MainWindow.xaml.cs updated." -ForegroundColor Green

# ============================================================
# BUILD
# ============================================================

Write-Host ""
Write-Host "Building TreasureShell..." -ForegroundColor Cyan
Write-Host ""

dotnet build $ProjectFile

if ($LASTEXITCODE -ne 0) {

    Write-Host ""
    Write-Host "BUILD FAILED." -ForegroundColor Red
    Write-Host ""
    Write-Host "Your original file is safe here:" -ForegroundColor Yellow
    Write-Host $Backup
    Write-Host ""

    exit 1
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host " TreasureShell Web v2 upgrade complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Try:" -ForegroundColor Yellow
Write-Host "  dotnet run"
Write-Host ""
Write-Host "Then inside TreasureShell:"
Write-Host ""
Write-Host "  web github.com"
Write-Host "  back"
Write-Host "  reload"
Write-Host "  web off"
Write-Host ""
