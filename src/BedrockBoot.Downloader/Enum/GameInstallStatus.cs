namespace BedrockBoot.Downloader.Enum;

public enum GameInstallStatus
{
    GetUrl, // 获取下载地址
    DownloadFile, // 下载文件，包括旧版安装逻辑和新版安装逻辑
    InstallGame, // 安装游戏，包括解压和新版本的安装逻辑
    UwpRegister, // UWP 注册过程
    Completed,
    Error // 报错呗
}