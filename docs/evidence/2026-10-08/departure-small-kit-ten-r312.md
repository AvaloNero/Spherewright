# 便携小套件与R312十写封窗

日期：2026-10-08（Asia/Singapore）。仅记录已核事实；不复制私有回执。

R312独立十写审计通过：`d2fe95a81be0442d9f53a51ef3584824:13`，SHA-256 `31CBEA275783AB89537DF023DE534021DA0978DD58304C719675FBB6C21ABDB8`。原writer记录为`cb3957b1c14945acb8b4e31d15f35cae:149`，SHA-256 `27C9B9293479D1537F8F732892A863E38BC672C6919B55D1F30876299D4B3523`。十笔Native终态均唯一闭合、由normal Save `101472344`覆盖，R29/J102，旧external `10`、lifetime `340`，无first append。随后root以R316核验并重开新的固定20写窗口，external为`0`、lifetime仍为`340`（`1ad27c47c24a480ca6188452ab57b089:4` / SHA-256 `68C120B8F8853E2E911A55F076DE5C1BA89B0D745E5DA96DAE806E998898E65C`）。

R312完整工厂数据复用63页原采集，关联frame tick `101474428`，未重复全厂采集；`6274 built / 0 prebuild`，无新增、删除或连接变化。唯一静态差异是对象2440的`resourceNodeIds`移除164；Native对164返回`INVALID_ENTITY`。剩余铜节点162、165、171共40502，工厂点读满电工作。这符合自然矿竭表现，但未观察精确耗尽tick，不能认证长期铜产能。原R309因该差异失败的记录仍保留：`a4bc84113ded4d0e837dcd2cf3df4065:66`，SHA-256 `3754CCAA5BF9B4724F7B3EED97EE078063210957B220A0BF26CE7CA72C0B082B`；R312的解释不追溯改判R309通过。

本窗便携套件已准备：ILS `2104×1`、物流运输船`5002×1`、矿机`2301×1`、电塔`2201×2`、风机`2203×2`、belt `2001×30`、普通分拣器`2011×2`和Warper `1210×30`；Rod carry仍未备齐。J101/J102的首次手工PLS `2103`与ILS `2104`发生在此前窗口，R312完整保全102条Journal记录、没有新追加。R306以四笔transfer实际取材Fe `47`、磁铁`10`、铜`5`和Warper `30`；五次递归手搓用完三种基础材料并消耗已有电路板`4`，库存净增矿机`1`、风机`2`、电塔`2`、belt `21`、普通分拣器`2`及Warper `30`。

持续供给与整案门仍未通过：H/D/Fe自动共享来源、Rod carry、整合保存恢复、连续至少36,000 ticks及最终同SHA双候选包未验收；continuous credit为`0`，`wholeSupplyPassed=false`。下一有限步骤使用已有3955 Rod暂存、取5件并恢复两个filter，不新增工厂；之后仍须fresh核验完整来源和供需。
