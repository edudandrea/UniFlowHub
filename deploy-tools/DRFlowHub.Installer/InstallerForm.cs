using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace DRFlowHub.Installer;

public sealed class InstallerForm : Form
{
    private readonly TextBox _targetPath = new();
    private readonly Button _browseButton = new();
    private readonly Button _installButton = new();
    private readonly CheckBox _preserveAppSettings = new();
    private readonly CheckBox _useAppOffline = new();
    private readonly CheckBox _restartIis = new();
    private readonly TextBox _siteName = new();
    private readonly TextBox _appPoolName = new();
    private readonly TextBox _log = new();
    private readonly ProgressBar _progress = new();
    private readonly Label _statusLabel = new();

    private static readonly Color PageBackColor = Color.FromArgb(244, 247, 251);
    private static readonly Color HeaderStartColor = Color.FromArgb(11, 31, 58);
    private static readonly Color HeaderEndColor = Color.FromArgb(22, 91, 170);
    private static readonly Color CardBackColor = Color.White;
    private static readonly Color BorderColor = Color.FromArgb(218, 226, 238);
    private static readonly Color PrimaryColor = Color.FromArgb(20, 101, 192);
    private static readonly Color PrimaryHoverColor = Color.FromArgb(15, 82, 159);
    private static readonly Color TextColor = Color.FromArgb(27, 39, 54);
    private static readonly Color MutedTextColor = Color.FromArgb(91, 108, 130);

    public InstallerForm()
    {
        Text = "Instalador DRFlowHub";
        Width = 920;
        Height = 760;
        MinimumSize = new Size(880, 720);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5F);
        BackColor = PageBackColor;
        DoubleBuffered = true;

