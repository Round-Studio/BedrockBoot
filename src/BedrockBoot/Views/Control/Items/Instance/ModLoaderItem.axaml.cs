using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BedrockBoot.Base.Entry.Game;
using BedrockBoot.Interface;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Views.Control.Items.Instance;

public partial class ModLoaderItem : UserControl
{
    private ImageLoader? _imageLoader = new ImageLoader();
    private readonly VersionConfig _instance;
    private readonly IModsLoader _loader;

    public ModLoaderItem()
    {
        InitializeComponent();
    }

    public ModLoaderItem(VersionConfig instance, IModsLoader loader) : this()
    {
        _instance = instance;
        _loader = loader;
        _ = UpdateUi();
    }

    public async Task UpdateUi()
    {
        _loader.InitLoader(_instance);

        LoaderName.Text = _loader.LoaderName;
        LoaderCard.Description = _loader.LoaderDescription;
        LoaderInstallStatus.Text = !_loader.IsInstalled() ? "未安装" : _loader.GetInstalledVersion();
        if (!string.IsNullOrEmpty(_loader.IconUri))
        {
            LoaderCard.IsFontIcon = false;
            LoaderCard.ImageIcon = await _imageLoader!.LoadIconAsync(_loader.IconUri);
        }

        if (!_loader.IsInstalled())
        {
            var isActInstance = await _loader.ApplicableInstance();
            LoadingRing.IsVisible = false;
            NotApplicableLabel.IsVisible = !isActInstance;
            if (isActInstance)
            {
                LoaderCard.Click += (_, _) => _loader.Install();
            }
        }
        else
        {
            DeleteBtn.IsVisible = _loader.CanRemove;
            LoadingRing.IsVisible = false;
            DeleteBtn.Click += (_, _) => _loader.Remove();
            LoaderCard.Click += (_, _) => _loader.ViewInfo();
        }
    }
}