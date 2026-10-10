# 煤矿接近与库存变化观察（R692–R724）

本记录核销一次短程移动、记录后续采煤候选拒绝及后续正常保存。它不构成采煤、加燃料或持续供给通过。

R692普通Move按28 m目标及5 m到达容差完成，root R694核销唯一原动作成功、tick `105914332`、物品无变化。writer原件 `b0ba0f0ea12f4a078697c6ad219b854e:52`，SHA-256 `F0B4981845B93A0BDAC82AD8AE3C56909923F3CD44E1A8DFB5E2738DA8364A08`；root R694 `73d61ea7ff2d4270a09608488738d0d2:1`，SHA-256 `64AC9C64CAD6B38D7E17848E4D5B7896E03B588AF304B1DC413322B36D332881`。

随后Harvest prepare返回Native `STALE_STATE`，0 commit、0 accepted；没有采煤、加燃料或Save。R695 Save-only入口在三次只读请求后因玩家石矿数量与调用方基线不同而停止，0 prepare、0 commit、0 accepted。writer原件 `3f94f2fd4bc34bfa8ac8ded71c37518f:6`，SHA-256 `5B185367F954CB0E2872B1B87B3B4B2C29B8EF47588CB395597EB3D2E3DF1F6B`；root R697 `13ba2054ce1c486f90e632f010eca814:1`，SHA-256 `699A91D5EDFF674675C099649B37FD6AB98802D89B5A20C79CA9A8813439A272`，独立核对完整背包、增量、held及位置。

R697观察tick `105944276`：只有石矿1005由0增至6、inc为0，held及其他背包物品不变；玩家位置已变化、Walk速度为0、CoreEnergy为`14,505,333.3333354 J`，fuel与reactor为0。Walk速度0不证明已在陆地停稳；石矿增量来源未归因，Stone6获取/供给信用为0，未将其认定为用户手动操作。R690预览中的多数候选位置位于水下。旧orthogonal 4 m移动候选族已有两次拒绝并退役；采煤候选只有一次Harvest prepare拒绝，不能与旧候选族合并称为第二次拒绝。

在R697/R699截面，状态为P104/R26、正常Save `105708553`、durable Journal 102；固定20窗口external `1/20`、整案lifetime `435`。当时R692成功Move尚未被保存覆盖；无在途或unknown，新写冻结。已有R682/R683铁矿至磁铁差异仍未解决；wholeSupplyPassed为false、continuous credit为0，Warper/Rod各至少1/min及连续36,000-tick等验收门保持不变。

R698 writer原件 `c1df7c272fc94d6fb9e81b79f5d52ff9:74`，SHA-256 `D6E83AA5D3F6129B0A1441299488D248451CFBC26F9B60092AFEA8BD2F47E085`，73次只读请求、0 accepted、无prepare或commit。root R699 `13138399745b4a0b96c5aa965d9fa347:1`，SHA-256 `8C62304C57CE4EEE7927D26F5F2891459B501CEA0B76E2B972DF0C100BC8BCCE`，独立核对完整65页、6475 built/0 prebuild；相对R672拓扑无新增、移除、静态变化或非互惠边，P104本地Native网络成员及容量保持且满供。煤点438剩余58069、minerCount为0、资源hash不变，距玩家61.145658548078586 m，未采煤。两次玩家书挡Walk速度均为0，位置与R697石矿6件读回一致；末次观察tick `105947358`、CoreEnergy `18,614,666.66666808 J`、fuel与reactor为0。这些读数没有解释石矿增量来源，也不证明dryland。

R704为9次只读请求、0 accepted；原件 `931523e72e364dbd94b187471cb7ce4e:11`，SHA-256 `BB4E4012AA7C77079D860CDCCC875027969EE62F060AC3A95C04D0FF6A0D09EF`。root R705 `9bf3c63087994542aba2301f3ac25516:1`，SHA-256 `493E7EFE9E9D5FD0DFA169BE412263EE26C31439491F0AD6C22A3A511AB29ACB`，独立核原件：完整Iron页有91个正剩余节点；group 3中9个未占用节点距磁铁炉1216为18.428–26.487 m，合计221723，node 40为28076。R704 fresh检查的1213没有矿点或缓存；这不证明附近矿点耗尽。磁铁炉1216配方ID 2，铁矿输入与磁铁输出均为0，未工作且满供；1496仍工作、矿点51/56正剩余，矿石缓存44。该小范围fresh读复用了R699封存的完整工厂快照，没有重采6475实体。

