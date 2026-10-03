# Spherewright 当前快照

更新：2026-10-03（Asia/Singapore）。阶段原件索引见[Gate 2证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。

## 当前已核状态

- Source HEAD `d8c4fdb`；installed `3fe31d1`、native `29104`，现有同批MCP能力为64 tools / 1 resource；本阶段未部署。必要normal restart后恢复到healthy owned primary，Steam保持开启，旧DSP结束后由Steam启动了新DSP；228项文件哈希匹配。
- Tail后正常保存为 **88629662 / R80 / J99**；随后cover save为 **88808715 / R81 / J100**、观察tick **88808731**。恢复后的owned保存为 **88808747 / R1 / J100**、恢复观察tick **88808759**。实际Codex关闭存活测试已取消，不能声称通过。
- 当前计数 **external 5 / lifetime 135**；恢复动作无unknown、重放或在途。阶段已停止且新写入被阻止（不是十项冻结），没有新施工授权。
- 恢复后sampler原件 `599570b88f2549919f5fee21d45f6fb9`记录三个独立600-tick窗口，每窗1210 produced/consumed=1/0，N3 ratio 1、capacity 1,914,000 J/t；5331库存依次248/249/250。读取耗时37.717秒，共41 sampler reads、46次只读读取；continuous credit为0。
- 最新完整capture `a0a3ce0a2351425ca68142a88c0a07a5:1–90`为 **60页 / 5945 built / 0 prebuild / 11620互逆边**，factory tick **88831552**、closing observation **88833228**。root审计proof `53d4bb2b10134bf395f5e6adfa52a487:1`，SHA-256 `8944A575EB057597A147BD19F36653538FE94F30651749CE408FFFB6FC121C8F`；审计26.282秒、0次新Game调用。快照与5945实体的既有pose/config/ID/连接无漂移，inventory与J连续，无duplicate、unknown、inflight或replay。N3为216 nodes / 538 consumers / 131 generators，ratio 1、capacity 1,914,000 J/t；5331库存254。

## 尚未闭合的边界

normal save → protected restart → 恢复后1210实际非零产出的垂直生命周期门已闭合；这不代表持续供料、36,000-tick验收或Gate 2全链通过。883/r99仍间歇，恢复后短窗PC读数为0/1/0，1210每窗虽产出1但消费为0；下一工作仍是1210持续供给归因与至少1分钟/36,000-tick验收，不转向燃料、远征或最终包。详见[阶段证据](evidence/2026-10-02/warper-automatic-source-and-build.md)。
