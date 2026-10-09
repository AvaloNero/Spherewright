# R472 空带回收后的隔离状态与未决动作

更新：2026-10-09（Asia/Singapore）。本页记录R467四笔回收及其后的只读核验。它不把不完整终态提升为成功，也不授权在隔离状态下继续写入。

## 冷部署与恢复基线

执行源码为`d6517834d4d8c55f48f841525008b4cde0a5f6e5`，Windows Core CI `37857133940`成功；同批安装含228个文件、64个工具和1项资源。R459记录实际冷部署，R463/R464记录同一受保护主档的健康恢复；R465/R466独立确认恢复后的完整工厂静态连续性。冷部署和恢复通过不覆盖随后发生的R467未决动作。

- R459：`c10078dedc63465d8b0d0f66df08a8a9:6`，SHA-256 `337B867A51956B2F91BE3103212CFB5DCAE383093F6EE2ED787930CFA9EDAB3B`。
- R462安装独立核验：`30ccdd6bbe08457cad3f9b046a9496cd:1`，SHA-256 `979FC494C7FA922CEE7D4AEFA2BF4FF3EA9E696AFDA1D7E1964CBD0D7D54587E`。原保护记录单文件Get-FileHash复核为64位SHA-256。
- R463：`7cbfb7ffc14146d7abd45e1b2ce8df6e:16`，SHA-256 `ECD6DADFB0E186D127456A0FF68B25DE309EEAAFF3FF11D791CFAD3BE98005CD`；R464：`cf7d1c28820d498e9ee1a765a4e3888f:1`，SHA-256 `27D6EA9889F3520A3F1E955F68598D714E2884A5F7C0E6616A0D871542975A73`。
- R465：`25f124b4138c451aba0a0c43f86c8ffa:73`，SHA-256 `C54FD1AE12436D44E10F9ADD16BE38B25F655ABDBCAEC10191B1D36B741C7894`；R466：`9bcb43005fca4303bc8e1ac769005f10:1`，SHA-256 `770519959901794F6CC2123A921D54F07D2ABB872D29512CE44E9F363A80A550`。

## R467四笔结果与冻结

R456开启固定20笔窗口。R457正常保存与R463健康恢复各计1笔；R467再接受4笔，使当前外部计数为6/20、lifetime为398。R467原件：`cb38664c33a14032a61ff131fa2ac6db:45`，SHA-256 `A6AA208BB6EFB97DFDF1C80E7168A5D324A3F68768BBD87337F748EF4C3FFB8E`。

| 原生目标 | 结果 | 读回 |
|---|---|---|
| 普通分拣器5325 | 完成 | 退还普通分拣器2011×1 |
| 空带5324 | 完成 | 退还空带2001×1 |
| 空带5323 | 完成 | 退还空带2001×1 |
| 空带5322 | accepted，`outcome_unknown` | 返回内容显示2001×1，但无完成证明 |

前三笔各有原生完成与读回。第四笔保留为未知：出现的`InvalidOperationException`无法定位到具体调用点或根因，不能据退款显示补出完成证明。原action与幂等意图保持原样，不重放、不清除计数。四笔均未由最近正常保存`102875419` / J102覆盖；之后没有保存、关闭、加载或解除隔离。

## 只读状态核验

- R469：`0de207df08c744e594af1ccb4bac30a8:68`，SHA-256 `FF6CE776B294A0D483F1ADEFC7F0B8BF0C36A48D5E37CC68D1D97EDD5C80AFC1`。其原68条回执已封存为partial capture；66次只读请求中的64页读到6311个built实体。因遗漏必需的ExpectedEntityCount，后续动态观察未完成，该快照没有读取prebuild。R465基线为6315个built实体。R470将R469快照与R465基线比较，没有重采：仅5325、5324、5323、5322移除，无新增或非互惠边。存活对象的DTO投影只显示87、5316、5321的相关连接移除，以及5321旋转变化；它不提供完整Native collider/pool事后证明。R470：`f987389a1bc64cb18e133e164a781f40:1`，SHA-256 `920AC90728CC55F8A19CC11959523EFA12749617FBD7EC781C5CFA8073D7E708`。
- R469完整工厂快照位于tick`102911191`。R471：`173e7b78cad04471b32bde1a841a1340:10`，SHA-256 `4C5EDE3B8993FB4BA3EA1B83EABA98F1D264D9F6C6E219BF21D71D2347AA44D1`；共8次只读请求，prebuild、玩家、Journal和电力分别读取；其中35对象同tick完整货物cut位于tick`102929959`。这些读数不与R469快照拼成同tick全厂状态。剩余5321→5320→5319→5318→5317→5316为115格开放、独立、空载完整路径。货物cut可见该路径；但5322原动作所需的即时Native完整pool/collider后验证明不可观察，仍不能改判成功。87和3725仍保留。库存inc/held一致，持有空带35、普通分拣器4；玩家静止、核心电量1.6 GJ，3座工厂均获满额供电。
- R472权威审计：`9046c53096544dbbb10b54532f3527ec:1`，SHA-256 `099E6B17090704384815EB1379D40D4AAC253B6DAC1439CD55DB4ADFB54AC814`。确认当前Native状态为和平、非沙盒、1×资源倍率的P104/R8，最新观察tick`102929987`，最近正常保存`102875419`/Journal durable102；固定窗口6/20、lifetime398，写健康为`quarantined`且`writesAllowed=false`。持续供给信用仍为0，`wholeSupplyPassed=false`。

## 恢复边界

当前隔离入口仅支持核销原保留的`outcome_unknown` Build，不支持本次Dismantle在进程内核销。常规冷维护要求健康保存且没有未决动作，因此不适用于当前状态。固定LastExit隔离恢复不在既有授权中。游戏保持运行；下一步需由用户选择受保护的恢复路径。在恢复完成并重新核清身份、计数和未决动作前，不保存、不关闭、不加载、不解除隔离，也不执行构筑。

R473只读核对：两份受保护恢复票据哈希一致、未过期，并绑定当前owned身份、游戏版本及未决动作；当时游戏与Steam仍运行，未关闭、加载或安装。核对原件：`a9ab0ee655144f248ff4ce8374a3689b:1`，SHA-256 `1407337CDD8FB8A46BB744C860CBBDE3FE51A19EF7DFED64E2A377F6E6F2305E`。这只证明票据一致性，不证明固定LastExit候选已合格或当前状态已保存，也没有核销未知动作。

待用户选择的候选恢复流程仍未授权或执行：正常关闭当前DSP一次（限15秒，不强制结束，保留Steam）；确认固定LastExit存档header的gameTick至少为`102929987`，仍属同一owned身份/版本/和平状态、票据有效且原进程已退出，同时确认Journal durable仍为102且连续完整；再经既有桌面入口启动一次。使用现有quarantine-ticket默认恢复路径做fresh prepare及唯一resume，最多1个accepted，不重置外部计数。该默认resume后的自动primary重存计入同一accepted，不另计一次Save；随后核同一action终态、新健康票据以及6311 built实体/全库存/Journal连续性，独立审计后才可解除冻结。拟定上限为100请求/360秒、resume不超过180秒、最多一次load、零安装。此处不要求仅适用于healthy-ticket的`VerifiedNewerLastExit`模式。恢复未获选择前，以上仅为边界明确的候选方案。

Gate 2边界不变：Warper与Rod各至少1/min、连续至少36,000 ticks的标准保留；R438/R441来源条件失败及continuous credit为0仍有效，封闭的H侧候选不重开。
