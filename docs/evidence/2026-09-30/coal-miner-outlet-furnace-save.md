# 同档恢复、煤矿出料段与熔炉炉位施工

日期：2026-09-30（Asia/Singapore）。公开存档别名 owned-world-001，母星 104，原生 DSP 0.10.35.29104。没有改名、迁档、加载 LastExit 或重放旧施工。原始 Bridge 响应由受保护本机目录留存；本文只列脱敏索引，不包含 planToken、票据或真实存档名。

## 本批写入与结果

| 阶段 | 原回执索引 | 终态与读回 |
| --- | --- | --- |
| 精确主档恢复 | resume run c3d4de024d874a1ab219374e21f3edcd；action 9798fa3d-f368-464d-86db-c910631e9a22 | terminal/succeeded；上一主档最小 tick 79867177 / J95，恢复正常保存 79867209，owned、非受限、healthy、J95 durable。恢复调用约 8.6 秒。 |
| 新矿机出料带 | action run 530e116172424ad0b0137f76b670aaca；action c5ec923c-7aab-4f4b-b1ac-beee8491b123 | fresh 源端点和玩家状态绑定；原生完整 stage1 / native_device_port / native_grid，逐点与已批准的 15 点路径相差不超过 0.05 m，预算 2001×15，零 blocker。唯一 commit 后 DSP 建立 15 个预建筑并扣料；后来同一 action 于 tick 79913556 返回 completed/succeeded，实体 5172–5186。没有第二次 commit。 |
| 东侧熔炉 | action run 4a852557b31143108eca5dd8b8e4f386；action 6237b912-0ded-4e8e-ada6-59dcfc398de5 | fresh 原生站位与预批中心一致、预算 2302×1；约 48.34 秒后 terminal/succeeded，唯一实体 5187，玩家炉 1→0。 |
| 正常保存 | action run e26c41b1a6484ea0bb01671b0e32e4a2；action 3a7f3e0c-97ac-45eb-90c2-16a79a3bde01 | terminal/succeeded，保存 tick 79921801 / revision 6；owned/healthy、恢复可用、Journal 95/95 durable、无 pending/error。新票据最小 tick 79921801 / J95 / 29104。 |

恢复后、正式施工前的 Luna 调用在仓库外层工作目录寻找客户端，超过快路径时限仍没有业务 prepare；代理明确报告零请求/零 accepted/零在途，主会话从保护目录序号复核后接手。此为本地调用方路径错误，不是 DSP 原生施工失败，也没有更改存档。

## 已接受动作的观察超时

15 带 build 在约 09:54:25 首次 prepare/commit；普通客户端 180 秒等待期限先到时，原 action 仍为 waiting_for_game，原始回执说明 15 个普通预建筑已建立且材料已扣。不能将该本地超时解释成未提交或失败。随后只读同一 action、玩家及预建筑：玩家 Walk/速度0/能量约 793 MJ，三架施工无人机均工作；预建筑后来降为 0，原 action 在受保护 run 55a527ecf2b5487abf555473406c2664 中最终明确 completed/succeeded。终态补读约在 10:01:42；从 prepare 响应到这次补读约 7 分 17 秒，包含真实无人机施工与本地观察，不是单纯对话等待。没有换键、没有复用旧 token、没有重放整图。

## 保存后的受影响对象核验

只读 run dcefd09ca5054ce2bb6a4fc79231c2c7 在 tick 79922320 逐一复读矿机 5171、15 条新带、熔炉 5187、玩家、Journal 与预建筑。15 条带全部为 item 2001；矿机接向靠近其端的带 5186，矿机和新带合计 30 条内部有向边均找到互返端点，0 prebuild。背包带 393→378、熔炉 1→0。新带两端都观察到煤矿 item1006，各为瞬时段货物 2；矿机煤缓冲仍为 50。这证明源端接出与现场有煤，**不证明未来连续流量**。

熔炉 5187 位于原生吸附中心附近，电网3、serve ratio 1，但 recipeId 仍为 0，连接数为 0；本批没有配置配方、输入分拣器、输出路线或石墨消费者。零预建筑及健康保存成立，但还没有对 5187 的后续重启恢复或全厂 5187 对象静态差异做完整审计。本窗共 4 个已接受动作（受保护恢复、两次施工、正常保存），未触发十写冻结。

下一唯一施工条件是对开放带端、熔炉 5187 及玩家做 fresh 端点/物料/距离预检：合法输入分拣器接煤，正常配置配方17，再为石墨输出选一条原生可行且真实有需求的独立线路。旧带 3373 是石墨链，不可把煤直接并入；现有炉位建成不等于持续石墨、磁环、氢去路或黄/紫矩阵恢复。下一动作仍须 fresh prepare、唯一 commit、同 action 终态与逐端点核验。
