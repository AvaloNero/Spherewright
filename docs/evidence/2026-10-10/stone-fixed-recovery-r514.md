# R514 固定 LastExit 恢复与 R508 双包预检

更新：2026-10-10（Asia/Singapore）。本事件记录一次已确认的固定 LastExit 恢复、恢复后独立核验，以及同一源码SHA的离线双包预检；不构成持续供给或最终发行验收。

## 固定恢复与保存

用户确认绑定本次固定 LastExit 恢复（授权证明：R509 `36a5687fcbee4192a01f2a2ecd1e9cd2:1`，SHA-256 `D0FC26331EB82922C8B6EE296566D00F3B053504A1C3B6C554E8FE2A9EA545BB`）。旧DSP进程正常退出一次，未推测退出原因；LastExit header tick `104944137`经核对覆盖最新观察tick `104942116`及完整owned身份和Journal。保留Steam，之后经桌面入口启动一次。

启动后首次菜单只读请求发生Native `REQUEST_TIMEOUT` / Unity主线程读取超时；原R510记录 `c552bbdc746742f889f429b8143b4321:6`、SHA-256 `8A5074398A7E567641A191CDE934F674DE361330ADEA4AF26C76965B54D33F27`完整保留，该次为0 prepare、0 commit、0 accepted。root核对其没有创建写入意图且进程仍在同档后，R511在同一进程续接，没有重启、重安装或重置预算。R513原件 `b5d1c6ceddbe4395900ee6fedec2c674:82`、SHA-256 `1904D6C99D67276A072645C63ADE41708F8BAEA0FA7274C7383F412664393B34`记录唯一resume accepted并Native completed；只查询原动作，不重提交。

恢复后原生自动普通主档保存至tick `104944169`。R514独立审计 `ae832a1b24a841379240ae40140477dc:1`、SHA-256 `134A2B51938952B7753504FB6E4A271F0D07AFEE7380A84337013C253C502E72`确认新P104/R1健康、peaceful/非sandbox/1×、完整owned fingerprint与主档header、Journal完整历史/版本链durable 102。当前external 2/20、lifetime 401，当前无在途或未知动作；新窗口未打开，普通写仍冻结。先前Build的原始action仍保持历史 `outcome_unknown`，未重放或改判成功。

完整64页工厂为6313 built / 0 prebuild；与核验基线相比无新增、删除、静态差异或非互惠边。35个相关对象与5条完整Native货物路径读回保持，回程153格带链仍空载、未接新矿源。玩家位置及背包count/inc/held保持，2001×33、2011×4；本地电网满供。该次恢复和保存只核销已确认的恢复边界，不构成矿源接入或持续供给通过。

## 同SHA双包离线预检

R508使用干净源码提交 `bd749ee5380d6c374dbe43483f01b81c550892bb`，CI `37954973052`成功。离线预检seal：`07f93e2ec9c745c3b0151bab033d97ae:1`，SHA-256 `0A1CD15A2B2D2FC275705555D51DC7EAD9505C565666D53E556D3E59FD589EF4`。

手动自包含包 `Spherewright-0.4.0-win-x64.zip` SHA-256 `7d2a9390498cbb29354075b8a3cf7b342d60782f2e83ed1593a0e239da68ef28`，manifest SHA-256 `226fc2904934922733e5e1239d7c2ff5828ab0721fbe179dee69c16f9634deb5`；238项manifest文件加manifest共239项。Thunderstore包 `Spherewright-0.4.0-thunderstore.zip` SHA-256 `35b09822536bde389699b4c83e0d4fb764c47ed238a9504fe5e122bdb94d6e61`，完整性manifest SHA-256 `46a295ab447114355d70d7af7a1b0f7fad807dd163f7f0fe82eeb5439cdf84e5`；11项清单文件加完整性manifest共12项。两边manifest均标注同一sourceCommit且 `sourceDirty=false`；4个Plugin文件逐一哈希相同。

Windows PowerShell `5.1.26100.9444`下，手动包surface验证为64项工具/1项资源且退出码0，Thunderstore静态结构验证通过。包内单文件MCP经隔离本机stdio probe完成initialize、tools/list、resources/list与playbook读取，64项工具、1项资源、playbook完全匹配、进程退出码0；Bridge descriptor与LOCALAPPDATA均隔离。Thunderstore manifest仍标 `runtimeBlackBoxTested=false`。未启动Mod Manager、未安装或启动游戏、未连接Bridge、未上传；双包仅为离线预检产物，不是最终候选或0.4验收。

## 保留的未通过边界

R438/R441来源条件失败和continuous credit 0保持；`wholeSupplyPassed=false`。Warper/Rod产率门、连续≥36,000 ticks、完整29项物料、双自动补给、整合保存/恢复、实际Mod Manager安装及最终同SHA双候选包验收均未通过。R514之后的fresh sorter/source检查仍是未执行候选。
