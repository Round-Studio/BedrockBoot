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

namespace BedrockBoot.Standard.Entity.Game;

public class GameDumpAnalysis
{
    public string FilePath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public DateTime CreatedTime { get; set; }

    public DateTime CaptureTime { get; set; }

    public string? Phase { get; set; }

    public string? Title { get; set; }

    public uint ProcessId { get; set; }

    public uint CrashedThreadId { get; set; }

    public bool HasException { get; set; }

    public uint ExceptionCode { get; set; }

    public string? ExceptionCodeName { get; set; }

    public string? ExceptionDescription { get; set; }

    public ulong ExceptionAddress { get; set; }

    public string? ExceptionModulePath { get; set; }

    public string? Owner { get; set; }

    public IReadOnlyList<ulong> ExceptionParameters { get; set; } = [];

    public string? AccessViolationOperation { get; set; }

    public ulong? AccessViolationTarget { get; set; }

    public ulong InstructionPointer { get; set; }

    public ulong StackPointer { get; set; }

    public Dictionary<string, ulong> Registers { get; set; } = new();

    public string? Details { get; set; }

    public string? DumpFilePath { get; set; }

    public List<GameDumpStackFrame> StackTrace { get; set; } = [];

    public string? TopFrameDisplay => StackTrace.Count > 0 ? StackTrace[0].Display : null;
}