        var header = new HeaderPanel
        {
            Location = new Point(0, 0),
            Size = new Size(ClientSize.Width, 136),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var brandMark = new Label
        {
            Text = "DR",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(55, 128, 222),
            Location = new Point(28, 28),
            Size = new Size(54, 54)
        };

        var title = new Label
        {
            Text = "Instalação do DRFlowHub",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Location = new Point(100, 25)
        };

        var description = new Label
        {
            Text = "Publique a aplicação no IIS com preservação das configurações locais e controle seguro do site.",
            AutoSize = false,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(221, 232, 247),
            BackColor = Color.Transparent,
            Location = new Point(102, 70),
            Width = 720,
            Height = 34
        };

        var versionPill = new Label
        {
            Text = "Deploy assistido",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(72, 145, 232),
            Location = new Point(748, 32),
            Size = new Size(126, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        header.Controls.AddRange([brandMark, title, description, versionPill]);

        var targetCard = CreateCard(new Point(28, 160), new Size(836, 132));
        targetCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        var targetTitle = CreateSectionTitle("Destino da instalação", new Point(22, 18));
        var targetHint = CreateMutedLabel("Escolha a pasta onde os arquivos publicados do DRFlowHub serão copiados.", new Point(22, 46), new Size(650, 24));

        _targetPath.Text = @"C:\inetpub\drflowhub";
        _targetPath.Location = new Point(22, 78);
        _targetPath.Width = 650;
        _targetPath.Height = 32;
        _targetPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        StyleTextBox(_targetPath);

        _browseButton.Text = "Procurar";
        _browseButton.Location = new Point(690, 76);
        _browseButton.Width = 118;
        _browseButton.Height = 36;
        _browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        StyleSecondaryButton(_browseButton);
        _browseButton.Click += BrowseButton_Click;

        targetCard.Controls.AddRange([targetTitle, targetHint, _targetPath, _browseButton]);

        var optionsCard = CreateCard(new Point(28, 312), new Size(404, 186));
        var optionsTitle = CreateSectionTitle("Opções de instalação", new Point(22, 18));

        _preserveAppSettings.Text = "Preservar appsettings.json existente";
        _preserveAppSettings.Checked = true;
        _preserveAppSettings.Location = new Point(22, 58);
        StyleCheckBox(_preserveAppSettings);

        _useAppOffline.Text = "Colocar aplicação offline temporariamente";
        _useAppOffline.Checked = true;
        _useAppOffline.Location = new Point(22, 94);
        StyleCheckBox(_useAppOffline);

        _restartIis.Text = "Parar IIS antes da cópia e reiniciar ao final";
        _restartIis.Checked = true;
        _restartIis.Location = new Point(22, 130);
        StyleCheckBox(_restartIis);

        optionsCard.Controls.AddRange([optionsTitle, _preserveAppSettings, _useAppOffline, _restartIis]);

        var iisCard = CreateCard(new Point(460, 312), new Size(404, 186));
        iisCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        var iisTitle = CreateSectionTitle("Configuração IIS", new Point(22, 18));
        var siteLabel = CreateFieldLabel("Site IIS", new Point(22, 58));
        var appPoolLabel = CreateFieldLabel("Application Pool", new Point(210, 58));

        _siteName.Text = "DrFlowHub";
        _siteName.Location = new Point(22, 84);
        _siteName.Width = 160;
        _siteName.Height = 32;
        StyleTextBox(_siteName);

        _appPoolName.Text = "DrFlowHub";
        _appPoolName.Location = new Point(210, 84);
        _appPoolName.Width = 160;
        _appPoolName.Height = 32;
        StyleTextBox(_appPoolName);

        _installButton.Text = "Instalar agora";
        _installButton.Location = new Point(210, 130);
        _installButton.Width = 160;
        _installButton.Height = 40;
        StylePrimaryButton(_installButton);
        _installButton.Click += InstallButton_Click;

        iisCard.Controls.AddRange([iisTitle, siteLabel, _siteName, appPoolLabel, _appPoolName, _installButton]);

        var progressCard = CreateCard(new Point(28, 518), new Size(836, 72));
        progressCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _statusLabel.Text = "Pronto para instalar";
        _statusLabel.AutoSize = true;
        _statusLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        _statusLabel.ForeColor = TextColor;
        _statusLabel.Location = new Point(22, 15);

        _progress.Location = new Point(22, 42);
        _progress.Width = 792;
        _progress.Height = 12;
        _progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _progress.Style = ProgressBarStyle.Continuous;

        progressCard.Controls.AddRange([_statusLabel, _progress]);

        var logCard = CreateCard(new Point(28, 610), new Size(836, 92));
        logCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        var logTitle = CreateSectionTitle("Atividade da instalação", new Point(22, 14));

        _log.Location = new Point(22, 42);
        _log.Width = 792;
        _log.Height = 34;
        _log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _log.Multiline = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.ReadOnly = true;
        _log.BorderStyle = BorderStyle.None;
        _log.BackColor = Color.FromArgb(248, 250, 253);
        _log.ForeColor = Color.FromArgb(44, 56, 70);
        _log.Font = new Font("Consolas", 9F);

        logCard.Controls.AddRange([logTitle, _log]);

        Controls.AddRange([
            header,
            targetCard,
            optionsCard,
            iisCard,
            progressCard,
            logCard
        ]);
    }

    private static RoundedPanel CreateCard(Point location, Size size)
    {
        return new RoundedPanel
        {
            Location = location,
            Size = size,
            BackColor = CardBackColor,
            BorderLineColor = BorderColor,
            Radius = 18,
            Padding = new Padding(18)
        };
    }

    private static Label CreateSectionTitle(string text, Point location)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            ForeColor = TextColor,
            Location = location
        };
    }

