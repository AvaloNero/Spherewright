# Spherewright 当前快照

更新：2026-10-03（Asia/Singapore）。本文覆盖当前已核截面；阶段原件索引见[Gate 2证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。

## Owned世界与保存边界

- 同一owned世界的source HEAD为`69c157ccc488c10b8fecac582ee6ae1bcf851937`，installed `3fe31d1`、DSP `0.10.35.29104`未变；本阶段未部署、关闭、重启或重载。最近正常保存为**88501586 / R75 / J99**；capture closing observation **88516528**不是新保存。用户取消的专门Host退出存活测试不作为本阶段或Gate的通过证据/前置门。
- 本阶段完成10个accepted步骤，耗时**233.021秒**；external **10/10**、lifetime **130**保持冻结，无unknown、在途或重放。审计、文档与CI不会自动开启下一写入窗口。

## Gate 2当前边界

**完整1210自动链、整链新鲜actual-ID联合资格、持续供料/功率和保存后protected restart仍未完成。** 本阶段新增59条带、5个分拣器：Diamond两端为`5880: 5879→5333.slot10`、`5881: 5334.slot4→5875`；Strange A/D/H分别新增12/12/27带（H段NEW对象5906–5932），分拣器为5933、5934；Lens地面8带按`5937, 5935, 5936, 5938–5942`有序铺设，末端分拣器5943接到5329.slot8。

- 完整capture `02a5c9f4867840228a29fdcc645cb35f`（open `:1`、pages `:2–61`、P `:62`、J `:63`、Power `:64`、prebuild `:65`、details `:66–90`、closing `:91`、summary `:92`）为**60页 / 5943 built / 0 prebuild / 11612互逆边**。root proof `3bffcf5f6c6d4fd79a17ae2670abcfde:1`，SHA-256 **`243CF3A6385910EE31E7A1D6074A112B749BFE4813CC87A67D0107055288655A`**；独立审计33.621秒、0次新Game调用。无删除或未解释旧配置漂移；5项旧设备姿态补核见proof `78f528489e254d2da4060f1f12d197f4:1`，SHA-256 **`A8480435EAF3815A9554E7CA3CAAF15E40410A8422A8CB28CDB7C351A2254E78`**。
- J99记录`production_line_item_first`：item1209/引力透镜、observed 1、`source=factory-production-register`，gameTick **88500867** / gameTime **017d01:43:34** / actualTime **2026-10-03T18:27:51.7537198+08:00**；事件名称取原DTO字段`.name`。这是首次自动产出事件，不是持续产率。
- capture时5333/r101输入Diamond 8、Strange 2，Lens输出10且因输出满notWorking；5329/r78 Lens与1210输出均为0；5326/r104输入PC/Fe/D为4/4/6、Strange输出0。869/r31当帧Graphite 0、acid 2、output1123=0、working=true；870储仓acid 100、Graphite 0，不将其写作有石墨库存。
- N3为216 nodes / 536 consumers / 131 generators，动态capacity **1,806,000 J/t**、required=served、ratio 1。相对声明峰值的**+2,200 J/t**仅为该时点条件余量，不证明持续功率。剩余施工只有Lens源端和已批准的Graphite并行出口；现有输出→5331路线已完成，无需新增1210下游施工。仍需验证1210启动正例、正常保存、protected restart及恢复后再次输出；当前无持续产率或全链通过结论。
