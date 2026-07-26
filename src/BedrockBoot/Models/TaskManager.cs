using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using BedrockBoot.Base.Entry.Task;

namespace BedrockBoot.Models;

public class TaskManager
{
    public List<TaskEntry> Tasks { get; } = new();
    public Action OnChanged { get; set; }

    public double OverallProgress { get; private set; }

    private readonly List<Action<double>> _overallProgressCallbacks = new();
    private readonly object _lock = new();

    public void AddOverallProgressCallback(Action<double> callback)
    {
        lock (_lock)
        {
            _overallProgressCallbacks.Add(callback);
        }
    }

    public void RemoveOverallProgressCallback(Action<double> callback)
    {
        lock (_lock)
        {
            _overallProgressCallbacks.Remove(callback);
        }
    }

    public string AddTask(Control item)
    {
        var entry = new TaskEntry
        {
            Item = item
        };

        if (entry.TaskItem is { } taskItem)
            taskItem.ProgressUpdated += OnTaskProgressUpdated;

        Tasks.Add(entry);

        OnChanged?.Invoke();
        RecalculateOverallProgress();

        return entry.TUID;
    }

    public void RemoveTask(string tuid)
    {
        var entry = Tasks.FirstOrDefault(x => x.TUID == tuid);
        if (entry?.TaskItem is { } taskItem)
            taskItem.ProgressUpdated -= OnTaskProgressUpdated;

        Tasks.RemoveAll(x => x.TUID == tuid);
        OnChanged?.Invoke();
        RecalculateOverallProgress();
    }

    private void OnTaskProgressUpdated(ITaskItem taskItem)
    {
        RecalculateOverallProgress();
    }

    private void RecalculateOverallProgress()
    {
        var tasks = Tasks.Select(t => t.TaskItem).Where(t => t != null).Cast<ITaskItem>().ToList();
        if (tasks.Count == 0)
        {
            OverallProgress = 0;
        }
        else
        {
            var running = tasks.Where(t => !t.IsCompleted).ToList();
            var completed = tasks.Count - running.Count;

            if (running.Count == 0)
            {
                OverallProgress = 100;
            }
            else
            {
                var runningProgress = running.Sum(t => t.Progress);
                var total = completed * 100.0 + runningProgress;
                OverallProgress = total / tasks.Count;
            }
        }

        lock (_lock)
        {
            foreach (var callback in _overallProgressCallbacks)
                callback(OverallProgress);
        }
    }
}
