using BedrockBoot.Base.Enum;

namespace BedrockBoot.Base.Helper;

public class IsolationPolicyHelper
{
    public static CatalogStrategyEnum PublicCatalogStrategy { get; set; }
    public static string ParsePolicyConfig(CatalogStrategyEnum strategy)
    {
        return strategy switch
        {
            CatalogStrategyEnum.Independence => "independence",
            CatalogStrategyEnum.Shares => "shares",
            // 全局值同为 FollowTheBigPicture 时回退到默认值，避免无限递归
            CatalogStrategyEnum.FollowTheBigPicture => PublicCatalogStrategy == CatalogStrategyEnum.FollowTheBigPicture
                ? "independence"
                : ParsePolicyConfig(PublicCatalogStrategy),
            _ => "independence"
        };
    }
}