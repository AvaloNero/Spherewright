# Spherewright 当前快照

更新：2026-10-03（Asia/Singapore）。本文是覆盖式当前摘要；本阶段原件及其边界见[Gate 2证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。

## 世界与保存边界

- DSP/Steam保持同一owned世界运行，DSP版本`0.10.35.29104`。本窗正常保存tick **87389880 / R37 / J97**；完整工厂审计closing **87396048**。之后局部只读诊断最新closing为 **87565358 / R37 / save87389880**，不是新保存或完整工厂审计；J97只记完整capture中已核的持久读数。source审计HEAD为`21096a0`，installed `3fe31d1`及native `29104`未变、未部署。Codex Host退出、重启或断线不关闭、保存、重载或重置DSP；旧宿主事件的确切终止原因仍未直接证明，用户不要求专门的Codex退出存活测试，本次未测不冒充通过，DSP保持运行。
- 本十动作均有completed/succeeded终态，无replay、unknown或在途；external **10/10**、lifetime **110**继续冻结。独立proof `ef588417d6d546f2b4b724363066894a:1`，SHA-256 `91DB78937BCEB515B8864778654752F7E6B7458F2619E3EF3629DA3782889FD6`；root审计43.44s、0 Game调用。

## Gate 2当前边界

**1210完整自动链、全链联合原生预检、持续产量和保存后protected restart仍未证明；Gate 2未完成。** 完整capture `b0a67d710c744189a0db205a22372361:1–100`为 **59页 / 5837 built / 0 prebuild / 11398互逆有向边**。对基线`3fce122b339f46d19dd966388f699e2e`只新增150带、2分拣器和电力感应塔5734，无删除或未解释静态配置漂移。capture的N3读数为212节点、529消费者、127发电机、容量1,858,000 J/t，required=served 219,831 J/t、ratio 1。

- 重氢路径已通过原生端点接线并连接到5326：`3074.slot2 → 5736(filter1121) → 5655 → 98条带路径 → 5657 → 5735(filter1121) → 5326.slot4`。这条路径复用既有A/D/H2来源，两端实际D分拣器当帧均full serve。5326/r104的capture读数为D(item1121)=20、Fe(item1101)=4、粒子容器(item1206)=0、输出(item1127)=0、not working；连接路径不等于产出或持续供给。
- 粒子容器路径仅有A24、D13、H2 free 33及H1 NEW 31这些已建部分；H3及两个粒子容器端点分拣器尚未提交，不能称为完整自动供料。当前forecast余95带/9分拣器/0塔只是计划预测。玩家库存123带/17分拣器/0塔；累计已建459带/11分拣器/2塔。
- 电力预算仍阻止重开写入：N3容量依赖当前tick与燃料，是native动态值，不是固定安装额定值。独立只读跟进`1d75f049f852442881f729a9c47a641a`观察到容量降至1,786,000 J/t；样本仍为当帧required=served、ratio 1。声明的既有峰值1,797,800加全案20 sorter余量6,000，较该最低观测有**−17,800 J/t条件余量**，故`powerBudgetPassed=false`、`writeReopeningBlocked=true`。发电机具体原因、燃料库存及持续供电均未知；不推断燃料不足、故障或当前断电。N3节点增加1已由原件核实；`CapturePower`不填充power-node网络字段，5734的`powerNetworkId=null / connectionCount=0`不是断网证据。两端实际D分拣器full serve不代表全部20个计划分拣器均有覆盖。

完整阶段统计、逐动作索引及四项精确原生belt旋转核验见上述阶段证据。external计数保持冻结；在功率预算门重新核销之前不继续写入。
