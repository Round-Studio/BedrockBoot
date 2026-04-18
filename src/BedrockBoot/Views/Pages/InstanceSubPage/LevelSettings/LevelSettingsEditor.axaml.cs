using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BedrockBoot.Base.Entry.Game.Pack.Archive;
using BedrockBoot.LevelNbt;

namespace BedrockBoot.Views.Pages.InstanceSubPage.LevelSettings;

public partial class LevelSettingsEditor : UserControl
{
    private readonly ArchiveInfo _info;
    private bool _isInternalUpdating;

    public LevelSettingsEditor()
    {
        InitializeComponent();
    }

    public LevelSettingsEditor(ArchiveInfo info) : this()
    {
        _info = info;
        UpdaterUI();
    }

    public Action? BackAction { get; set; }

    /// <summary>
    ///     将 LevelWorldData 实体类的数据同步到 UI 控件 (Data -> UI)
    /// </summary>
    private void UpdaterUI()
    {
        if (_info?.LevelWorldData == null) return;
        _isInternalUpdating = true;

        var d = _info.LevelWorldData;

        // --- 基础信息 ---
        LevelNameText.Text = d.LevelName;
        SeedBox.Text = d.RandomSeed.ToString();
        // 游戏模式映射 (NBT: 0=生存, 1=创造, 2=冒险, 3=旁观)
        GameTypeCombo.SelectedIndex = d.GameType;

        // 游戏难度映射 (NBT: 0=和平, 1=简单, 2=普通, 3=困难)
        DifficultyCombo.SelectedIndex = d.Difficulty;

        // --- 核心 & 规则 (根目录下) ---
        CheatsSwitch.IsChecked = d.CheatsEnabled;
        HardcoreSwitch.IsChecked = d.IsHardCore;
        CmdBlockSwitch.IsChecked = d.CommandBlocksEnabled;
        CmdOutputSwitch.IsChecked = d.CommandBlockOutput;
        AdminCmdSwitch.IsChecked = d.CommandsEnabled;
        CmdFeedbackSwitch.IsChecked = d.SendCommandFeedback;
        BonusChestSwitch.IsChecked = d.HasBonusChest;
        StartMapSwitch.IsChecked = d.HasStartMap;
        ImmediateRespawnSwitch.IsChecked = d.DoImmediateRespawn;
        RecipeUnlockSwitch.IsChecked = d.RecipeUnlock;
        LimitedCraftSwitch.IsChecked = d.LimitedCrafting;
        TextureRequiredSwitch.IsChecked = d.TexturepacksRequired;

        // --- 界面 ---
        ShowCoordSwitch.IsChecked = d.ShowCoordinates;
        ShowDaysSwitch.IsChecked = d.ShowDaysPlayed;
        ShowDeathMsgSwitch.IsChecked = d.ShowDeathMessages;
        ShowRecipeSwitch.IsChecked = d.ShowRecipeMessages;
        ShowTagsSwitch.IsChecked = d.ShowTags;
        ShowBorderSwitch.IsChecked = d.ShowBorderEffect;

        // --- 生物与环境 ---
        DaylightCycleSwitch.IsChecked = d.DoDaylightCycle;
        WeatherCycleSwitch.IsChecked = d.DoWeatherCycle;
        MobSpawnSwitch.IsChecked = d.DoMobSpawning;
        InsomniaSwitch.IsChecked = d.DoInsomnia;
        MobGriefSwitch.IsChecked = d.MobGriefing;
        MobLootSwitch.IsChecked = d.DoMobLoot;
        EntityDropSwitch.IsChecked = d.DoEntityDrops;
        TileDropSwitch.IsChecked = d.DoTileDrops;
        FireTickSwitch.IsChecked = d.DoFireTick;
        TntExplodeSwitch.IsChecked = d.TntExplodes;
        RespawnExplodeSwitch.IsChecked = d.RespawnBlocksExplode;

        // --- 玩家规则 ---
        KeepInvSwitch.IsChecked = d.KeepInventory;
        NaturalRegenSwitch.IsChecked = d.NaturalRegeneration;
        PvpSwitch.IsChecked = d.Pvp;
        FallDamageSwitch.IsChecked = d.FallDamage;
        FireDamageSwitch.IsChecked = d.FireDamage;
        DrownDamageSwitch.IsChecked = d.DrowningDamage;
        FreezeDamageSwitch.IsChecked = d.FreezeDamage;

        // --- 出生点 ---
        SpawnXText.Text = d.SpawnX.ToString();
        SpawnYText.Text = d.SpawnY.ToString();
        SpawnZText.Text = d.SpawnZ.ToString();

        // --- 能力与权限 (abilities 嵌套内) ---
        MineSwitch.IsChecked = d.Mine;
        BuildSwitch.IsChecked = d.Build;
        AtkMobSwitch.IsChecked = d.AttackMobs;
        AtkPlayerSwitch.IsChecked = d.AttackPlayers;
        DoorSwitch.IsChecked = d.DoorsAndSwitches;
        ContainerSwitch.IsChecked = d.OpenContainers;
        OpSwitch.IsChecked = d.Op;
        TpSwitch.IsChecked = d.Teleport;

        FlyingSwitch.IsChecked = d.Flying;
        MayflySwitch.IsChecked = d.Mayfly;
        InstabuildSwitch.IsChecked = d.Instabuild;
        InvulnerableSwitch.IsChecked = d.Invulnerable;
        NoClipSwitch.IsChecked = d.NoClip;
        WorldBuilderSwitch.IsChecked = d.WorldBuilder; // 对应 NBT 的 lightning

        // --- 速度数值 ---
        WalkSpeedText.Text = d.WalkSpeed.ToString();
        FlySpeedText.Text = d.FlySpeed.ToString();
        VFlySpeedText.Text = d.VerticalFlySpeed.ToString();

        _isInternalUpdating = false;
    }