R704当时玩家观察为tick `105956022`：Walk速度0、CoreEnergy `30,166,666.66665732 J`、fuel/reactor为0，石矿仍为6且位置与R697读回相同；石矿增量来源未明，也无dryland证据。root R706只完成离线候选设计：近源矿机、1条出料路线、最多20个传送带段、过滤1001的普通分拣器，必要时电塔与保存，至多5次accepted。Native当前不支持矿机直接接既有带入口，1218的slot 1仍由1213占用；拟用带到带分拣器保留旧功能。位置/覆盖、接缝、碰撞和功率仍待fresh预检，`constructionApproved=false`，不属于当前批准施工范围，未扣料或建造。root R706设计记录 `267c3368e0104938b1182513a2c1f436:1`，SHA-256 `0944D96D7033DE090F52DC80CDCB3D2F8BEB1EE1D6599EEA7EE54FA7B9050631`。

R709查询已核销的旧Move时收到`ACTION_NOT_FOUND`，属于Native action缓存保留期已过；R694不可变终态仍证明该Move成功，不重放，也不将缓存缺失当作原终态失效。R711随后只执行一次普通Save：原件 `f74598e02cca41088f7e9d17ae69e813:86`，SHA-256 `4AC73965976959C11FF539D8A38158F9E7B4C32B31EA974EAA6B596F178D057D`，1 accepted、79请求，没有Move、Harvest或Fuel动作。root R713 `087c33bf7d7d40d3b22cf32a4a6d64da:1`，SHA-256 `883551112656CA164FF2BC1D2B541D00A4CD3640368E89B0AB14C25B6FE31660`，fresh核验完整健康身份、余额、Journal与无未决动作；R715 `4af9a728e51b4f6a95cdc759b34b0264:1`，SHA-256 `F0327A174070AE05C51C19976661BA82546285AA80DC51C2C1BDDF9B34539298`，独立核原Save及86条记录：正常Save `105985786` 覆盖R692成功Move，完整工厂6475 built/0 prebuild、静态与互惠连接差异为0、本地满供，石矿6及全背包计数/inc/held保持。

R717 Harvest原件 `45c22f2a856842688473a9c03e1e5f26:103`，SHA-256 `8C5D9A6A82E666717399B66F0938A3AF6B3AE2231901D7A62785109F91E5735E`：唯一Harvest已accepted；caller等待超时前进度为44/50，动作仍在进行。root R720 `55ebad49217e4e17a590bc145dc4394e:9`，SHA-256 `12A5F592622B6EA5FB64D832F891C3DD06B8AC0C54557727D9B47B79916368B5`，只读核原action terminal成功；root R721 `2f9e209b6d884e968bf81bc1f4734ae1:1`，SHA-256 `0B641881DC15D0F816A87F11D0879B27AC2E6A2524E81C6B5B64EB0345CED2B5`，核实煤点438由58069降至58019、玩家煤由0增至50，唯一原action闭合，无在途/unknown；没有重发。

R722随后以两个accepted完成50煤正常Refuel及普通Save：writer原件 `69f5a251f8cf4240835938535547cf2d:122`，SHA-256 `555BD001A52CC954CBBE7B5C0AB0BAFC3F87526172C4E8A898C3E25D1272FFD8`，111请求、约145.832秒。煤背包50→0并转入燃料；Save为 `105999520`。root R724 `d336f61465c94249b6891c803389d3d6:1`，SHA-256 `20727153B11FDC402310F7D2AC0572C1B053C58FBF692C9809AEC3D15A91C6EE`，独立核完整122条原件、唯一prepare/intent/ACK/terminal、全库存及增量/held、完整65页6475 built/0 prebuild、静态/互惠连接差异0及本地满供；Harvest和Refuel均由Save覆盖。原始摘要曾把lifetime误算为440；按原窗口起点434和五个唯一accepted，正确值为439（R721为437，本阶段再加2），未清零或漏计动作。

R713核验取代R697/R701/R708中将“需确认用户手动操作”作为来源阻塞的结论；这不归因石矿变化，Stone6获取/供给信用仍为0。当前为P104/R31、正常Save `105999520`、durable Journal 102；固定20窗口external `5/20`、整案lifetime `439`，整案40次accepted、5次移动、3次手工材料、3次refuel、10次普通Save。无在途或unknown。最新玩家读回tick `105999745`：Walk速度0、CoreEnergy `91,408,177 J`、reactor `893,333 J`；尚无≥100 MJ到达reserve或dryland的fresh证明。后续仅按既定普通有限阶段推进，任意新施工不由本次Save授权。R682/R683记录的1216缺铁与共享供给问题、wholeSupply、continuous credit、Warper/Rod各至少1/min和连续36,000-tick等验收门仍未通过。
