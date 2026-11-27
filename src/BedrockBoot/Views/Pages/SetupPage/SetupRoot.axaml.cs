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

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace BedrockBoot.Views.Pages.SetupPage;

public class NewSetupStepItem
{
    public string TitleKey { get; set; } = "Setup.Root.Title";
    public Type? Content { get; set; }
    public bool IsAutoShowNextBtn { get; set; } = true;
    public bool IsAutoShowLastBtn { get; set; } = true;
    public string SmallTitleKey { get; set; } = "Setup.Root.WelcomeTitle";
    public string SmallMessageKey { get; set; } = "Setup.Root.WelcomeMessage";
    public string NextBtnKey { get; set; } = "Setup.Root.Action.Next";
    public string LastBtnKey { get; set; } = "Setup.Root.Action.Previous";
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
        TitleKey = "Setup.Root.Title",
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
                SmallTitleKey = "Setup.Step.Style.Title",
                SmallMessageKey = "Setup.Step.Style.Message"
            },
            new()
            {
                Content = typeof(SetupLayout),
                IsAutoShowNextBtn = true,
                IsAutoShowLastBtn = true,
                SmallTitleKey = "Setup.Step.Layout.Title",
                SmallMessageKey = "Setup.Step.Layout.Message"
            },
            new()
            {
                Content = typeof(SetupImport),
                IsAutoShowNextBtn = true,
                IsAutoShowLastBtn = true,
                SmallTitleKey = "Setup.Step.Import.Title",
                SmallMessageKey = "Setup.Step.Import.Message"
            },
            new()
            {
                Content = typeof(SetupCompleted),
                IsAutoShowNextBtn = false,
                IsAutoShowLastBtn = true,
                SmallTitleKey = "Setup.Step.Completed.Title",
                SmallMessageKey = "Setup.Step.Completed.Message"
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
        NextBtn.Content = I18nManager.Instance[item.NextBtnKey];
        LastBtn.Content = I18nManager.Instance[item.LastBtnKey];
        BigTitle.Text = I18nManager.Instance[item.TitleKey];
        SmallTitle.Text = I18nManager.Instance[item.SmallTitleKey];
        SmallMessage.Text = $"/ {I18nManager.Instance[item.SmallMessageKey]}";

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
            crumbs.Insert(0, I18nManager.Instance[current.TitleKey]);
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