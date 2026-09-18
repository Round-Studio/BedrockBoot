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

using System.Text;
using System.Text.Json;
using BedrockBoot.Base.Entry.Info.News;
using BedrockBoot.Base.Enum.News;

namespace BedrockBoot.Core.Models.News;

public class NewsGenerate
{
    public static int LineCount { get; } = 3;
    public static NewsItemType GetRandomNewsItemType() => (NewsItemType)new Random().Next(0, 2);
    public static List<NewsItemType> GetRandomLine()
    {
        var result = new List<NewsItemType>();
        var random = new Random();
        var remainingSlots = LineCount;

        while (remainingSlots > 0)
        {
            NewsItemType selectedType;

            if (remainingSlots >= 3) selectedType = (NewsItemType)random.Next(0, 3);
            else if (remainingSlots == 2)
                selectedType = random.Next(0, 2) == 0 ? NewsItemType.Medium : NewsItemType.Small;
            else selectedType = NewsItemType.Small;

            switch (selectedType)
            {
                case NewsItemType.Big:
                    result.Add(NewsItemType.Big);
                    remainingSlots -= 3;
                    break;
                case NewsItemType.Medium:
                    result.Add(NewsItemType.Medium);
                    remainingSlots -= 2;
                    break;
                case NewsItemType.Small:
                    result.Add(NewsItemType.Small);
                    remainingSlots -= 1;
                    break;
            }
        }

        return result;
    }
    public static async Task<List<MojangNewsManifest.PatchNoteEntry>> GetPatchNotesAsync(string url)
    {
        using var client = new HttpClient();
    
        // 发送请求
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
    
        // 读取并反序列化
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<MojangNewsManifest>(json);
    
        return result?.Entries ?? new List<MojangNewsManifest.PatchNoteEntry>();
    }
}