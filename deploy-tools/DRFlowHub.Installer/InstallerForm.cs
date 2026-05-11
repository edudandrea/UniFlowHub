using System.Diagnostics;

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

    public InstallerForm()
    {
        Text = "Instalador DRFlowHub";
        Width = 760;
        Height = 660;
        MinimumSize = new Size(720, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F);

        var title = new Label
        {
            Text = "Instalação do DRFlowHub",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 16F, FontStyle.Bold),
            Location = new Point(24, 20)
        };

        var description = new Label
        {
            Text = "Escolha a pasta de destino. O instalador publica a aplicação e preserva o appsettings.json existente quando solicitado.",
            AutoSize = false,
            Location = new Point(24, 58),
            Width = 690,
            Height = 42
        };

        var targetLabel = new Label
        {
            Text = "Pasta de destino",
            AutoSize = true,
            Location = new Point(24, 112)
        };

        _targetPath.Text = @"C:\inetpub\drflowhub";
        _targetPath.Location = new Point(24, 138);
        _targetPath.Width = 570;

        _browseButton.Text = "Procurar...";
        _browseButton.Location = new Point(606, 136);
        _browseButton.Width = 110;
        _browseButton.Click += BrowseButton_Click;

        _preserveAppSettings.Text = "Preservar appsettings.json existente";
        _preserveAppSettings.Checked = true;
        _preserveAppSettings.AutoSize = true;
        _preserveAppSettings.Location = new Point(24, 180);

        _useAppOffline.Text = "Colocar aplicacao offline durante a instalacao";
        _useAppOffline.Checked = true;
        _useAppOffline.AutoSize = true;
        _useAppOffline.Location = new Point(24, 212);

        _restartIis.Text = "Parar IIS antes da copia e reiniciar ao final";
        _restartIis.Checked = true;
        _restartIis.AutoSize = true;
        _restartIis.Location = new Point(24, 244);

        var siteLabel = new Label
        {
            Text = "Site IIS",
            AutoSize = true,
            Location = new Point(44, 282)
        };

        _siteName.Text = "DrFlowHub";
        _siteName.Location = new Point(112, 278);
        _siteName.Width = 210;

        var appPoolLabel = new Label
        {
            Text = "App Pool",
            AutoSize = true,
            Location = new Point(344, 282)
        };

        _appPoolName.Text = "DrFlowHub";
        _appPoolName.Location = new Point(424, 278);
        _appPoolName.Width = 210;

        _installButton.Text = "Instalar";
        _installButton.Location = new Point(606, 318);
        _installButton.Width = 110;
        _installButton.Height = 36;
        _installButton.Click += InstallButton_Click;

        _progress.Location = new Point(24, 372);
        _progress.Width = 692;
        _progress.Height = 18;
        _progress.Style = ProgressBarStyle.Continuous;

        _log.Location = new Point(24, 406);
        _log.Width = 692;
        _log.Height = 210;
        _log.Multiline = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.ReadOnly = true;

        Controls.AddRange([
            title,
            description,
            targetLabel,
            _targetPath,
            _browseButton,
            _preserveAppSettings,
            _useAppOffline,
            _restartIis,
            siteLabel,
            _siteName,
            appPoolLabel,
            _appPoolName,
            _installButton,
            _progress,
            _log
        ]);
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Selecione a pasta onde o DRFlowHub sera instalado",
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

        try
        {
            await Task.Run(Install);
            SetProgress(100);
            Log("Instalacao concluida.");
            MessageBox.Show(this, "Instalacao concluida com sucesso.", "DRFlowHub", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log("ERRO: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Falha na instalacao", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            throw new InvalidOperationException("A pasta de destino nao pode ser a mesma pasta do pacote de deploy.");
        }

        Log("Pacote: " + source);
        Log("Destino: " + target);
        Directory.CreateDirectory(target);

        SetProgress(10);
        Log("Banco de dados PostgreSQL externo: nenhum arquivo SQLite sera usado.");

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
                File.WriteAllText(appOfflinePath, "<html><body>DRFlowHub em manutencao.</body></html>");
                createdAppOffline = true;
                Log("Aplicacao colocada offline temporariamente.");
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
                    Log("Aplicacao retirada do modo offline.");
                }
                catch (Exception ex)
                {
                    Log("Nao foi possivel remover app_offline.htm: " + ex.Message);
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
            throw new FileNotFoundException("DRFlowHub.exe nao foi encontrado no destino.");
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

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Nao foi possivel iniciar DRFlowHub.exe --migrate.");
        process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("Falha ao aplicar migrations. Codigo: " + process.ExitCode);
        }

        Log("Migrations aplicadas com sucesso.");
    }

    private void StopIis(string siteName, string appPoolName)
    {
        if (!string.IsNullOrWhiteSpace(siteName))
        {
            Log("Parando site IIS: " + siteName);
            RunAppCmd(["stop", "site", "/site.name:" + siteName], "Nao foi possivel parar o site. Execute como Administrador ou confira o nome do site.");
        }

        if (!string.IsNullOrWhiteSpace(appPoolName))
        {
            Log("Parando App Pool IIS: " + appPoolName);
            RunAppCmd(["stop", "apppool", "/apppool.name:" + appPoolName], "Nao foi possivel parar o App Pool. Execute como Administrador ou confira o nome do App Pool.");
        }
    }

    private void StartIis(string siteName, string appPoolName)
    {
        if (!string.IsNullOrWhiteSpace(appPoolName))
        {
            Log("Iniciando App Pool IIS: " + appPoolName);
            RunAppCmd(["start", "apppool", "/apppool.name:" + appPoolName], "Nao foi possivel iniciar o App Pool. Inicie manualmente pelo IIS.");
        }

        if (!string.IsNullOrWhiteSpace(siteName))
        {
            Log("Iniciando site IIS: " + siteName);
            RunAppCmd(["start", "site", "/site.name:" + siteName], "Nao foi possivel iniciar o site. Inicie manualmente pelo IIS.");
        }
    }

    private void RunAppCmd(string[] arguments, string warning)
    {
        var appCmd = Path.Combine(Environment.SystemDirectory, "inetsrv", "appcmd.exe");
        if (!File.Exists(appCmd))
        {
            Log("appcmd.exe nao encontrado. Pulando controle do IIS.");
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
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => Log(message));
            return;
        }

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
}
