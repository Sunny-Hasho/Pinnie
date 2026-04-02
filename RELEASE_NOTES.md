🐹 Pinnie v1.0.3 - Silent & Smooth

📦 **Downloads**
1. **Pinnie Portable (Recommended)**: `Pinnie-v1.0.3-Portable.zip`
2. **Pinnie Lightweight**: `Pinnie-v1.0.3-Lightweight.zip` (Requires .NET 8)

✨ **What's New in v1.0.3**

🔕 **Silent Auto-Startup (UX Fix)**
- The "Pinnie added Successfully!" welcome message now shows **only once** — on the very first launch
- All subsequent startups, including auto-starts at boot, are now completely **silent and non-intrusive**
- No more popup appearing every time your PC restarts


✨ **What's New in v1.0.2**

🚀 **Auto-Startup Overhaul (Elevated + No UAC)**
- Pinnie registers itself via the **Windows Task Scheduler COM API** for reliable elevated auto-startup
- Runs as **Administrator at boot with no UAC prompt** — enabling Task Manager & elevated window pinning from the very first launch
- Replaced the previous PowerShell-based approach which was incorrectly flagged as a security threat by antivirus software

🔒 **No More Antivirus False Positives**
- Removed all `powershell.exe` / `schtasks.exe` process spawning
- Startup registration is now done entirely in-process via the Windows COM API

🐛 **Bug Fixes**
- Fixed: App appearing in Windows Startup apps list but silently failing to launch after reboot
- Fixed: App not running as Administrator when launched via startup, preventing pinning of elevated windows

🔧 **v1.0.1 Features (unchanged)**
- **Administrator Enforcement**: Mandatory elevation for Task Manager support
- **Multi-Monitor Fix**: "Quiet Sync" positioning logic for secondary monitors
- **1080p Density**: Improved overlay fit on standard resolution screens
- **Sound Toggle**: Persistent mute/unmute settings

🤝 **Support**
If you like Pinnie, please star the repository! ⭐
