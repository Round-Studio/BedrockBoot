using BedrockBoot.Controls.ListItem;
using BedrockBoot.Versions;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Preview.GamesEnumeration;
using Windows.Gaming.XboxLive.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Controls.FlyoutContent
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EazyChooseGameContent : Page
    {
        public bool IsEdit = false;
        public EazyChooseGameContent()
        {
            InitializeComponent();

            global_cfg.cfg.JsonCfg.GameFolders.ForEach(x => FolderChooseBox.Items.Add(new ComboBoxItem() { Content = $"{x.Name} - {x.Path}" }));
            FolderChooseBox.SelectedIndex = global_cfg.cfg.JsonCfg.ChooseFolderIndex;
            UpdateUI();
        }

        private void FolderChooseBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsEdit) {
                global_cfg.cfg.JsonCfg.ChooseFolderIndex = FolderChooseBox.SelectedIndex;
                UpdateUI();
            }
        }
        public void UpdateUI()
        {
            IsEdit = false;

            GamesList.Items.Clear();
            List<string> games = new List<string>();
            globalTools.SearchVersionJson(global_cfg.cfg.JsonCfg.GameFolders[global_cfg.cfg.JsonCfg.ChooseFolderIndex].Path, ref games, 0, 2);

            games.ForEach(x =>
            {
                var entry = globalTools.GetJsonFileEntry<NowVersions>(x);

                GamesList.Items.Add(new GameVersionItem()
                {
                    VersionRealName = entry.RealVersion,
                    VersionName = entry.VersionName
                });
            });
            GamesList.SelectedIndex = global_cfg.cfg.JsonCfg.GameFolders[global_cfg.cfg.JsonCfg.ChooseFolderIndex].SelectVersionIndex;

            IsEdit = true;
        }

        private void GamesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsEdit)
            {
                global_cfg.cfg.JsonCfg.GameFolders[global_cfg.cfg.JsonCfg.ChooseFolderIndex].SelectVersionIndex = GamesList.SelectedIndex;
            }
        }
    }
}
