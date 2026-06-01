using System.Text.Json;

namespace WinFormsApp5;

public partial class Form1 : Form
{
    private static readonly string[] Symbols = ["EURUSD", "GBPUSD", "USDJPY", "XAUUSD", "US30", "NAS100", "BTCUSD"];
    private static readonly string[] Timeframes = ["M15", "H1", "H4", "D1"];

    private readonly ConfluenceSignalEngine _engine = new();
    private readonly Mt5WebSocketBridge _bridge = new();
    private readonly Dictionary<string, Dictionary<string, TimeframeReading>> _matrix = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _clockTimer = new();
    private readonly System.Windows.Forms.Timer _marketPulseTimer = new();

    private DataGridView _signalGrid = null!;
    private TextBox _mt5IdTextBox = null!;
    private TextBox _mt5PasswordTextBox = null!;
    private Label _clockLabel = null!;
    private Label _identityBadge = null!;
    private Label _connectionStatus = null!;
    private Label _accountStatus = null!;
    private Label _headlineSignal = null!;
    private Label _confidenceMetric = null!;
    private Label _coverageMetric = null!;
    private RichTextBox _intelligenceFeed = null!;
    private FlatSignalButton _connectButton = null!;
    private FlatSignalButton _evaluateButton = null!;
    private FlatSignalButton _pulseButton = null!;
    private bool _systemActive;

    public Form1()
    {
        InitializeComponent();
        ConfigureShell();
        BuildLayout();
        WireEvents();
        SeedMatrix();
        RefreshMatrix();
        StartTimers();
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        _clockTimer.Stop();
        _marketPulseTimer.Stop();
        await _bridge.DisposeAsync();
        base.OnFormClosing(e);
    }

