open System.IO

// 编译完毕后，将构建前生成的真实修改日志还原为占位符，
// 保证版本库中的 ChangLog.txt 始终只包含占位内容
let content = "CuO~"

let filename = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Text", "ChangLog.txt")

try
    File.WriteAllText(filename, content)
    printfn "Restore: %s" filename
with
| ex -> printfn "Failed to Restore: %s" ex.Message
