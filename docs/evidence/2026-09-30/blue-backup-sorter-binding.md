# 蓝色后备分拣器请求绑定与读回

日期：2026-09-30（Asia/Singapore）。记录蓝色后备线分拣器868的一次原生过滤配置，以及针对“建造回显”误用的工具说明、离线指南与测试修正。未改工具实现逻辑、权限或工具数量；本文不代表紫糖已送达科研站。

## 原生配置与保全

run `32b2cc3657084ae5b833d714d3a94348`：ord8 fresh prepare 后，ord10 commit / ord11 terminal 对 action `24ede7d8-5d6b-4dc3-8a7f-eda4afd14fc9` 返回 `completed` / `succeeded=true`，tick 80442879。sorter 868 的过滤由1202改为1301；连接读回为26↔868↔76。该配置终态 `targetObjectIds=[]`、`itemDeltas=[]`，是合法的配置型动作结果，不表示未执行。

ord3–7与ord12–16覆盖的五个对象，其九项静态字段/连接保持；sorter573与312的过滤不变；玩家库存从ord2到ord17完全一致。原配置回读 ord4→13 显示868过滤1202→1301。Lab76采样为coils6、circuits6、blue output10、working=false；输出缓存满只描述采样点，不证明868已向Lab76投递或持续供给。

request-bound `expectedStateHash`绑定fresh配置、目标对象及请求参数。root以Core canonical hash和请求默认值离线复算旧run5119 ord4/9的配置并精确匹配；改变entity、filter或mode均不匹配。配置不要求`plannedSorterFilterItemId`或`targetObjectId`这两个建造回显字段。保留原请求绑定、fresh token、同一action终态和目标/端点/库存读回；accepted动作不得因caller断言失败而重放。

## 状态、测试与计时边界

最终ord18：tick80442890 / revision33、healthy，lastSaved仍为80425709。新外部窗口accepted为2/10，无在途动作；本次配置尚未保存。相关离线MCP测试通过6项、失败0项，包括指南绑定、配置映射与Sorter/Warehouse hash域分离。

入口total约2167.759ms，shared-action约348.095ms，commit-terminal receipt约79.755ms；这些是不同测量范围，不含模型、委派或CI总耗时，不据此声称端到端提速。

下一接口仍是Lab84紫糖输送的最小消费者预检。其紫糖科研点最后fresh读数为0（此前run `2e501713…`）；本次未重新读取Lab84，因此该结论止于原样本边界。不要把分拣器配置成功当作科研供料或解锁证明。
