using System;
using System.IO;
using BedrockBoot.Chunker.Base.Entry;
using BedrockBoot.Chunker.Base.Entry.Info;
using BedrockBoot.Chunker.Base.Enum;
using Round.SDK.Helper;

namespace BedrockBoot.Models.Pack.Chunker;

public class ChunkerHelper
{
    public static readonly string ChunkerTempFolderPath =
        Path.Combine(BedrockBoot.Chunker.Chunker.ChunkerFolderPath, "Temp");

    private readonly ChunkerInfo _info;

    public ChunkerHelper(
        ChunkerType type,
        string gameVersion,
        string archivePath,
        JavaInfo javaInfo,
        IProgress<double> progress)
    {
        _info = new ChunkerInfo
        {
            JvmInfo = javaInfo,
            ChunkerType = type,
            Progress = progress
        };
        if (type == ChunkerType.BedrockToJava)
            _info.JavaEditionVersion = gameVersion;

        if (type == ChunkerType.JavaToBedrock)
            _info.BedrockEditionVersion = gameVersion;

        if (!IsDirectory(archivePath))
        {
            _info.Progress?.Report(10);
            var folder = Path.Combine(ChunkerTempFolderPath,
                $"pack_input_{Guid.NewGuid().ToString().Replace("-", "")}");

            ZipHelper.ExtractZipFile(archivePath, folder);
            archivePath = folder;
        }

        if (type == ChunkerType.BedrockToJava)
            _info.BedrockWorldFolder = archivePath;
        if (type == ChunkerType.JavaToBedrock)
            _info.JavaWorldFolder = archivePath;

        _info.Progress?.Report(20);
    }

    public string Conversion()
    {
        var outputDir = Path.Combine(ChunkerTempFolderPath,
            $"pack_output_{Guid.NewGuid().ToString().Replace("-", "")}");

        if (_info.ChunkerType == ChunkerType.BedrockToJava)
            _info.JavaWorldFolder = outputDir;
        if (_info.ChunkerType == ChunkerType.JavaToBedrock)
            _info.BedrockWorldFolder = outputDir;
        var chunker = new BedrockBoot.Chunker.Chunker();
        chunker.BeginChunker(_info);

        return outputDir;
    }

    public void ConversionToFile(string file)
    {
        var outputDir = Path.Combine(ChunkerTempFolderPath,
            $"pack_output_{Guid.NewGuid().ToString().Replace("-", "")}");

        if (_info.ChunkerType == ChunkerType.BedrockToJava)
            _info.JavaWorldFolder = outputDir;
        if (_info.ChunkerType == ChunkerType.JavaToBedrock)
            _info.BedrockWorldFolder = outputDir;
        var chunker = new BedrockBoot.Chunker.Chunker();
        chunker.BeginChunker(_info);

        if (File.Exists(file)) File.Delete(file);

        ZipHelper.CreateZipFile(outputDir, file);
    }

    public void ConversionToFolder(string folder)
    {
        var outputDir = folder;

        if (_info.ChunkerType == ChunkerType.BedrockToJava)
            _info.JavaWorldFolder = outputDir;
        if (_info.ChunkerType == ChunkerType.JavaToBedrock)
            _info.BedrockWorldFolder = outputDir;
        var chunker = new BedrockBoot.Chunker.Chunker();
        chunker.BeginChunker(_info);
    }

    private bool IsDirectory(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        try
        {
            var attr = File.GetAttributes(path);
            return attr.HasFlag(FileAttributes.Directory);
        }
        catch
        {
            return false;
        }
    }
}