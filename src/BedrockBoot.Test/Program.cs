using BedrockBoot.Dependence;
using BedrockBoot.Netease.Utils;

File.WriteAllBytes("XOREncryptDLL.dll", Dependence.GetResource("BedrockBoot.Dependence.Dependence.XOREncryptDLL.dll"));
var savePath = "E:\\Netease_Bedrock\\TestWorld";

Console.WriteLine(LevelDbEncryptHelper.DecryptRecord(savePath));