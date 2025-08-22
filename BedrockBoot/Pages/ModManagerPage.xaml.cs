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
        public bool delayInject = false;
        public string mods_dir => Path.Combine(_selectedVersion.Version_Path, "mods");
        public string delay_mods_dir => Path.Combine(_selectedVersion.Version_Path, "d_mods");

        public object deleteLock;
        // 添加构造函数接收版本参数
        public ModManagerPage(NowVersions version,bool delayinject)
        {
            this.delayInject = delayinject;
            this.InitializeComponent();
            _selectedVersion = version;
            if (!Directory.Exists(mods_dir))
            {
                Directory.CreateDirectory(mods_dir);
            }
            if (!Directory.Exists(delay_mods_dir))
            {
                Directory.CreateDirectory(delay_mods_dir);
            }
            LoadMods(delayinject);
        }
        public void LoadMods(bool delayInject)
        {
            Manager.ModsList.Clear();
            TaskContainer.Children.Clear();
            List<DllFileInfo> dllFileInfos;
            if (delayInject != true)
            {
                 dllFileInfos = globalTools.GetDllFiles(mods_dir);
            }
            else
            {
                dllFileInfos = globalTools.GetDllFiles(delay_mods_dir);
            }
            
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
            if (!delayInject)
            {
                var storageFolder = await StorageFolder.GetFolderFromPathAsync(mods_dir);
                await Launcher.LaunchFolderAsync(storageFolder);
            }
            else
            {
                var storageFolder = await StorageFolder.GetFolderFromPathAsync(delay_mods_dir);
                await Launcher.LaunchFolderAsync(storageFolder);
            }
        }

        private void ButtonBaseLeft_OnClick(object sender, RoutedEventArgs e)
        {
           LoadMods(delayInject);
        }
    }
}