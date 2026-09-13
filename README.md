# P-Helper

A lightweight, automated companion utility for G-Helper. P-Helper seamlessly switches your performance profile based on the active application.

<div align="center">
  <img width="600" alt="main" src="https://github.com/user-attachments/assets/cc06fdfb-e82f-4e87-963e-5d48ca401682" />
</div>

## Features

* **App-Specific Profiles:** Assign distinct G-Helper performance profiles (Silent, Balanced, Turbo) to individual games or programs.
* **Default Profile:** Set a fallback system profile that automatically activates when no monitored applications are running.
* **Add App:** Quickly add programs by picking from a list of currently running processes, or browse your disk for a specific `.exe` file.

<div align="center">
  <img width="450" alt="add_app" src="https://github.com/user-attachments/assets/90d25ca2-7b54-40f9-87b5-dd3290d183d2" />
</div>

* **Batch Folder Scan:** Effortlessly scan entire directories to find and add multiple executables at once. You can also define and save your custom default folders for faster future scans.

<div align="center">
  <img width="500" alt="scan_folders" src="https://github.com/user-attachments/assets/aad5f3ed-5b36-4bd6-ac31-9028e47e35e9" />
</div>

* **System Tray Integration:** Control the application rapidly straight from your taskbar's system tray context menu.

<div align="center">
  <img width="200" alt="tray" src="https://github.com/user-attachments/assets/00927bf6-64e9-420f-91c4-ded3e2ce1eb0" />
</div>

* **Pause Monitoring:** Temporarily suspend automatic profile switching for a specific duration or indefinitely when you need manual control.
* **Run on Startup:** Launch the app automatically and silently in the background when Windows starts.

## Prerequisites

* **[G-Helper](https://github.com/seerge/g-helper):** Must be installed and running on your system.
* **[.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download):** Required to run the program (will be auto-prompted to install if missing). *Note: Since G-Helper also relies on .NET 10 Desktop Runtime, you may already have it installed.*

## Installation & Usage

1. Download the latest version from the [Releases](https://github.com/converdario/p-helper/releases) page.
2. Run `PHelper.exe`.
3. Add your favorite games or demanding applications, select your preferred profiles, and let the app handle the background switching automatically.

## Acknowledgments

Maintained by [converdario](https://github.com/converdario).  
Based on the [original project](https://github.com/LunarstarFurry/GHelper-Auto-Profile-Switcher) by [LunarstarFurry](https://github.com/LunarstarFurry).

## License

This project is licensed under the MIT License - see the `LICENSE` file for details.