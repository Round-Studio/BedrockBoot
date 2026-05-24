namespace BedrockBoot.Models.Pack.System.DropFile;

public class DropFileOperation
{
    private readonly string _file;

    public DropFileOperation(string file)
    {
        _file = file;
    }
    
    public void RunAsync()
    {
        var type = DropFileCheck.CheckFile(_file);
    }
}