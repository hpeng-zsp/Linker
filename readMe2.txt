Linker项目说明文档
================================

文档版本：v0.1
目标运行环境：服务端 .NET 10
客户端 SDK：netstandard2.0
管理前端：Vue 3 + Element Plus
主要通信协议：gRPC
一致性协议：Raft
项目定位：面向 C#/.NET 微服务的配置中心、服务注册发现、定时任务和 gRPC 服务治理平台


一、项目总体目标
================

本项目计划构建一个面向 .NET 生态的云原生基础平台，目标功能包括：

1. 基于 Raft 的高可用集群；
2. Web 图形化节点初始化和集群管理；
3. 配置中心；
4. 服务注册与发现；
5. 客户端 .NET SDK；
6. gRPC 双向流通信；
7. 定时任务调度；
8. 服务端向客户端主动下发任务；
9. 微服务之间的 gRPC 通讯；
10. gRPC 调用耗时和执行日志记录；
11. OpenTelemetry 可观测性；
12. Docker、Kubernetes、.NET Aspire 等云原生集成。

服务端使用 .NET 10，客户端 SDK 使用 netstandard2.0，以便兼容多个 .NET 版本和部分旧项目。


二、总体架构
============

系统分为以下几个层次：

1. Raft 控制面
2. 服务连接与注册面
3. 配置中心
4. 定时任务调度面
5. gRPC 通信面
6. 观测和管理面


2.1 Raft 控制面
----------------

Raft 只负责需要强一致的数据，不负责所有高频运行状态。

Raft 中保存：

- 集群成员配置；
- 节点加入和退出；
- 配置中心数据；
- 配置版本；
- 持久化服务注册信息；
- 客户端连接注册事件；
- 客户端连接解除事件；
- 服务连接归属节点；
- 定时任务定义；
- 定时任务状态；
- 任务分配结果；
- 任务租约；
- 任务执行结果；
- 权限策略；
- 路由规则；
- 灰度发布规则；
- 平台关键元数据。

Raft 不保存：

- 每次 gRPC 心跳；
- 每次连接保活；
- 高频调用日志；
- 所有实时连接指标；
- 每次请求的详细 Trace；
- 临时内存缓存；
- 高频健康检查数据。


2.2 实时连接面
----------------

客户端通过 gRPC 双向流连接到某个集群节点。

连接成功后，节点本地保存：

- 客户端 ID；
- 服务名称；
- 实例 ID；
- 客户端连接对象；
- 当前节点 ID；
- 最近心跳时间；
- 连接状态；
- 支持的命令；
- 客户端版本；
- 元数据；
- 当前连接任务数。

连接对象只保存在拥有该连接的节点本地，不通过 Raft 复制连接对象本身。

其他节点通过 Raft 日志知道：

- 客户端连接到了哪个集群节点；
- 客户端是否已经注册；
- 客户端最后一次连接事件；
- 客户端应该由哪个节点负责投递任务。


三、节点初始化和集群管理
========================

3.1 第一次启动
--------------

节点第一次启动时，如果没有本地运行配置，则显示引导页面。

引导页面提供两个选项：

A. 创建新集群
B. 加入已有集群


3.2 创建新集群
--------------

用户选择“创建新集群”后，填写：

- NodeId；
- 节点名称；
- IP 或 Host；
- 端口；
- 数据目录；
- 集群名称。

保存类似配置：

{
  "NodeId": "node-001",
  "Port": 7001,
  "Bootstrap": true,
  "LeaderAddress": null,
  "DataDirectory": "data/raft/node-001",
  "PublicEndpoint": "http://127.0.0.1:7001/"
}

保存配置后提示：

“配置已保存，请重启当前进程。”

第一次真正启动 Raft 时：

- Bootstrap=true；
- ColdStart=true；
- 当前节点创建初始集群；
- 当前节点通过选举成为 Leader。

以后重启时：

- Bootstrap 仍然可以是 true；
- 但如果已有持久化 cluster-config，则 ColdStart=false；
- 节点恢复已有 Raft 状态；
- 节点不会因为 Bootstrap=true 就永远认为自己是 Leader。


3.3 加入已有集群
----------------

用户选择“加入已有集群”后，填写：

- 当前节点地址；
- 当前节点端口；
- Leader 地址；
- 数据目录。

示例：

当前节点：

http://127.0.0.1:7004/

Leader：

http://127.0.0.1:7001/

