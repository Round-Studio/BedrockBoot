using System;
using Avalonia.Controls;

namespace BedrockBoot.Base.Entry.Task;

public class TaskEntry
{
    public Control Item { get; set; }
    public string TUID { get; set; } = Guid.NewGuid().ToString();
    public ITaskItem? TaskItem => Item as ITaskItem;
}