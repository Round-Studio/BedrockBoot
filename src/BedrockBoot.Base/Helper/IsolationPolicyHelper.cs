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
            CatalogStrategyEnum.FollowTheBigPicture => ParsePolicyConfig(PublicCatalogStrategy)
        };
    }
}