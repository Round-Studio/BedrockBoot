using System;

namespace BedrockBoot.Base.Entry.Task;

public interface ITaskItem
{
    double Progress { get; }
    string StatusText { get; }
    string Title { get; }
    bool IsCompleted { get; }
    bool IsIndeterminate { get; }

    event Action<ITaskItem>? ProgressUpdated;
}
