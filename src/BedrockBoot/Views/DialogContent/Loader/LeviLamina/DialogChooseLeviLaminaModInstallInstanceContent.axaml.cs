using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Core.Global;
using BedrockBoot.Core.Models.Helper;
using BedrockBoot.Models.Pack.LeviLamina;

namespace BedrockBoot.Views.DialogContent.Loader.LeviLamina;

public partial class DialogChooseLeviLaminaModInstallInstanceContent : UserControl
{
    private readonly PackageInfo _pkg;
    private readonly string _versionId;
    private readonly VersionInfo _version;
    public bool IsOnlyDownload => DownloadType.SelectedIndex == 1;

    public string? SavePath => IsOnlyDownload ? _selectPath :
        _ableInstance.Count > 0 ? _ableInstance[ComboBox.SelectedIndex].conf.VersionPath : string.Empty;

    private string? _selectPath;
    private List<(VersionConfig conf, string llVersion)> _ableInstance;

    public DialogChooseLeviLaminaModInstallInstanceContent()
    {
        InitializeComponent();
    }

    public DialogChooseLeviLaminaModInstallInstanceContent(PackageInfo pkg, string versionId, VersionInfo version) :
        this()
    {
        _pkg = pkg;
        _versionId = versionId;
        _version = version;
        UpdateUi();
    }

    public void UpdateUi()
    {
        DepsList.IsVisible = _version.Dependencies.Count > 0;
        _version.Dependencies.ToList().ForEach(dep =>
        {
            DepsList.Children.Add(new TextBlock() { Text = $"{dep.Key.Split("/")[2]} {dep.Value}" });
        });
        var nowChooseFolder = GlobalModel.Config.Data.GameFolders[GlobalModel.Config.Data.GameFolderSelIndex]
            .GameFolderPath;
        var instances = GameInfoHelper.GetVersionConfigs(nowChooseFolder);
        _ableInstance = new List<(VersionConfig conf, string llVersion)>();
        instances.ForEach(instance =>
        {
            var loader = new Models.Pack.Game.Loaders.LoaderInstance.LeviLamina();
            loader.InitLoader(instance);

            if (loader.IsInstalled())
                _ableInstance.Add((instance, loader.GetInstalledVersion()));
        });

        ComboBox.ItemsSource = _ableInstance.Select(x =>
            $"{x.conf.Info.VersionName} ({x.conf.Info.Version}) - LeviLamina {x.llVersion}");
        NotFound.IsVisible = _ableInstance.Count <= 0;
        ComboBox.IsVisible = _ableInstance.Count > 0;
        if (_ableInstance.Count > 0)
        {
            ComboBox.SelectedIndex = 0;
        }
    }

    private async void SelectFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(Models.Global.GlobalModel.MainWindow);
        if (topLevel == null)
            return;

        var storageProvider = topLevel.StorageProvider;

        var options = new FilePickerSaveOptions
        {
            Title = "保存 ZIP 文件",
            SuggestedFileName = $"{_pkg.Info.Name}-{_versionId}.zip",
            DefaultExtension = "zip",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("ZIP 压缩文件")
                {
                    Patterns = new[] { "*.zip" }
                }
            }
        };

        var file = await storageProvider.SaveFilePickerAsync(options);
        _selectPath = file?.TryGetLocalPath();
    }
}