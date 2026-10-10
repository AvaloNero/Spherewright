# Iron40矿机与已资格侧接缝（R736–R743）

本记录核销新矿机建造、保留Save及一处相邻接口的只读资格；实际源绑定路线、供料和连续生产仍未通过。

R739原施工记录 `9b96743837084055860574e4cf95346f:45`。R741原核销及预留Save记录 `f7c5ab4f75b648c9852ba78d1b7c06b3:98`，SHA-256 `A10EAEC809D325D56E7569F40D14572BDAD9D3A1695EF6CD1282494C85D94DB3`。root R743独立审计 `60d43515e4584e5aa7194ef4d049f569:1`，SHA-256 `DB7565865AFB65CFD84750F194077A682FDED16ECFB78835AE32DC468A676EDE`：矿机6476使用矿机2301×1正常建成；group 3的正剩余节点为29、31、34、38、40。矿机由现有N3供电，Native serve ratio为1，无需新增电杆；旧静态保持，完整工厂65页、6476 built/0 prebuild且无非互惠连接。

R736→R737只读资格证明自由Native `grid/2001`侧段可用，及普通分拣器2011/filter1001输入现有带1220的Native计划为`Ok`、span 3。R737记录 `85ea4bf6305e48568bea0cb56355420e:1`，SHA-256 `1A8C61E2B88BCA6874820BBEC11C0560B32FC225893F65BCF1C348E77E276991`。这只证明已测侧段接口；实际源绑定输送段和该分拣器尚未建造或资格确认，`sourceAdmitted=false`。

本阶段固定20窗口为15/20，lifetime 449，opening lifetime 434；15笔唯一原动作均已Save覆盖。当前P104/R49，普通Save `106038638`，连续durable Journal 102，无在途或unknown；整案累计50次accepted、2台矿机、12次普通Save。Native版本29104，执行cohort仍为源码`369f35c0d7ea352d505ec686cc22a52294d5601a`，无代码或部署变化。Stone6仍未归因，Agent获取/供给信用为0。

下一阶段先只读资格实际矿机6476绑定的Native出料路线并接入已资格侧尾，再进入有限施工与Save；当前源未接通，`wholeSupplyPassed=false`、continuous credit为0，连续36,000 ticks、双自动补给、整合保存恢复和最终同SHA双候选包等门仍未通过。R732为已核销的零accepted接口参数错误，R734重叠候选已关闭，不重放。