保存：

{
  "NodeId": "node-004",
  "Port": 7004,
  "Bootstrap": false,
  "LeaderAddress": "http://127.0.0.1:7001/",
  "DataDirectory": "data/raft/node-004",
  "PublicEndpoint": "http://127.0.0.1:7004/"
}

重启后：

1. 当前节点启动为待加入节点；
2. 当前节点访问 Leader；
3. Leader 执行 AddMemberAsync；
4. Leader 对新节点进行日志或快照同步；
5. 新节点完成 warmup；
6. Raft 成员配置提交；
7. 新节点成为正式 Follower。


3.4 Leader 页面
---------------

Leader 页面可以显示和操作：

- 当前 Leader；
- 当前任期；
- 成员数量；
- 多数派数量；
- 提交索引；
- 最后日志索引；
- 最后应用索引；
- 节点状态；
- 节点日志同步进度；
- 添加节点；
- 移除节点；
- 查看节点数据目录；
- 查看 WAL 和快照状态；
- 查看配置中心；
- 查看任务中心；
- 查看服务连接情况。

只有 Leader 可以执行：

- 发布配置；
- 删除配置；
- 添加 Raft 成员；
- 移除 Raft 成员；
- 创建定时任务；
- 修改任务定义；
- 取消任务；
- 修改持久化服务注册信息。


3.5 Follower 页面
-----------------

Follower 页面只提供：

- 查看本节点状态；
- 查看当前 Leader；
- 跳转 Leader 管理页面；
- 退出集群；
- 查看本地恢复状态；
- 查看本地最后日志索引；
- 查看本地最后应用索引；
- 查看本节点连接的客户端。

Follower 不允许修改控制面数据。


3.6 节点退出
------------

正常退出步骤：

1. Follower 请求 Leader 删除自己；
2. Leader 执行 RemoveMemberAsync；
3. Raft 配置变更提交；
4. 确认当前节点不再出现在 Members；
5. 删除本地 WAL、快照和 cluster-config；
6. 删除本地 runtime.json；
7. 关闭当前进程。

异常关闭不等于成员移除。

临时异常：

- 保留成员资格；
- 节点恢复后继续追赶日志。

永久下线：

- Leader 执行 RemoveMemberAsync；
- 从成员配置删除；
- 目标节点数据可以清理。


四、客户端第一次连接和节点分配
==============================

4.1 客户端连接流程
------------------

客户端 SDK 启动时配置多个配置中心地址：

{
  "ClusterEndpoints": [
    "http://127.0.0.1:7001",
    "http://127.0.0.1:7002",
    "http://127.0.0.1:7003"
  ]
}

客户端随机选择或轮询一个节点发起首次连接。

首次连接的目的：

- 找到当前可用节点；
- 获取当前集群信息；
- 获取客户端应该连接的服务节点；
- 获取配置；
- 获取服务发现信息。


4.2 第一次连接和第二次 gRPC 连接
--------------------------------

建议使用两阶段连接：

第一阶段：Bootstrap/Discovery 连接

用于：

- 获取当前 Leader；
- 获取可用集群节点；
- 获取客户端分配的服务节点；
- 获取配置；
- 获取服务端能力；
- 获取协议版本。

第二阶段：业务双向流连接

客户端根据第一次返回的分配结果，连接指定节点：

- 建立 gRPC 双向流；
- 发送客户端注册信息；
- 服务端确认注册；
- 服务端保存本地连接；
- 客户端开始接收配置变更和任务通知。

第一次连接可以使用普通 gRPC Unary RPC，第二次连接使用 gRPC Bidirectional Streaming。


4.3 节点分配算法
----------------

第一次连接的节点可以根据以下算法选择：

- 当前连接数最少；
- 加权最少连接；
- RoundRobin；
- WeightedRoundRobin；
- ZoneAware；
- 客户端所在区域；
- 服务端节点健康状态；
- 节点容量；
- 节点版本；
- 节点负载。

默认建议：

1. 过滤不健康节点；
2. 过滤不可用节点；
3. 按连接数从小到大排序；
4. 同连接数时按权重选择；
5. 同权重时使用轮询；
6. 返回一个推荐节点；
7. 客户端建立第二条 gRPC 双向流。

不能保证绝对平均，只能在相同权重和正常网络环境下尽量均衡。


4.4 分配是否写入 Raft
---------------------

