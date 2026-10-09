# Spherewright 当前快照

更新：2026-10-09（Asia/Singapore）。当前运行代码仍来自 `d6517834d4d8c55f48f841525008b4cde0a5f6e5`，该批冷部署228个文件、64个工具、1项资源；源码2722项测试通过，CI `37857133940`成功。本次证明逻辑修复已通过202项相关策略测试和本机完整Release构建，尚未冷部署或实机验证。

## 当前 owned 状态

- 同一和平、非沙盒、1×的owned P104/R2处于写隔离。R498接受一笔两段空带接入5321的Build，原action `30d8ca72-480b-4986-af81-c3ae694bbad1`仍为 `outcome_unknown`、`succeeded=false`；未重放或改判成功。分拣器、保存和新矿源施工均未尝试。新固定20窗口1/20、lifetime400，原7/20窗已独立封存，未用额度不转移。当前普通写入冻结。
- R503完整独立核验6313 built / 0 prebuild：新增5323→5322→旧5321，全部连接互惠；八段返回带的完整153格Native路径空载，其他四条完整路径的成员、长度、输入/输出路径ID及旧实体静态几何保持。2001库存35→33，其余count/inc/held及玩家位置保持；Journal完整历史/版本链为durable102，当前本地网络满供。旧实体差异为5321唯一输入与Native旋转，以及1213的节点37消失；R504 fresh Native确认37已不存在，正常采掘耗尽属于推断。
- 最近正常主档保存仍为 `103307910` / Journal durable102，未覆盖本次Build。R504精确原action的进程内隔离核销preview返回 `ACTION_OUTCOME_UNKNOWN`，明确缺少保留建造计划或隔离时库存快照；R505独立核对原失败回执。物理前缀已证明与Native终态未知同时成立。DSP仍运行，未关闭、加载或安装；需准备本次固定受保护恢复方案并取得本次隔离恢复的后续确认。此前R492确认只涵盖此前候选。
- 本次修复仅在Native创建后识别已证明的精确negative prebuild输入，保持prepare和完整绑定/拓扑门；同时在Native创建前留存prepared步骤、在异常隔离时留存库存。修复无法回填已运行旧进程的丢失记录。
- 新返回前缀仍未接入新矿源。R438/R441来源失败、continuous credit0及 `wholeSupplyPassed=false` 保持。29物料、Warper/Rod各至少1/min且连续≥36,000 ticks、双自动补给、整合保存恢复及最终同SHA双候选包仍未通过；已通过的蓝图生命周期、Governor 2×等范围保持，封闭的H侧候选不重开。

阶段索引：[R505接缝未知终态与修复](evidence/2026-10-09/stone-return-join-quarantine-r505.md)、[R492固定隔离恢复](evidence/2026-10-09/fixed-quarantine-recovery-r492.md)、[R472此前隔离](evidence/2026-10-09/empty-belt-live-quarantine-r472.md)、[R449石料来源](evidence/2026-10-09/stone-source-recovery-r449.md)、[R438/R441来源书挡](evidence/2026-10-09/full-source-bookends-r441.md)。当前快照记录现状；历史事实留在各自事件页。