    private static Label CreateMutedLabel(string text, Point location, Size size)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = MutedTextColor,
            Location = location,
            Size = size
        };
    }

    private static Label CreateFieldLabel(string text, Point location)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            ForeColor = MutedTextColor,
            Location = location
        };
    }

    private static void StyleTextBox(TextBox textBox)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = Color.FromArgb(249, 251, 254);
        textBox.ForeColor = TextColor;
        textBox.Font = new Font("Segoe UI", 10F);
    }

    private static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.AutoSize = true;
        checkBox.Font = new Font("Segoe UI", 9.5F);
        checkBox.ForeColor = TextColor;
        checkBox.BackColor = Color.Transparent;
    }

    private static void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = PrimaryColor;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.MouseEnter += (_, _) => button.BackColor = PrimaryHoverColor;
        button.MouseLeave += (_, _) => button.BackColor = PrimaryColor;
    }

    private static void StyleSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = BorderColor;
        button.FlatAppearance.BorderSize = 1;
        button.BackColor = Color.White;
        button.ForeColor = PrimaryColor;
        button.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.MouseEnter += (_, _) => button.BackColor = Color.FromArgb(238, 245, 255);
        button.MouseLeave += (_, _) => button.BackColor = Color.White;
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Selecione a pasta onde o DRFlowHub será instalado",
            SelectedPath = _targetPath.Text
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _targetPath.Text = dialog.SelectedPath;
        }
    }

    private async void InstallButton_Click(object? sender, EventArgs e)
    {
        ToggleControls(false);
        _log.Clear();
        _progress.Value = 0;
        _statusLabel.Text = "Preparando instalação...";

        try
        {
            await Task.Run(Install);
            SetProgress(100);
            Log("Instalação concluída.");
            MessageBox.Show(this, "Instalação concluída com sucesso.", "DRFlowHub", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log("ERRO: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Falha na instalação", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleControls(true);
        }
    }

    private void Install()
    {
        var source = Path.GetFullPath(AppContext.BaseDirectory);
        var target = Path.GetFullPath(GetText(_targetPath));

        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidOperationException("Informe a pasta de destino.");
        }

        if (string.Equals(source.TrimEnd('\\'), target.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A pasta de destino não pode ser a mesma pasta do pacote de deploy.");
        }

        Log("Pacote: " + source);
        Log("Destino: " + target);
        Directory.CreateDirectory(target);

        SetProgress(10);
        Log("Banco de dados PostgreSQL externo: nenhum arquivo SQLite será usado.");

        SetProgress(20);
        var appOfflinePath = Path.Combine(target, "app_offline.htm");
        var createdAppOffline = false;
        var restartIis = IsChecked(_restartIis);
        var siteName = GetText(_siteName);
        var appPoolName = GetText(_appPoolName);

        try
        {
            if (restartIis)
            {
                StopIis(siteName, appPoolName);
            }

            if (IsChecked(_useAppOffline) && !File.Exists(appOfflinePath))
            {
                File.WriteAllText(appOfflinePath, "<html><body>DRFlowHub em manutenção.</body></html>");
                createdAppOffline = true;
                Log("Aplicação colocada offline temporariamente.");
                Thread.Sleep(1500);
            }

            SetProgress(35);
            CopyPackage(source, target);
            SetProgress(70);
            RunMigrationsIfNeeded(source, target);
            SetProgress(95);
        }
        finally
        {
            if (createdAppOffline && File.Exists(appOfflinePath))
            {
                try
                {
                    File.Delete(appOfflinePath);
                    Log("Aplicação retirada do modo offline.");
                }
                catch (Exception ex)
                {
                    Log("Não foi possível remover app_offline.htm: " + ex.Message);
                }
            }

            if (restartIis)
            {
                StartIis(siteName, appPoolName);
            }
        }
    }

    private void CopyPackage(string source, string target)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Install-DRFlowHub-GUI.exe",
            "Install-DRFlowHub-GUI.dll",
            "Install-DRFlowHub-GUI.deps.json",
            "Install-DRFlowHub-GUI.runtimeconfig.json",
            "Install-DRFlowHub-GUI.pdb",
            "Install-DRFlowHub.cmd",
            "Install-DRFlowHub.ps1"
        };

        if (IsChecked(_preserveAppSettings) && File.Exists(Path.Combine(target, "appsettings.json")))
        {
            excluded.Add("appsettings.json");
            Log("Preservando appsettings.json existente.");
        }

        foreach (var entry in Directory.GetFileSystemEntries(source))
        {
            var name = Path.GetFileName(entry);
            if (excluded.Contains(name))
            {
                Log("Preservando: " + name);
                continue;
            }

            var destination = Path.Combine(target, name);
            if (Directory.Exists(entry))
            {
                if (string.Equals(name, "wwwroot", StringComparison.OrdinalIgnoreCase) && Directory.Exists(destination))
                {
                    Log("Limpando wwwroot antigo...");
                    Directory.Delete(destination, recursive: true);
                }

                CopyDirectory(entry, destination);
            }
            else
            {
                File.Copy(entry, destination, overwrite: true);
            }
        }

        Log("Arquivos copiados.");
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }

    private void RunMigrationsIfNeeded(string source, string target)
    {
        var migrationsDir = Path.Combine(source, "migrations");
        var hasMigrations = Directory.Exists(migrationsDir) && Directory.GetFiles(migrationsDir, "*.sql").Length > 0;
        if (!hasMigrations)
        {
            Log("Nenhuma migration encontrada no pacote.");
            return;
        }

        var exePath = Path.Combine(target, "DRFlowHub.exe");
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException("DRFlowHub.exe não foi encontrado no destino.");
        }

        Log("Aplicando migrations pendentes...");
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = "--migrate",
            WorkingDirectory = target,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Não foi possível iniciar DRFlowHub.exe --migrate.");
        process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("Falha ao aplicar migrations. Código: " + process.ExitCode);
        }

        Log("Migrations aplicadas com sucesso.");
    }

    private void StopIis(string siteName, string appPoolName)
    {
        if (!string.IsNullOrWhiteSpace(siteName))
        {
            Log("Parando site IIS: " + siteName);
            RunAppCmd(["stop", "site", "/site.name:" + siteName], "Não foi possível parar o site. Execute como Administrador ou confira o nome do site.");
        }

        if (!string.IsNullOrWhiteSpace(appPoolName))
        {
            Log("Parando App Pool IIS: " + appPoolName);
            RunAppCmd(["stop", "apppool", "/apppool.name:" + appPoolName], "Não foi possível parar o App Pool. Execute como Administrador ou confira o nome do App Pool.");
        }
    }

    private void StartIis(string siteName, string appPoolName)
    {
        if (!string.IsNullOrWhiteSpace(appPoolName))
        {
            Log("Iniciando App Pool IIS: " + appPoolName);
            RunAppCmd(["start", "apppool", "/apppool.name:" + appPoolName], "Não foi possível iniciar o App Pool. Inicie manualmente pelo IIS.");
        }

        if (!string.IsNullOrWhiteSpace(siteName))
        {
            Log("Iniciando site IIS: " + siteName);
            RunAppCmd(["start", "site", "/site.name:" + siteName], "Não foi possível iniciar o site. Inicie manualmente pelo IIS.");
        }
    }

    private void RunAppCmd(string[] arguments, string warning)
    {
        var appCmd = Path.Combine(Environment.SystemDirectory, "inetsrv", "appcmd.exe");
        if (!File.Exists(appCmd))
        {
            Log("appcmd.exe não encontrado. Pulando controle do IIS.");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = appCmd,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            Log(warning);
            return;
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(output))
        {
            Log(output.Trim());
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            Log(error.Trim());
        }

        if (process.ExitCode != 0)
        {
            Log(warning);
        }
    }

    private void ToggleControls(bool enabled)
    {
        if (InvokeRequired)
        {
            Invoke(() => ToggleControls(enabled));
            return;
        }

        _targetPath.Enabled = enabled;
        _browseButton.Enabled = enabled;
        _preserveAppSettings.Enabled = enabled;
        _useAppOffline.Enabled = enabled;
        _restartIis.Enabled = enabled;
        _siteName.Enabled = enabled;
        _appPoolName.Enabled = enabled;
        _installButton.Enabled = enabled;
        _statusLabel.Text = enabled ? "Pronto para instalar" : "Instalação em andamento...";
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(message));
            return;
        }

        _statusLabel.Text = message;
        _log.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
    }

    private void SetProgress(int value)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetProgress(value));
            return;
        }

        _progress.Value = Math.Clamp(value, _progress.Minimum, _progress.Maximum);
    }

    private static string GetText(TextBox textBox)
    {
        if (textBox.InvokeRequired)
        {
            return (string)textBox.Invoke(() => textBox.Text);
        }

        return textBox.Text;
    }

    private static bool IsChecked(CheckBox checkBox)
    {
        if (checkBox.InvokeRequired)
        {
            return (bool)checkBox.Invoke(() => checkBox.Checked);
        }

        return checkBox.Checked;
    }

    private sealed class HeaderPanel : Panel
    {
        public HeaderPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var brush = new LinearGradientBrush(ClientRectangle, HeaderStartColor, HeaderEndColor, 0F);
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }

    private sealed class RoundedPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderLineColor { get; set; } = InstallerForm.BorderColor;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Radius { get; set; } = 16;

        public RoundedPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            using var path = CreateRoundedRectanglePath(ClientRectangle, Radius);
            Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundedRectanglePath(ClientRectangle, Radius);
            using var brush = new SolidBrush(BackColor);
            using var pen = new Pen(BorderLineColor);
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var rectangle = new Rectangle(bounds.Location, new Size(diameter, diameter));
            var path = new GraphicsPath();

            path.AddArc(rectangle, 180, 90);
            rectangle.X = bounds.Right - diameter - 1;
            path.AddArc(rectangle, 270, 90);
            rectangle.Y = bounds.Bottom - diameter - 1;
            path.AddArc(rectangle, 0, 90);
            rectangle.X = bounds.Left;
            path.AddArc(rectangle, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
