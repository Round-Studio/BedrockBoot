using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace BedrockBoot.Views.Pages.SetupPage;

public class NewSetupStepItem
{
    public string Title { get; set; } = "我们需要进行一些配置";
    public Type? Content { get; set; }
    public bool IsAutoShowNextBtn { get; set; } = true;
    public bool IsAutoShowLastBtn { get; set; } = true;
    public string SmallTitle { get; set; } = "欢迎使用 BedrockBoot";
    public string SmallMessage { get; set; } = "单击 “下一步” 以继续";
    public string NextBtnText { get; set; } = "下一步";
    public string LastBtnText { get; set; } = "上一步";
    public bool IsNodeRoot { get; set; } = false;
    public List<NewSetupStepItem>? Nodes;
    public NewSetupStepItem? Parent;
}

public partial class SetupRoot : UserControl
{
    public static SetupRoot? Instance { get; private set; }

    private Stack<NewSetupStepItem> _stepStack = new();
    private NewSetupStepItem? _currentStep;
    private NewSetupStepItem? _rootStep;

    public static NewSetupStepItem StepRoot { get; } = new()
    {
        Title = "初次使用向导",
        Content = typeof(SetupWelcome),
        IsAutoShowLastBtn = false,
        IsAutoShowNextBtn = false,
        IsNodeRoot = true,
        Nodes = new()
        {
            new()
            {
                Content = typeof(SetupStyle),
                IsAutoShowNextBtn = true,
                IsAutoShowLastBtn = false,
                SmallTitle = "个性化",
                SmallMessage = "选择您心仪的个性化选项"
            },
            new()
            {
                Content = typeof(SetupLayout),
                IsAutoShowNextBtn = true,
                IsAutoShowLastBtn = true,
                SmallTitle = "选择布局",
                SmallMessage = "选择一个启动器页面布局"
            },
            new()
            {
                Content = typeof(SetupImport),
                IsAutoShowNextBtn = true,
                IsAutoShowLastBtn = true,
                SmallTitle = "导入目录",
                SmallMessage = "导入属于您的游戏目录，或导入其他启动器的目录"
            },
            new()
            {
                Content = typeof(SetupCompleted),
                IsAutoShowNextBtn = false,
                IsAutoShowLastBtn = true,
                SmallTitle = "完成！",
                SmallMessage = "单击屏幕中间的 “开始使用” 进入主屏幕"
            }
        }
    };

    public SetupRoot()
    {
        Instance = this;
        InitializeComponent();
        _rootStep = StepRoot;
        SetParentReferences(StepRoot, null);
        StepTo(StepRoot);
    }

    private void SetParentReferences(NewSetupStepItem node, NewSetupStepItem? parent)
    {
        node.Parent = parent;
        if (node.Nodes != null)
        {
            foreach (var child in node.Nodes)
            {
                SetParentReferences(child, node);
            }
        }
    }

    public void StepTo(NewSetupStepItem item)
    {
        _currentStep = item;

        NextBtn.IsVisible = item.IsAutoShowNextBtn;
        LastBtn.IsVisible = item.IsAutoShowLastBtn;
        NextBtn.Content = item.NextBtnText;
        LastBtn.Content = item.LastBtnText;
        BigTitle.Text = item.Title;
        SmallTitle.Text = item.SmallTitle;
        SmallMessage.Text = $"/ {item.SmallMessage}";

        if (item.Content != null)
        {
            var control = Activator.CreateInstance(item.Content);
            SetupNavigationFrame.NavigateTo(control!);
        }

        UpdateNavigationStack(item);
    }

    private void UpdateNavigationStack(NewSetupStepItem current)
    {
        if (current.Parent == null)
        {
            _stepStack.Clear();
            return;
        }

        if (_stepStack.Contains(current))
        {
            while (_stepStack.Peek() != current)
            {
                _stepStack.Pop();
            }
        }
        else
        {
            if (_stepStack.Count == 0 || _stepStack.Peek() != current)
            {
                _stepStack.Push(current);
            }
        }
    }