如果节点分配只是临时优化，可以不写入 Raft，只保留本地缓存。

如果分配结果影响任务投递，则必须写入 Raft，至少保存：

- ClientId；
- InstanceId；
- AssignedNodeId；
- AssignmentVersion；
- LeaseId；
- LeaseUntilUtc。

建议：

服务连接本身使用本地缓存；
客户端归属和任务分配使用 Raft；
每次心跳不写 Raft。


五、服务注册和发现
==================

5.1 服务实例模型
----------------

服务实例包含：

- ServiceName；
- InstanceId；
- Namespace；
- Group；
- Host；
- Port；
- Protocol；
- Version；
- Weight；
- Zone；
- Metadata；
- Ephemeral；
- Healthy；
- LastHeartbeatUtc；
- ExpireAtUtc。


5.2 临时实例
------------

临时服务实例适合普通微服务。

生命周期：

服务启动：

RegisterConnection

服务运行：

本地心跳和 gRPC KeepAlive

连接断开：

立即在本地标记 Disconnected

宽限期内重新连接：

恢复 Connected

超过租约时间没有重连：

ServiceExpired

只有以下事件写入 Raft：

- 注册成功；
- 连接归属变更；
- 正常注销；
- 租约确认过期；
- 任务分配变化。

每次心跳不写 Raft。


5.3 持久实例
------------

持久实例适合：

- 数据库代理；
- 固定网关；
- 基础设施服务；
- 固定地址服务；
- 长期保留的服务实例。

持久实例可以写入 Raft，除非手动注销，否则不会因为短暂断线自动删除。


5.4 服务发现查询
----------------

客户端可以通过任意集群节点查询：

GET /api/naming/services/{serviceName}/instances

当前节点优先从本地缓存读取。

如果本地缓存版本太旧：

1. 请求当前 Leader；
2. 获取最新服务列表；
3. 更新本地缓存；
4. 返回客户端。

不建议所有查询都转发 Leader。


六、配置中心
============

6.1 配置模型
------------

配置键使用：

- Namespace；
- Group；
- DataId。

示例：

Namespace：

production

Group：

ORDER_SERVICE

DataId：

appsettings.json

完整配置键：

production/ORDER_SERVICE/appsettings.json


6.2 配置发布流程
----------------

管理员发布配置：

1. 请求发送到任意节点；
2. Follower 转发到 Leader；
3. Leader 校验权限；
4. Leader 生成 ConfigPublished 命令；
5. 命令写入 Raft；
6. 多数派确认；
7. 所有节点执行 ApplyAsync；
8. 配置版本递增；
9. 通知订阅客户端；
10. 记录配置审计。

配置删除同理：

ConfigDeleted


6.3 客户端启动获取配置
----------------------

客户端启动时：

1. 连接任意集群节点；
2. 获取 Namespace、Group、DataId；
3. 获取配置内容；
4. 获取配置版本和 MD5；
5. 保存本地缓存；
6. 使用配置初始化服务；
7. 建立配置监听流；
8. 配置变化后热更新。

本地缓存文件：

config-cache.json

配置中心不可用时，客户端启动策略支持：

- FailFast；
- UseLastKnownGood；
- UseDefault。

配置策略必须可以配置。


6.4 配置推送
------------

建议使用 gRPC Server Streaming：

客户端：

WatchConfig

服务端：

ConfigChanged

客户端断线后带上：

- LastVersion；
- LastRevision；
- LastMd5。

服务端可以直接发送最新完整配置，不要求客户端必须收到每条中间事件。


七、定时任务
============

7.1 调度原则
------------

不能让所有 Raft 节点都执行同一个任务。

正确方式：

1. 所有节点拥有一致的任务定义；
2. Leader 扫描到期任务；
3. Leader 选择目标节点；
4. Leader 写入 TaskLeased；
5. 只有获得租约的节点执行；
6. 目标节点通过 gRPC 双向流向客户端下发；
7. 客户端返回结果；
8. 结果再次写入 Raft；
9. 所有节点更新任务状态。


7.2 任务状态
------------

任务状态：

- Pending；
- Leased；
- Dispatched；
- Running；
- Succeeded；
- Failed；
- Retrying；
- Cancelled；
- DeadLetter。


7.3 任务分配
------------

任务分配日志包含：

- TaskId；
- RequestId；
- ExecutionId；
- AssignedNodeId；
- AssignedClientId；
- LeaseId；
- LeaseUntilUtc；
- AttemptCount。

