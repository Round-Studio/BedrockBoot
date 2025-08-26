using BedrockBoot.Models.Entry.Pack;
using BedrockLauncher.Core.JsonHandle;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using BedrockBoot.Models.Classes.Helper.Pack;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BedrockBoot.Pages.ManagerPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ManagerResourcePacksPage : Page
    {
        public ObservableCollection<ResourcePackManifestEntry> PackItems { get; set; } = new();
        public ManagerResourcePacksPage()
        {
            InitializeComponent();

            ResourcePackReader.ReadAnyResourcePacks().ForEach(x => { PackItems.Add(x); });
        }
    }
}