    /// <summary>
    ///     当任何 CheckBox 改变时同步到数据类 (UI -> Data)
    /// </summary>
    private void OnCheckChanged(object? sender, RoutedEventArgs e)
    {
        if (_isInternalUpdating) return;
        SyncUiToData();
    }

    private void OnLevelNameChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isInternalUpdating) return;
        _info.LevelWorldData.LevelName = string.IsNullOrWhiteSpace(LevelNameText.Text) ? "我的世界" : LevelNameText.Text;
    }

    /// <summary>
    ///     全量同步 UI 状态到实体类，确保与 JSON 结构对应的属性一致
    /// </summary>
    private void SyncUiToData()
    {
        var d = _info.LevelWorldData;

        // 核心 & 规则
        d.CheatsEnabled = CheatsSwitch.IsChecked ?? false;
        d.IsHardCore = HardcoreSwitch.IsChecked ?? false;
        d.CommandBlocksEnabled = CmdBlockSwitch.IsChecked ?? true;
        d.CommandBlockOutput = CmdOutputSwitch.IsChecked ?? true;
        d.CommandsEnabled = AdminCmdSwitch.IsChecked ?? false;
        d.SendCommandFeedback = CmdFeedbackSwitch.IsChecked ?? true;
        d.HasBonusChest = BonusChestSwitch.IsChecked ?? false;
        d.HasStartMap = StartMapSwitch.IsChecked ?? false;
        d.DoImmediateRespawn = ImmediateRespawnSwitch.IsChecked ?? false;
        d.RecipeUnlock = RecipeUnlockSwitch.IsChecked ?? true;
        d.LimitedCrafting = LimitedCraftSwitch.IsChecked ?? false;
        d.TexturepacksRequired = TextureRequiredSwitch.IsChecked ?? false;

        // 同步下拉框数据到实体类
        d.GameType = GameTypeCombo.SelectedIndex;
        d.Difficulty = DifficultyCombo.SelectedIndex;

        // 界面
        d.ShowCoordinates = ShowCoordSwitch.IsChecked ?? false;
        d.ShowDaysPlayed = ShowDaysSwitch.IsChecked ?? false;
        d.ShowDeathMessages = ShowDeathMsgSwitch.IsChecked ?? true;
        d.ShowRecipeMessages = ShowRecipeSwitch.IsChecked ?? true;
        d.ShowTags = ShowTagsSwitch.IsChecked ?? true;
        d.ShowBorderEffect = ShowBorderSwitch.IsChecked ?? true;

        // 环境
        d.DoDaylightCycle = DaylightCycleSwitch.IsChecked ?? true;
        d.DoWeatherCycle = WeatherCycleSwitch.IsChecked ?? true;
        d.DoMobSpawning = MobSpawnSwitch.IsChecked ?? true;
        d.DoInsomnia = InsomniaSwitch.IsChecked ?? true;
        d.MobGriefing = MobGriefSwitch.IsChecked ?? true;
        d.DoMobLoot = MobLootSwitch.IsChecked ?? true;
        d.DoEntityDrops = EntityDropSwitch.IsChecked ?? true;
        d.DoTileDrops = TileDropSwitch.IsChecked ?? true;
        d.DoFireTick = FireTickSwitch.IsChecked ?? true;
        d.TntExplodes = TntExplodeSwitch.IsChecked ?? true;
        d.RespawnBlocksExplode = RespawnExplodeSwitch.IsChecked ?? true;

        // 玩家规则
        d.KeepInventory = KeepInvSwitch.IsChecked ?? false;
        d.NaturalRegeneration = NaturalRegenSwitch.IsChecked ?? true;
        d.Pvp = PvpSwitch.IsChecked ?? true;
        d.FallDamage = FallDamageSwitch.IsChecked ?? true;
        d.FireDamage = FireDamageSwitch.IsChecked ?? true;
        d.DrowningDamage = DrownDamageSwitch.IsChecked ?? true;
        d.FreezeDamage = FreezeDamageSwitch.IsChecked ?? true;

        // 权限 & 能力
        d.Mine = MineSwitch.IsChecked ?? true;
        d.Build = BuildSwitch.IsChecked ?? true;
        d.AttackMobs = AtkMobSwitch.IsChecked ?? true;
        d.AttackPlayers = AtkPlayerSwitch.IsChecked ?? true;
        d.DoorsAndSwitches = DoorSwitch.IsChecked ?? true;
        d.OpenContainers = ContainerSwitch.IsChecked ?? true;
        d.Op = OpSwitch.IsChecked ?? false;
        d.Teleport = TpSwitch.IsChecked ?? false;

        d.Flying = FlyingSwitch.IsChecked ?? false;
        d.Mayfly = MayflySwitch.IsChecked ?? false;
        d.Instabuild = InstabuildSwitch.IsChecked ?? false;
        d.Invulnerable = InvulnerableSwitch.IsChecked ?? false;
        d.NoClip = NoClipSwitch.IsChecked ?? false;
        d.WorldBuilder = WorldBuilderSwitch.IsChecked ?? false;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isInternalUpdating) return;
        SyncUiToData();
    }

    private void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var d = _info.LevelWorldData;

        // 1. 同步数值输入框
        if (int.TryParse(SpawnXText.Text, out var x)) d.SpawnX = x;
        if (int.TryParse(SpawnYText.Text, out var y)) d.SpawnY = y;
        if (int.TryParse(SpawnZText.Text, out var z)) d.SpawnZ = z;

        if (float.TryParse(WalkSpeedText.Text, out var ws)) d.WalkSpeed = ws;
        if (float.TryParse(FlySpeedText.Text, out var fs)) d.FlySpeed = fs;
        if (float.TryParse(VFlySpeedText.Text, out var vfs)) d.VerticalFlySpeed = vfs;

        // 2. 更新最后游玩时间 (Unix 时间戳)
        d.LastPlayed = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 3. 执行保存
        var path = Path.Combine(_info.Path, "level.dat");
        LevelDatSaver.Save(path, d, d.HeaderVersion);
    }
}