    public void GoToNextStep()
    {
        if (_currentStep == null) return;

        if (_currentStep.Nodes != null && _currentStep.Nodes.Any())
        {
            StepTo(_currentStep.Nodes[0]);
            return;
        }

        var nextSibling = GetNextSibling(_currentStep);
        if (nextSibling != null)
        {
            StepTo(nextSibling);
            return;
        }

        var next = FindNextInTree(_currentStep);
        if (next != null)
        {
            StepTo(next);
            return;
        }

        CompleteSetup();
    }

    private NewSetupStepItem? GetNextSibling(NewSetupStepItem item)
    {
        if (item.Parent == null || item.Parent.Nodes == null) return null;

        var siblings = item.Parent.Nodes;
        var index = siblings.IndexOf(item);

        if (index >= 0 && index < siblings.Count - 1)
        {
            return siblings[index + 1];
        }

        return null;
    }

    private NewSetupStepItem? FindNextInTree(NewSetupStepItem current)
    {
        var parent = current.Parent;
        while (parent != null)
        {
            var nextSibling = GetNextSibling(parent);
            if (nextSibling != null)
            {
                return nextSibling;
            }

            parent = parent.Parent;
        }

        return null;
    }

    public void GoToPreviousStep()
    {
        if (_currentStep == null) return;

        var prevSibling = GetPreviousSibling(_currentStep);
        if (prevSibling != null)
        {
            var lastChild = GetLastDescendant(prevSibling);
            StepTo(lastChild ?? prevSibling);
            return;
        }

        if (_currentStep.Parent != null && !_currentStep.Parent.IsNodeRoot)
        {
            StepTo(_currentStep.Parent);
            return;
        }

        if (_currentStep.Parent != null && _currentStep.Parent.IsNodeRoot)
        {
            StepTo(_currentStep.Parent);
            return;
        }
    }

    private NewSetupStepItem? GetPreviousSibling(NewSetupStepItem item)
    {
        if (item.Parent == null || item.Parent.Nodes == null) return null;

        var siblings = item.Parent.Nodes;
        var index = siblings.IndexOf(item);

        if (index > 0)
        {
            return siblings[index - 1];
        }

        return null;
    }

    private NewSetupStepItem? GetLastDescendant(NewSetupStepItem item)
    {
        var current = item;
        while (current.Nodes != null && current.Nodes.Any())
        {
            current = current.Nodes.Last();
        }

        return current;
    }

    public string GetBreadcrumb()
    {
        var crumbs = new List<string>();
        var current = _currentStep;

        while (current != null)
        {
            crumbs.Insert(0, current.Title);
            current = current.Parent;
        }

        return string.Join(" > ", crumbs);
    }

    public string GetProgress()
    {
        var allSteps = GetAllNodes(_rootStep);
        var currentIndex = allSteps.IndexOf(_currentStep);
        return $"{currentIndex + 1} / {allSteps.Count}";
    }

    private List<NewSetupStepItem> GetAllNodes(NewSetupStepItem root)
    {
        var result = new List<NewSetupStepItem> { root };
        if (root.Nodes != null)
        {
            foreach (var node in root.Nodes)
            {
                result.AddRange(GetAllNodes(node));
            }
        }

        return result;
    }

    private void CompleteSetup()
    {
        Console.WriteLine(@"向导完成！");
    }

    private void OnNextBtnClick(object? sender, RoutedEventArgs e) => GoToNextStep();
    private void OnLastBtnClick(object? sender, RoutedEventArgs e) => GoToPreviousStep();

    public void ShowNextBtn(bool isShow = true) => Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        NextBtn.IsVisible = isShow);

    public void ShowLastBtn(bool isShow = true) => Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        LastBtn.IsVisible = isShow);
}