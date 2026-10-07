# 同星仿真驻留工厂与材料切片读取维护

日期：2026-10-08（Asia/Singapore）。本事件记录只读工厂范围与材料切片的代码维护和离线验证；当前游戏状态未变，新增读取尚未部署或实机验证。

## 读取边界与库存单位

旧详细读取曾返回 `BRIDGE_NOT_READY: factory is not loaded; no inventory observed`，原件 `911e19353cc349448a80b27a116ed111:3`，SHA-256 `3DEE947BAD80DAF453191E1E92E7E9A15B2695BEA12B35AC5865976306BD59E6`。这只说明当时 factory display 未加载，不代表库存为零。已核对的DSP 0.10.35.29104 Native `Assembly-CSharp.dll`（SHA-256 `6C122E5443E6843979B4064050DFCB5E0D75577A0B64F6AE4111290238B33C12`）显示，`PlanetData.UnloadFactory()`卸载显示并调用 `UnloadDisplay()`/`FlushPools()`，模拟工厂数据池可继续驻留。

本次只读详细检查允许显式请求当前owned恒星内已创建、simulation-resident且身份可双向核对的factory，即使display未加载。仍须匹配session/current-star、planet与factory索引以及两边对象引用；其他恒星、未驻留或身份不一致均不可用，缺失状态不转换为零。它不创建、加载或遍历其他工厂。normal-action prepare/commit保持当前本地星球限制，动作哈希和MCP工具/参数均未改变。

显式同tick material cut补充两类库存：物流站以 `storageSlots.count/inc` 读真实槽存量，不把订单或机队货物当作库存；普通thermal/fusion generator以 `fuelPowerState.bufferedFuelCount/inc` 读缓冲燃料，`loadedFuelEnergyJoules`是残余能量，不折算成燃料物品。电力generation buffer仍是J/t且排除在物品库存合计外。cut仍受256对象、64条路径、32768 cells和4096 path members上限约束；身份、tick、字段或单位不完整时整项不可用，不拼异tick库存。

root封存的11文件原件 `30504f6b8fae4db9be99bdda9cae92ec:1`，SHA-256 `5E18ED43075B1721FDF7AF7D35367C07BAEFCCFE24082546FDC0D9BDD3515C51`，并记录直接验证结果：相关Core测试52/52、MCP测试6/6通过；按29104 Native引用的完整Plugin Release构建0 warnings/0 errors。当前安装仍是 `f6694ee11e17a5b32c52c8499c495cbf2a97d301` 同源4+224 cohort；这批修改尚未冷部署，新的resident detail read/material cut也未在运行时验证。已知的旧P104短期运输观察不由本次代码验证重解释。

## 当前运行边界

当前owned状态仍为P104/R36/J100、Save `100342332`、external `5/10 FROZEN`、lifetime `285`，无unknown或在途，普通Save槽仍保留。前一窗口在P104需求站1657观察到一次短期运输/进料活动，但连续供给credit仍为0；它不是由未部署改动取得的新样本。

后续须经过获准的同批维护部署与健康同档恢复，再fresh确认simulation-resident factory detail和包含station/fuel的同tick物料切片。完整H/D/G、副产物与旧燃料竞争仍待核；共享物料/功率平衡、连续≥36000-tick验证、燃料/翘曲器双自动补给、保存恢复整合及最终同SHA双候选包均未通过，整案保持 `executable=false`。
