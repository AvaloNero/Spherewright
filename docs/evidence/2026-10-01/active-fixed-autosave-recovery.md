# 有效票据的固定 AutoSave0 恢复

日期：2026-10-01（Asia/Singapore）。先完成代码/离线验证，再完成下述同批冷部署和固定候选恢复实机闭环。

## 已定位问题与授权

原固定 AutoSave0 模式只接受过期健康票据。当前 primary 正常保存点80526460低于已知进度80627903，但票据仍有效；不能加载旧 primary 回档，也不能改票据到期时间来套用过期模式。固定候选80731193的只读身份/版本/完整文件证据见[上一事件](../2026-09-30/owned-recovery-readonly-and-error-fields.md)。用户已明确要求直接恢复这个已披露的 owned 候选，不应重复询问。

新增 `reauthorize_fixed_autosave0` / evidence version4，只使用有效健康凭据下的原生固定 AutoSave0。保留两副本一致、原 durable Journal、精确进度下限/候选 tick、同版本/和平/完整 embedded identity、原 primary 覆盖目标和全文件只读 lease。fresh prepare 不加载或消费，commit 仍需准确披露摘要及明确对话授权字段；已有授权匹配候选时不再问一次。durable attempt/tombstone 必须先于原生加载。旧过期模式/version3不变，禁止任意存档、其他槽位、迁移、fallback或失败后重放。

恢复 action 自身只有在采用同 owned identity、Journal 连续、正常另存回原 primary且签发匹配的新凭据后才可成功。先前成功施工必须另行读回核销，不能由存档头推定已恢复。

## 已验证 / 未验证

- full solution Release build：0 warnings / 0 errors。
- 生产源码链接的 AutoSave0 Core 筛选：120/120；MCP相关筛选：13/13。覆盖两模式年龄隔离、双副本/Journal/quarantine/消费/到期漂移、授权摘要、固定槽无fallback、消费先于load、幂等不二次加载、严格v4 echo且拒绝旧v3/错mode。均为离线fixture，不访问真实游戏。
- 旧恢复路径回归筛选（OwnedWorldReauthorization / OwnedWorldResumeTicketStore）：66/66；包面策略脚本：37/37。
- root核只读run `ee9d0a4b31254130a79d628b61a5baeb` ord1：success，菜单gameLoaded=false /29104/R0/healthy；本地guard错误不代表Bridge失败，不曾调用close/load。之后发现DSP进程已退出，原因未定。
- 离线阶段外部accepted仍8/10；当时没有加载或新写。以下实机阶段单独核销，不用fixture替代。

## 冷部署与真实恢复闭环

- 修复commit `6bf35b7b81a2e50c8e9f42feebbc1f15552096de`已push，Windows Core CI `36743257284` success。旧恢复筛选66/66、包面策略37/37通过；原生DLL与编译引用一致。
- clean该commit生成本地手动候选包，SHA-256 `28cd7bb4a3da7b70d37814c57349c98a25cf7f88e36656c3d87349bfadec3863`；包测试证明64 tools /1 resource、包内和embedded playbook一致。这只是冷部署候选，不是0.4最终验收或发布。
- 冷部署run `3d625ab8f84b468698e73629b61771be` ord1/2：公共安装器正常完成，228个Plugin/MCP安装文件逐一匹配，原runtime/handoff票据hash不变；安装器保留可恢复备份，无游戏load。
- Steam正常启动后首个run `744a9e9374354b29a6ff50ec17f79589` ord4真实返回`BRIDGE_NOT_READY`：prototype/model preload未完成。ord5证明零commit/零accepted/无unknown。等待20秒，同一进程重新fresh prepare，没有重启或重放已接受动作。
- 成功run `1aa85a52a1e843519ba733a9756da76b`：ord4绑定v4/fixedAutoSave0/80731193/104/29104/完整identity；唯一blocker为`USER_CONFIRMATION_REQUIRED`。ord6/7唯一intent/accepted且非幂等回放，明确授权flag和digest匹配；action `9d04c465-cfd6-46a1-a47f-0e7bf632daa1` ord12为`resume-owned-game/completed/terminal/succeeded`。
- ord13：sameowned /104/29104/和平/非沙盒/1×，normal primary save tick**80731225**，观察80731293/R1/healthy、新票据可用。ord14：原Journal ID连续，**J96 durable**、无pending/error。ord15及fresh票据核对新session/正常save/J对齐。root直接独立核ord4/6/7/12/13/14，不只接受执行者摘要。
- 成功run首条到完成证据约8.9秒；不是从开发、测试、打包、启动至恢复的端到端时长。新增accepted仅1，外部窗口**9/10**、无在途；正常保存由同一native恢复action内部完成，不另发save。

## 恢复后独立核销

只读run `669740f987b54a2f9f073a370b4578b0` ord1/2/3–10/11分别为session/player/八实体/Journal，零新写。观察**80766832/R1**，最近正常save仍80731225。root直接核原回执并确定性比较，而不是重采相同现场：

- `5198→5197→5199→84`以及`5202→5201→5200`身份、位置、旋转、配方/加速配置、真实双向connections、storageConfiguration、sorter pick/insert/filter和resourceNodeIds共**14静态字段×8对象均无差异**。基线分别为consumer-stub run `bacc806ec604461491e462b5ad5e399f` ord27、consumer-sorter run `b72b179587f54856ba235c0362ed840e` ord27–29、source run `37217b5f0c7645e9ae039d4ee646aa91` ord12–15；包含5198的独立原基线，不误用缺该对象的sorter快照。动态buffer/cargo/功率与静态配置分开，不把它们全部忽略后宣布持续产出。
- 完整玩家**16行item/count与source run ord16相同**，其中belt367、sorter1。5199仍filter6004，真实`5197:4→5199:1`与`5199:0→84:2`双端对应；sorterEndpoints的unavailable和belt虚拟slot=-1不当作真实端口保证。3051原输入`slot4←5113:0`保留。
- Journal的**全部96事件**及origin/current versions、versionTransitions、Journal ID、tracking mode、创建/追踪起点、历史覆盖字段与原run `062cd8fce1024bbf805f67a0381da62a` ord3完全相同。root派生核证记录run `f670096f14844f74ab89be740f6d6a65` ord1，零游戏调用/写入；session/tick仅为本次新现场，不伪装旧值。
- 本次证明选定施工前缀和材料恢复，不是全厂十写审计。源head5202与tail5200仍有自由端，consumerhead5198未接主干，3051库存紫糖1824、Lab84紫色研究点0；**没有证明互通、实际送达或持续科研**。下一唯一物流blocker仍是旧主干路径在1932处原生碰撞，不重试原路径、不改已成功前缀。

新增模式不替代十写、单写者或独立验收，无tag/release/Thunderstore发布。
