# 0.4 持续授权与新油源接通

2026-10-08（Asia/Singapore）。本阶段直接服务已建油源5949→3964及0.4剩余验收；不是正式发布授权。成功的5946–5949和既有三级链保留，不重建。

## 授权与续接

用户的目标范围持续授权已替换现行AGENTS/playbook中的旧临时业务禁令。六项语义自检均为“硬条件满足、范围明确时root可批准并继续”：有限取材制作、合法输送段、方案内燃料过滤、结构不同候选重设计、十写审计/保存/准确SHA绿CI后重开、必要最小修复及同批维护。整体 `executable=false`、尚未取得持续供给证明与当前有限阶段授权分别记录，原生保护不改。

授权提交 `29fd66b308db40253994835dd074876f8c1ec375` 仅含三份现行授权文档；focused MCP指导17/17、包规范37/37通过，精确SHA的[Windows Core CI](https://github.com/AvaloNero/Spherewright/actions/runs/37573629266)成功。没有为文档修改冷部署或重复部署fuelPowerState。三处原有dirty保留，未混入提交。

## 最新截面：lifetime260的P102到达与G供电十写封窗

R43已将G源4450经新belt 6198接入6090，并保存至 `99571937/R139`；R47独立只读原件 `1a3ec36fde674264add804b05ed66eb9:171`（SHA-256 `776B192785E11C2ED8D0801E54A2F9D055CACFF2D91A9E869D3E24B023167385`）确认11条新G路径上的实际货物及3台新增thermal有燃料并发电。该观察只证明当时实际到料/发电，不是持续供给或稳定产率；新diamond支路5334仍未证明，旧煤源5580保留。

R49原件 `9fefd015dd6546e8bd3583ccea2c4a87:1–56`（seal SHA-256 `55ECC822542DB258BFD0D6EAA6926D240D837DACC0AB894CF2E8702EC05E529A`）完成从870取G300、3次正常refuel各100及Save `99642768`；G源库存2500→2200，玩家背包末态不变。root独立核验 `b9992d139f334af28828cae0f3a0bbb6:1`（SHA-256 `E1DA675FA4ED029400624ED281175F4BADE20EE77EE09FB7DE9673C30ED97FD4`）。

R54正常同星系飞行由行星104抵达102，原件 `dc9a1d8315a14213a699011544c7c4bc:1–58`（seal SHA-256 `176757AE2FDEE6B8C25369ECE0A1A52E7E84BFF8B3AF14FC6CC6040A1A34CC60`），external `8/10`、lifetime `258`、R147。root R56落地核验 `00c71d4f61054291a92d20749f8c478a:1–22`（SHA-256 `58F5C6328668ADBAE88E56E66C7F9C6A29581CFF7BE93358411F3C4336B6AFB6`）确认Native飞行及605 tick落地；Si miner17的4节点保留。R59随后正常Move至矿区附近并完成预留Save；原件 `a45984e9bb7d45868b9c040f40c2b1b2:1–97`（seal SHA-256 `6F7E16B5AE668F6CD834880C9DC4E44CBD020026B8D9B3B0E8CE0A5D808BA7A5`），最终保存 `99724071/R150/J100`。

R61独立十写审计 `71c09951f02c4d6c8acd2465acf5d532:13`（SHA-256 `B0FB94AF464DAA835693D2407BA9841FE4243AA73C269E893BA3A8E5B9B9AD24`）核销R43的2笔、R49的5笔、R54的1笔与R59的2笔：10笔unique accepted全completed，0 replay/unknown/unpaired；Save覆盖全部10笔、Journal 100/100 exact，external `10/10 FROZEN`、lifetime `260`，无在途或unknown。

R61在P102的fresh完整capture为181 built/0 prebuild；对照immutable着陆基线无新增、移除、静态变化或非互反边。一次同tick cut覆盖163个支持对象、5条完整Native路径及151个完整belt成员。Source logistics station 44单独点读，因为station不在cut支持类型内。行星102电网N1为17 nodes/10 consumers/10 generators，full-serve，需求4350 J/tick、capacity 55000 J/tick。当前库存为Fe0/gear0/belt1/normal sorter0/fast sorter0/pole0/thermal0/circuit938/brick144/magnetic coil1，inc0、held null。

Ti来源尚未接通：station 44读到Ti 158/max200、remoteSupply、无订单、0 vessels；full-dispatch阈值配置为199，但现有证据不能判定阈值是无补货的原因。source miner1虽接在N1且点读full-serve，但resources为空、库存空、not working；Ti节点仍非空，资源组剩余1,268,791且minerCount为0；root确认后续可经有界正常接近/采集取得，非工具阻塞，不能将miner1的空resources读数归因为矿脉耗尽。R50的30-item 600-tick P/C只是短窗，没有continuous credit；不拼接为长窗供需证明。

R62 prepare-only以13次只读取得source miner1正常拆除positive prepare、0 commit且external仍为10；没有施工或accepted。待本次准确SHA CI成功且root明确重开后，再fresh核验实际动作、有限材料与接入；尚未批准新的飞行/建造。共享供需、净分配和连续≥36000-tick验收仍未通过，整案保持 `executable=false`，不涉及正式发布或部署。

## 上一截面：lifetime250的R33主干接缝十写封窗

本窗从lifetime `240`推进至`250`。R33原件 `d73d34622e244bd987f359a39d638955:1–57`（seal SHA-256 `45D3B1C4077D0EAA93E9541C0409C76A7B4357BC8B027759F67499B7DCECA6AB`）的sorter `6189`已实际completed；调用方添加`actualSourceSlot`摘要字段时失败并停止后续写入。root只读核销 `d0dd22ae3a85486b8bbac0e8ed548359:1–14`（SHA-256 `E7D413984C59DD0AD7B8D898DA1CD03494666A0689E2FA6639F3560E2E6F34CE`），确认成功终态且未重放。R33b原件 `8a83728ddf3144fc8260a6aff5abbe8f:1–344`（seal SHA-256 `7CC705F697F6567E6A4928192703724692670D16DEDC2A338E0C54CC157DCDE6`）以306请求/383.2秒完成sorter `6190–6197`及预留normal Save。root十写审计 `6a0e6fbadb66424383db65752bac1723:1`（SHA-256 `995F795247D36627C09D769A1AF368CD7BDA2E4D2C16D3EADC7A2D78A265DF63`）通过：10笔unique accepted全completed，0 replay/unknown/inflight/unpaired；Save `99543357/R136/J100`覆盖全部10笔，Journal 100/100 exact，external `10/10 FROZEN`、lifetime `250`。

完整capture `69a80ac0b06047debea4896def063c17:1–86`（完成记录SHA-256 `3FC0570C98B01183570A35CAEB75B834BE4EF21C78A6AFC611250AE8092C630E`）为6197 built/0 prebuild、tick `99544765`。相对6188基线新增sorter `6189–6197`共9项，0移除；12112条有向边、0非互反，差异仅为预期的18个互惠端点连接新增。同tick cut `86ad1e80b7214366ab53825cdc0b0ea4:1–13`（SHA-256 `66B19B4364FA4A182F047F54D605CFFE0157B320522442641B1E7428C0CD7344`）位于`99545987`，覆盖118对象（105 belts、13个normal G sorter）与11条完整Native路径，全部空货。N3为224 nodes/554 consumers/134 generators且full-serve；3台新增thermal仍fuel0、capacity0、generation0。累计105 dry belt/106上限、13 normal sorter/14总上限、3 thermal/3上限、2 pole/2上限；库存Fe0/gear0/belt1/normal sorter0/fast sorter1/pole0/thermal0/circuit938/brick144/magnetic coil1，inc0、held null。旧油路5949→3964、H filters、煤源5580及既有generator配置保留，source `4450`尚未admit。

9个sorter的实际连接为：`6189:6112→6136`、`6190:6142→6143`、`6191:6152→6159`、`6192:6164→6183`、`6193:6176→6185`、`6194:6182→6188`、`6195:C6184→6130/slot2`、`6196:A6186→6129/slot2`、`6197:B6187→6131/slot2`。原件中的Native `STT/progressRequired`为`6189–6191=400000`、`6192–6194=600000`、`6195–6197=400000`；只记原件值，不据此推算实际吞吐。本段只证明dry端点连接，不证明source `4450`到料、共享供需或连续供给。

后续R42 prepare-only原件 `c93a6c1242e146cd89288f1c70348a28:1–15`（seal SHA-256 `A2AFB631B8D9DF41FC7EE4F9A8099B84C195721AE0FD3B2796CCF8D5682752FE`）为`4450→6090` fast `2012/filter1109`取得1项positive Native prepare、0 commit；两端槽位和offset精确，旧`4449`输入/`4451`输出保留，`6090→6089`仍为空且开放。source admission尚未批准或执行；准确SHA绿CI并由root重开后才可进入fresh施工和实际G到料读回。整案`executable=false`、共享供需及连续≥36000-tick门仍未通过。

另，R35只读原件 `6ac4c70345fc4dedbeb7c827acbb1be4:1–22`（seal SHA-256 `62B417A05D729918E4C233E37367BCC1163A57B21A93541FE5250CAE039F5396`）观察到1657的Ti1004槽为0且无订单、530 Ti为0；1657的Ti出口belt映射正确，但planet102工厂未载入，远端来源仍须经支持的同星系正常飞行后fresh复核。

## 上一截面：lifetime240的pole、dry main与三tap十写封窗

R28材料窗口原件 3fa04498f5da4dee896646896db83955:1–76（seal SHA-256 5DD9C9536C8B18B2F9C55D05067004AC9CCB056BBE9D6E9DFDC158B01A6D7BF6）完成4个唯一accepted材料动作。原调用方最终positive=false发生于kit之后的旧对象比较；root只读复核 34786d7e7e7b471e85dd019ec0803079:19（SHA-256 1645B340E1F36053795BA600BF82A53C8E14A047A0FBB76925A396BECDC24BD8）确认6129.sorterEndpoints.capturedAtGameTick从99457972变为99458400是唯一差异，endpointStateHash、端点姿态/占用和燃料未变。四个accepted动作全部完成；pole从未在该原件中prepare或commit。R28b第二pole原件 6d3cf90a443a40eba72976ba59aeaede:1–57（seal SHA-256 61CD82AAC3A6EF751C383A1530B0B821668A22B8E35519570FDE151E656DF7A1）完成新增pole 6161，随后R29原件 fe635b290cbe4c55a4a7f4da21790a83:1–346（seal SHA-256 CABF3D75D6E345F0EB6A66D3A8FC1912F3391AD970AADAD27AF24E341314B63F）324请求完成5笔：main 21 belts、三条tap各2 belts、normal Save。

root独立封窗审计 7a0c87061d884e8ca8b623c5b1425a65:1（SHA-256 D6009D7633D4F7EF68C184CE402B8D942358BCA289A34F82FEB385E2C39F1F62）从受保护原件核销10笔unique accepted全部completed、0 replay/unknown/unpaired/inflight，Save 99483674/R117/J100覆盖全部10笔且Journal 100/100 exact；external 10/10 FROZEN、lifetime 240。

L240完整capture c38240a45bc74992982d1b3eea4afdf3:1–86（SHA-256 FC833C929CA2836F31DAA2E590FA71C4D59B302A9E37F898E8862E333649B76E）为6188 built/0 prebuild、tick 99484808。相对6160基线增加6161–6188共28项：pole 6161、belt 6162–6188共27；0移除、12076条边、0非互反。声明的native源覆盖只在6154的rotation/connections有差异；6156原输入保留且rotation本次未变。main按6162–6182；tap C为6183→6184，A为6185→6186，western B为6188→6187，不可按ID数值排序解释路径。

same-tick cut d80925eaf8844d9b89ba4e06b537e37f:1–13（SHA-256 F26BBAE886F7D165B69E001B8624BC0812EEF558D52748AC5FF13362B49D9A90）位于tick 99485210，109对象（105 belts及4个既有G sorter）、11条完整Native路径全部空货。D族累计105条实际dry belt/106上限、4个实际sorter/14上限、3 thermal/3上限、2 pole/2上限；14个sorter累计工作需求上限为4500 J/t（13普通×300、1快速×600）。N3为224 nodes、545 consumers、134 generators且点读full-serve；三台新thermal仍空燃料、实际capacity/generation为0。source4450仍未admit；旧油路5949→3964、既有filters及煤源/旧机组配置保留。末库存Fe0/gear0/belt1/normal sorter9/fast sorter1/pole0/thermal0/circuit938/brick144/magnetic coil1，inc0、held null。连续供给、共享供需与整合恢复均未验收，整案executable=false。

九个普通接缝已通过prepare-only Native资格：root run `8886c579d68747ebb08f7d60239596b9:1–53`，完成记录 `:53` 文件SHA-256 `293B6A616B1FFDB208B3909CEC37E9972677E7966C765924ACF85CECDFAB155C`，51请求/9 prepare/0 commit，九项均positive，R117/Save99483674/J100及全库存保持精确、source仍未admit。在该截面，准确SHA绿CI并由root重开后的拟续接为九普通接缝施工并将normal Save留作第10笔封窗动作；fast source admission延至后续窗口并先核下游。

## 上一截面：lifetime230的D族返回段十写封窗

本窗从lifetime `220`推进至`230`。受保护原件紧凑索引覆盖四个run：R17 `5eeccd1d4b2e402e9f805109d1e10349:1–144`（seal SHA-256 `E42244580E6D3021D90E825A9946F6026885C92D30B97DCF38D59F39F082B386`，4笔accepted）、R17 remainder `9f648eb2c501421589cc5cbccb4e9595:1–105`（seal `D1825A382CDA918EEF0FB76ECFB755081CC1FCA93C89A4F069CAF114C85887FD`，2笔）、R22 `cef2c0da5cf743e89513448a84e38291:1–219`（seal `8C745DD0F36E75E0E37E2D1156C2B30E202179BD4F712CF4BD49956DBD2CAA86`，2笔）、R25 `3befe027bced40fc847490d7be6d73c5:1–108`（seal `3D52415DDA1F025FD49AA23C69FABE860DA58B4707CB8FCF11625F706926293D`，2笔）。索引读取328条标准commit/terminal回执，10笔唯一accepted全completed，0 replay、rejected、unpaired、unknown或未决；`newWriteBlocked=false`只描述该回执集合，不替代十写审计。

root独立十写审计 `f596f8881e8644bc9f3979c140bfb6fa:1`（SHA-256 `B9CFE365D95C2F1394B9BC919D5D3A3DD4C51D4EC9A9C6D37F4DBBD1D1D3F53C`）确认10/10 unique completed、无replay/unknown，普通Save `99414378/R99/J100`覆盖全部10笔，external `10/10 FROZEN`、lifetime `230`、无在途。

完整capture `440a69d54eda42c78d39995685e11e21:1–86`（SHA-256 `DAC55A067B6CCC7C5E8B804DB4BB7AC243B05B2C1709DB850177CA158A1341E5`）为6160 built/0 prebuild、snapshot tick `99417148`。相对6127基线新增6128–6160共33个实体：pole `6128`、thermal `6129/6130/6131`及29条belt `6132–6160`；0移除、0旧静态变化、12028条互返边、0非互反。same-tick cut `a2cec63d988f4440bf35e8fcf0b553d0:1–13`（SHA-256 `7D8FAF206EC1E1DAD57FF7BDA2187D59EB8850503BD12E51CABFA44F5C7C867F`）位于tick `99417713`，读82个对象（78条belt、4个既有G sorter）和8条完整Native路径，货物均为空。

三台新thermal仍接在N3但为空燃料，当前实际capacity及generation均为0；N3为223节点、545消费者、134发电机且点读full-serve，铭牌108000不等于当前实际capacity。源`4450`尚未admit；旧油路、四条H filter、煤源`5580`与13台既有generator配置保留。当前背包Fe0/gear0/belt22/normal sorter4/fast sorter1/pole1/thermal0/circuit943/brick144/magnetic coil1，inc0、held null。

R25结束时私有caller将nullable空数组序列化为`[null]`并停止。root只读复核 `4d94a113b40f47fbab6c748091caf76b:6`（SHA-256 `BE00047A6B8AA973710305288458EEC11381C2715419EE8E9911DFBA554B1658`；5次read、0写）从原两action核终态、owned/session/Journal/player，确认仍10/10 FROZEN，无重放、unknown或计数变化。之后仅把`.local`成熟helper的nullable-array归一为`[]`；离线验证0 Game calls且仍阻断unknown，不涉及Plugin/MCP或部署。该数组序列化问题不同于PowerShell把空数组折叠为null的既有经验，通用边界记于[经验账本](../../experience-ledger.md)。

施工前资格和候选仍需按范围区分。R18–R23没有新增accepted；两次旧`6080` south-head失败后退役，mixed-climb被Native拒绝而R21 descent未尝试。R19 pure climb、R20b三个port-2 tap分别通过单体Native资格。R22 free-return虽然单体positive，但与未来descent tail/tap head重合，施工前退役。R23 row26把return修为7格、main预测21条NEW belt；整体条件上限106 belt、14 sorter（13 normal+1 fast）、4500 J-tick、3 thermal、2 pole，不新增配方机器。额外Fe11/circuit5的gear2、belt6、normal sorter5材料仍未备出。

整案仍`executable=false`；源`4450`未admit，实际joins/source/filters、共享供需及≥36000-tick长窗均未通过。准确SHA绿CI并由root重开后，计划先取Fe11、制作gear2+belt6+normal sorter5，再建第二座pole（新增1座，累计2座）、actual-tail/main21与3 taps6，并将normal Save预留为第10个accepted。上述顺序是后续有限计划，不等于接口已验证、执行已完成或最终Gate通过。

## 上一截面：G端延伸与lifetime220窗口

四个受保护run的紧凑回执索引覆盖 `ee6e78fb84544ffe96fc3a673c8bda1d`、`260fd03cf4a24332ba0eb8c6e9985676`、`a489904c4f774d58959ae6f4d5c9be0d`、`d62aac8ca898411c817f11e8f2ca89cf`，共266条记录：R12的3笔、R14的4笔和R15b的3笔共10个唯一accepted均completed，0 replay、rejected commit response、unknown、unpaired或未决intent。R12原run记录247项，达到240请求观察预算；原调用方只完成前三笔中的前两笔读回。`260fd...`是只读续接，核销第三笔终态。该索引仅覆盖动作回执，索引字段`newWriteBlocked=false`仅描述已审集合；root独立十写审计 `87458dde429e4c05b4ef95bf6d740ef7:1`（SHA-256 `4A379B5C8DCE8580E38E46BE9A9E9919EE0F17FA7ADBEA7CA23C1A24FE7EB6BD`）另行核实全部10笔、Save覆盖全10、Journal 100/100 durable及完整库存/inc/held守恒。窗口为 `10/10 FROZEN`、lifetime `220`，Save `99297127/R80/J100`；封存不构成下一写窗授权。

备料run R15原件 `94e98fbda034403b891e62cb1a2eb278:1–5`只读4次；当时 `get_build_catalog` 不含 `handcraft` 字段，因此没有prepare、commit或accepted。root closure `cf74b71951b940c5a2a6efbf6c31e947:1`（SHA-256 `05004F1A7E7B80DD03EB8966C0DD539AAB77C4561781D4D9DCA241573F4E5689`）记录该入口问题。改用真实 `get_recipe_catalog` 后，R15b `d62aac8ca898411c817f11e8f2ca89cf`完成三笔：从1511取Fe36、r5手搓11件（Fe11→gear11）及普通Save；没有部署二进制。当前玩家库存Fe25/gear11/belt18、`2011×1`、`2012×1`、`2201×2`、`2204×3`、circuit946、inc0、held null。

本阶段D族新建42条实际belt `6082–6123`和4个G sorter：`6124`（`6082→6101`）、`6125`（`6091→6102`）、`6126`（`6123→6081`）、`6127`（`6078→5334` slot 5）；均为`2011/filter1109`，N3点读为full-serve。source `4450`尚未admit，不能据此声称新G物料到达。完整capture `9227907ce1534b708fc6b17c39147510:1–86`（SHA-256 `CCA119A4C382D6C8B951A6E8A4BC179469C90CBDCE99F10320BC6D4B7C52B30B`）为62页、6127 built/0 prebuild、snapshot `99298318`；相对6081基线新增42 belts与4 sorters、0移除、11976条边/0非互反，仅`6081`、`6078`、`5334`三处声明连接变化。完整capture的`beltCargo=null`是detail-only未知，不能解释为空货。另一次same-tick cut `27e2e8a775e142778ed505aa693382c5:1–10`（SHA-256 `AF22FF553EC2DD25B47F2F1D68A6DF7511721F1A4BD784DDF23DC6FF8A3135E6`）在tick `99311737`读取53个对象（49 belts与4 sorters）、5条完整native路径，路径货物均为空；该cut独立绑定R80/Save99297127/J100的末态。旧油路 `5949→3964`及旧`4193`保留，油路累计123条belt/2个sorter。

整案仍 `executable=false`。source4450未admit，共享供需和持续≥36000 ticks均未验收。R16首次9次只读且0 prepare；调用方误将pole的`NetworkId=3`作为硬条件。修正后R16b `5c268e481f6948f694139cb5387f9119:1–20`（seal SHA-256 `AB7AD2725E7F526EB3EED28F875A2A5AD2BCB55AE8E9113FCBA440633E413331`）为17次只读、4次prepare、0 commit/accepted；root独立原审 `f8aca907db92447b8cd32476b29b0032:1`（SHA-256 `2E7C94A21CFC3F75C3AD7F895BC36DBA0C717867C442ED3B664AB7976F91F000`）确认三thermal与一pole各自的单体Native资格均为正例，无原生候选失败，也未发生施工、供料、二进制变化或计数变化；这些单体正例未验证候选同时存在时相互无碰撞。5330实体detail中的`powerNetworkId=null`仅证明物理link长16.014m及未来覆盖≤7.820m；不证明已接入N3，实际Network成员须施工后按差量核实。下一步为fresh施工与实际读回，仍不等于共享供需或最终验收。

## 上一截面：G端空带与材料包十写封存

新G source端按 `[6077,6076,6075]`、consumer端按 `[6081,6080,6079,6078]` 建成7条空货带，共两个唯一build accepted；fresh全厂读回确认两端有向内部边与自由端，尚无G供料、连接中段或新发电机，也没有改filter。R3 source原件 `e05ee4d43a8e4d3a990e3cb425739b65:1–63`（seal SHA-256 `1D8ADEACF00722ADB9D47BE5F65A054AD6055D9291275C751D8E5B0F8B1CC7CD`）初始false仅因returning drone超过15秒调用方readiness timeout；只读closure `9fb255735180400f99ecac1b765d2afb:1–13`（SHA-256 `3AEAF694C55A8C0A49BB8446C5069C91B939FFE7E8EC12CC8B566B7459958AA9`）核销其状态、0写。R4 consumer原件 `dd36c0f7448548dfa6a094e3e6769b61:1–59`，seal SHA-256 `E833B1A6E6E4B75FD0F7926A5A79AB15A20B27F78911B09FFCD9AC94512BD25E`。source接口4461→6077和consumer接口6078→5334的fresh native资格均为positive；surface读回使用planet radius 200对齐，root抽查11点最大偏差0.00001353m，无native失败或commit。此前false来自调用方把raw player radius约200.217直接比较，而非原生拒绝。

R6原件 `9f509635965b43a795c50c5b7b1bb40f:1–114`，seal SHA-256 `1BA529C52BC7D76320723B1003B7D36806FABAD28C56D44DB63D8DE3DBB7FF88`：105请求/96.5185秒、8 accepted全成功、unknown=false；内容限正常Move 10m、从562取1102×14与1104×7、r6×7/r5×12/r8×2/r64×3手搓及第8笔normal Save。Save `99154541/R62/J100`，closing `99155715/R62`，external `10/10 FROZEN`、lifetime `210`。末库存Fe0/gear0/magnet0/Cu0/coil1/stone144/belt60/2011×5/2012×1/2201×2/2204×3，其余全库存/inc/held守恒；562的1102库存1915→1901吻合取14，玩家Cu+7而562即时库存1900→1894（自动回补1），按原始双边读回核销，不用显示净差反推取6。

完整capture `139066f11dec4025ae706ac364138148:1–85`，seal SHA-256 `35538FDEB5547F97C065101FEA8358363ED33F38D2CAAE955614FCD44AB44CEB`：83请求/24.421秒、61页、6081 built/0 prebuild，snapshot `99155347`、end `99155715/R62`、Save `99154541/J100`。相对上一全厂capture `3084c9e07f5145c2b963dbf550fad4d0` 的root projection `f83521a2de7543979ace10b6b50d2fab:1`（SHA-256 `252C39B0A580EEBDB12D0E36C7455B319E3E1C4E23B85E67A33A48FEECD0ECB3`）核销恰新增6075–6081七项、0移除、0静态变化、175正常动态变化、11882条连接/0非互反；7点逐点匹配原native plannedPath、位置差0，旧N3/N4网成员与full-serve均保留。root独立十写审计 `b89f5ff0426a4a52877381b79affc667:1`（SHA-256 `42FAE7F05D0B03F592E848E7090AF2C9AE635547D342660602B5E2610F2D9C04`）确认10 unique completed、无replay/unknown、Save覆盖全部10、Journal 100/100，旧配置/电网及完整库存/inc/held守恒；R6的1102转移为1915→1901，玩家Move终点距562为72.9751m。`newWriteBlocked=false`仅描述已审集合；窗口保持冻结。

整案仍 `executable=false`：这次只完成7条空带端点与材料kit，没有G线供料、中段/实际接缝、新generator或filter改动；没有稳定净供需证明或连续≥36000 ticks验收。此前R5两项接口native正例未重跑。下一动作须等本提交准确SHA的Windows Core CI成功并由root重开，再fresh资格有限2012中段/空带接缝与条件发电分支；是否装新thermal及实际数量由供需读回决定。

## 上一截面：备料窗口封存，独立十写审计通过

备料前的固定18-read原件为 `b507b864728b4953964eb3d26a4978db:1–26`，完成记录 `:26` 文件SHA-256 `556D8B10F818E2952B8AE8E59755A02E3D5CC6199A5EF97627ED019EFFDA0979`；root审计 `582441c53ef04269966c522a40a777a6:1`，SHA-256 `66D00BD39F0EBD9384AFD936CC0567609306648F2FF44EA04C4E48674821FEE2`。该固定读取包25请求/4.872秒。

唯一获准的本次备料run `ee2932d1889b47f18f2977586c88b27e:1–88`，完成记录 `:88` 文件SHA-256 `C80A2CED0A00705B815A16C926F3AFAB09738F0AD55E492EA3AFD46993CD2CE1`：83请求/115.074秒，包含4个唯一completed动作——从1511取出112 Fe至玩家库存、recipe r5手搓22件、recipe r84手搓22件，以及第4项normal Save。玩家库存、增量与held核对只见各动作预期的精确差量；Save为 `99037141/R45/J100`，closing `99037143/R45`，external `10`、lifetime `200`。原件索引已核对前consumer侧3笔、source/save侧3笔与本次4笔，共10个唯一completed且无replay。

root独立十写全审proof `ad29ade6e82b486bb75943d75e45a1aa:1`，SHA-256 `0E06303CD78CD302BC434F886FB438EF9B45BB3AB7F33BC88F41121D4C9A5F64`，已通过（纯离线、0 Game calls/0 writes）。审计确认10笔唯一completed、0 replay，`newWriteBlocked=false`；Save `99037141`覆盖全10笔，Journal 100/100 durable，玩家库存/inc/held与全厂原件守恒。该字段是已审集合状态，不构成下一写窗授权；下一窗口仍需root明确重开，本节不记录后续accepted。

从1511取Fe后，该原生来源库存点读由3000降至2888；之后fresh读回为2889，时点差异与自动回补一致，不能把2888写成最终库存。玩家备料后为Fe46/gear0/belt67/stone156/sorter5；其余库存、增量与held只按原件中的精确差量解释。

最新完整工厂capture `3084c9e07f5145c2b963dbf550fad4d0:1–82`，完成记录 `:82` 文件SHA-256 `E1D139A7EA0214FE53AA045910945668B22415BE8C9D11CDFEBA10CC5137C6BA`：80请求/16.668秒、61页、6074 built、0 prebuild；snapshot `99038022`，closing `99038255/R45`、Save `99037141/J100`。root独立差异审计确认相对 `c4b50b48c0b1424590f90b40d2e9f8c9` 基线新增6061–6074共14项、0移除、11872条连接且0非互反；10项静态变化逐项符合最新readback，200项动态变化为正常变化。旧电网成员保持，仅新增2个sorter消费者。完整原件已与十写审计交叉核销；不作其他未核对差异归因。

root已为后续fresh资格选定有界D族候选：以870的自动r58石墨来源接至5334，并评估至多3台实际负载的石墨燃料热电设备。纸面上限为64个NEW belt、6个sorter、2个pole、3个thermal、0台新配方机器，保留5187/5580现有功能；这些是条件模型上限，不是通过的计划或必须施工的数量。新增thermal的实际数量与旧fusion燃料棒消耗仍须以动态净供需核实。当前批准仅覆盖上述4项备料动作；没有新石墨施工、filter调整、整链fresh native资格或持续供给验收。安装cohort仍为 `d744b8e6eb5b898b9e9484b2209c30380e40ddda`、64 tools/1 resource；本次维护提交 `d5059e517a862e3fb5f556f871e57277f2e45aae` 的Windows Core CI `37585985747`成功。

## 上一截面：新油路首次送达3964，供需门仍未通过

准确SHA `9041944ca2cb742105220013780ebbbc491fd901` 的CI成功后，root以 `6e14902fd4fe4ccb806d60206fa2bd97:4` 重开窗口。writer先完成从既有油路到新消费者尾端的2+4条native belt及consumer sorter `6067`（`2011/filter1007`），从尾端 `6066` 接至 `3964` 的slot 1；3个accepted完成，external `3`、lifetime `193`、revision `34`，当时未保存。root独立审计 `155674d08ed64f0a92d0ec32ac7679e1:1` 的SHA-256为 `6F9E8285831C9961CE5C0F350C715DBD2E8AD6DFD5B8650050793F7AD60376D8`。

随后writer以完整native `native_device_port` 计划建立5949出口6带，实体顺序为 `[6073,6072,6071,6070,6069,6068]`，再建source sorter `6074`（`2011/filter1007`），将出口接入既有 `5960`。本阶段共6个唯一accepted；此前的 `155674d08ed64f0a92d0ec32ac7679e1:1` 审核consumer侧3笔，随后 `9ed6b32c1dda4827aec1907ec8cecd5c:1` 审核source建设与Save侧3笔，两份独立分包审计共同核销全部6笔均completed、无replay、inflight或unknown。普通Save完成于 `98801251/R39/J100`，健康恢复票据可用；窗口为 `6/10`、lifetime `196`，Save覆盖6笔且未重置计数。`9ed6...:1` SHA-256 `7330E19C4C65746B904F19D3BE850FD8404D9E092B2AA5B09218A223BE202BDC`。

新油路共123条belt及2个sorter：保守164 belt上限尚余41，两个sorter额度已用完，背包余1条belt和5个sorter。完整油路由源口 belt `6073–6068`、source sorter `6074` 接既有 `5960`，再经主路及新延伸belt `6061–6066`，由consumer sorter `6067` 接入 `3964/r16` slot 1组成。root原件 `a7d27e5a5dc346019e974655f2fb9286:1–15`，SHA-256 `257A2F54C3D936157CB172AEAA8B46BC2292D41CAB26DBFBEA49C2B4CD275A80`：8次同tick cut各读9个对象，并观察到主路 `5950–6066` 与源口 `6068–6073` 两条完整原生货物流路径。tick `98813432` 时 `6067`持有原油 `1007`，固定目标为 `3964/slot1`；tick `98813924` 时 `6067` sorter处于Working/Returning，held归零且native progress已推进至 `290000`。这组读回证明新增油路的原油到达消费者并进入原生插入周期，旧 `4193` 保留；不依赖consumer缓存净增来归因，也不证明稳定供率。13请求/9.328秒后以 `98813936/R39` 收尾，Save仍为 `98801251`、J100 durable、external `6`。

123带拓扑证据是不可变6060全厂基线加当前原始局部实体读回overlay，不是新的全厂capture，也不据此更新built总数。root完成独立短诊断与暖机后20-read补充，均为零游戏写入；三个600-tick窗口有间隙、连续credit为0。完整原件和解释见下文“首次到油后的共享供需短诊断”及后续补充。整案仍 `executable=false`，连续≥36000 ticks、共享氢/石墨/legacy fuel配平及完整有限缓存排除均未通过。当时root与writer仍在比较有界完整方案，尚未选定或批准新施工；后续候选选择与备料边界见上方最新截面。

## 历史封窗基线：第二个十写窗口

第一窗提交 `71a513bd08431446ecd09e2290333d66346c32dd` 的[Windows Core CI](https://github.com/AvaloNero/Spherewright/actions/runs/37576418127)成功后，root以证明 `d835d738cb3c49f4aaa74332afc2a05b:1` 重开窗口；本窗从external `0/10`、lifetime `180` 连续推进至external `10/10 FROZEN`、lifetime `190`。第二个十写窗口的独立审计通过：十个唯一accepted均completed，0 replay、unknown或在途；正常保存 `98676748/R28/J100` 覆盖全部十笔。封窗后的只读原生资格观察为 `98726758/R28`，accepted仍为10、lifetime仍为190、Save/Journal边界未变。

最新不可变全厂快照 `c4b50b48c0b1424590f90b40d2e9f8c9:1–82`，SHA-256 `2FCD71C44E45DB3FD24B70CB7427B56C1C6A5D8578E2F3D4251E78D2512B981D`：snapshot tick `98680938`，61页、6060 built、0 prebuild。独立差异 `2f0edd7ba58746daa520bb1be2265898:1`，SHA-256 `F12C8447DE830A2B298BA4E09360DFADA39481350362DDEDEDF8487ABCFABF07`，相对5969基线恰新增5970–6060共91对象，0删除，11838条互惠边、0坏边；199项动态变化。三个静态差异为2440铜矿节点集合减少167，以及5950原生cover旋转和新增输出连接6045。root用固定5请求、1页、67节点完整原生铜矿目录复核：162/164/165/171仍active，167已不存在；该子集变化与正常矿竭相容，不归因历史操作者，未解释项为0。

截至第二个十写窗口封窗，本窗油路累计111条belt（保守164上限尚余53），由104点主路径与独立7点下降组成；111条belt的空货状态来自各施工阶段逐项实体原件读回，封窗后的fresh全厂捕获只核验全量拓扑与位置，不是111条belt的fresh cargo核验。该时点不能据此声称油已送达。玩家余13条belt、两个sorter预算仍为0/2；油源5949仍未连接/放料，旧消费者配置保留。点功率full-serve只代表该次观察，不代表持续功率或物料平衡。独立审计原件 `291396ee1ab148e38b2f54fae1402693:7`，SHA-256 `F9484A6B096B021B73DB8C9800836F726963921084D3348617FC85C4154DF43D`。下降与预留普通Save原件 `2ced5c4c3f9b4b65a17bfa22f114e1eb:1–60`，SHA-256 `F444DBBEE60C9FE128BF16564299162522B6915372EC232C4B8BB46D8E8503F9`；完整工序如下文。整体仍 `executable=false`，持续供油与最终Gate均未通过。

R0止于离线准备、零游戏调用；root随后收敛为R1固定九读，1.6秒完成：session/Journal/player/5949/3964/1511/power/session/Journal。原始 `62a700134dbf485e9ed353f163548696:1–9`，root独立证明 `6f04b18db9e04a07a21ad30f4b9071e9:1`，SHA-256 `3C72644183D068D1031E4B072AE8C6B78FC9206E315F2DACC7023EDD9D66919F`。首末tick98470417–98470479、R1、save97255779、J100连续且healthy，无新accepted。5949/N3/serve1、油缓冲50且无连接；3964/r16的slot1空闲，旧4193/4180/4181保留。玩家铁37/带28/齿轮0/分拣器7，1511铁3000、距离52.238米/buildArea80；可达资格仍由每次原生prepare判断。N3容量1914000、需140381、serve1仅为点观察，不是持续功率预算。

Root另核58条原意图/终态，当前窗口5个唯一accepted均completed，0 replay/unknown/未决；lifetime175来自上次封窗170加五笔，不由R/J推算。索引覆盖为四个明确run：`f164f182824f49cfb8205c05e8822c04`、`2898f09523924f379fe548944b301da1`、`03a2455d00234c3a8302a1bf2b5811d4`、`474ac6f2e168439a91b1fef591ea81b8`。root证明 `863beb73a68c47449354e624bb712a91:1`，SHA-256 `A170F270EF51D63BD0BCFBF74E0FEF7E0DF478928403A717F4AD0921970CE043`。这是开片核销，尚不是本窗十写全厂审计。

## 统一供需表与工程上限

下面的路径是保留的静态设计及原回执索引，速率是按native29104配方去重后的最低目标模型，均不冒充本次实测或持续来源。原配方见 `25bd4f5f56044577b62a381fac341393:4`；已声明目标仍为1210≥1/min及1802≥1/min，旧竞争支路另计。最终长窗前必须把实际入口、出口、副产物及竞争分流全部纳入同一声明，未核验项不得省略后先跑。

| 物料/支路 | 保留来源→消费者 | 联合目标最低模型/仍需证明 |
| --- | --- | --- |
| 原油1007 | 5949→native出口带6073–6068→source sorter6074/filter1007→既有5960；既有主路及新增6061–6066→consumer sorter6067/filter1007→3964/r16 slot 1；保留129/2802→141/707及共享炼油支路 | 到油/插入周期已由原生readback确认；短窗P为2/2/2、C为2/0/2，窗口有间隙且无连续credit。仍需证明持续供率、有限缓存排除及共享支路净供需 |
| 精炼油1114 | r16→3965/3966及3083/3084等r58；保留其他下游 | 28对象cut读得3964 input crude 4、refined output 40/40/39；3965/3966各自refined input 2、H input 4、G output 20，progress 2,400,000且停住。后续cut仍见两机各G output20、批次接纳堵塞；3083 refined input0、3084 input2且working。root判为原生批次输出未被下游接纳，不是Overseer finding；持续净供需仍须核 |
| 净氢1120 | r16+r58→3073/r40；保留旧氢火电和科研/生产竞争支路 | 三窗P为8/6/7、C为5/14/5；C含r58循环，不能当净氢消耗。最新单个600-tick窗口读P13/C16（含循环）。1210与1802各需20H/min，合计至少40；旧负载及r58实际净量仍须核 |
| 重氢1121 | 3073→3074→5326及3403；保留3081/3075/3405分流 | 三窗local104计数P为0/0/5、C为0/0/0；5326的D input cut为3→5→8，后续点读为0；旧3403为2→2→4，后续点读为7。最新单个600-tick窗口P5；两个目标各需10D/min，不能以点库存或短窗替代持续供给 |
| 高能石墨1109 | 5187/r17→5334；r58共享→石墨火电及869 | 三窗P为3/2/3、C为2/1/2；最新单个600-tick窗口P6/C2。store 870读为石墨2500、酸100。金刚石4+石墨烯6=10/min基础需求，燃料另计；r58的H/G循环及旧负载净需求尚待核 |
| 硫酸1116 | 861→870→869/r31及既有酸支路 | 目标至少2/min，核原料与全部竞争出口；库存100不是持续来源 |
| 石墨烯1123 | 869/r31→871→883/r99 | 目标4/min；与金刚石1112区分，不能把871满仓当自动源证明 |
| 粒子容器1206 | 883/r99（1204+Cu+1123）→5326/r104 | 2/min；涡轮及铜共享源、旧消费者、实际输送同验 |
| 奇异物质1127 | 5326/r104→5333/r101 | 1/min；需铁2/container2/D10，实际产消而非缓冲增长 |
| 金刚石1112 | 5334/r60→5333/r101 | 4/min；5187石墨来源及其他煤/石墨消费者同验 |
| 引力透镜1209 | 5333/r101→5329/r78 | 1/min；上游与实际出口同验 |
| 空间翘曲器1210 | 5329→5364→既有29带→5365→5331 | 三个短窗P/C均0/0，最新单个600-tick窗口也为0/0；有间隙，不能推成永久零需求/零供给。5329后续点读working，5331库存从2549到2552，但单点差不能当持续产量；≥1/min、连续≥36000ticks与自动补给仍待证明 |
| 选定燃料1802 | 3403/r41→3955与既有发电支路 | 三个短窗local104 P/C均0/0，最新单个600-tick窗口也为0/0，但不足以判为永久零需求/零供给；13个分时点fuel读中3079/4227/4229/4230持有实际缓冲及loaded rods且均productive/generation>0。不能据此核销≥1/min；旧发电额外配额仍须明确 |
| 旧发电/科研与新油路功率 | 保留既有13台燃料机及所有已声明支路；新油源及2sorter | 13项分时点读中相关机器均报 `loadedFuelProductive=true` 且 `currentGeneration>0`；这些不同时点的fuel状态不是同tick库存cut。融合loaded energy下降而缓存rod仍10，不能声称fuel未消耗。N3三次点读full-serve、capacity 1,914,000、req 146,474/342,516/148,788；后续读req 150,433仍full-serve。N4点读均full-serve 1800。短窗未证明stable burn、净分配或持续功率 |

油路总新增暂定上限2001×164、2011×2；已建塔/采油器新增为0。原先123带接缝forecast现已由新增实体核销为123条belt，尚余41条belt与0个sorter预算；精确点数按各片native计划及实际ID核销。阶段初期优先准备r5/r84各32批，叶铁96、最多补取59、带产96；不用101铁/46批保守上限取满。新工程不从旧已成功前缀累计重算。

## 首个十写窗口的施工与保存

R2a材料包的60秒首读准备上限耗尽，writer明确停止：无live请求、run、commit意图、accepted或活动句柄。root没有追加该声明额度，判定为调用方准备过度；直接提供复用现有NormalAction/受保护transport的三动作薄入口，真实 `pwsh -File -Mode smoke` 零游戏调用通过，避免继续读旧固定模板。

R3实际完成storage1511→player铁59、r5×32、r84×32，三笔fresh原生精确计划/唯一commit/成功终态和双边读回：玩家铁/齿轮/带为37/0/28→96/0/28→64/32/28→0/0/124，叶铁实际96、带产96；其他inventory/inc/held保全。原始 `dbb0605deb764c50bd673dd562483671:1–49`，45请求/48.234秒，accepted5→8、lifetime175→178，closing R6/tick98519419，仍未覆盖新增工作的旧save97255779。root独立证明 `45542a39486844ff96a9fdbfb9852dfc:1`，SHA-256 `415166898A98284FF493B0EC26515F7B061B7E882DC9A56ABB0CED2E2639A60B`。

R4按已验证五段走廊的首个完整20点native_grid自由空段施工，原生扣带124→104、20个实体成功终态tick98549088。其原声明90请求/360秒/≤2accepted预留保存；无人机完成约149.409秒，调用方在完成13个实体读回后耗尽90请求、152.457秒停止，没有继续派发或保存。不能把该调用方预算停止改判为未执行或重建。原始 `e4a7ad71299b4f01a201db9d5e0af412:1–92`，commit6/terminal77；root直接核全部原记录与台账，无unknown/未决，accepted8→9/lifetime178→179。root证明 `8499db80fc644dc5a001856368855042:1`，SHA-256 `B6C9E770F0E600C7EBCC0BBB59D72B66E29E521B87DF00653E411CDC0E2765B0`。

root批准不同目的的R4b保留成功前缀、补全20个读回并执行预留第10笔普通Save，不追加R4原额度。40请求/120秒/≤1accepted，实际29请求/5.165秒完成。实体按原生路径位置映射为 `5960→5961→5962→5964→5965→5967→5968→5969→5966→5963→5959→5958→5957→5956→5955→5954→5953→5952→5951→5950`；全段空货、内部19组有向互惠边、两端自由，无外部接线。正常保存98559834/R9/J100/restart ticket可用，external10/lifetime180立即冻结。原始 `f10e2f713f5641dcb42dcf80589c3553:1–31`，save commit27/terminal28，阶段SHA-256 `472FF3EF4CA348A37BF03FD2E78A00EC077772037A2E5A4D512C9B1AE6A8100C`。这不是新油源实际供料或重启证明。

## 首个十写封窗审计与历史续接点

root只读捕获 `de1353bfbd7b4d7b8d7ba23582c65e86:1–81`：79请求/28.522秒，built不可变snapshot tick98564357，完整60页/5969实体，独立0prebuild；首末session、Journal、玩家、功率和九个明确消费者/源/仓全部读取，closing98565858/R9/save98559834/J100。原capture seal SHA-256 `759F5F8BC04CB37B466E1566B3970CE5FE099F16AA94204DE5404E1A0999A966`。

与封存基线 `80dd28e14adb4206b13c69166abd6805:1–78` 的全字段投影差异：新增5948–5969恰为此前塔/采油器2和当前20带；无删除，11658条边全部互惠，旧配置/连接除以下两项资源成员变化均相同，动态变化196个对象。矿机1213的[36,37,43]→[36,37]与1496的[44,52,53]→[44,53]保留为明确异常项。root随后固定十次请求/60秒/最多四页的铁矿只读声明，实际5请求/1.629秒/1页95个active铁矿：保留36/37/44/53均存在、43/52在完整匹配产品目录中消失，兼容正常矿竭；不声称历史操作者归因。最新核验98583436/R9，Journal与基线的100条事件逐项相同、durable/pending/error正常，无unknown/在途。

十笔原commit/终态逐一核销，均completed、无幂等回放。基线至当前完整背包净差为铁−37、带+76、塔−1、采油器−1；来源为精确取铁59、制作耗铁96/产带96及施工耗带20，另含此前塔/采油器各1；其余物品/inc/held保全。原记录包括开片五笔、R3三笔、R4一笔、R4b一笔；正常Save覆盖全部十笔。完整差异证明 `043ecb139159458f983554ddd93bfd2f:1`，SHA-256 `DF0701C288097512B475A5826DE43319A8354C0C0B47974658EBA0970408A149`；独立最终审计 `7262afc3389b4eaaa0e164f1b4bc3c04:1–7`（proof7），SHA-256 `A1DB96D225BB172C45BDE8D1EE4BFB223EB9F9EBB6F8F275B3564C0B4E0A1E27`。完整覆盖、确切差异和点功率满serve通过；持续生产/补给与整案验收未通过。

当前DLL的列表行比基线新增detail-only `fuelPowerState:null`，旧投影调用方原本拒绝该未分类字段。最小修正只允许缺失/null占位，非null（observed/unknown/空对象/空数组）仍明确拒绝，不丢弃有内容的燃料观察；真实pwsh的factory-evidence回归26/26、0游戏调用通过。没有Plugin/MCP二进制修改或重复部署。

本窗新增油路仅2001×20，距总新带164上限剩144；玩家带104，原123预测剩103仍待实际NEW点核销，2011新增0/上限2，5946–5949不重建。待本次必要提交与准确SHA绿CI后由root明确OPEN重开external0、lifetime180并立即续接：按现有原生正例完成余下四个完整空段，再以实建ID核真实cover接缝；消费者接入、源口最后放料仍未执行。下一片须fresh精确计划，不裁点、不把自由端预检费用相加当真实接头账。

最终结束条件中的“原油实际进入3964”已由本次短读原生货物流与插入周期观察确认；共享供需与连续≥36000tick、双补给/准备清单、整合正常保存及受保护恢复、同干净SHA双候选包仍待验收。此次材料/空段/十写审计不替代这些门，也没有正式tag/Release/Thunderstore上传。

## 第二个十写窗口：下降、保存与独立审计

第一窗准确SHA的CI通过并由root重开external窗口后，R5新增20点空自由段，实际实体按fresh原生计划完整读回，空货、方向互惠且两端自由；玩家belt `104→84`，新油路累计40。R5成功后caller的fresh玩家状态显示两架无人机正在返航、0个build target，因此在下一段prepare前停止。root核实该段未prepare、没有后续accepted或在途动作；这是readiness stop，不是原生拒绝，不重建成功前缀。阶段run `e6a95df836794a8d9af0e7dde9d5b0c0`；root原件 `94895a69453841539c8f53a836bb7393:4`，SHA-256 `E4CCC0544AF4271A723D23C3E0375A3109ECBB67DAA0AB32EA84062DED316996`。

R5b以三个fresh原生完整路径各建17、20、16点，共53条成功belt，精确读回并保全库存、inc及手持物；玩家belt `84→31`，油路累计93。阶段run `939a8f7d9d7d4031b6e58a5ae40d9deb`；root原件 `11e5d7e6567c440893c245e7a823f6c3:1`，SHA-256 `BBD63BD1AC0D75727656526094416F5AC9F2682E6B005FEA7AAD8D2AEF58ECA5`。随后四个真实cover分别原生建成3、3、3、2点，共11条；计划材料与实际实体相符，路径共104点，全部空货、内部有向互惠、首尾自由。该阶段复用封存局部快照，原件含8项native rotation changes，不是新的全厂快照。阶段run `1d940b262b904a4a929df540328012f2`（107条原件，完成记录SHA-256 `5B017C120C5A53316F0AB3D889CF8F92E0D40997C341650753A2ABE5E53E3BE5`）；root原件 `a1eaf3dea5c04736a05d74ffdc7f3c52:1`，SHA-256 `667807DF57A6C038BAF9F48E81D760FAC14088302CDC0B5CC7BCA5DFFE11AC60`。至四covers时external `8/10`、lifetime `188`、玩家余20带；本阶段无新Save，保存仍是 `98559834/R9/J100`，5949未启用。

本窗最后两个accepted为7点自由下降和预留普通Save。下降实际建成6054–6060，完整空货有向路径，build action `e3c465fe-cdae-4187-b6ed-a2225a2982b3` 于tick `98676648` completed。随后唯一Save action `f8a498a2-689a-40f5-9971-85b56e06eb6a` 于tick `98676748` completed，保存点为 `98676748/R28/J100`，受保护健康恢复票据可用；玩家余13带。原阶段run `2ced5c4c3f9b4b65a17bfa22f114e1eb` 的最终原件 `:60` SHA-256 `F444DBBEE60C9FE128BF16564299162522B6915372EC232C4B8BB46D8E8503F9`；proof同时核出external10/lifetime190、冻结、下降7条、累计新建油路111条、旧4193保留、5949未启用。未重放任何accepted动作。

root独立十写审计逐项覆盖本窗10个唯一accepted，均completed且Save覆盖全部；完整背包只有belt `−91`，其他物品、inc及held不变。根级原件 `291396ee1ab148e38b2f54fae1402693:7`（`passed=true`），SHA-256 `F9484A6B096B021B73DB8C9800836F726963921084D3348617FC85C4154DF43D`。审计还复核与封存5969基线的完整拓扑差异、5950 cover及资源节点167变化，并确认0 unexplained。factory delta比较器对5950变化默认标记 `allowed=false`；这是比较器状态，不是root逐项审计结论。root另按四covers原生回执核销了5950原生覆盖及其旋转/新增输出6045。

当时的封窗静态边界：5949尚未连接/放料至3964；104点主路径与7点下降已读回，但末端连接、sorter接口及源admission仍待续接。随后施工、保存与实际到油证据见本文件下文。旧3964/4193及其它静态配置保留；当时油路距保守164上限53条、sorter预算2个。该阶段source-to-consumer实际油流、净氢/石墨/旧燃料共享供需、连续≥36000 ticks、补给准备清单、整合保存与受保护恢复、最终双候选包均未通过。本次审计没有重置游戏计数，也没有正式tag/Release/Thunderstore发布。

## 封窗后的只读原生资格

十写审计后只读原件 `1f72839a89e342ef9e2d830361ea2b36:1–16` 保留三个positive候选：6042→6054的2点gap与从6060继续的4点source-only尾延各自通过原生完整计划；3964 slot1 / 原油1007 / sorter2011 的tokenless consumer preview请求完整4个NEW belt点，原生回复 `nativeSpan=2`，不是2个NEW点。调用方在重新原生snap后对第一点作JSON严格相等检查并停止；root后续核对该坐标差 `0.0000161236m` 小于调用方几何对应容差 `0.0001m`，原生check已通过。三项成功候选均保留，未重跑。

focused continuation的root只读证明 `8339955b569e4f2da16b8183afc8cb0a:1–11`，SHA-256 `1C2A3FD8F1965ABC8463728F082050C341F556425589212AC1893A53B6FA4330`：仅新fresh准备了5949 source outlet的完整6点native span，并复核健康与consumer边界；同时离线核销 `1f728...:9,:12,:15` 的A/B和preview正例及重吸附差异，没有重新执行已通过候选。9请求后以 `98726758/R28` 结束，保存 `98676748`、J100 durable、external `10/10 FROZEN`、lifetime190，新增accepted为0。总belt forecast为 `2+4+6=12`，背包有13；sorter仅是2个预估，实际两个sorter实体ID及各自ordinary fresh prepare仍待后续阶段输出，SourceSort实际能力未证明。5949未放料。root当时给出的续接顺序为提交后准确SHA CI绿并重开，再完成2点gap、4点尾延及actual consumer sorter；source-port 6带与source sorter最后处理，实际接口须由阶段新读回核验。此资格只证明native候选，不是施工或供油结果。

## 资格续接后的建设、保存与首次到油读回

准确SHA CI成功及root重开后，writer `64b3f43cbcbd445daf70f72e69633c38:1–113`完成2+4带及consumer sorter `6067`；三个动作均完成，external升至3、lifetime193、R34，未保存。root独立审计 `155674d08ed64f0a92d0ec32ac7679e1:1` 的SHA-256为 `6F9E8285831C9961CE5C0F350C715DBD2E8AD6DFD5B8650050793F7AD60376D8`。

接续writer `8e42280c3d6a44828a12c2b15e60b377:1–87`以原生设备口完整span施工5949出口6带，ID按 `[6073,6072,6071,6070,6069,6068]` 排列，再将source sorter `6074`（`2011/filter1007`）连接 `6068→5960`。external共6/10、lifetime196、R39，六个唯一accepted均completed，无replay/inflight/unknown；正常Save `98801251/R39/J100`覆盖全部6笔，健康恢复票据可用。root独立审计 `9ed6b32c1dda4827aec1907ec8cecd5c:1` 的SHA-256为 `7330E19C4C65746B904F19D3BE850FD8404D9E092B2AA5B09218A223BE202BDC`。本段没有新全厂capture。

到油只读原件 `a7d27e5a5dc346019e974655f2fb9286:1–15`，SHA-256 `257A2F54C3D936157CB172AEAA8B46BC2292D41CAB26DBFBEA49C2B4CD275A80`，以8次同tick、每次9对象的cut覆盖来源、两段原生货物流路径、sorter和消费者边界。第一次观察 `98813432` 时 `6067` 持有原油1007，固定目标为 `3964/slot1`；随后 `98813924` 时，`6067` sorter处于Working/Returning，held为0且native progress `290000`，表明原生插入周期已经推进。旧`4193`保全。13请求/9.328秒后以 `98813936/R39` 收尾，Save仍是 `98801251/R39/J100`，external为6。该证据确认至少一次真实新源到油，不从consumer缓存单增归因，也不构成长时供率/供需平衡证据。

油路累计123条belt及2个sorter（belt保守上限164尚余41；sorter额度2/2已用），sourceSorter6074经新源口6带接既有5960；consumerSorter6067由新增尾端6066接至3964/r16 slot1。完整拓扑核对采用不可变6060全厂基线与施工后raw local readback overlay，不是fresh全厂capture；因此不报告新built总数或新的全厂差异审计。此后root完成的短窗供需与燃料读数见下节；36,000-tick连续供给及共享H/G/legacy fuel配平维持未通过，整案 `executable=false`。

## 首次到油后的共享供需短诊断

root只读采样run `a2820be3fd314980aeb65232a9605483` 的原件索引为 `:1–70`；完成记录 `:70` 文件SHA-256 `BD4C168AA82C7B799847C00BE0DA58DF564B0E018271CDBDD1C967646B79B470`。root独立审计 `69d1142aca204befa042d8a460fcacc2:1`，SHA-256 `24D69F2718B6B36D1F813D23CAB3EDE27A9EF8FAE3A7B82012207A5E2E978DD8`。63次实际read/37.328秒，0游戏写；closing `98829462/R39`，Save仍 `98801251`、J100、external6、lifetime196，窗口计数未变。三个工厂的完整分页均返回，22物料视图使用local104。

三个彼此有间隔的600-tick窗口为 `98827394–98827993`、`98828131–98828730`、`98828842–98829441`；因此continuous credit明确为0，不能拼作一段连续窗口。local104的P/C计数按这三个窗口顺序为：粗油 `2/2/2` / `2/0/2`；高能石墨1109 `3/2/3` / `2/1/2`；氢1120 `8/6/7` / `5/14/5`，其中消费者侧含r58循环、不是净氢；重氢1121 `0/0/5` / `0/0/0`。1210和1802的local104计数均为三窗 `0/0`；这只是所采短窗结果，不是永久零需求或零供给判定。

28对象的同tick库存cut分别在 `98827960`、`98828714`、`98829425`；每个cut去重覆盖123条带上的两条完整原生货物流路径。3964/r16 crude input为4，refined output为 `40/40/39`。3965与3966的每台cut均见refined input 2、H input 4、G output 20，生产progress `2,400,000`并停止；root独立解释为原生批次输出未被下游接纳。这不是Overseer finding，不能写成Overseer直接报告堵塞。5326的D input在三cut为`3→5→8`，尚未凑足批次10；旧3403为`2→2→4`。store 870保持石墨2500与酸100，5331 warper库存2549，各自库存样本稳定。

13项fuel状态来自各自时间戳的独立读数，不是同tick合并cut：134/183/2516读到实际缓冲及loaded氢1120；3058/3059/3060/3061/3062/3063读到实际缓冲及loaded石墨1109；3079/4227/4229/4230读到实际缓冲及loaded燃料棒1802。13项相关机器均报 `loadedFuelProductive=true` 且 `currentGeneration>0`；fusion loaded energy下降、燃料棒缓存仍为10。不得据此说燃料没有消耗，也不得将这些分时读数当作同一刻库存关系。N3点观察均full-serve、capacity `1,914,000`，三次req为`146,474/342,516/148,788`；N4点观察full-serve `1800`。这些短窗没有证明stable burn、净物料分配或持续功率。

四条既有filter `3069–3072`仍过滤1120，来源`3065/3066`没有自动输入；不能把更改这些filter写成持续供给修复。结合相间隔的短窗与点状fuel读数，尚不能闭合净氢/石墨/旧燃料配平、有限缓存排除或≥36,000-tick连续供给。暖机后的动态读回已完成，完整方案比较正在进行，详见下节；这些诊断没有批准任何新业务写。

### 暖机后补充读回与离线候选比较

补充run `e5241bdbdf7544c1add160ca5db56bc4` 的原件索引为 `:1–22`；完成记录 `:22` 文件SHA-256 `17DA7F35B62EE64E745FDAFF0A92C4308062BEAF421508F6EBC5AD39EAD99D74`：root固定20次实际读取/3.779秒、0游戏写；closing `98872758/R39`，Save `98801251`、J100、external6、lifetime196均未变化，J100完整耐久边界不变。same-tick 28对象cut位于 `98872689`，仍读取123带去重后的两条完整路径。

该cut/后续点读见3964 refined output 40，3965/3966各自G output20且仍有批次接纳堵塞；3083 refined input为0，3084 input为2且working。5329为working，5331库存2552（前次观察2549）；这两个点值不能作为持续产量。3403 D input为7，5326 D input为0。最新单个600-tick窗口P/C为：1210 `0/0`、1802 `0/0`、D生产5、G生产6/消费2、H生产13/消费16（含循环）；这些是单个短窗读数，不补回先前三个相间隔窗口的continuous credit。N3点读req `150,433`、capacity `1,914,000`且full-serve，N4仍full-serve `1800`。

静态支路关系补核为：3964精炼油4180只接到3965/3966，H4181接四台cracker、3064和3074；偶数组3058/3060/3062消费3965/3966支路，奇数组3059/3061/3063消费3083/3084支路。870可从3966/3084石墨输出到达，石墨库存2500、酸100。四条3069–3072 filter仍为1120，来源3065/3066无自动输入。

在该暖机诊断时点，root与writer仍在比较有界完整方案，尚未选定或批准新施工；后续选择及备料现状见本节顶部最新截面。该时点的单点动态读回不证明最终可行。
