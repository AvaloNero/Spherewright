# PowerShell stdin 入口空执行与证据核销

日期：2026-10-01（Asia/Singapore）。这是调用入口诊断与离线回归，不是游戏施工或产线里程碑；没有调用Bridge或游戏。

## 入口诊断

一次here-string任务经 `pwsh -NoProfile -Command -` 管道传入，缺少批次结束空行；进程返回exit0、stdout空，也没有run或action。不能单凭exit0或空输出判定执行或未执行。root用同型纯离线try/catch fixture复现stdin没有进入body；补结束空行/使用Encoded入口可进入body，直接多行 `pwsh` 脚本也能看到marker。新增 `scripts/test-powershell-entrypoint.ps1` 覆盖stdin、物理 `-File`、Encoded入口和已知exit17错误传播，3/3通过，gameCalls=0；stdin为exit0但marker=false。

## 业务状态核销

受保护wrapper在发出commit前先持久化commit-intent。精确方法索引里最新prepare是run `c781f2adb51848bda796c8472bb324ee` ord5、最新intent是 `8b55f9337ce34fda89d5b2a1e784518b` ord5；最后已知`commit_build`仍是既有施工run `1af813c9e5104569ac6c21752e35bd61` ord8，不是这次空输入的新动作。

独立fresh run `9b723496266048af8d81fdfdc2a38281` ord1/2仍为同一owned session /planet104 /29104 /healthy，tick81441930/R7、lastSaved81208004；玩家2001=360、2011=1，无无人机pending/working。只读快照run `c8dcbb92daf1496985753aa9a780fbf7` ord1为tick81484414、prebuild=0、无nextCursor。结合wrapper顺序、receipt索引与两份fresh状态，root核销该调用为0 accepted；执行者已停止，窗口仍3/10，无in-flight、unknown或quarantine，也没有丢弃已accepted动作。

指南新增的是仓库内调用建议，尚未作为新版本MCP资源部署。该事件不证明后续施工结果，也不授权复用旧plan token；本次安全闭环仅纠正入口并核销失败调用。
