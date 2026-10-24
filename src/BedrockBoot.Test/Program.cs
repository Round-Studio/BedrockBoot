using BedrockBoot.Models.Pack.Plugin.Develop;
using BedrockBoot.Standard.Entity.Info.Develop;

var core = new DevelopCore(new ProjectInfo()
{
    ProjectPath = @"I:\TestPlugin",
    ProjectName = "My First Plugin"
});

core.CreatePluginProject();