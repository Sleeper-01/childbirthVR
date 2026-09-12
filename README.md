# VR分娩预演 - 序章：进入待产室

这是一个基于 Unity 的 VR 项目，实现了《VR分娩预演》的序章部分，包含表4中描述的5个步骤：
1. 手柄校准
2. 情境导入
3. 领取手册
4. 转场教学
5. 工具栏认知

## 功能特点

- **VR/桌面双模式**：支持 VR 头显和桌面预览（无头显）
- **手柄交互**：射线瞄准 + 扳机确认 + 摇杆翻页/选择
- **程序化音频**：所有提示音和台词配音均由代码合成，无需外部音频文件
- **中文支持**：内置中文字体回退机制，保证 UI 中文正常显示
- **舒适度保护**：晕动保护开关，转场时自动拉起暗角
- **完整流程**：表4五步骤完整实现，包括护士引导、手册翻页、场景转场等

## 目录结构

```
ChildbirthVR-Prologue/
├── Assets/
│   ├── Scripts/          # 核心脚本
│   │   ├── PrologueDefs.cs      # 数据/文案定义
│   │   ├── PrologueFont.cs     # 中文字体处理
│   │   ├── PrologueAudio.cs    # 程序化音频
│   │   ├── PrologueUI.cs       # 头部跟随 UI（字幕/任务卡/工具栏）
│   │   ├── PrologueInteractable.cs # 交互物基类
│   │   ├── LightDot.cs         # 光点（步骤1）
│   │   ├── ToolbarButton.cs    # 工具栏按钮
│   │   ├── SceneCard.cs        # 场景卡（步骤4）
│   │   ├── Handbook.cs         # 手册（步骤3）
│   │   ├── NurseController.cs  # 护士小安
│   │   ├── PrologueWorld.cs    # 世界场景与输入系统
│   │   ├── PrologueInput.cs    # 输入处理（VR/桌面）
│   │   ├── PrologueManager.cs  # 流程管理器（五步骤状态机）
│   │   └── PrologueBootstrap.cs # 引导启动器
│   ├── Scenes/           # 场景文件
│   │   └── Prologue.unity      # 序章主场景
│   └── Resources/
│       └── Voice/             # 台词语音（可选）
├── Packages/            # 依赖包清单
│   └── manifest.json
├── ProjectSettings/     # 项目设置
│   └── ProjectSettings.asset
├── .gitignore           # Git 忽略规则
└── README.md            # 项目说明
```

## 运行说明

### 项目设置

1. **使用 Unity Hub 打开项目**：
   - Unity 2021.3 LTS 推荐版本
   - 通过 Unity Hub 导入 `C:\Users\23089\Desktop\ChildbirthVR-Prologue`

2. **包管理**：
   - Unity 会自动下载所有必需的包
   - 确保启用 XR Plugin Management 和 OpenXR

3. **场景设置**：
   - 打开 `Assets/Scenes/Prologue.unity`
   - 在场景中创建一个空的 GameObject
   - 将 `PrologueBootstrap.cs` 脚本附加到该 GameObject
   - 运行场景

### 运行模式

1. **VR 模式**：
   - 确保 VR 设备已连接
   - 在 Unity 编辑器中点击 Play
   - 或 Build 后直接运行

2. **桌面模式**：
   - 不需要 VR 设备
   - 使用鼠标/键盘进行交互：
     - 鼠标移动：射线瞄准
     - 左键点击：确认操作
     - WASD：移动视角
     - P键：暂停/继续

## 编辑器工具

在 Unity 编辑器中，可通过菜单 `ChanFangVR/序章/` 访问以下工具：
- 重置到步骤1
- 跳转到步骤2-5
- 完成序章
- 切换VR模式

## 场景设置说明

1. **基本场景结构**：
   - 场景应包含一个主摄像机
   - 创建一个空对象附加 `PrologueBootstrap` 脚本
   - 所有其他组件会自动初始化

2. **组件依赖**：
   - `PrologueBootstrap` - 主入口点
   - `PrologueWorld` - 世界场景和输入系统
   - `PrologueUI` - UI系统和显示
   - `PrologueManager` - 流程管理器
   - `PrologueInput` - 输入处理
   - `PrologueAudio` - 音频系统

3. **常见问题解决**：
   - 如果出现编译错误，检查包依赖是否正确
   - 如果无法运行，确保已安装 XR Interaction Toolkit
   - 如果UI不显示，检查摄像机设置

## 故障排除

1. **包依赖错误**：
   - 删除 `Library` 文件夹，重新导入项目
   - 检查 `Packages/manifest.json` 是否正确

2. **编译错误**：
   - 确保使用 Unity 2021.3 LTS 版本
   - 检查所有脚本是否有语法错误

3. **运行时错误**：
   - 确保 `PrologueBootstrap` 已附加到场景对象
   - 检查摄像机是否正确配置

## 技术说明

- **Unity 版本**：2022.3.55f1（兼容 XR Interaction Toolkit）
- **输入系统**：XR Interaction Toolkit（VR） + 鼠标/键盘（桌面）
- **UI 系统**：WorldSpace Canvas + 头部跟随
- **音频**：程序化合成（提示音） + 可选外部配音
- **兼容性**：支持 Windows/macOS/Linux，VR 头显需 OpenXR 支持

## 注意事项

- 项目使用程序化资源（贴片、字体），无需外部资源文件
- 台词语音文件（可选）需放置在 `Assets/Resources/Voice/` 目录下
- 调试时可使用编辑器工具菜单快速跳转步骤

## 许可证

本项目仅供教育/研究用途，遵循 MIT 许可证。