    private void ConfigureShell()
    {
        BackColor = ThemeColors.Background;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        DoubleBuffered = true;
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(22),
            BackColor = ThemeColors.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildMetricStrip(), 0, 1);
        root.Controls.Add(BuildWorkspace(), 0, 2);
        root.Controls.Add(BuildFooter(), 0, 3);
        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill, BackColor = ThemeColors.Background };

        var title = new Label
        {
            AutoSize = true,
            Text = "Confluence Signal Matrix",
            ForeColor = ThemeColors.PrimaryText,
            Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold, GraphicsUnit.Point),
            Location = new Point(0, 8)
        };

        var subtitle = new Label
        {
            AutoSize = true,
            Text = "AI-powered market structure analyzer for MT5 multi-timeframe alignment",
            ForeColor = ThemeColors.SecondaryText,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
            Location = new Point(3, 53)
        };

        _connectionStatus = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Text = "MT5 ACCOUNT: LOCKED",
            ForeColor = ThemeColors.MutedText,
            Font = new Font("Consolas", 10F, FontStyle.Bold, GraphicsUnit.Point),
            Size = new Size(330, 34),
            Location = new Point(ClientSize.Width - 382, 16)
        };

        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        header.Controls.Add(_connectionStatus);
        header.Resize += (_, _) => _connectionStatus.Location = new Point(header.Width - _connectionStatus.Width, 16);
        return header;
    }

    private Control BuildMetricStrip()
    {
        var strip = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = ThemeColors.Background,
            Padding = new Padding(0, 6, 0, 10)
        };
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));

        _headlineSignal = CreateMetricValue("INITIALIZING MATRIX");
        _confidenceMetric = CreateMetricValue("0%");
        _coverageMetric = CreateMetricValue("0 / 28");

        strip.Controls.Add(CreateMetricCard("Dominant Institutional Read", _headlineSignal, ThemeColors.Accent), 0, 0);
        strip.Controls.Add(CreateMetricCard("Average Confidence", _confidenceMetric, ThemeColors.Success), 1, 0);
        strip.Controls.Add(CreateMetricCard("Live Timeframe Coverage", _coverageMetric, ThemeColors.Warning), 2, 0);
        return strip;
    }

    private static Label CreateMetricValue(string text) => new()
    {
        Dock = DockStyle.Fill,
        AutoSize = false,
        AutoEllipsis = true,
        Text = text,
        ForeColor = ThemeColors.PrimaryText,
        Font = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold, GraphicsUnit.Point),
        TextAlign = ContentAlignment.MiddleLeft,
        UseMnemonic = false
    };

    private static Control CreateMetricCard(string caption, Label valueLabel, Color accent)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(16, 8, 16, 10),
            BackColor = ThemeColors.Surface
        };

        var accentBar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accent };
        var captionLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            AutoEllipsis = true,
            Text = caption.ToUpperInvariant(),
            ForeColor = ThemeColors.MutedText,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point)
        };

        panel.Controls.Add(valueLabel);
        panel.Controls.Add(captionLabel);
        panel.Controls.Add(accentBar);
        return panel;
    }

    private Control BuildWorkspace()
    {
        var workspace = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 10, 0, 0),
            ColumnCount = 3,
            RowCount = 1,
            BackColor = ThemeColors.Background
        };
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 272));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 328));

        workspace.Controls.Add(BuildCommandDeck(), 0, 0);
        workspace.Controls.Add(BuildSignalGrid(), 1, 0);
        workspace.Controls.Add(BuildIntelligencePanel(), 2, 0);
        return workspace;
    }

    private Control BuildCommandDeck()
    {
        var deck = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 16, 0),
            Padding = new Padding(18),
            BackColor = ThemeColors.Surface,
            ColumnCount = 1,
            RowCount = 13
        };
        deck.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        deck.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        deck.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        deck.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        deck.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        deck.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        deck.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        deck.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        deck.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        deck.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        deck.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        deck.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        deck.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "MT5 SIGNAL BRIDGE",
            ForeColor = ThemeColors.PrimaryText,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point)
        };

        var idCaption = CreateInputCaption("MT5 ID");
        _mt5IdTextBox = CreateInputBox();

        var passwordCaption = CreateInputCaption("MT5 PASSWORD");
        _mt5PasswordTextBox = CreateInputBox();
        _mt5PasswordTextBox.UseSystemPasswordChar = true;

        _connectButton = new FlatSignalButton
        {
            Dock = DockStyle.Fill,
            Text = "ACTIVATE SYSTEM",
            AccentColor = ThemeColors.Accent
        };

        _evaluateButton = new FlatSignalButton
        {
            Dock = DockStyle.Fill,
            Text = "RUN MATRIX",
            AccentColor = ThemeColors.Success,
            Enabled = false
        };
        _evaluateButton.Click += (_, _) => RefreshMatrix();

        _pulseButton = new FlatSignalButton
        {
            Dock = DockStyle.Fill,
            Text = "SIMULATE PULSE",
            AccentColor = ThemeColors.Warning,
            Enabled = false
        };
        _pulseButton.Click += (_, _) => ApplySyntheticPulse();

        _accountStatus = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
            Text = "Account required",
            ForeColor = ThemeColors.MutedText,
            Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point)
        };

        deck.Controls.Add(title, 0, 0);
        deck.Controls.Add(idCaption, 0, 1);
        deck.Controls.Add(_mt5IdTextBox, 0, 2);
        deck.Controls.Add(passwordCaption, 0, 4);
        deck.Controls.Add(_mt5PasswordTextBox, 0, 5);
        deck.Controls.Add(_connectButton, 0, 7);
        deck.Controls.Add(_evaluateButton, 0, 9);
        deck.Controls.Add(_pulseButton, 0, 11);
        deck.Controls.Add(_accountStatus, 0, 12);
        return deck;
    }

    private static Label CreateInputCaption(string text) => new()
    {
        Dock = DockStyle.Fill,
        Text = text,
        ForeColor = ThemeColors.SecondaryText,
        Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
        TextAlign = ContentAlignment.BottomLeft
    };

    private static TextBox CreateInputBox() => new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = ThemeColors.Input,
        ForeColor = ThemeColors.PrimaryText,
        Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point)
    };

    private Control BuildSignalGrid()
    {
        _signalGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ThemeColors.Surface,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            EnableHeadersVisualStyles = false,
            GridColor = ThemeColors.GridLine,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeight = 38,
            RowTemplate = { Height = 42 }
        };

        _signalGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ThemeColors.Header,
            ForeColor = ThemeColors.SecondaryText,
            SelectionBackColor = ThemeColors.Header,
            SelectionForeColor = ThemeColors.PrimaryText,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point),
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };

        _signalGrid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ThemeColors.Surface,
            ForeColor = ThemeColors.PrimaryText,
            SelectionBackColor = ThemeColors.Selection,
            SelectionForeColor = ThemeColors.PrimaryText,
            Font = new Font("Segoe UI", 9.3F, FontStyle.Regular, GraphicsUnit.Point),
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };

        _signalGrid.Columns.Add("Asset", "ASSET");
        foreach (var timeframe in Timeframes)
        {
            _signalGrid.Columns.Add(timeframe, timeframe);
        }
        _signalGrid.Columns.Add("Confluence", "CONFLUENCE");
        _signalGrid.Columns.Add("Signal", "AI SIGNAL");
        _signalGrid.Columns.Add("Confidence", "CONF.");
        _signalGrid.Columns.Add("Status", "STATUS");

        _signalGrid.Columns["Asset"].FillWeight = 72;
        _signalGrid.Columns["Signal"].FillWeight = 230;
        _signalGrid.Columns["Status"].FillWeight = 98;
        _signalGrid.CellFormatting += SignalGrid_CellFormatting;
        return _signalGrid;
    }

    private Control BuildIntelligencePanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(16, 0, 0, 0),
            Padding = new Padding(16),
            BackColor = ThemeColors.Surface
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Text = "AI STRUCTURE FEED",
            ForeColor = ThemeColors.PrimaryText,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point)
        };

        _intelligenceFeed = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = ThemeColors.Surface,
            ForeColor = ThemeColors.SecondaryText,
            Font = new Font("Consolas", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            ReadOnly = true,
            Text = "Awaiting institutional confluence scan..."
        };

        panel.Controls.Add(_intelligenceFeed);
        panel.Controls.Add(title);
        return panel;
    }

    private Control BuildFooter()
    {
        var footer = new Panel { Dock = DockStyle.Fill, BackColor = ThemeColors.Background };

        _identityBadge = new Label
        {
            AutoSize = false,
            Width = 330,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "  SYSTEM LOCKED  |  ACCOUNT REQUIRED",
            ForeColor = ThemeColors.MutedText,
            Font = new Font("Consolas", 9F, FontStyle.Bold, GraphicsUnit.Point)
        };

        _clockLabel = new Label
        {
            AutoSize = false,
            Width = 210,
            Dock = DockStyle.Right,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = ThemeColors.SecondaryText,
            Font = new Font("Consolas", 10F, FontStyle.Bold, GraphicsUnit.Point)
        };

        footer.Controls.Add(_clockLabel);
        footer.Controls.Add(_identityBadge);
        return footer;
    }

    private void WireEvents()
    {
        _connectButton.Click += (_, _) => ActivateSystem();
        _mt5IdTextBox.TextChanged += (_, _) => ResetSystemActivation();
        _mt5PasswordTextBox.TextChanged += (_, _) => ResetSystemActivation();
        _bridge.MessageReceived += Bridge_MessageReceived;
        _bridge.StateChanged += (_, state) =>
        {
            if (IsDisposed)
            {
                return;
            }

            BeginInvoke(() =>
            {
                _connectionStatus.Text = $"MT5 STREAM: {state.ToUpperInvariant()}";
                _connectionStatus.ForeColor = state.Equals("connected", StringComparison.OrdinalIgnoreCase)
                    ? ThemeColors.Success
                    : ThemeColors.MutedText;
            });
        };
    }

    private void StartTimers()
    {
        _clockTimer.Interval = 1000;
        _clockTimer.Tick += (_, _) => _clockLabel.Text = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
        _clockTimer.Start();

        _marketPulseTimer.Interval = 5000;
        _marketPulseTimer.Tick += (_, _) =>
        {
            if (_systemActive && !_bridge.IsConnected)
            {
                ApplySyntheticPulse();
            }
        };
        _marketPulseTimer.Start();
    }

    private void SeedMatrix()
    {
        foreach (var symbol in Symbols)
        {
            _matrix[symbol] = _engine.CreateBaseline(symbol, Timeframes);
        }
    }

    private void ActivateSystem()
    {
        var mt5Id = _mt5IdTextBox.Text.Trim();
        var mt5Password = _mt5PasswordTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(mt5Id) || string.IsNullOrWhiteSpace(mt5Password))
        {
            SetSystemActive(false);
            _accountStatus.Text = "ERROR: MT5 ID and password required";
            _accountStatus.ForeColor = ThemeColors.Danger;
            _connectionStatus.Text = "MT5 ACCOUNT: LOCKED";
            _connectionStatus.ForeColor = ThemeColors.Danger;
            AppendFeed("Access denied: enter MT5 ID and password.", ThemeColors.Danger);
            return;
        }

        SetSystemActive(true);
        _accountStatus.Text = $"Account linked: {MaskAccount(mt5Id)}";
        _accountStatus.ForeColor = ThemeColors.Success;
        _connectionStatus.Text = "MT5 ACCOUNT: VERIFIED";
        _connectionStatus.ForeColor = ThemeColors.Success;
        AppendFeed($"Secure identity accepted for MT5 account {MaskAccount(mt5Id)}.", ThemeColors.Success);
        RefreshMatrix();
    }

    private void ResetSystemActivation()
    {
        if (!_systemActive)
        {
            return;
        }

        SetSystemActive(false);
        _accountStatus.Text = "Account changed. Reactivate required";
        _accountStatus.ForeColor = ThemeColors.Warning;
        _connectionStatus.Text = "MT5 ACCOUNT: LOCKED";
        _connectionStatus.ForeColor = ThemeColors.MutedText;
    }

    private void SetSystemActive(bool active)
    {
        _systemActive = active;
        _identityBadge.Text = active
            ? "  SYSTEM ACTIVE  |  MT5 IDENTITY SECURE"
            : "  SYSTEM LOCKED  |  ACCOUNT REQUIRED";
        _identityBadge.ForeColor = active ? ThemeColors.Success : ThemeColors.MutedText;
        _evaluateButton.Enabled = active;
        _pulseButton.Enabled = active;
    }

    private static string MaskAccount(string account)
    {
        if (account.Length <= 3)
        {
            return "***";
        }

        return new string('*', Math.Max(0, account.Length - 3)) + account[^3..];
    }

    private void Bridge_MessageReceived(object? sender, string payload)
    {
        if (IsDisposed)
        {
            return;
        }

        BeginInvoke(() =>
        {
            if (TryApplyMt5Payload(payload, out var note))
            {
                RefreshMatrix();
                AppendFeed(note, ThemeColors.Success);
            }
            else
            {
                AppendFeed(note, ThemeColors.Warning);
            }
        });
    }

    private bool TryApplyMt5Payload(string payload, out string note)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            var symbol = ReadString(root, "symbol").ToUpperInvariant();
            var timeframe = ReadString(root, "timeframe").ToUpperInvariant();
            if (!Symbols.Contains(symbol, StringComparer.OrdinalIgnoreCase) || !Timeframes.Contains(timeframe, StringComparer.OrdinalIgnoreCase))
            {
                note = $"Ignored MT5 payload outside matrix scope: {symbol} {timeframe}";
                return false;
            }

            var reading = new TimeframeReading(
                timeframe,
                ReadDecimal(root, "price"),
                MarketBiasParser.Parse(ReadString(root, "bias")),
                ReadBool(root, "liquiditySweep"),
                ReadBool(root, "breakOfStructure"),
                ReadDouble(root, "volumeDelta"),
                ReadDouble(root, "orderBlockQuality"),
                DateTimeOffset.Now);

            _matrix[symbol][timeframe] = reading;
            note = $"MT5 tick accepted: {symbol} {timeframe} {reading.Bias} price {reading.Price:0.#####}";
            return true;
        }
        catch (Exception ex)
        {
            note = $"Rejected malformed MT5 payload: {ex.Message}";
            return false;
        }
    }

    private void ApplySyntheticPulse()
    {
        if (!_systemActive)
        {
            _accountStatus.Text = "ERROR: activate account first";
            _accountStatus.ForeColor = ThemeColors.Danger;
            AppendFeed("Pulse blocked: MT5 account is not active.", ThemeColors.Danger);
            return;
        }

        foreach (var symbol in Symbols)
        {
            _matrix[symbol] = _engine.CreateSyntheticPulse(symbol, _matrix[symbol]);
        }

        RefreshMatrix();
        AppendFeed("Synthetic MT5 pulse applied while bridge is on standby.", ThemeColors.Warning);
    }

    private void RefreshMatrix()
    {
        _signalGrid.Rows.Clear();
        var evaluatedRows = Symbols
            .Select(symbol => _engine.Evaluate(symbol, _matrix[symbol]))
            .OrderByDescending(row => row.Confidence)
            .ThenBy(row => row.Asset)
            .ToList();

        foreach (var row in evaluatedRows)
        {
            _signalGrid.Rows.Add(
                row.Asset,
                DescribeTimeframe(row.Readings["M15"]),
                DescribeTimeframe(row.Readings["H1"]),
                DescribeTimeframe(row.Readings["H4"]),
                DescribeTimeframe(row.Readings["D1"]),
                row.ConfluenceScore.ToString("0.0"),
                row.SignalText,
                row.Confidence.ToString("0%"),
                row.Status);
        }

        var dominant = evaluatedRows.FirstOrDefault();
        if (dominant is not null)
        {
            _headlineSignal.Text = FormatHeadlineSignal(dominant);
        }

        _confidenceMetric.Text = evaluatedRows.Average(row => row.Confidence).ToString("0%");
        _coverageMetric.Text = $"{evaluatedRows.Sum(row => row.Readings.Count)} / {Symbols.Length * Timeframes.Length}";
        RenderNarrative(evaluatedRows);
    }

    private void RenderNarrative(IReadOnlyList<MatrixEvaluation> rows)
    {
        _intelligenceFeed.Clear();
        foreach (var row in rows.Take(5))
        {
            var color = row.Direction switch
            {
                MarketBias.Bullish => ThemeColors.Success,
                MarketBias.Bearish => ThemeColors.Danger,
                _ => ThemeColors.SecondaryText
            };

            AppendFeed($"{DateTime.Now:HH:mm:ss}  {row.Asset}  {row.SignalText}  | Score {row.ConfluenceScore:0.0}", color);
            AppendFeed($"  {row.Rationale}", ThemeColors.MutedText);
        }
    }

    private static string FormatHeadlineSignal(MatrixEvaluation evaluation)
    {
        var direction = evaluation.Direction switch
        {
            MarketBias.Bullish => "LONG",
            MarketBias.Bearish => "SHORT",
            _ => "WAIT"
        };

        return $"{evaluation.Asset}: {direction} / {evaluation.Status} / {evaluation.Confidence:0%}";
    }

    private static string DescribeTimeframe(TimeframeReading reading)
    {
        var bias = reading.Bias switch
        {
            MarketBias.Bullish => "BULL",
            MarketBias.Bearish => "BEAR",
            _ => "BAL"
        };

        var structure = reading.BreakOfStructure ? "BOS" : "RNG";
        var liquidity = reading.LiquiditySweep ? "LQ" : "EQ";
        return $"{bias} {structure}/{liquidity}";
    }

    private void AppendFeed(string message, Color color)
    {
        _intelligenceFeed.SelectionStart = _intelligenceFeed.TextLength;
        _intelligenceFeed.SelectionLength = 0;
        _intelligenceFeed.SelectionColor = color;
        _intelligenceFeed.AppendText(message + Environment.NewLine);
        _intelligenceFeed.SelectionColor = _intelligenceFeed.ForeColor;
        _intelligenceFeed.ScrollToCaret();
    }

    private void SignalGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || _signalGrid.Columns[e.ColumnIndex].Name is "Asset")
        {
            return;
        }

        var value = Convert.ToString(e.Value);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var cellStyle = e.CellStyle;
        if (cellStyle is null)
        {
            return;
        }

        if (value.Contains("BULL", StringComparison.OrdinalIgnoreCase) || value.Contains("LONG", StringComparison.OrdinalIgnoreCase))
        {
            cellStyle.ForeColor = ThemeColors.Success;
        }
        else if (value.Contains("BEAR", StringComparison.OrdinalIgnoreCase) || value.Contains("SHORT", StringComparison.OrdinalIgnoreCase))
        {
            cellStyle.ForeColor = ThemeColors.Danger;
        }
        else if (value.Contains("WATCH", StringComparison.OrdinalIgnoreCase) || value.Contains("PENDING", StringComparison.OrdinalIgnoreCase))
        {
            cellStyle.ForeColor = ThemeColors.Warning;
        }
    }

    private static string ReadString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static decimal ReadDecimal(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value) && value.TryGetDecimal(out var result) ? result : 0M;

    private static double ReadDouble(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value) && value.TryGetDouble(out var result) ? result : 0D;

    private static bool ReadBool(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.True;
}
