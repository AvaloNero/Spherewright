# 紫糖主干碰撞与保存入口未达业务层

日期：2026-09-30（Asia/Singapore）。记录主干预检的原生拒绝，以及随后保存入口在descriptor查找阶段退出；不据此宣称存档损坏、隔离、结果未知或已永久丢失施工进度。

## 原生施工拒绝

run `af819fc5861742c9ada6a731a708ce17` ord5返回`BUILD_LOCATION_INVALID`：planned point3与既有item2001 belt1932重叠，0对象创建。ord6为tick80627903 / R43，accepted仍为**8/10**。cached factory snapshot `d049453bbbdd4049ac142b27e850555b` 已核1932是真实旧belt，input1931、output1934。

native `SnapLine`在`useOldPath=true`时由绑定端点决定路径，`preferredYaw`不控制路径；不得同址重试，也不得删除绑定端点来绕过碰撞。已成功的前缀`5198→5197→5199→84`与`5202→5201→5200`不重做，但均尚未保存。

## 保存入口结果

保存尝试run `398b62002d954c71a1ed33c8752f340d` / shell `35727`以exit1结束在descriptor查找失败。root核对该run没有受保护业务回执；未到save prepare/commit、没有save accepted，也未新增accepted。当前DSPGAME进程数为0，旧进程已不存在；本事项不改判为crash、quarantine或outcome-unknown，也不推断原施工永久丢失。当前无在途动作。

最近确认的`lastSaved`仍为tick80526460；J96 durable只见于较早只读run `062cd8fce1024bbf805f67a0381da62a` ord3。最低保存tick为80526460的保护票据有效期至次日，虽仍有效但已落后已观察进度，不能用于回滚或自动重放。

## 下一安全边界

下一阶段仅限Steam正常启动到菜单并做只读恢复检查；当前没有加载批准。检查能否保留进度后，再由root设计绕行方案。不能把菜单出现、旧J96或旧lastSaved当作新施工已保存、恢复已通过的证据。
