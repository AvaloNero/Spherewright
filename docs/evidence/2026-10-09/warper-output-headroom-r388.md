# Warper 仓储空间与 R388 核销

R379 的独立读回 `6534fe4c6dd2459f9a6da31579e6f8a5:1`（SHA-256 `45690559DEC4673D0E3CD79B920E72E66A8BD1DF1E83D6B20ECE8A5AAD3D6272`）确认仓库`5331`有`1210`×3000并满载，外排队列为71；生产端`5329`的r78进度为6,000,000，10个输出受仓储空间限制。观察满仓和取出成品只说明有限空间限制，不证明生产速率。

R380 的唯一Move accepted后，caller等待90秒超时停止。R382 原件 `d9960583c0ed4f45816c925c8666222f:10`（SHA-256 `AC665D082D30B546F01765C6F1634144524B491EA22BF631B8F1650935A3C374`）核对同一原动作终态为`action_failed/position_stalled`、停滞180 ticks，并明确不重试同一目标。R383 独立读回 `c063e50600f347babef191146d284be5:1`（SHA-256 `D4AE12AB35F7464793E679482F65FC1D54EF4E04C36A0C357376E375340EDDA6`）确认完整库存未变、无在途；玩家距仓库73.1299m，处于78m范围内，因此没有另行移动或重放。

R384 对5350的7请求预检在0 accepted时停止。R387校对后采用实际Native来向`5365←5352`、路径202，修正的是固定读取参数。R386 原件 `d9784c76f1e04ea1a1ec99be0ba79f35:34`（SHA-256 `B3F03E7106F9014752EC233E579598E732734D11ED1F4C4C42AD83765F453FDA`）记录24请求/4.51秒/2笔accepted：普通转出`1210×100`及Save。Native仓库存量由3000降至2900，玩家库存由30增至130，正常Save为`102564883/R89/J102`。

R388 独立审计 `c63d2e87d66a43d89b27bb053b29ea7a:1`（SHA-256 `A2A5DF0CB2FE6426D8CB5A651F145D0F1EFBF2FD2B060A783B0A7B7AFEB5769C`）核对原计划、intent、唯一ACK及终态、完整库存/inc/held、7个受影响对象静态字段、完整Native路径202、供电、Journal和Save覆盖。总计3笔accepted额度（失败Move、转出、Save）已用尽；当前R89、external `3/20`、lifetime `378`，窗口仍开放但冻结，无在途或unknown。未新建对象、改配置、手搓或升级。

该100件取货仅腾出有限仓储空间，未证明r78稳定产率。此前R377确认一次原油到达`707`也不构成连续供给验收；continuous credit仍为0，`wholeSupplyPassed=false`，整案仍为`executable=false`。R323/R324来源条件失败及R344诊断保持不变；下一步持续供需观察范围尚待root另行预声明。
