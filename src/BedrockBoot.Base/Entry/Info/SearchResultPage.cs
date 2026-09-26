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
using System.Collections.Generic;

namespace BedrockBoot.Base.Entry.Info;

public class SearchResultPage
{
    public List<SearchResultItemInfo> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public static SearchResultPage Empty => new();

    public int GetTotalPages(int pageSize)
    {
        if (pageSize <= 0) return 0;
        return (int)Math.Ceiling((double)Math.Max(TotalCount, 0) / pageSize);
    }
}