示例：

{
  "taskId": "task-001",
  "requestId": "req-001",
  "executionId": "exec-001",
  "assignedNodeId": "node-002",
  "assignedClientId": "worker-001",
  "leaseId": "lease-001",
  "leaseUntilUtc": "2026-10-12T10:00:00Z"
}

只有 AssignedNodeId 对应的节点可以实际发送任务。


7.4 服务端主动下发任务
----------------------

服务端生成任务：

1. 生成 RequestId；
2. 生成 TaskId；
3. 生成 ExecutionId；
4. 将 TaskCreated 写入 Raft；
5. Leader 扫描任务；
6. Leader 分配到一个节点；
7. 目标节点通过 gRPC 双向流发送给客户端；
8. 客户端执行；
9. 客户端原样返回 RequestId；
10. 服务端写入 TaskCompleted 或 TaskFailed。

客户端返回：

{
  "requestId": "req-001",
  "taskId": "task-001",
  "executionId": "exec-001",
  "status": "Succeeded",
  "result": {},
  "startedAtUtc": "...",
  "completedAtUtc": "..."
}

RequestId 重试时保持不变。

ExecutionId 每次执行尝试可以变化。


八、幂等和重复执行
==================

8.1 RequestId 定义
------------------

RequestId 是业务幂等 ID，不是 Raft 日志索引。

Raft 日志索引由 Raft 生成：

entry.Index = 45

RequestId 由业务层生成：

requestId = req-001

两者不能混用。


8.2 Leader 故障场景
-------------------

可能发生：

1. 旧 Leader 已经提交任务；
2. 旧 Leader 尚未返回客户端；
3. 客户端以为失败；
4. 新 Leader 已经接管；
5. 客户端使用同一个 RequestId 重试；
6. 新 Leader 再次收到该请求。

状态机必须检查 RequestId：

如果已成功：

- 不重复执行；
- 返回之前的结果。

如果未成功：

- 执行业务；
- 保存结果；
- 写入去重记录。


8.3 幂等状态必须进入快照
------------------------

以下信息必须随状态机快照持久化：

- RequestId；
- TaskId；
- ExecutionId；
- 已处理状态；
- 业务结果；
- 错误结果；
- 任务状态；
- 任务租约。

不能只保存在：

- Controller 内存；
- LocalCache；
- 单机 Dictionary；
- 单机数据库而没有复制策略。


九、Raft 日志和快照
====================

9.1 日志生命周期
----------------

一条 Raft 日志经历：

1. Leader 接收命令；
2. Leader 写本地 WAL；
3. Leader 复制给 Follower；
4. 多数派确认；
5. 日志提交；
6. 各节点 ApplyAsync；
7. 状态机生成快照；
8. Snapshot 持久化成功；
9. WAL 删除快照覆盖的旧日志。


9.2 快照条件
------------

ApplyAsync 可以根据以下条件请求快照：

- 已应用日志数量；
- WAL 文件大小；
- 状态机状态大小；
- 时间间隔。

示例：

每应用 1000 条业务日志请求一次快照。

但 PersistAsync 必须真的保存完整状态，不能只是：

return ValueTask.CompletedTask;


9.3 快照必须保存
----------------

- 配置数据；
- 任务定义；
- 任务状态；
- 任务执行结果；
- RequestId 幂等表；
- 必要的持久化服务元数据。


9.4 不建议快照保存
------------------

- 临时服务心跳；
- 高频连接状态；
- gRPC 每次调用记录；
- 短期指标；
- 可重建缓存。


十、gRPC 协议
=============

10.1 任务执行协议
-----------------

TaskExecutor 服务：

- Execute；
- Cancel；
- Health。

TaskRequest 包含：

- RequestId；
- TaskId；
- ExecutionId；
- Command；
- Payload；
- Deadline。

TaskResponse 包含：

- RequestId；
- TaskId；
- ExecutionId；
- Status；
- Result；
- Error；
- StartedAt；
- CompletedAt。


10.2 配置推送协议
-----------------

ConfigService：

- GetConfig；
- WatchConfig；
- PublishConfig。

WatchConfig 使用 Server Streaming。

配置变更后服务端推送 ConfigChanged。


10.3 服务连接协议
-----------------

DiscoveryService：

- Bootstrap；
- GetAssignment；
- Register；
- Heartbeat；
- Unregister；
- WatchAssignment。

