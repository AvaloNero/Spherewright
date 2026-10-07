# 同星仿真驻留工厂与材料切片读取维护

日期：2026-10-08（Asia/Singapore）。本事件记录同星仿真驻留工厂读取能力从实现、同源冷部署、健康恢复到只读实机验证的闭环；它没有改变Normal-action写入边界，也不代表持续供需验收通过。

## 读取边界与库存单位

旧详细读取曾返回 `BRIDGE_NOT_READY: factory is not loaded; no inventory observed`，原件 `911e19353cc349448a80b27a116ed111:3`，SHA-256 `3DEE947BAD80DAF453191E1E92E7E9A15B2695BEA12B35AC5865976306BD59E6`。这只说明当时 factory display 未加载，不代表库存为零。已核对的DSP 0.10.35.29104 Native `Assembly-CSharp.dll`（SHA-256 `6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12`）显示，`PlanetData.UnloadFactory()`卸载显示并调用 `UnloadDisplay()`/`FlushPools()`，模拟工厂数据池可继续驻留。

本次只读详细检查允许显式请求当前owned恒星内已创建、simulation-resident且身份可双向核对的factory，即使display未加载。仍须匹配session/current-star、planet与factory索引以及两边对象引用；其他恒星、未驻留或身份不一致均不可用，缺失状态不转换为零。它不创建、加载或遍历其他工厂。normal-action prepare/commit保持当前本地星球限制，动作哈希和MCP工具/参数均未改变。

显式同tick material cut补充两类库存：物流站以 `storageSlots.count/inc` 读真实槽存量，不把订单或机队货物当作库存；普通thermal/fusion generator以 `fuelPowerState.bufferedFuelCount/inc` 读缓冲燃料，`loadedFuelEnergyJoules`是残余能量，不折算成燃料物品。电力generation buffer仍是J/t且排除在物品库存合计外。cut仍受256对象、64条路径、32768 cells和4096 path members上限约束；身份、tick、字段或单位不完整时整项不可用，不拼异tick库存。

源码离线验证由root封存于11文件原件 `30504f6b8fae4db9be99bdda9cae92ec:1`，SHA-256 `5E18ED43075B1721FDF7AF7D35367C07BAEFCCFE24082546FDC0D9BDD3515C51`：相关Core52/52、MCP6/6测试通过，按Native29104引用的Plugin Release构建0 warnings/0 errors。随后源码提交 `863d35546f6cb49fcdaec5b5814869d1e13af42b` 的Windows Core CI `37698616442`成功；干净同源构建/发布证据 `2441d64ed9064d06810bfcfd10acad36:5`，SHA-256 `A65BD874A05A0EE1C84588C2CA27E3D305EAC4D56690CECCAE07147E9EB8CC08`，完成locked restore、Native29104 Release build/test/publish，2672 tests、228 files、64 tools/1 resource。

## 同源部署与健康恢复

R130在部署前完成普通Save `100406334/R37/J100`，原件 `3035b7160d544011b58c5bc4b7577dbf:16`，SHA-256 `04DB192F67828A6AFE91224652D1618A52F93938C22ABDCF260BCA64C0739181`；root以R132独立核验 `8fb79216258f4ee99c011ee4fe13f9f8:7`、SHA-256 `5AFAE5316639207FA612452252B00F6D58F656DC79C3231DBE70FCD3146F3FA0`。R133完成同源冷部署：正常关闭并提交事务，228个文件哈希匹配，保留Steam版本36408，仅一次桌面启动分发；没有强制关闭、Host测试或手动加载。部署原件 `18d792c4777040648c69291dff55e8c4:6`，SHA-256 `3DC47C7964A95696D698873A9F552CDABCD0B856EF974D8FA097254368F5F112`；菜单原件 `236a7b44060b442e88cee0a7ae0065ad:9`，SHA-256 `94B4398018180406152B6782456399AC39919EF3D8B78D6196BA23FD1DC44A5E`。启动初期Bridge描述文件未就绪时没有发出Game请求，随后在同次启动中ready。

首次resume caller把旧Save值 `99927096` 用作firstGate参数，在任何Native请求前被拒绝，0 accepted；零写proof `ea7a377e4ae14f2c83bb5f17d97cb868:1`，SHA-256 `3F1C9281CEE0A8E916C8DF5890EA5C7522E8B7573A27D52B0384DA5D42B6BDE9`。修正调用方后，healthy primary resume正常完成并停止：receipt `44eda663e23c45909720336f220a070f:16`，SHA-256 `6DD4DDC136B3F75E5C17EC5BBADD31A89130A6FB1317BCC83FDF9960F69EECF2`；状态为P104/R1/J100、Save `100406366`、external7/lifetime287，无unknown或在途。该resume结果不提供key/completedTick，`exactEmbeddedIdentityVerified=false`是默认路径值，不适用于该路径；独立复核基于原intent与恢复核验，没有把默认值改报为true。

## 部署后只读实机核验

root独立复核原件 `98e07782eeb847ec85f57c9bf8621baa:8`，SHA-256 `1E2EE8767FD16176068E01C8287E7578999F7226F96F7CF0C35072941FB7D6DB`。其读取包含本地P104的46对象切片、station1657与16台普通fuel generator，以及远端P102的8对象/3条完整Native路径切片（观察到Ti200与Si500）。P104与P102切片各保留各自Native tick，没有跨星球拼接库存。既有R138五次成功只读前缀通过原件 `6ce6f6b96c1f4c97adffce9e0aa0f9b3:1–6`复用；root也单独核销了caller对缺失sessionId的路径DTO解析问题。

切片将station槽库存、普通燃料缓冲按其item count/inc读取；orders、loaded heat、J/t电力buffer均不当作item stock。负例原件 `8de4fa086ba6409ab2c40892fdc8522a:9`，SHA-256 `98A57A6901F4393B13536B382A0F86C78C391193DF3DCADC56F1EAB909503E66`记录unsupported实体/缺失身份仍为unobserved、257对象请求被拒绝、异星旧索引返回STALE_STATE；7次只读、0写，J100、健康和计数不变。原生读取能力因此已在运行时确认，同时fail-closed边界也已验证。

## 当前运行边界

当前为P104/R1/J100、Save `100406366`、external `7/10 FROZEN`、lifetime `287`；无unknown或在途，writer已停止。R98读取仅是有限只读样本，continuous credit仍为0。局部物料和功率观察不能代表长期净产量，也没有把不同星球或不同tick的库存合并。

完整H/D/G、副产物及旧燃料竞争仍待复核；共享物料/功率平衡、连续≥36000-tick验证、燃料/翘曲器双自动补给及最终同SHA双候选包仍未通过，整案保持 `executable=false`。后续启动诊断和有限修复须由root另行明示范围。
