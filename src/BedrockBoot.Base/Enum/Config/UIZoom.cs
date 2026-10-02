namespace BedrockBoot.Base.Enum;

public enum UIZoom
{
    Percent75,
    Percent100,
    Percent125,
    Percent150,
    Percent200,
    Percent500
}

public static class UIZoomExtensions
{
    public static double ToScale(this UIZoom zoom) => zoom switch
    {
        UIZoom.Percent75 => 0.75,
        UIZoom.Percent100 => 1.00,
        UIZoom.Percent125 => 1.25,
        UIZoom.Percent150 => 1.50,
        UIZoom.Percent200 => 2.00,
        UIZoom.Percent500 => 5.00,
        _ => 1.00
    };

    public static string ToDisplayText(this UIZoom zoom) => zoom switch
    {
        UIZoom.Percent75 => "75%",
        UIZoom.Percent100 => "100%",
        UIZoom.Percent125 => "125%",
        UIZoom.Percent150 => "150%",
        UIZoom.Percent200 => "200%",
        UIZoom.Percent500 => "500%",
        _ => "100%"
    };
}