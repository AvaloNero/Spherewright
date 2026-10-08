# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页以R282最新封窗事实覆盖当前状态；详见[铁源路线与全物料十写封窗](evidence/2026-10-08/iron-routing-full-material-ten-r282.md)。此前R218阶段保留在[铁源回收、备料与十写封窗](evidence/2026-10-08/iron-source-recovery-ten-r218.md)。

## 当前 owned 状态

- 当前恢复截面为和平、非沙盒、1× owned 世界，P104 / R1 / J100，normal Save `101069532`。R282十笔闭合中，`101069500`覆盖前八笔业务及第九笔Save；第十笔为健康 exact-primary Resume，之后正常自动保存至`101069532`。无在途或unknown；external `10/10 FROZEN`、lifetime `320`，普通写入仍冻结。
- 运行安装仍对应源码cohort`863d35546f6cb49fcdaec5b5814869d1e13af42b`、游戏版本`0.10.35.29104`；本次文档更新前的源码HEAD为`28a12a0d228613fe5533cf1d91ed1e3653375ecf`（CI `37724502979` success），文档更新不改变安装代码。
- 当前工厂快照为`6274 built / 0 prebuild`；R279完整采样帧为tick`101095800`、63页，封存基线之外新增7条belt、1个sorter，8个旧对象共15项允许静态字段变化、0条非互惠边。实际接缝为新belt`6273 → sorter6274/filter1001 → 旧belt1507`；矿机1496的铁源连接已确认，但不据此声称持续采矿、共享供需或长窗通过。
- 玩家库存读回为ore `0`、Fe `0`、belt `9`、fast belt `6`、sorter `0`。Fe主路与专用路的当前配置分别为`1511 slot6 → 5635 (2011/filter1101) → 1534 → 1533 → 1531`及`1511 slot0 → 1535 (2012/filter1101) → 5593 → 5592`；配置和短窗观察不证明持续净供给。
- R279复合物料采样引用`55c973661ecd49d99ed5b917686ea6c2:16` / SHA-256 `13F6ADD48166B3F0F42C8531CF685394F5C573F4139E6DC395FC276D9F39A315`，覆盖4组、29种物料、147条Native路径、source对象169及16台燃料机。各组保留自己的采样tick，不跨不同tick求和。启动观察的3个600-tick窗口合计Warper2件、Rod2件，氘产20/min对耗60/min、铁产30/min对耗54/min；这不是36,000-tick门的通过证据。Ti路径仍有缺口：P102来源经原料需求站1657、smelter530及本地供需918/916进入合金链的完整运输与平衡须在长窗前核实。
- 便携kit仍是历史配方计划，尚未备齐或执行；本页不将计划材料记作库存或准备完成。

## Gate 2 边界

- R282独立十写审计通过：`607fb94bf159473b9a38f94c4473d82c:1`，SHA-256 `87056F8785D4F8F50DD37AC1E490151A2122EE6F2A3505FF7834D57B24B46403`。这是十写窗口核销，不是整案供需验收；`wholeSupplyPassed=false`，continuous credit为`0`。
- R220、R250的原生终态成功与调用方本地失败分别保留；R273、R274、R280本地审计失败同样保留。R279是R274后另行声明的有限尾部预算，不补足或延长R274原预算。具体边界见本次[事件记录](evidence/2026-10-08/iron-routing-full-material-ten-r282.md)。
- 持续至少36,000 ticks、双补给和最终同 SHA 候选包仍未通过。铁源持续流量与共享供需仍待核验；便携kit未备齐。既有蓝图生命周期、Governor 2×门按先前验证范围保留，不重开或扩大；整案仍为`executable=false`。
