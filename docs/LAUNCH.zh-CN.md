# 用 VS Code + PowerShell 发布到 GitHub

如果你只想使用现有Git命令直接提交，不需要GitHub CLI：请优先看仓库根目录的 [UPLOAD.zh-CN.md](../UPLOAD.zh-CN.md)。下文是可选的CLI方式。

源码包已准备好，但尚未为你创建 GitHub 仓库或上传文件。按顺序逐段执行；任何命令报错时先停止，解决后再继续。

## 1. 打开正确的文件夹

解压 launch ZIP。用 VS Code 打开包含 `README.md`、`src`、`scripts` 的文件夹，而不是它的上级目录。

打开 **Terminal → New Terminal**，确认终端使用 **PowerShell**。执行 `Get-Location` 和 `Get-ChildItem` 检查当前目录。

## 2. 安装 GitHub CLI 并登录

你的当前电脑已有 Git、VS Code；缺少 `gh`。安装：

```powershell
winget install --id GitHub.cli --exact --source winget
```

安装完成后关闭并重新打开 VS Code，让 PATH 更新。然后：

```powershell
git --version
gh --version
gh auth login --hostname github.com --git-protocol https --web
gh auth setup-git
gh auth status
```

在浏览器完成 GitHub 登录，按终端提示操作。不要把密码或 token 写入源码。若提示没有 `winget`，使用 [GitHub CLI 官方安装说明](https://github.com/cli/cli#installation)。

## 3. 测试并生成发行包

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

成功后会看到 `ALL PASSED`，并生成：

- `dist/QuietQuote-v1.3.0-windows.zip`
- `dist/SHA256SUMS.txt`

这里的 `ExecutionPolicy Bypass` 只对这次启动的 PowerShell 进程生效，不修改机器的持久策略。如果公司策略仍阻止脚本，请遵循本机管理员的要求。

## 4. 初始化源码仓库

以下两个身份值先改成你的信息。提交邮箱可从 GitHub **Settings → Emails** 复制其提供的 noreply 地址。命令只设置当前仓库，不改全局配置。

```powershell
git init -b main
git config user.name "YOUR_GITHUB_NAME"
git config user.email "YOUR_GITHUB_NOREPLY_EMAIL"
```

MIT 的版权行目前是 `2026 QuietQuote contributors`。可在 `LICENSE` 中改为你的姓名或 GitHub 昵称；改完后重新运行 `package.ps1`，使发行包也带上同一份许可证。

```powershell
git add .
git status --short
git diff --cached --stat
git commit -m "Release QuietQuote v1.3.0"
```

`.build/`、`dist/`、个人 `settings.json`、`markets.json` 和 EXE 已忽略。暂存列表应包含源码和文档，不应包含这些本地文件。

## 5. 创建公开仓库并上传源码

```powershell
gh repo create QuietQuote --public --source=. --remote=origin --push --description "A lightweight bilingual Windows ticker for OKX perpetual futures"
gh repo view --web
```

`--public` 表示所有人都可查看源码。如需先私下检查，可改用 `--private`，之后再自行改为公开。

若你的账号里已存在同名仓库，改用新名字；或对已有的**空仓库**使用：

```powershell
git remote add origin https://github.com/YOUR_GITHUB_NAME/QuietQuote.git
git push -u origin main
```

不要同时执行两种创建方式。已有远端提交时先检查差异，不要直接强制推送。

推送后，GitHub **Actions → Windows build** 会构建、测试和上传构建产物。可执行 `gh run list` 查看状态。这里的自动流程尚未在你的 GitHub 账号运行过；如果它失败，查看日志后再发布。

## 6. 发布可下载的 Release

在本地检查和 GitHub 构建成功后，创建版本标签并发布：

```powershell
git tag -a v1.3.0 -m "QuietQuote v1.3.0"
git push origin v1.3.0
gh release create v1.3.0 ".\dist\QuietQuote-v1.3.0-windows.zip" ".\dist\SHA256SUMS.txt" --verify-tag --title "QuietQuote v1.3.0" --notes-file ".\docs\releases\v1.3.0.md"
gh release view v1.3.0 --web
```

如果想先预览，在 `gh release create` 后追加 `--draft`。确认后执行 `gh release edit v1.3.0 --draft=false` 正式发布。

让普通用户下载 **Assets → QuietQuote-v1.3.0-windows.zip**，不是 GitHub 自动提供的 Source code ZIP。

## 7. 以后更新

修改 `version.txt`，例如 `1.3.1`；更新 `CHANGELOG.md` 并创建 `docs/releases/v1.3.1.md`。运行 `package.ps1`，提交并推送代码，再使用新的 `v1.3.1` 标签和对应 ZIP 发布。不要覆盖已发布版本的标签。

## 官方参考

- [将本地代码上传 GitHub](https://docs.github.com/en/migrations/importing-source-code/using-the-command-line-to-import-source-code/adding-locally-hosted-code-to-github)
- [gh repo create](https://cli.github.com/manual/gh_repo_create)
- [gh auth login](https://cli.github.com/manual/gh_auth_login)
- [gh release create](https://cli.github.com/manual/gh_release_create)
- [MIT 许可证](https://choosealicense.com/licenses/mit/)
