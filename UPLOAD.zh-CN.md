# 直接从这个文件夹提交到 GitHub

当前文件夹就是完整仓库根目录：Windows正式源码、macOS预览源码、README、MIT许可证、构建脚本与GitHub Actions都在这里。没有个人配置、行情缓存或Git历史。

## 一次性设置

先在 GitHub 网页新建一个空仓库（不要额外勾选README或LICENSE），复制它的HTTPS地址。
在 VS Code 中打开此文件夹，然后在 PowerShell 终端运行：

```powershell
git init -b main
git add .
git commit -m "Initial release: Windows ticker and macOS preview"
git remote add origin https://github.com/YOUR_NAME/QuietQuote.git
git push -u origin main
```

把 `YOUR_NAME` 换成自己的账号。如 Git 提示缺少身份，再设置当前仓库的名字与提交邮箱，然后重新执行 commit：

```powershell
git config user.name "YOUR_NAME"
git config user.email "YOUR_GITHUB_COMMIT_EMAIL"
```

这里不需要 GitHub CLI。Git/VS Code 会在需要时引导你登录。顺序是 **add → commit → push**。不要把远端地址放在推送命令之前误执行两套初始化教程。

## 后续修改

```powershell
git add .
git commit -m "Describe your change"
git push
```

## 上传后的产物

GitHub Actions 会出现：

- **Windows build**：构建并测试 Windows 正式版。
- **macOS preview build**：分别在 Apple Silicon 和 Intel 上尝试构建并测试 Mac 预览版。

各任务成功后，在该次运行页面的 **Artifacts** 下载ZIP。Actions不会自动创建Release。

**Mac源码目前未在Mac上编译或运行。必须先看CI结果，再到真实Mac上测试。** 已完成和待完成的范围见 [macos/README.md](macos/README.md)。如果某个Mac任务失败，把日志交给维护者修复，不要把它当成已可用的Mac发行包。

对外发布时，在GitHub的Releases页面创建版本并上传Windows ZIP；Mac建议单独使用预发布版本，如 `v1.3.0-macos-preview.1`，并注明未经Developer ID签名/公证。普通用户无需下载源码来运行Windows版。
