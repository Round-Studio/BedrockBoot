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

namespace BedrockBoot.Helpers;

public static class DateHelper
{
    /// <summary>
    ///     计算相对时间（如：1天前，1月前，1年前）
    /// </summary>
    /// <param name="dateTime">要计算的日期</param>
    /// <returns>相对时间字符串</returns>
    public static string GetRelativeTime(DateTime dateTime)
    {
        var now = DateTime.Now;
        var span = now - dateTime;

        // 如果日期在未来（不应该发生，但处理一下）
        if (span.TotalSeconds < 0) return "刚刚";

        // 按时间范围返回不同格式
        if (span.TotalSeconds < 60) return "刚刚";

        if (span.TotalMinutes < 60)
        {
            var minutes = (int)span.TotalMinutes;
            return $"{minutes} 分钟前";
        }

        if (span.TotalHours < 24)
        {
            var hours = (int)span.TotalHours;
            return $"{hours} 小时前";
        }

        if (span.TotalDays < 30)
        {
            var days = (int)span.TotalDays;
            return $"{days} 天前";
        }

        if (span.TotalDays < 365)
        {
            var months = (int)(span.TotalDays / 30);
            return $"{months} 个月前";
        }

        var years = (int)(span.TotalDays / 365);
        return $"{years} 年前";
    }

    /// <summary>
    ///     计算相对时间（带具体时间）
    /// </summary>
    public static string GetDetailedRelativeTime(DateTime dateTime)
    {
        var now = DateTime.Now;

        // 如果是今天
        if (dateTime.Date == now.Date) return dateTime.ToString("今天 HH:mm");
        // 如果是昨天
        if (dateTime.Date == now.Date.AddDays(-1)) return dateTime.ToString("昨天 HH:mm");
        // 如果是前天
        if (dateTime.Date == now.Date.AddDays(-2)) return dateTime.ToString("前天 HH:mm");
        // 一周内
        if (dateTime.Date >= now.Date.AddDays(-7))
        {
            var days = (now.Date - dateTime.Date).Days;
            return $"{days}天前 {dateTime:HH:mm}";
        }
        // 今年内

        if (dateTime.Year == now.Year)
        {
            var months = now.Month - dateTime.Month;
            if (months <= 0) months += 12;
            return $"{months}个月前 {dateTime:MM-dd HH:mm}";
        }
        // 其他情况

        var years = now.Year - dateTime.Year;
        return $"{years}年前 {dateTime:yyyy-MM-dd}";
    }
}