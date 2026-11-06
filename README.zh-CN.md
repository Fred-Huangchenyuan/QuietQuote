# QuietQuote

这个文件夹可直接提交到GitHub，见[最简上传步骤](UPLOAD.zh-CN.md)。Windows版已本地验证；新增[macOS原生预览源码](macos/README.md)及Apple Silicon/Intel自动构建，**尚未在Mac上编译或实测**。请先确认Mac CI成功，再进行真机验收。

一个轻量的 Windows 悬浮行情胶囊：显示 OKX 永续合约最新成交价，支持七段数码管数字、自动明暗配色、毛玻璃和中英文界面。无需 API 密钥。

[English](README.md) · [VS Code 命令行发布教程](docs/LAUNCH.zh-CN.md)

| 浅色 | 深色 |
| --- | --- |
| ![浅色](docs/images/light.png) | ![深色](docs/images/dark.png) |

图片为示例价格，并非实时行情。

## 下载与使用

从仓库的 **Releases → Assets** 下载 `QuietQuote-v1.3.0-windows.zip`，解压到可写文件夹，双击 `QuietQuote.exe`。升级前从托盘退出旧版。复制旧 `settings.json` 可保留偏好。

建议 Windows 10/11 + .NET Framework 4.8。无需安装、管理员权限或额外字体。当前版本未做代码签名。

- 默认 `KORU-USDT-SWAP`；设置内可选币种和实际可用的计价单位。
- 刷新间隔 1–3600 秒；每次请求完成后等待指定间隔。
- 扁胶囊／圆形可选，宽高、背景浓度、数字大小、置顶可调。
- 关闭毛玻璃后，可选自动适应背景、浅色或深色。
- 设置内切换中文／English，保存后菜单和提示同步切换。
- 左键拖动；右键或双击打开设置；`Ctrl+Alt+Q` 或双击托盘图标隐藏／恢复。
- 断线和过期有提示，自动重试；切换币种会清空旧价并丢弃旧请求。

显示的是最新成交价，不是标记价或指数价。计价单位选择对应的实际合约，不作汇率换算。

## 本地构建

在仓库根目录运行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

`package.ps1` 会重新构建并运行离线核心测试。程序在 `.build/app/`，发布 ZIP 和 SHA256 校验值在 `dist/`。

可选：`test.ps1 -Desktop` 打开测试窗口并检查绘制和背景采样，设置窗口可能请求公开合约目录；`test.ps1 -Live` 还会读取真实 OKX 行情。两者需要交互式 Windows 桌面，CI 仅运行离线核心测试。

## 隐私与兼容性

只请求 OKX 公共行情接口；没有账户接入、交易、遥测或自动更新。使用系统代理。`settings.json` 与 `markets.json` 保存在程序旁边，并已从 Git 跟踪中忽略。

自动配色每秒在本机采样周围最多 16 个像素，只计算明暗，不保存截图、不上传屏幕内容。它不能精确知道浮窗正下方的内容；复杂背景可手动固定配色。

毛玻璃效果依赖 Windows 合成器。模糊请求已返回成功，但最终桌面合成效果未完整目视确认；不可用时使用半透明外观。不同 DPI、多显示器、远程桌面、所有币种均未穷尽实测。快捷键可能被占用，可使用托盘。

## 许可证

[MIT](LICENSE)。本项目独立开发，与 OKX 无隶属或官方背书关系。
