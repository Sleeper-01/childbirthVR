# VR 分娩预演 · 序章：进入待产室

基于 Unity 的 VR 项目，实现《VR 分娩预演》的序章（需求表4 的 5 个步骤）。

- **Unity 版本**：2022.3.62f3c1
- **渲染管线**：Built-in（Forward）
- **模型加载**：UnityGLTF 2.14.1（运行时加载 GLB）
- **VR / 桌面双模式**：有头显走 OpenXR，没头显用鼠标键盘也能完整体验

> 要接手继续开发，请看 **[开发交接说明.md](开发交接说明.md)**（里面有环境、目录、操作键位、未完成项、调参入口、模型踩坑记录）。

---

## 快速开始

1. Unity Hub → Open → 选本项目文件夹
2. 打开 `Assets/Scenes/Prologue.unity`
3. 按 Play（场景里已有 `PrologueBootstrap`，不用手动挂脚本）

首次打开需要联网拉包，导入约 3~10 分钟。

---

## 已实现的流程

| 步骤 | 内容 |
|---|---|
| 1 | 手柄校准（光点跟随） |
| 2 | 情境导入（护士小安引导 + 字幕） |
| 3 | 领取待产手册（可翻页） |
| 4 | 转场教学（三张场景卡 → 选房间） |
| 5 | 工具栏认知 |

---

## 操作

**桌面**

| 操作 | 键位 |
|---|---|
| 转视角 | 按住鼠标右键拖动 |
| 确认 | 鼠标左键 / Enter / 空格 / E |
| 摇杆（翻页、选择） | ←/→ 或 A/D |
| 视角回正 | C |
| 暂停 / 重播 | P / R |
| 帮助 | H |
| 切换 VR 模式 | V |
| 切换房间 | M |
| 跳过当前步骤（调试） | L |

**VR**：手柄射线瞄准 + 扳机确认，右摇杆翻页/转身。

---

## 功能特点

- **VR / 桌面双模式**：同一套流程，两种输入都走 `PrologueInput.cs` → `DesktopInput.cs` 兼容层
- **程序化音频**：提示音与台词配音全部由 `PrologueAudio.cs` 代码合成，不依赖外部音频文件
- **中文支持**：内置字体回退，WorldSpace Canvas 上中文正常显示
- **舒适度保护**：舒适模式开局即开启，转场自动拉暗角
- **性能兜底**：掉帧时自动降渲染分辨率保帧率（不减模型面数）

---

## 目录结构

```
ChildbirthVR-Prologue/
├── Assets/
│   ├── Scenes/Prologue.unity      # 序章主场景
│   ├── Scripts/                   # 全部代码（详见开发交接说明.md）
│   ├── Resources/Models/          # OperatingRoom / ActivityRoom / Nurse / OperatingTable (GLB)
│   ├── XR/Settings/               # OpenXR 配置
│   └── Editor/BuildScript.cs      # 打包脚本
├── Packages/manifest.json
├── ProjectSettings/
└── 开发交接说明.md
```

---

## 常见问题

| 现象 | 解法 |
|---|---|
| 报 `User access token is expired` | Unity 账号登录过期，Hub 退出重登 |
| 一按 Play 就自动暂停 | Console 的 **Error Pause** 点灭 |
| UnityGLTF 拉不下来 | 网络连不上 GitHub，手动装 2.14.1 |
| 模型加载失败 / 白模 | 如果走 Git clone，执行 `git lfs install && git lfs pull` |

---

## 已知未完成项

1. 第三张场景卡「产房」缺少房间模型
2. 尾声承诺的「第一幕」尚未实现
3. 待产手册第 5、6 页为占位文案
4. `Assets/Resources/Voice/` 为空，暂无真人配音（现为代码合成）
5. 呼叫铃只有视觉反馈，无实际逻辑
6. 打包为桌面版，VR 版需在 XR Plug-in Management 中给 Standalone 启用 OpenXR

---

## 许可证

本项目仅供教育 / 研究用途，遵循 MIT 许可证。
