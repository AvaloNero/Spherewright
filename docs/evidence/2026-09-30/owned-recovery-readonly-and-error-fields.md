# 只读恢复核验与Bridge错误元数据

日期：2026-09-30。以下为root已核原回执和候选索引；本轮维护未访问Bridge、游戏或受保护运行目录，也未执行加载、保存或部署。

## 只读恢复证据

- run `63473cb7e2cb4418b57d22578fb52120` ord1返回`success=false`、`REQUEST_TIMEOUT`及`The Unity main-thread read request timed out.`，属于主线程读超时，不是descriptor查找失败。
- run `9ad0821ccfce4bc597b55a676578ca90` ord1报告`BridgeConnected=true`、`gameLoaded=false`、版本29104、R0、healthy及`restartResumeAvailable=true`；session、tick、localPlanet为空，DTO没有`menuReady`字段。菜单级响应不证明native载入就绪。
- 只读恢复证明run `6a6264c35dbf46e9bdf62277af6a13f5` ord1：exact-primary tick80526460低于已知进度下限80627903。LastExit解析抛出`InvalidDataException`（native save length/header version unsupported）；不能据此判定身份错误、存档损坏分类或崩溃。
- 固定别名`fixedAutoSave0`候选tick80731193、版本29104、完整owned身份匹配、和平且非沙盒，覆盖已知进度下限；只读文件长度12,100,181 bytes，完整文件绑定指纹 `sha256:ca928a863fc8d75ee4388717fff15af4e01c55f4d9bd829b5a8e55850a925e3f`。此别名来自当前DLL默认映射；未通过native `GameSave.SavePath` prepare重绑，故不是已恢复或已验证实体的证据。

用户已要求直接读取已披露的固定候选。root正在修复仅限固定`fixedAutoSave0`的有效票据恢复路径；候选尚未加载。此指令不扩展为任意路径/候选访问，不允许回滚到低于已知进度的primary，也不放宽现有受保护恢复、身份、Journal或终态检查。accepted历史仍为8/10；J96 durable仅见于较早只读run `062cd8fce1024bbf805f67a0381da62a` ord3，不能代表候选已恢复。

## Bridge错误字段与离线验证

调用方仅在异常`Data`中提供封闭的operation、code、message、retryable及recovery元数据；不复制响应body或token、不自动重试，也不改变accepted动作语义。测试结果：`test-action-client.ps1` 87项通过、0游戏调用；`test-stage-tools.ps1` 26项通过（storage 28项、material 41项检查）、0游戏调用；私有入口`-Mode smoke`通过，0游戏调用、0游戏写入。执行时间不是端到端恢复耗时。

未证明：候选已加载、实体/工厂恢复、Journal连续性已复核，或正常保存已覆盖恢复后的状态。
