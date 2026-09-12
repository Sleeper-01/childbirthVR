# 序章工程修复说明（2026-09-11 第三次更新）

## 一、安全模式的真实根因（最新发现）

22:24 这次会话的 `Editor-prev.log` 揭示了真正的编译错误——**根本不是 XRI**，而是一个
对 Unity 版本极其敏感的 Editor 包：

```
Library\PackageCache\com.unity.ai.navigation@1.1.6\Editor\NavMeshAssetManager.cs
  error CS1061: 'NavMeshSurface' does not contain a definition for 'IsPartOfPrefab'
  (出现 8 次，行 48 / 90 / 174 / 229)
```

`com.unity.ai.navigation@1.1.6` 的 Editor 代码调用 `NavMeshSurface.IsPartOfPrefab`，
这个 API 是 Unity 2022.3 才加的。而项目 `ProjectSettings/ProjectVersion.txt` 写的是
**2021.3.45f2c1**——用 2021.3 打开必败，整个 `Assembly-CSharp` 没法产出 → 安全模式。

之前（13:00 那次）看到的 XRI 错误是次要问题（也已经修掉了），但真正的拦路虎是这个。

**修复**：把 `ProjectSettings/ProjectVersion.txt` 升到 **2022.3.62f3c1**。
所有包缓存里已经是 2022.3 兼容的版本（inputsystem 1.14.0、XRI 2.6.4、
OpenXR 1.14.3、ai.navigation 1.1.6），所以这次改完无需重新下载。

同时删除根目录那个误导性的 `projectVersion.txt`（写的是 2022.3.55f1，Unity 根本
不读它，留着只会让人误解）。

## 二、本轮改动

| 改动 | 说明 |
|---|---|
| `ProjectSettings/ProjectVersion.txt` | `2021.3.45f2c1` → `2022.3.62f3c1` |
| `projectVersion.txt`（项目根目录）| 已删除（误导性残留） |
| `Library_broken_20260911` | 已删除（被 2022.3 弄坏的缓存备份） |
| `Library_bak_20260911` | 后台删除中（约 500MB+，删完磁盘会干净很多） |
| `Library/CorruptedLibraryDetection` | 已删除（损坏标记，让 Unity 干净启动） |
| `Temp/UnityLockfile` | 已删除 |

## 三、本轮验证（22:24 日志 + 之前 13:00 日志）

**22:24 日志**（用户用 2021.3 打开的会话）：
- 工程用了 2021.3.45f2c1
- 编译阻塞在 `ai.navigation@1.1.6/Editor/NavMeshAssetManager.cs` —— 已确认无遗漏脚本错误

**13:00 日志**（之前的错误）：5 条 XRI 引用错误——这些**也已经修掉**了（`PrologueInput.cs`
现在是手写 `Physics.Raycast` + `InputDevices` 直读，没有 XRI 依赖）

## 四、验证方式与结果（真实数据，非推断）

主工程在本机一直卡在资源导入（无 ImportWorker TCP 端口绑定，疑似沙箱/防火墙），所以
在 `%TEMP%\ChildbirthVR-CompileTest` 建了同构工程验证。**该工程里的脚本与主工程
逐字节一致**（已用 diff 逐文件核对）。该工程已成功产出 `Assembly-CSharp.dll` /
`Assembly-CSharp-Editor.dll`。

### 4.1 编译
```
build.log: error CS 计数 = 0
Exiting batchmode successfully now!   return code 0
```

### 4.2 构建期运行时（BuildTest，逐项真实输出）
```
BT world=True cam=True          BT audio=True        BT ui=True
BT manager=True step=Boot       BT input=True
BT props lightdot=True handbook=True cards=3 nurse=True
BT flip True/True/False page=2        ← 翻页正确，第三次被夹在末页
BT room=2                       BT speak duration=0  ← 无配音包，按字幕时长走（预期）
BT font=True name=Microsoft YaHei     ← 中文字体命中，无需额外放 TTF
BT ALL INTERACTIONS OK
```
`BT ERRORS:` 里只有一类：**`Destroy may not be called from edit mode`**。
这是因为测试是在编辑模式下直接调 `Create()`，运行时（Play 模式）不存在该问题，
不影响实际运行。

### 4.3 场景文件
早先重写 `Prologue.unity` 时漏了 Unity 2020+ 组件必需的
`m_CorrespondingSourceObject / m_PrefabInstance / m_PrefabAsset / m_GameObject`
四个头字段，打开场景会报 `Component (Transform) has a broken GameObject reference`。
**已补齐**，重跑后该警告消失。

### 4.4 未完成的验证
进 Play 模式的冒烟（`SmokeTest`）始终跑不到检查点——日志停在
`Scanning for USB devices`（OpenXR 在无头显环境下反复扫描）。属环境限制，非代码问题。

## 五、你现在要做的

1. **关掉所有 Unity 窗口**（Unity Hub 可以留着）
2. 从 Hub 打开本项目。`ProjectSettings/ProjectVersion.txt` 现在是 **2022.3.62f3c1**，
   Hub 会自动选 2022.3 — **不要手动切回 2021.3**（一切就又卡安全模式）
3. 第一次打开会触发 `ArtifactDBVersion changed` 重新导入，**不要碰它**，等 5~15 分钟
   （Library 已经清理过损坏标记，导入会比较顺利）
4. 打开 `Assets/Scenes/Prologue.unity` 播放（桌面模式：右键拖动转视角、
   左键点击=扳机、A/D 或 ←/→ = 摇杆、P 暂停、R 重播、H 帮助、V 切 VR 模式、C 回正）
5. 打开后看 Console 如果还有红色错误，请把第一条 `error CS` 截给我

## 六、仍未处理（非阻断）

- 无 3 分钟总时长控制（表4 要求整段约 3 分钟）
- `Assets/Scripts` 下残留 4 个测试脚本（TestProject / SimpleTest / BasicTest / UltraSimpleTest）
  和 `Assets/Scenes/UltraSimpleTest.unity`，类名无冲突、不影响编译，可随时删
- 暂未接 XRI 的 Interactor 体系（当前用 Physics.Raycast 自实现，VR 手柄位姿由
  `InputDevices` 直接读取，可跑但没走 XRI 的标准事件流）