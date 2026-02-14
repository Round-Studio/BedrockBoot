using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using BedrockBoot.Entity;
using BedrockBoot.Models.Global;
using Round.SDK.Entity;

namespace BedrockBoot.Models.Helper.ExceptionHelper;

public class ExceptionHelper
{
    public static string ExceptionFilesFolder => Path.Combine(PathsList.ReportPath);

    public static List<ErrorReport> GetAllReport()
    {
        if (!Path.Exists(ExceptionFilesFolder))
            return new();

        return Directory.GetFiles(ExceptionFilesFolder)
            .ToList()
            .Select(file =>
            {
                var conf = new ConfigEntity<ErrorReport>(file).Data;
                conf.FileName = file;
                return conf;
            })
            .ToList();
    }
}