客户端可以先进行 Bootstrap，再建立业务双向流。


十一、gRPC 调用耗时
===================

每次微服务调用需要记录：

- CallId；
- TraceId；
- SourceService；
- TargetService；
- Method；
- StartedAtUtc；
- CompletedAtUtc；
- DurationMs；
- Status；
- Error；
- Attempt。

调用日志不写入 Raft。

建议：

- Trace 使用 OpenTelemetry；
- Metrics 使用 OpenTelemetry Metrics；
- 调用明细写独立数据库；
- 审计信息写独立审计存储；
- 配置变更和任务状态才写入 Raft。


十二、客户端 SDK
================

SDK 目标框架：

netstandard2.0

建议拆分：

- DotNetCloudControl.Client；
- DotNetCloudControl.Configuration；
- DotNetCloudControl.Discovery；
- DotNetCloudControl.Tasks；
- DotNetCloudControl.Grpc；
- DotNetCloudControl.Diagnostics。

SDK 主要功能：

1. 多节点连接；
2. 随机或轮询选择初始节点；
3. 获取当前 Leader；
4. 获取分配节点；
5. 建立 gRPC 双向流；
6. 注册服务；
7. 发送心跳；
8. 获取配置；
9. 监听配置变更；
10. 接收定时任务；
11. 返回任务结果；
12. 自动重连；
13. 复用 RequestId 重试；
14. 本地缓存配置；
15. 服务发现；
16. gRPC 客户端代理；
17. 统计调用耗时。


SDK 配置示例：

{
  "ClusterEndpoints": [
    "http://127.0.0.1:7001",
    "http://127.0.0.1:7002",
    "http://127.0.0.1:7003"
  ],
  "NamespaceId": "production",
  "Group": "DEFAULT_GROUP",
  "ServiceName": "order-service",
  "InstanceId": "order-service-001"
}


十三、管理前端
==============

前端技术：

- Vue 3；
- TypeScript；
- Vite；
- Element Plus；
- Pinia；
- Vue Router；
- ECharts。

注意：

Vue 3 应使用 Element Plus，不使用主要面向 Vue 2 的 Element UI。


13.1 页面

初始化：

/setup

集群：

/cluster

成员管理：

/cluster/members

配置中心：

/config

配置历史：

/config/history

服务发现：

/naming

服务实例：

/naming/instances

定时任务：

/tasks

任务执行记录：

/tasks/executions

gRPC 调用监控：

/observability/grpc

日志和 Trace：

/observability/traces


13.2 Follower 页面限制

Follower 只显示：

- 本节点状态；
- 当前 Leader；
- 跳转 Leader；
- 退出集群；
- 本地连接信息；
- 本地日志状态。

服务端仍然必须校验权限和 Leader 身份，不能只依赖前端隐藏按钮。


十四、云原生集成
================

部署形态：

- Docker；
- Docker Compose；
- Kubernetes StatefulSet；
- PersistentVolumeClaim；
- Service；
- ConfigMap；
- Secret；
- Helm；
- PodDisruptionBudget；
- Kubernetes Operator；
- .NET Aspire；
- OpenTelemetry。

Raft 节点必须使用 StatefulSet 和持久化磁盘。

不能把 Raft 节点当作无状态 Deployment 任意扩缩容。


十五、项目开发阶段
==================

Phase 1：Raft 基础稳定

目标：

- 初始化；
- Bootstrap；
- Leader/Follower；
- 动态加入；
- 动态退出；
- 故障选举；
- 节点恢复；
- 日志同步；
- 状态机应用；
- Snapshot；
- WAL 压缩；
- RequestId 幂等。

验收：

- Leader 宕机后其他节点选出 Leader；
- 旧 Leader 恢复为 Follower；
- 日志提交不会丢失；
- 未提交日志可以被覆盖；
- 已提交日志不会被覆盖；
- 节点可以安装快照；
- 节点重启后状态可恢复。


Phase 2：配置中心

目标：

- Namespace；
- Group；
- DataId；
- 配置发布；
- 配置读取；
- 配置版本；
- 配置历史；
- 配置回滚；
- 配置监听；
- 权限控制；
- 审计记录。

验收：

- 配置变更通过 Raft；
- 所有节点最终版本一致；
- 客户端可以监听变更；
- 客户端可以断线恢复；
- 配置发布支持幂等。


Phase 3：服务发现

目标：

