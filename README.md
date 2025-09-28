# FloatView

<div align="center">
  <img src="floatview_logo.png" alt="FloatView Logo" width="128" height="128">
</div>

A lightweight desktop overlay application for displaying browser sources on top of other windows, similar to OBS browser sources but for your desktop.

## Features

- 🌐 **Browser Source Overlays**: Display any web content as transparent overlays
- 🎯 **Click-Through Mode**: Overlays become transparent to mouse clicks when locked
- ✏️ **Edit Mode**: Toggle between locked overlay mode and movable/resizable edit mode
- 📝 **Source Management**: Add, rename, delete, and reorder browser sources
- 💾 **Persistent Configuration**: Automatically saves window positions and source URLs
- 🎮 **Global Hotkeys**: Control overlays from any application

## Browser Source Compatibility

FloatView supports any web-based content that works in modern browsers, including:

- 📺 **Streaming overlays** (alerts, chat widgets, donation goals)
- 🎮 **OBS-compatible browser sources** 
- 💬 **Discord talking heads** (like [Reactive by Fuji](https://reactive.fugi.tech))
- 📊 **Dashboard widgets** and monitoring tools
- 🌐 **Any web application** or interactive content

*If it works in OBS Studio's browser source, it will work in FloatView!*

## Quick Start

1. **Download** the latest release from the [Releases](../../releases) page
2. **Extract** the ZIP file to a folder of your choice
3. **Run** `FloatView.exe`
4. **Press Home** to open the configuration window
5. **Add a browser source** by entering a URL and clicking "Add"
6. **Press Home again** to lock the overlay in place

## Controls

| Key | Action |
|-----|--------|
| **Home** | Toggle between edit mode (config window) and overlay mode |

*Note: The hotkey can be customized in the configuration window.*

## Usage

### Adding Browser Sources
1. Press **Home** to open the configuration window
2. Enter a URL in the text field (e.g., `https://example.com`)
3. Click **Add** or press **Enter**
4. The browser source window will appear

### Managing Sources
- **Right-click** on any source in the list to rename or delete it
- **Drag and drop** sources in the list to reorder them
- Use the **Delete** button to remove selected sources
- Click **Save All** to manually save configuration

### Edit vs Overlay Mode

**Edit Mode (Config Window Open):**
- Browser source windows have borders and title bars
- Windows can be moved by dragging the title bar
- Windows can be resized using corner/edge handles
- Sources are **not** click-through

**Overlay Mode (Config Window Hidden):**
- Browser source windows are borderless and transparent
- Windows are locked in position
- Sources are click-through (mouse clicks pass through to underlying windows)
- Perfect for overlays during streaming, gaming, or productivity

## System Requirements

- Windows 10 or later
- Microsoft Edge WebView2 Runtime (usually pre-installed on modern Windows)

## Configuration

Configuration is automatically saved to `overlay.config.json` in the application directory. This includes:
- Browser source URLs and custom names
- Window positions and sizes
- Configuration window bounds

## Troubleshooting

**Browser sources not loading:**
- Ensure you have an active internet connection
- Some websites may block embedding in WebView2
- Try using `https://` URLs instead of `http://`

**Overlays not click-through:**
- Press **F8** to toggle click-through mode
- Ensure you're in overlay mode (config window closed)

**Can't move/resize windows:**
- Press **Home** to enter edit mode
- Windows are only movable when the config window is open

## Building from Source

Requirements:
- .NET 7.0 SDK or later
- Windows development environment

```bash
git clone https://github.com/Efelion/FloatView.git
cd FloatView
dotnet build --framework net7.0-windows
```

For release build:
```bash
dotnet publish --framework net7.0-windows --configuration Release --self-contained true --runtime win-x64 --output ./publish
```

## License

This project is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.

This means:
- ✅ You can use, modify, and distribute this software freely
- ✅ Commercial use is allowed
- ⚠️ **Any derivative works must also be open source** and use a compatible license
- ⚠️ **Companies cannot take this code and make it proprietary**

This ensures FloatView and any improvements remain free and open source forever.

See the [LICENSE](LICENSE) file for full details.

## Author

**Created by:** Efelion

- 🐙 **GitHub:** [@Efelion](https://github.com/Efelion)
- 🎥 **YouTube:** [@Calabi-YauProject](https://www.youtube.com/@Calabi-YauProject)
- 📝 **Substack:** [@calabiyauproject](https://substack.com/@calabiyauproject)
- 🐦 **X (Twitter):** [@CalabiYauProj](https://x.com/CalabiYauProj)
- 🦋 **Bluesky:** [@calabiyauproject.bsky.social](https://bsky.app/profile/calabiyauproject.bsky.social)
- 🦋 **Bluesky (Alt):** [@xefelionx.bsky.social](https://bsky.app/profile/xefelionx.bsky.social)

## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.
