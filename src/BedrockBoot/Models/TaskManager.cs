using System;
using System.Collections.Generic;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Task;

namespace BedrockBoot.Models;

public class TaskManager
{
    public List<TaskEntry> Tasks { get; private set; } = new();
    public Action OnChanged { get; set; }

    public string AddTask(Control item)
    {
        var entry = new TaskEntry()
        {
            Item = item
        };
        
        
        Tasks.Add(entry);

        if (OnChanged != null) OnChanged.Invoke();
        
        return entry.TUID;
    }

    public void RemoveTask(string tuid)
    {
        Tasks.RemoveAll(x => x.TUID == tuid);
        if (OnChanged != null) OnChanged.Invoke();
    }
}