# 磁铁供料分拣器周期升级与保存核验（R798–R802）

本页记录一个有限准备阶段及其独立核验。该升级提升指定分拣器的周期能力，不证明来源或持续产量。

R798 的七个唯一动作依次完成有限取料（仓库 28 的铁料 1、仓库 727 的 Motor 1）、recipe 85×1、recipe 88×1、正常移动、分拣器 6492 原生升级，以及普通 Save `106821990`。玩家净差量为物料 1301 `−1`、分拣器 2012 `+1`；Warp 400、Rod 5、Stone 6 保持。原始 writer 记录为 `f1f94261b6e047de837e52e226f6e67a:183`，SHA-256 `C9DEBB361E672C875996E1C40DF4BAAA26B143ED10F007342B68245B4E3756A6`；153 次请求耗时 65.477904 秒。

R802 独立核验确认七个动作均唯一闭合并由该保存覆盖，durable Journal 102，窗口 external `14/20`、lifetime `468`，无在途或未知动作。root 审计原件为 `0299d7dfe49841f0b44eefb14c697743:1`，SHA-256 `C901AC2DFFB60456ECF040A0273678C0F5997A2DA53A74E1ADB96C2EE932BD08`；审计耗时 37.504137 秒。

完整工厂为 6492 built/0 prebuild。相较阶段前，唯一静态变化是对象 6492 的 itemId 从普通分拣器 2011 升级为快速分拣器 2012；对象数量、双向连接和其他静态字段不变。filter 1001、pick/insert 端点 `6477→1220`、持货 count/inc 与连接保持，Native `progressRequired` 从 600000 降为 300000；N3 满供电。两个准备前的本地只读解析问题（皮带 DTO 字段与 build catalog 集合名）已通过原件核验；它们均为 0 prepare/0 commit 的调用方问题，不是 Native 拒绝。

R783 的长窗来源失败仍有效：P102 Ti Ore（1004）与 P104 Ti Ingot（1106）速率为 0/min，另有六项库存超单批容差。R798 的有限取料、手搓和升级不等同于持续生产。下一步需短窗口核验磁铁供料路径实际吞吐，并对铁与钛进行最小来源修复；`sourceConditionsPassed=false`、`wholeSupplyPassed=false`、continuous credit 为 0。
