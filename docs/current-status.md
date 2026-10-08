# Spherewright 当前快照

更新：2026-10-08（Asia/Singapore）。本页覆盖R312封窗及随后重开的新窗口；详见[便携套件与十写审计](evidence/2026-10-08/departure-small-kit-ten-r312.md)。更早窗口保留在[R303事件](evidence/2026-10-08/departure-ils-ten-r303.md)与[R282事件](evidence/2026-10-08/iron-routing-full-material-ten-r282.md)。

## 当前 owned 状态

- 当前为和平、非沙盒、1× owned 世界。R312封窗为normal Save `101472344` / R29 / J102，external `10`、lifetime `340`；十笔Native终态均已核销并由该Save覆盖，没有first append。随后R316核验时无intent，并重开新的固定20写窗口，external计数为`0`、lifetime仍为`340`（`1ad27c47c24a480ca6188452ab57b089:4` / SHA-256 `68C120B8F8853E2E911A55F076DE5C1BA89B0D745E5DA96DAE806E998898E65C`）。不将旧窗口计数写作新窗口状态。
- 工厂为`6274 built / 0 prebuild`；R312复用63页已采集数据，关联frame tick `101474428`，没有重复全厂采集、建筑新增删除或连接变化。唯一资源差异为对象2440的resourceNodeIds移除节点164，Native读回确认164已不存在。其余162、165、171节点合计资源量40502；这符合自然矿竭表现，但不确定精确耗尽tick，也不认证长期铜产能。受影响矿机2440满电工作，loaded电网点读为full-serve。
- 已选便携套件包括ILS `2104×1`、物流运输船`5002×1`、矿机`2301×1`、电塔`2201×2`、风机`2203×2`、belt `2001×30`、普通分拣器`2011×2`和Warper `1210×30`；Rod carry仍缺。此前J101/J102记录首次手工PLS `2103`与ILS `2104`，R312完整保全102条Journal记录，无新追加。
- Native gameVersion为`0.10.35.29104`；当前源码HEAD为`e5d95d34297a468e11d61509d03f04a38c70aaf4`（Windows CI `37763171765` success），已安装二进制仍为cohort `863d35546f6cb49fcdaec5b5814869d1e13af42b`，不把源码HEAD等同于安装版本。

## Gate 2 边界

- R312独立十写审计：`d2fe95a81be0442d9f53a51ef3584824:13`，SHA-256 `31CBEA275783AB89537DF023DE534021DA0978DD58304C719675FBB6C21ABDB8`；原writer记录为`cb3957b1c14945acb8b4e31d15f35cae:149`，SHA-256 `27C9B9293479D1537F8F732892A863E38BC672C6919B55D1F30876299D4B3523`。R309未解释资源差异失败仍保留在`a4bc84113ded4d0e837dcd2cf3df4065:66` / SHA-256 `3754CCAA5BF9B4724F7B3EED97EE078063210957B220A0BF26CE7CA72C0B082B`，不改写为原审计通过。
- 全局持续供需、H/D/Fe自动共享来源、Rod carry与剩余套件、整合保存恢复、连续至少36,000 ticks及最终同SHA双候选包仍未通过；continuous credit为`0`，`wholeSupplyPassed=false`。既有蓝图生命周期和Governor 2×门保持已通过范围，见[先前Gate证据](evidence/2026-10-02/warper-automatic-source-and-build.md)，不重开或扩大；整案仍为`executable=false`。