- 临时实例；
- 持久实例；
- 服务注册；
- 服务注销；
- gRPC 双向流；
- 心跳；
- TTL；
- 健康状态；
- 节点分配；
- 服务订阅；
- 负载均衡。

验收：

- 客户端可以连接任意集群节点；
- 客户端可以得到分配节点；
- 客户端可以建立第二条 gRPC 双向流；
- 连接断开后服务状态更新；
- 服务实例过期后自动移除；
- 心跳不写入 Raft；
- 服务发现不会拖慢 Raft。


Phase 4：定时任务

目标：

- Cron；
- 任务定义；
- 任务租约；
- 节点分配；
- 客户端任务执行；
- 超时；
- 重试；
- 取消；
- 结果；
- 死信；
- 幂等。

验收：

- 同一任务不会被多个节点同时执行；
- Leader 切换后任务不丢失；
- 客户端重复收到相同 RequestId 时不重复执行；
- 任务结果可查询；
- 失败任务可以重试；
- 任务状态在集群内一致。


Phase 5：gRPC 生态

目标：

- SDK；
- 服务解析；
- gRPC 客户端；
- 重试；
- 超时；
- 熔断；
- 限流；
- Load Balancer；
- Trace；
- Metrics；
- 调用耗时；
- 调用记录。

验收：

- 客户端可以通过服务名调用服务；
- 自动选择健康实例；
- 支持超时；
- 支持重试；
- 支持 RequestId；
- 能显示调用耗时；
- 能显示失败原因；
- 能生成 TraceId。


Phase 6：云原生生态

目标：

- Docker；
- Helm；
- Kubernetes；
- StatefulSet；
- PVC；
- Operator；
- .NET Aspire；
- OpenTelemetry；
- Dashboard；
- 自动扩容和安全缩容。

验收：

- 集群节点可持久化；
- 滚动升级不破坏 Raft；
- 节点扩容走 AddMember；
- 节点缩容走 RemoveMember；
- Pod 重启后可以恢复；
- OTel 数据可查看；
- 管理页面可以查看集群状态。


十六、当前实现中的关键原则
==========================

1. Bootstrap 只用于第一次创建集群；
2. Bootstrap=true 不代表永远是 Leader；
3. 进程启动不等于 Raft 成员加入；
4. 进程关闭不等于 Raft 成员移除；
5. AddMember 和 RemoveMember 必须由 Leader 串行提交；
6. 临时服务注册不应和 Raft 集群成员混用；
7. 心跳不应每次写入 Raft；
8. 任务分配必须有 AssignedNodeId；
9. 任务执行必须有 RequestId；
10. RequestId 重试时不能改变；
11. ExecutionId 可以随着执行尝试改变；
12. 幂等状态必须进入状态机和快照；
13. gRPC 调用明细不应写入 Raft；
14. 配置中心变更必须写入 Raft；
15. Snapshot 成功后才可以压缩旧日志；
16. PersistAsync 和 RestoreAsync 必须使用完全一致的数据格式；
17. LocalCache 只能作为展示缓存，不能代替持久化状态；
18. 节点重启后必须从 WAL/Snapshot 恢复状态；
19. Leader 故障后新 Leader 继续使用已有日志，不重新生成业务 RequestId；
20. 未提交日志可能被覆盖，已提交日志不能被覆盖。


十七、推荐的数据分类
====================

强一致 Raft 数据：

- 配置；
- 任务定义；
- 任务状态；
- 持久化服务元数据；
- 租约；
- 权限；
- 路由；
- 集群成员。

最终一致或本地实时数据：

- 临时服务实例；
- 心跳；
- 连接状态；
- 健康状态；
- 节点连接数；
- 短期缓存。

独立观测数据：

- gRPC 调用记录；
- 请求耗时；
- Trace；
- Metrics；
- 错误统计；
- 性能数据。


十八、与其他 AI 沟通时的上下文摘要
==================================

这是一个正在开发的 .NET 10 云原生平台项目。

当前基础技术：

- 服务端 .NET 10；
- DotNext.Net.Cluster 6.9.0；
- DotNext.AspNetCore.Cluster 6.9.0；
- ASP.NET Core；
- Raft；
- WriteAheadLog；
- SimpleStateMachine；
- Scalar；
- gRPC；
- 客户端 SDK 目标 netstandard2.0；
- 前端 Vue 3 + Element Plus。

当前已经验证的功能：

