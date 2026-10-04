# Spherewright 当前快照

更新：2026-10-04（Asia/Singapore）。本阶段权威事实见[材料库存与恢复证据](evidence/2026-10-04/material-inventory-cuts.md)。

## 当前状态

- 当前已验证并安装的代码 cohort 来自 `b1557bb`，Windows Core CI 37206538632 已成功。此提交的隔离 cohort 完成 locked restore、Release build、测试与 MCP publish：2551/2551 通过，4 个 Plugin 文件和224个 MCP 文件、64 tools/1 resource，playbook匹配；受保护回执 2ed96cf586834604b91ad1fd066f67a2:1 / 11725B3778E2365B9839F8F7CF211E141F95C7E803642EAFC869CBDBAD4066A9。该离线 cohort 随后经必要冷事务安装并核228文件同批匹配；当前 DSP 使用本阶段构建，native 0.10.35.29104。Steam保持运行；未做专门Host关闭存活测试，用户已取消此测试。
- 重启前正常保存为 tick92128707 / R2 / J100，external8 / lifetime148；必要冷事务保留Steam并启动新的DSP实例。随后受保护默认primary恢复到同一owned世界，新存档 tick92128739 / R1 / J100，external9 / lifetime149；inventory、手持和100条Journal连续。恢复准备阶段的exactEmbeddedIdentityVerified为false，因此不声称加载前已完全读回；native terminal adoption/readback确认后才核实同一primary。保存约2.73秒、恢复约9.5秒。
- 此前恢复/采样阶段的完整工厂捕获为60页、5945 built、0 prebuild、17 details、11620互逆边，无实体增删；这是905…阶段的既有证据，不是92128739这次恢复后重新采集。1535和2317保留2011→2012升级；1512在相应基线已为2012。唯一允许的资源节点差异为已核实耗尽的2440/node168；198项动态buffer差异单独保留，不泛化忽略。root proof d7fc1a41af3a4e67a06e5e259c618d34:1 / 64D0B819291E6E69BEED440DACD10D8443C9812A9C974878B13E60B7C7105B0C。
- 此前阶段的六个分离600-tick观察窗中，1210仅见1件原生生产，其余窗为0；coveredContinuousTicks=0，不计长窗信用。1210库存1011→1013含窗间未采样生产，不当作六窗3件实产。N3各窗full-serve，容量1878000–1914000 J/t，高于当时声明峰值1804700 J/t。这些窗口不是92128739恢复后的新采样。原件 c2c6ae9c3199460380d5b6a359ac79f4:1–143；独立核验45a1756ad0844cb2b93e172832a77837:1 / AF988AC6A02B44EA05AA32C705793F609A35B173A6487AC0F0A4E465F2443713。

## 尚未闭合

- 既有连续采样只通过声明的最低产率门：1210非重叠产量下界18≥需求11，PC2832→2835；旧材料union仅给计数区间，不能证明来源已配平。有限缓存排除、全源归因、Governor与完整Gate2仍未通过。
- 14项、0写入的材料 cut 点读已完成并由 root 核验：run `095da7e9b72e4c1ebb14d989c3b16093`，耗时7.6554198秒，proof `7430a5fd350347cfb15d3bc9b98d34c5:1` / `C725958C50E94FF46E2551FE8809A11FFF4D3F74A481B98EB823DF66E66BDE3F`。7个 cut 均在各自同一 tick 观测；不同 cut 的库存不可拼接或相加。已观测 item 1209/1112/1127/1206/1121/1101/1210 的库存分别为0/118/0/3264/10/3446/1762。该点读不证明来源已配平、持续供给或 finite-buffer 排除；下一项是基于已核完整 native 路径制定前瞻来源/有限缓存核验，而非重复全厂 cut。最新观察为 `S92211338/R1`；正常保存仍为 `92128739/J100`，external9/lifetime149，无unknown、inflight或未核销accepted动作，未新增保存。Game writes保持blocked，未开启其它施工或再加载。

历史十写外部计数按已核动作连续累计；本阶段当前为external9 / lifetime149。正常保存与恢复事实、完整快照和短窗边界见阶段证据；未把过去信用重置或与后续窗口拼接。
