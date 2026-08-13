using BedrockBoot.Base.Entry.Info.Develop;
using BedrockBoot.Models.Pack.Plugin.Develop;

var core = new DevelopCore(new ProjectInfo()
{
    ProjectPath = @"I:\TestPlugin",
    ProjectName = "My First Plugin"
});

core.CreatePluginProject();