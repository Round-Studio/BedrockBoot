using BedrockBoot.Base.Enum;

namespace BedrockBoot.Base.Helper;

public class IsolationPolicyHelper
{
    public static CatalogStrategyEnum PublicCatalogStrategy { get; set; }
    public static CatalogStrategyEnum ParsePolicyConfig(CatalogStrategyEnum strategy)
    {
        return strategy switch
        {
            CatalogStrategyEnum.Independence => CatalogStrategyEnum.Independence,
            CatalogStrategyEnum.Shares => CatalogStrategyEnum.Shares,
            CatalogStrategyEnum.FollowTheBigPicture => PublicCatalogStrategy
        };
    }
}