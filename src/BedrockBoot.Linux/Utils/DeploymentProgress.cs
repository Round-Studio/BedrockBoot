namespace Windows.Management.Deployment;

public struct DeploymentProgress
{
    /// <summary>
    /// 获取整个部署操作过程完成的百分比。
    /// </summary>
    /// <returns>
    /// 一个 0 到 100 之间的值，表示完成的百分比。
    /// </returns>
    public uint percentage { get; }
        
    /// <summary>
    /// 获取部署状态的当前可读状态消息。
    /// </summary>
    /// <returns>
    /// 一个字符串，包含部署操作的当前状态消息。
    /// </returns>
    public string stateText { get; }
}