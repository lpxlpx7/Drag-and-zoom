# vatSys Persistent Pan Plugin

这个插件让 vatSys 的 ASD 使用鼠标中键拖动后保持拖动结束的位置，不再回到拖动前的中心点。

## 构建

项目目标为 vatSys 使用的 `.NET Framework 4.7.2` 和 `x86`：

```powershell
msbuild .\PersistentPan\PersistentPan.csproj /p:Configuration=Release /p:Platform=x86
```

如果系统没有 `msbuild`，可用 Visual Studio Developer PowerShell，或直接使用：

```powershell
dotnet msbuild .\PersistentPan\PersistentPan.csproj /p:Configuration=Release /p:Platform=x86
```

## 安装

vatSys SDK 要求插件放在**当前 Profile 目录**的 `Plugins` 子目录，而不是固定放在 `I:\vatSys\bin`。例如：

```text
<Profile目录>\Plugins\vatSys.PersistentPan.dll
```

将 `PersistentPan\bin\Release\vatSys.PersistentPan.dll` 复制到该目录，重启 vatSys。加载 Profile 后，在 ASD 上按住中键拖动并释放，视图会停留在释放位置。

插件使用 vatSys SDK 的公开 `IPlugin` 接口，并通过反射连接公开的 ASD 控件，以适配当前安装版的内部窗口结构。
