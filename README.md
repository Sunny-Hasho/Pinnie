# PinMe - Professional "Always On Top" Utility

**PinMe** is a robust, lightweight Windows utility designed for developers and power users. Unlike basic "always on top" scripts, PinMe offers **visual feedback**, **glitch-free handling**, and **smart window stacking**.

![PinMe Demo](https://via.placeholder.com/800x400?text=PinMe+Demo+Image) 
*(Replace this with a real screenshot of the white border effect)*

## 🚀 Key Features

*   **📌 Global Hotkey**: Press **`Ctrl` + `Win` + `T`** to toggle pinning on any active window.
*   **👁️ Visual Feedback**: A clean **White Border** appears around pinned windows so you never forget what's pinned.
*   **🏗️ Smart Stacking (Zipper Logic)**:
    *   **First Pinned Stays Top**: If you pin Window A, then Window B, Window A stays visually above B.
    *   **Kernel-Level Stability**: Uses native Window Ownership ("Zipper Chaining") to physically prevent flickering or z-order glitches.
*   **👻 Ghost-Free**: The border overlay automatically vanishes if the target window is minimized, closed, or becomes "cloaked" (backgrounded UWP apps like Calculator/ChatGPT).
*   **⚙️ System Tray**: 
    *   Right-click the tray icon to **Pin Specific Windows** from a list.
    *   Exit the application cleanly.

## 🛠️ Usage

1.  **Download** the latest release (or build from source).
2.  Run `PinWin.exe` (Administrator permissions recommended for pinning system apps like Task Manager).
3.  **Pin a Window**: Click a window and press `Ctrl` + `Win` + `T`.
    *   *Result*: Window stays on top + White Border appears.
4.  **Unpin a Window**: Press the hotkey again.
    *   *Result*: Window returns to normal + Border disappears.

## 📦 Build Instructions

Requirements: **.NET 8 SDK** or later.

```powershell
# Clone the repository
git clone https://github.com/your-username/PinMe.git

# Navigate to project
cd PinMe/PinWin

# Build
dotnet build -c Release
```

## 🔧 Technical Details

PinMe solves the classic "Flickering Border" problem using a unique **Zipper Chaining** architecture:
- Instead of using a timer to force windows to the top (which causes flickering), PinMe links the window and its border using `SetWindowLong(GWLP_HWNDPARENT)`.
- **Chain**: `Bottom Window` -> `Bottom Border` -> `Top Window` -> `Top Border`.
- This relies on the Windows Kernel to enforce the Z-Order, providing **zero-latency stability**.

## 📄 License

This project is licensed under the [MIT License](LICENSE) - free to use and modify.