- 节点初始化；
- Bootstrap 节点创建；
- Follower 加入；
- Leader 故障选举；
- 旧 Leader 恢复后成为 Follower；
- 动态成员添加；
- 动态成员移除；
- Raft 日志复制；
- `ApplyAsync` 在多个节点执行；
- Scalar 查看集群；
- Scalar 查看本地日志；
- Follower 写请求转发 Leader；
- 连接节点状态查看。

当前重点问题：

1. 完善 `requestId`/`raftId` 幂等；
2. 将业务状态和幂等表写入 Snapshot；
3. 实现 PersistAsync；
4. 实现 RestoreAsync；
5. 解决 WAL 日志压缩；
6. 实现配置中心；
7. 实现客户端 SDK；
8. 实现服务注册发现；
9. 实现 gRPC 双向流；
10. 实现定时任务；
11. 实现调用耗时记录；
12. 实现 Vue3 管理界面。

当前核心设计：

- Raft 管理强一致控制面；
- gRPC 双向流管理实时客户端连接；
- 服务连接成功/断开等生命周期事件可以写入 Raft；
- 心跳不写入 Raft；
- 客户端第一次连接任意集群节点；
- 集群根据连接数和节点负载返回推荐连接节点；
- 客户端第二次连接目标节点建立业务双向流；
- Leader 负责任务扫描和任务分配；
- 任务分配通过 Raft 保证一致；
- 被分配节点通过 gRPC 向客户端下发任务；
- 客户端返回原始 RequestId；
- 重试时 RequestId 不变；
- ExecutionId 用于区分不同执行尝试；
- 配置变化写入 Raft 并通知客户端；
- gRPC 调用耗时不写入 Raft，而使用 OpenTelemetry 或独立存储。


十九、与其他 AI 协作时需要避免的错误
====================================

1. 不要把所有心跳写入 Raft；
2. 不要把服务发现临时实例和 Raft 集群成员混为一谈；
3. 不要把 Raft log index 当成业务 requestId；
4. 不要由 Leader 为客户端重试请求重新生成 requestId；
5. 不要让所有节点同时执行同一个定时任务；
6. 不要只在 Controller 内存中做幂等；
7. 不要只在 LocalCache 中保存任务结果；
8. 不要在没有实现 Snapshot 的情况下删除 WAL；
9. 不要在没有多数派时强行修改成员配置；
10. 不要先删除节点数据再执行 RemoveMember；
11. 不要把 gRPC 详细调用日志写入 Raft；
12. 不要让 Frontend 的权限控制代替服务端权限控制；
13. 不要把 Raft 节点部署成没有持久化存储的普通无状态 Pod；
14. 不要认为进程启动后自动就是 Raft 正式成员；
15. 不要认为 Bootstrap=true 的节点每次重启都必须是 Leader。


二十、当前最优先开发任务
========================

第一优先级：

1. 稳定状态机恢复；
2. 完善 Snapshot；
3. 完善 WAL 压缩；
4. 完善 requestId 幂等；
5. 完善 Leader 故障后任务和配置恢复。

第二优先级：

1. 配置中心数据模型；
2. 配置发布；
3. 配置读取；
4. 配置监听；
5. 客户端 SDK 基础连接。

第三优先级：

1. 服务注册；
2. gRPC 双向流；
3. 连接状态缓存；
4. 连接生命周期事件；
5. 服务发现查询；
6. 节点分配。

第四优先级：

1. 定时任务；
2. 任务租约；
3. 任务分配；
4. gRPC 任务执行；
5. 任务结果；
6. 任务重试。

第五优先级：

1. 调用耗时；
2. OpenTelemetry；
3. Vue3 管理页面；
4. Docker；
5. Kubernetes；
6. .NET Aspire。


二十一、最终设计原则
====================

系统的核心原则是：

Raft 管理一致性事实。

gRPC 管理实时连接和任务执行。

服务发现缓存管理在线状态。

客户端 RequestId 管理业务幂等。

Snapshot 管理状态恢复和 WAL 压缩。

OpenTelemetry 管理调用观测。

Vue3 管理平台状态和运维操作。

不要让 Raft 变成所有业务数据的数据库，也不要让临时连接和高频心跳拖垮 Raft。

最终目标：

一个适合 .NET 生态、支持强一致配置、动态服务发现、可靠任务执行、gRPC 通讯和云原生部署的微服务基础平台。