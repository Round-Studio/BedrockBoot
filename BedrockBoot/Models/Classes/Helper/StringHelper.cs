namespace BedrockBoot.Models.Classes.Helper;

public class StringHelper
{
    public static string RemoveEscapeCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace("\\n", "")   // 移除换行符
            .Replace("\\r", "")   // 移除回车符
            .Replace("\\t", "")   // 移除制表符
            .Replace("\\\"", "\"") // 将转义的双引号还原
            .Replace("\\'", "'")  // 将转义的单引号还原
            .Replace("\\\\", "\\"); // 将转义的反斜杠还原
    }
}