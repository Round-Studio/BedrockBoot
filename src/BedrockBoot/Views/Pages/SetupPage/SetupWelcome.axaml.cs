using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace BedrockBoot.Views.Pages.SetupPage;

public partial class SetupWelcome : UserControl
{
    public SetupWelcome()
    {
        InitializeComponent();

        Task.Run(() =>
        {
            Thread.Sleep(4800);
            SetupRoot.Instance!.ShowNextBtn();
        });
    }
}