# 近铁源有限到达与保存核销（R725–R729）

本记录只核销有限到达动作及其保存覆盖；它不证明dryland、施工路线clearance、铁源接入或持续供给。

R725 prepare-only资格原件 `4261fe8cfb884fdcac55d44c5acc03d0:12`，SHA-256 `DF718B2CB1560EE6500C2F65DD8BD9C9A63ED938041545FB24359B7D016C11FC`；root R726 `704093c75d8a44f985e18310abb78ebf:1`，SHA-256 `469ED94C36F19D4AE703634AFB8CB6CAECADD3BF8F72EB8B6D19B7A7A2F2B8EA`，核验首段28 m Native候选具有完整29点与58条ray、无岸线风险，CoreEnergy为192.96 MJ。R728原件 `b0a322f8a3334c0b9e20461ff9fd501e:1`，SHA-256 `59D5889A26363E58478834C2DBEC044927844C7BF7049D6FE0D5869F0A4CB602`，限定本阶段最多8次Move加1次Save，不延长原施工/固定窗口预算，也不重试已退役的4 m候选族。

R727 writer原件 `59896294941d4cee9fc618205b909c22:266`，SHA-256 `B7548F66EB3CDB656A136147B5CA7FF2E912D6E816690440B734A4721C4EADAF`：实际完成7次普通短Move与1次Save，共8笔accepted、231请求、209076.6753 ms。root R729 `bd06e982049e43bd97125b623baae402:1`，SHA-256 `65EFAEAF9FA7D07EBE9C6B111B06BB661365A1FE50BEA56C9A4F548A6FA70004`，独立核对全部266条原记录、8个唯一Native/intent/ACK/成功terminal，以及固定窗口13笔唯一动作均被正常Save覆盖；没有重放。

R729核验的当前状态为P104/R46、正常Save `106012859`、durable Journal 102、external `13/20`、lifetime `447`，无在途或unknown。完整工厂65页、6475 built/0 prebuild；相对封存基线没有新增、移除、静态变化或非互惠边，本地电网满供。Iron节点40剩余28076、minerCount为0且在玩家建造范围内；核验记录中的玩家至节点40距离约45.344 m、至磁铁炉1216约34.729 m。最新玩家读回tick `106013069`，Walk速度0、CoreEnergy约207.546 MJ、reactor为0；≥100 MJ到达reserve已有fresh证明，但Walk速度0不能证明dryland或上岸。

Stone6仍未归因，获取/供给信用为0；不以用户手动操作作为阻塞。下一步是对节点40做fresh Native矿机覆盖预检，并核验独立出料带与带到带分拣器接入1218。施工批准仍为false，铁源供给、Warper与Rod各至少1/min、连续36,000 ticks、双自动补给、整合保存恢复与最终同SHA双候选包均未通过。
