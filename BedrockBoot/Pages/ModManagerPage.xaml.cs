using BedrockBoot.Versions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using Windows.System;

namespace BedrockBoot.Pages
{
    public sealed partial class ModManagerPage : Page
    {
        private NowVersions _selectedVersion;
        public  ModManager Manager = ModManager.Instance;

        public string mods_dir => Path.Combine(_selectedVersion.Version_Path, "mods");
        // 添加构造函数接收版本参数
        public ModManagerPage(NowVersions version)
        {
            this.InitializeComponent();
            _selectedVersion = version;
            BreadcrumbBar.Text=$"Mods管理{_selectedVersion.DisPlayName}";
            if (!Directory.Exists(mods_dir))
            {
                Directory.CreateDirectory(mods_dir);
            }

           LoadMods();
        }

        public void LoadMods()
        {
            Manager.ModsList.Clear();
            TaskContainer.Children.Clear();
            var dllFileInfos = globalTools.GetDllFiles(mods_dir);

            Button newButton(DllFileInfo info, SettingsExpander data)
            {
                var button = new Button();
                button.Content = "删除mod";
                button.Click += (s, e) =>
                {
                    var removeMod = Manager.RemoveMod(info);
                    if (removeMod != true)
                    {
                        globalTools.ShowInfo("删除失败");
                    }
                    else
                    {
                        globalTools.ShowInfo("删除成功");
                        TaskContainer.Children.Remove(data);
                    }
                };
                return button;
            }
            foreach (var dllFileInfo in dllFileInfos)
            {
                var settingsExpander = new SettingsExpander()
                {
                    Margin = new Thickness(20),
                    Description = dllFileInfo.FullPath,
                    Header = dllFileInfo.FileName,
                    IsExpanded = false,
                    HeaderIcon = new FontIcon() { Glyph = "&#xEA37;" },
                };
                settingsExpander.Items = new List<object>()
                {
                    new SettingsCard()
                    {
                        Header = "删除mod",
                        Content = newButton(dllFileInfo, settingsExpander)
                    }
                };
                TaskContainer.Children.Add(settingsExpander);
                Manager.ModsList.Add(dllFileInfo);
            }
        }
        // 无参构造函数（如果需要）
        public ModManagerPage()
        {
            this.InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var storageFolder = await  StorageFolder.GetFolderFromPathAsync(mods_dir); 
           await Launcher.LaunchFolderAsync(storageFolder);
        }

        private void ButtonBaseLeft_OnClick(object sender, RoutedEventArgs e)
        {
           LoadMods();
        }
    }
}