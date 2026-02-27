namespace BedrockBoot.Base.Entry.Info.Xbox;


public class XboxStatus
{
    public bool IsLoggedIn { get; set; }
    public bool XblAuthServiceRunning { get; set; }
    public bool XboxNetApiServiceRunning { get; set; }
    public bool XboxGipSvcRunning { get; set; }
    public bool RegistryUserFound { get; set; }
    public bool XboxProcessesRunning { get; set; }
    public bool XboxApiUserFound { get; set; }
    public bool XboxAppInstalled { get; set; }
    public bool WindowsTokenFound { get; set; }
    public XboxUserInfo XboxUserInfo { get; set; } = new ();
    
    public void PrintStatus()
    {
        Console.WriteLine(@"
=== Xbox 登录状态详细报告 ===");
        Console.WriteLine($@"登录状态: {(IsLoggedIn ? "✓ 已登录" : "✗ 未登录")}");
        Console.WriteLine($@"
【核心服务】");
        Console.WriteLine($@"XblAuthManager 服务: {(XblAuthServiceRunning ? "✓ 运行中" : "✗ 未运行")}");
        Console.WriteLine($@"XboxNetApiSvc 服务: {(XboxNetApiServiceRunning ? "✓ 运行中" : "✗ 未运行")}");
        Console.WriteLine($@"XboxGipSvc 服务: {(XboxGipSvcRunning ? "✓ 运行中" : "✗ 未运行")}");
        
        Console.WriteLine($@"
【用户信息】");
        Console.WriteLine($@"注册表用户: {(RegistryUserFound ? "✓ 存在" : "✗ 不存在")}");
        Console.WriteLine($@"Xbox API 用户: {(XboxApiUserFound ? "✓ 存在" : "✗ 不存在")}");
        Console.WriteLine($@"Windows 令牌: {(WindowsTokenFound ? "✓ 存在" : "✗ 不存在")}");
        
        if (!string.IsNullOrEmpty(XboxUserInfo?.Gamertag))
        {
            Console.WriteLine($@"Gamertag: {XboxUserInfo.Gamertag}");
            Console.WriteLine($@"XUID: {XboxUserInfo.XUID}");
            Console.WriteLine($@"显示名称: {XboxUserInfo.DisplayName}");
            if (XboxUserInfo.TokenExpiration.HasValue)
            {
                Console.WriteLine($@"令牌过期: {XboxUserInfo.TokenExpiration.Value:yyyy-MM-dd HH:mm:ss}");
            }
        }
        
        Console.WriteLine($@"
【应用状态】");
        Console.WriteLine($@"Xbox 进程: {(XboxProcessesRunning ? "✓ 运行中" : "✗ 未运行")}");
        Console.WriteLine($@"Xbox 应用包: {(XboxAppInstalled ? "✓ 已安装" : "✗ 未安装")}");
    }
}