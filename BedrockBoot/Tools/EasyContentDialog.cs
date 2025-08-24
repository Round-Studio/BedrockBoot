using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Tools
{
    internal class EasyContentDialog
    {
        public static async void CreateDialog(
            XamlRoot root,
            string title, 
            string content, 
            string primaryButtonText,
            string secondaryButtonText, 
            string closeButtonText)
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = root;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = title;
            dialog.PrimaryButtonText = primaryButtonText;
            dialog.SecondaryButtonText = secondaryButtonText;
            dialog.CloseButtonText = closeButtonText;
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = new TextBlock() { Text= content };

            var result = await dialog.ShowAsync();

        }
        public static async void CreateDialog(
            XamlRoot root,
            string title,
            string content,
            string primaryButtonText,
            string closeButtonText)
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = root;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = title;
            dialog.PrimaryButtonText = primaryButtonText;
            dialog.CloseButtonText = closeButtonText;
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = new TextBlock() { Text = content };

            var result = await dialog.ShowAsync();

        }
        public static async void CreateDialog(
            XamlRoot root,
            string title,
            string content,
            string closeButtonText)
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = root;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = title;
            dialog.CloseButtonText = closeButtonText;
            dialog.DefaultButton = ContentDialogButton.Close;
            dialog.Content = new TextBlock() { Text = content };

            var result = await dialog.ShowAsync();

        }

        public static async void CreateDialog(
            XamlRoot root,
            string title,
            string content)
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = root;
            dialog.Title = title;
            dialog.Content = new TextBlock() { Text = content,TextTrimming = TextTrimming.WordEllipsis };
            dialog.CloseButtonText = "确定";
            dialog.DefaultButton = ContentDialogButton.Close;

            try
            {
                dialog.ShowAsync();
            }
            catch
            {
                MessageBox.ShowAsync(content, title);
            }
        }
    }
}
