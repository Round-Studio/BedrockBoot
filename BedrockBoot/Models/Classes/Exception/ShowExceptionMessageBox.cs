using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using static System.Net.Mime.MediaTypeNames;

namespace BedrockBoot.Models.Classes.Exception
{
    public class ShowExceptionMessageBox
    {
        public static async void ShowException(string mesage,string exception = null)
        {
            MessageBox.ShowAsync($"{mesage}\n{exception}","错误", MessageBoxButtons.OK);
        }
    }
}
