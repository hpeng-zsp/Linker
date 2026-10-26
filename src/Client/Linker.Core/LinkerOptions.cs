namespace Linker.Core;

public sealed class LinkerOptions
{
    /// <summary>
    /// 是否扫描当前已经加载的程序集。
    /// </summary>
    public bool ScanLoadedAssemblies { get; set; } = true;

    /// <summary>
    /// 是否递归扫描入口程序集引用的程序集。
    /// </summary>
    public bool ScanReferencedAssemblies { get; set; } = true;

    /// <summary>
    /// 是否扫描应用程序目录下的 DLL。
    /// 默认关闭，避免加载无关 DLL。
    /// </summary>
    public bool ScanDirectoryAssemblies { get; set; } = false;

    /// <summary>
    /// 是否输出扫描日志。
    /// </summary>
    public bool EnableScanLog { get; set; } = true;

    /// <summary>
    /// 没有扫描到程序集时是否抛出异常。
    /// </summary>
    public bool ThrowIfNoAssemblyFound { get; set; } = true;
}