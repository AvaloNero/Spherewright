# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页覆盖最新已核事实；本窗口封存见[R185燃料耦合施工与十写封窗](evidence/2026-10-08/fuel-coupling-ten-r185.md)。整案验收仍未通过。

## 当前 owned 状态

- 当前为和平、非沙盒、1× owned 世界，P104/R23/J100，normal Save `100842338`；external `10/10 FROZEN`、lifetime `300`。Root R185 独立审计 `74ddae3f45a441129389cc8e51f412d1:1`（SHA-256 `48AD375BDEE557B7ED75D44A735218098BBD6064C4A654F43C58970BE2C5B7B2`）确认10个唯一意图均已闭合：9笔成功、1笔已知Move失败；无重放、unknown或在途，Save覆盖全部10笔。普通commit保持冻结。
- 当前仍运行源码提交 `863d35546f6cb49fcdaec5b5814869d1e13af42b`（CI `37698616442` success）对应cohort，游戏版本 `0.10.35.29104`；本次是文档提交，不改变已安装代码。
- R184完整工厂快照 `099a4fd7c32b421db7a27f7a88b2b43c:75`（SHA-256 `C332D0973353EFD2D1465F0B6CA90DDAE044F35779040B047F19C3861EE40612`）为6229 built、0 prebuild；相对封存6198基线新增28条belt和3个sorter、无删除。旧对象4205与3303各有一条新增互惠连接声明，其余已核静态拓扑/配置无变化。N3为224节点/558消费者/134发电机、full-serve；该帧不冒称完整电网姿态快照。
- 当前玩家库存：Fe `2`、gear `0`、belt `37`、普通sorter `5`、fast sorter `0`、pole `0`、circuit `930`；其余物品、inc/held及科研状态保持。玩家在Move的已知停滞终态后保持当前位置，未将停滞位置当作目标到达证据。

## Gate 2 边界

- 本窗完成G侧两段各8条belt和3个sorter；两条完整8-belt Native path观察到物品`1109`。G sorters分别为`6215:6199→6214`、`6216:6207→3303`、`6217:4205→6206`，均为2011/filter1109，源端最后接入。R侧已建的12条Native带按序为`6218,6220,6219,6221,6222,6223,6224,6225,6226,6227,6228,6229`，该路径仍空载。此对象顺序不得按ID排序推断。
- 同tick对`3063`的读回为`bufferedFuel=0`、已装载燃料item `1109`（能量`1 J`）、capacity/generated均`0`；这不是稳定燃烧或持续供料证明。旧燃料接口`3069–3072`保持不变；本窗没有核销持续电力/副产物流量。
- 保守整段上限为64 belt、8 sorter；完整设计为61 belt、7 sorter。当前余下R路径为20+13共33条belt及4个filter1114接缝，现有37条belt和5个sorter足够，并预计施工后剩4条belt和1个sorter；无需新增材料制作、电塔、发电机或拆厂。旧`G-south-two-grid-rows`候选族因两次Native拒绝已退役，不重试。
- 下一步须在后续准确SHA绿CI及root明确重开后施工剩余R段并核4个接缝，再以新鲜读回核燃料/共享来源、竞争与电力。当前continuous credit为`0`；完整共享供需、连续≥36000 tick、燃料/翘曲器双自动补给、整合保存恢复和最终同SHA双候选包均未通过，整案保持`executable=false`。已通过的旧蓝图生命周期和Governor 2×门保持历史通过，不由本窗重开或替代。
