using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linker.Client.Contracts
{
    public sealed class LinkerOptions
    {
        /// <summary>
        /// 微服务所在组
        /// </summary>
        public string GroupName { get; set; } = "default";
        /// <summary>
        /// 微服务名称
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;
        /// <summary>
        /// Linker主机
        /// </summary>
        public string AgentHost { get; set; } = string.Empty;
        /// <summary>
        /// Linker主机端口号（默认8899）
        /// </summary>
        public int AgentPort { get; set; } = 8899;

        /// <summary>
        /// 微服务配置文件Key
        /// </summary>
        public List<string> ConfigKey { get; set; } = new();

    }
}
