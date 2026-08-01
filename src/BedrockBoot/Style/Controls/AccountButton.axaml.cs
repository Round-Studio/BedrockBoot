using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using BedrockBoot.Views.Control.Widgets;

namespace BedrockBoot.Style.Controls;

public class AccountButton : Button
{
    private LocalImageRenderWidget? _headerImage;
    private TextBlock? _accountName;

    public static readonly StyledProperty<string?> HeaderImageUrlProperty =
        AvaloniaProperty.Register<AccountButton, string?>(nameof(HeaderImageUrl));

    public string? HeaderImageUrl
    {
        get => GetValue(HeaderImageUrlProperty);
        set => SetValue(HeaderImageUrlProperty, value);
    }

    public static readonly StyledProperty<string?> AccountNameProperty =
        AvaloniaProperty.Register<AccountButton, string?>(nameof(AccountName));

    public string? AccountName
    {
        get => GetValue(AccountNameProperty);
        set => SetValue(AccountNameProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _headerImage = e.NameScope.Find<LocalImageRenderWidget>("PART_HeaderImage");
        _accountName = e.NameScope.Find<TextBlock>("PART_AccountName");

        UpdateHeaderImage();
        UpdateAccountName();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HeaderImageUrlProperty)
        {
            UpdateHeaderImage();
        }
        else if (change.Property == AccountNameProperty)
        {
            UpdateAccountName();
        }
    }

    private void UpdateHeaderImage()
    {
        if (_headerImage != null && !string.IsNullOrEmpty(HeaderImageUrl))
        {
            _headerImage.ImageUrl = HeaderImageUrl;
        }
    }

    private void UpdateAccountName()
    {
        if (_accountName != null)
        {
            _accountName.Text = AccountName ?? "未登录";
        }
    }
}