# 有效票据的固定 AutoSave0 恢复

日期：2026-10-01（Asia/Singapore）。这是代码/离线验证事件，尚非恢复实机通过。

## 已定位问题与授权

原固定 AutoSave0 模式只接受过期健康票据。当前 primary 正常保存点80526460低于已知进度80627903，但票据仍有效；不能加载旧 primary 回档，也不能改票据到期时间来套用过期模式。固定候选80731193的只读身份/版本/完整文件证据见[上一事件](../2026-09-30/owned-recovery-readonly-and-error-fields.md)。用户已明确要求直接恢复这个已披露的 owned 候选，不应重复询问。

新增 `reauthorize_fixed_autosave0` / evidence version4，只使用有效健康凭据下的原生固定 AutoSave0。保留两副本一致、原 durable Journal、精确进度下限/候选 tick、同版本/和平/完整 embedded identity、原 primary 覆盖目标和全文件只读 lease。fresh prepare 不加载或消费，commit 仍需准确披露摘要及明确对话授权字段；已有授权匹配候选时不再问一次。durable attempt/tombstone 必须先于原生加载。旧过期模式/version3不变，禁止任意存档、其他槽位、迁移、fallback或失败后重放。

恢复 action 自身只有在采用同 owned identity、Journal 连续、正常另存回原 primary且签发匹配的新凭据后才可成功。先前成功施工必须另行读回核销，不能由存档头推定已恢复。

## 已验证 / 未验证

- full solution Release build：0 warnings / 0 errors。
- 生产源码链接的 AutoSave0 Core 筛选：120/120；MCP相关筛选：13/13。覆盖两模式年龄隔离、双副本/Journal/quarantine/消费/到期漂移、授权摘要、固定槽无fallback、消费先于load、幂等不二次加载、严格v4 echo且拒绝旧v3/错mode。均为离线fixture，不访问真实游戏。
- 旧恢复路径回归筛选（OwnedWorldReauthorization / OwnedWorldResumeTicketStore）：66/66；包面策略脚本：37/37。
- root核只读run `ee9d0a4b31254130a79d628b61a5baeb` ord1：success，菜单gameLoaded=false /29104/R0/healthy；本地guard错误不代表Bridge失败，不曾调用close/load。之后发现DSP进程已退出，原因未定。
- 外部accepted仍8/10，无新施工或恢复commit；J96仍仅为历史durable证据。本事件不宣称已部署、已加载、Journal已复验或施工前缀已恢复。

下一步唯一恢复阶段：同批冷部署 → fresh native固定候选prepare/精确核验 → 使用已授予授权唯一commit → 同action终态/正常save/新凭据/Journal → 独立核此前实体与材料。新增模式不替代十写、单写者或独立验收。
