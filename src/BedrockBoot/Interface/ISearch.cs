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

using System.Collections.Generic;
using System.Threading.Tasks;
using BedrockBoot.Base.Entry.Info;
using BedrockBoot.Base.Enum.Search;

namespace BedrockBoot.Interface;

public interface ISearch
{
    Task<List<SearchResultItemInfo>> SearchAsync(string keyword);
    Task<List<SearchResultItemInfo>> SearchAsync(string keyword, int page, int pageSize);
    Task<List<SearchResultItemInfo>> GetRecommendAsync(int count = 2);
    SearchResourceType SearchType { get; }
    void SetExtraParameter(object parameter);
}