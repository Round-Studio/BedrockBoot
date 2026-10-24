/*
 * BedrockBoot - A launcher for Minecraft Bedrock Edition.
 * Copyright (C) 2025-2026 Round-Studio
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;

namespace BedrockBoot.Models.Helper;

public static class FuzzyMatchHelper
{
    public static bool IsFuzzyMatch(string source, string keyword, double threshold = 0.7)
    {
        if (string.IsNullOrEmpty(keyword)) return true;
        if (string.IsNullOrEmpty(source)) return false;

        var sourceLower = source.ToLower();
        var keywordLower = keyword.ToLower();

        if (sourceLower.Contains(keywordLower)) return true;

        var distance = CalculateLevenshteinDistance(sourceLower, keywordLower);
        var maxLength = Math.Max(sourceLower.Length, keywordLower.Length);
        var similarity = 1.0 - (double)distance / maxLength;

        return similarity >= threshold;
    }

    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return string.IsNullOrEmpty(target) ? 0 : target.Length;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;

        var matrix = new int[sourceLength + 1, targetLength + 1];

        for (var i = 0; i <= sourceLength; matrix[i, 0] = i++)
        {
        }

        for (var j = 0; j <= targetLength; matrix[0, j] = j++)
        {
        }

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[sourceLength, targetLength];
    }

    public static string ExtractNumbers(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return new string(input.Where(char.IsDigit).ToArray());
    }

    public static string RemoveAllZeros(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Replace("0", string.Empty);
    }

    public static string ProcessMinecraftVersionForFuzzy(string version)
    {
        if (string.IsNullOrEmpty(version)) return string.Empty;
        var numbers = ExtractNumbers(version);
        return RemoveAllZeros(numbers);
    }
}