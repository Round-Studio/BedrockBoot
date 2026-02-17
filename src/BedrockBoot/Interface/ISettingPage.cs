using System.Collections.Generic;
using OnePointUI.Avalonia.Base.Entry;

namespace BedrockBoot.Interface;

public class ISettingPage : ISetting
{
    public List<BreadcrumbItemInfo> BreadcrumbItem { get; set; }
    public static I18nManager i18n => I18nManager.Instance;
}