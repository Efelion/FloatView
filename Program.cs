// FloatView — Multi-Window Browser Source Manager
// -------------------------------------------------------------------------------------------------
// Purpose: A Windows application that manages multiple transparent browser source windows
// for streaming overlays. Each browser source gets its own resizable, movable window.
//
// Features
// - Configuration window to manage browser sources
// - Individual transparent windows for each browser source
// - Lock/unlock mode: movable when config open, click-through when config minimized
// - Aspect ratio preservation and content scaling
// - Save/restore window positions and URLs
// - Perfect for Discord calls with multiple participants
//
// Runtime Hotkeys
//   Home → Toggle configuration window (customizable in settings)
//
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

// Configuration data structure
public sealed class AppConfig
{
    public List<BrowserSourceConfig> Sources { get; set; } = new();
    public Rectangle ConfigWindowBounds { get; set; } = new(100, 100, 480, 480);
    public KeybindSettings Keybinds { get; set; } = new();
}

public sealed class KeybindSettings
{
    public string ToggleConfig { get; set; } = "Home";
}

public sealed class BrowserSourceConfig
{
    public string Url { get; set; } = "";
    public string Name { get; set; } = "";
    public Rectangle WindowBounds { get; set; } = new(200, 200, 420, 420);
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : Url;
}

// Configuration window
public class ConfigWindow : Form
{
    private readonly SourceManager _sourceManager;
    private TextBox _urlTextBox = null!;
    private Button _addButton = null!;
    private Button _saveButton = null!;
    private ListBox _sourcesList = null!;
    private Button _deleteButton = null!;
    
    // Keybind controls
    private TextBox _toggleConfigTextBox = null!;

    public ConfigWindow(SourceManager sourceManager)
    {
        _sourceManager = sourceManager;
        InitializeComponent();
        LoadSources();
        LoadKeybinds();
    }

    private void InitializeComponent()
    {
        Text = "FloatView - Configuration";
        Size = new Size(480, 480);
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = true;
        // Icon will be loaded from floatview_logo.ico automatically via project settings
        try
        {
            Icon = new Icon("floatview_logo.ico");
        }
        catch
        {
            Icon = SystemIcons.Application; // Fallback to system icon
        }

        // URL input
        var urlLabel = new Label
        {
            Text = "Browser Source URL:",
            Location = new Point(10, 15),
            Size = new Size(120, 20)
        };

        _urlTextBox = new TextBox
        {
            Location = new Point(10, 40),
            Size = new Size(320, 25),
            PlaceholderText = "https://example.com"
        };

        _addButton = new Button
        {
            Text = "Add Source",
            Location = new Point(340, 38),
            Size = new Size(80, 29)
        };
        _addButton.Click += AddButton_Click;

        // Sources list
        var sourcesLabel = new Label
        {
            Text = "Browser Sources:",
            Location = new Point(10, 80),
            Size = new Size(120, 20)
        };

        _sourcesList = new ListBox
        {
            Location = new Point(10, 105),
            Size = new Size(320, 150),
            DisplayMember = "DisplayName",
            AllowDrop = true
        };
        
        // Add context menu
        var contextMenu = new ContextMenuStrip();
        var renameItem = new ToolStripMenuItem("Rename");
        var deleteItem = new ToolStripMenuItem("Delete");
        
        renameItem.Click += RenameItem_Click;
        deleteItem.Click += DeleteItem_Click;
        
        contextMenu.Items.AddRange(new ToolStripItem[] { renameItem, deleteItem });
        _sourcesList.ContextMenuStrip = contextMenu;
        
        // Add drag-drop functionality
        _sourcesList.MouseDown += SourcesList_MouseDown;
        _sourcesList.DragOver += SourcesList_DragOver;
        _sourcesList.DragDrop += SourcesList_DragDrop;

        _deleteButton = new Button
        {
            Text = "Delete",
            Location = new Point(340, 105),
            Size = new Size(80, 29),
            Enabled = false
        };
        _deleteButton.Click += DeleteButton_Click;

        _sourcesList.SelectedIndexChanged += (s, e) => 
            _deleteButton.Enabled = _sourcesList.SelectedItem != null;

        // Save button
        _saveButton = new Button
        {
            Text = "Save All",
            Location = new Point(340, 220),
            Size = new Size(80, 29)
        };
        _saveButton.Click += SaveButton_Click;

        // Enter key handling
        _urlTextBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                AddButton_Click(null, e);
                e.Handled = true;
            }
        };

        // Keybind settings
        var keybindLabel = new Label
        {
            Text = "Hotkey Settings:",
            Location = new Point(10, 260),
            Size = new Size(120, 20),
            Font = new Font(Font, FontStyle.Bold)
        };

        var toggleConfigLabel = new Label
        {
            Text = "Toggle Config Window:",
            Location = new Point(10, 285),
            Size = new Size(140, 20)
        };

        _toggleConfigTextBox = new TextBox
        {
            Location = new Point(155, 283),
            Size = new Size(100, 20),
            ReadOnly = true
        };

        var keybindHelpLabel = new Label
        {
            Text = "Click in the textbox and press your desired key combination",
            Location = new Point(10, 320),
            Size = new Size(450, 40),
            ForeColor = Color.Gray,
            Font = new Font(Font.FontFamily, Font.Size - 1)
        };

        // Add key capture event
        _toggleConfigTextBox.KeyDown += (s, e) => CaptureKeybind(_toggleConfigTextBox, e);

        Controls.AddRange(new Control[] {
            urlLabel, _urlTextBox, _addButton,
            sourcesLabel, _sourcesList, _deleteButton, _saveButton,
            keybindLabel, toggleConfigLabel, _toggleConfigTextBox, keybindHelpLabel
        });
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        var url = _urlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(url)) return;

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;

        var config = new BrowserSourceConfig { Url = url };
        _sourceManager.AddSource(config);
        LoadSources();
        _urlTextBox.Clear();
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (_sourcesList.SelectedItem is BrowserSourceConfig config)
        {
            _sourceManager.RemoveSource(config.Id);
            LoadSources();
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        _sourceManager.SaveConfig();
        MessageBox.Show("Configuration saved!", "FloatView", 
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LoadSources()
    {
        var selectedIndex = _sourcesList.SelectedIndex;
        _sourcesList.DataSource = null;
        _sourcesList.DataSource = _sourceManager.GetSources().ToList();
        _sourcesList.DisplayMember = "DisplayName"; // Ensure display member is set
        _deleteButton.Enabled = false;
        
        // Restore selection if possible
        if (selectedIndex >= 0 && selectedIndex < _sourcesList.Items.Count)
        {
            _sourcesList.SelectedIndex = selectedIndex;
        }
    }
    
    private void RenameItem_Click(object? sender, EventArgs e)
    {
        if (_sourcesList.SelectedItem is BrowserSourceConfig config)
        {
            var currentName = config.DisplayName;
            var newName = ShowInputDialog("Enter new name for this browser source:", "Rename Browser Source", currentName);
                
            if (!string.IsNullOrEmpty(newName) && newName != currentName)
            {
                config.Name = newName;
                LoadSources(); // Refresh the display
                _sourceManager.SaveConfig(); // Auto-save
            }
        }
    }
    
    private string ShowInputDialog(string prompt, string title, string defaultValue = "")
    {
        var form = new Form
        {
            Text = title,
            Size = new Size(400, 150),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };
        
        var label = new Label
        {
            Text = prompt,
            Location = new Point(10, 15),
            Size = new Size(360, 20)
        };
        
        var textBox = new TextBox
        {
            Text = defaultValue,
            Location = new Point(10, 40),
            Size = new Size(360, 20)
        };
        
        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(215, 70),
            Size = new Size(75, 25)
        };
        
        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(295, 70),
            Size = new Size(75, 25)
        };
        
        form.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
        form.AcceptButton = okButton;
        form.CancelButton = cancelButton;
        
        textBox.SelectAll();
        textBox.Focus();
        
        return form.ShowDialog(this) == DialogResult.OK ? textBox.Text : "";
    }
    
    private void CaptureKeybind(TextBox textBox, KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;
        
        var keyString = "";
        
        // Build modifier string
        if (e.Control) keyString += "Ctrl+";
        if (e.Alt) keyString += "Alt+";
        if (e.Shift) keyString += "Shift+";
        
        // Add the main key
        keyString += e.KeyCode.ToString();
        
        textBox.Text = keyString;
        
        // Save the keybind immediately
        SaveKeybinds();
    }
    
    private void LoadKeybinds()
    {
        var config = _sourceManager.GetConfig();
        _toggleConfigTextBox.Text = config.Keybinds.ToggleConfig;
    }
    
    private void SaveKeybinds()
    {
        var config = _sourceManager.GetConfig();
        config.Keybinds.ToggleConfig = _toggleConfigTextBox.Text;
        _sourceManager.SaveConfig();
        _sourceManager.UpdateHotkeys(); // Update the actual hotkeys
    }
    
    private void DeleteItem_Click(object? sender, EventArgs e)
    {
        // Use the same logic as the delete button
        DeleteButton_Click(sender, e);
    }
    
    // Drag-drop functionality
    private int _dragIndex = -1;
    
    private void SourcesList_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragIndex = _sourcesList.IndexFromPoint(e.Location);
            if (_dragIndex >= 0)
            {
                _sourcesList.DoDragDrop(_sourcesList.Items[_dragIndex], DragDropEffects.Move);
            }
        }
    }
    
    private void SourcesList_DragOver(object? sender, DragEventArgs e)
    {
        e.Effect = DragDropEffects.Move;
    }
    
    private void SourcesList_DragDrop(object? sender, DragEventArgs e)
    {
        var point = _sourcesList.PointToClient(new Point(e.X, e.Y));
        int targetIndex = _sourcesList.IndexFromPoint(point);
        
        if (targetIndex >= 0 && _dragIndex >= 0 && targetIndex != _dragIndex)
        {
            var sources = _sourceManager.GetSources().ToList();
            var draggedItem = sources[_dragIndex];
            
            sources.RemoveAt(_dragIndex);
            sources.Insert(targetIndex, draggedItem);
            
            _sourceManager.ReorderSources(sources);
            LoadSources(); // Refresh the display
            _sourceManager.SaveConfig(); // Auto-save
        }
        _dragIndex = -1;
    }

    protected override void SetVisibleCore(bool value)
    {
        System.Diagnostics.Debug.WriteLine($"ConfigWindow.SetVisibleCore called with value: {value}");
        base.SetVisibleCore(value);
        if (value)
        {
            LoadSources(); // Refresh the list when showing
        }
        // Note: Window movability is now controlled by the hotkey handler, not here
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            // Exit the entire application when config window is closed
            Application.Exit();
        }
        base.OnFormClosing(e);
    }
}

// Individual browser source window
public class BrowserSourceWindow : Form
{
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    
    // SetWindowPos flags
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;

    private WebView2 _webView = null!;
    private readonly BrowserSourceConfig _config;
    private bool _isMovable = false;
    private bool _applyingClickThrough = false;

    public string SourceId => _config.Id;
    public BrowserSourceConfig Config => _config;
    public bool IsMovable => _isMovable;

    public BrowserSourceWindow(BrowserSourceConfig config)
    {
        _config = config;
        InitializeComponent();
        
        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };
        
        Controls.Add(_webView);
        InitializeWebView();
    }

    private void InitializeComponent()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        TransparencyKey = Color.Black;
        
        // Restore bounds
        Bounds = _config.WindowBounds;
        
        // Track bounds changes
        LocationChanged += (s, e) => _config.WindowBounds = Bounds;
        SizeChanged += (s, e) => _config.WindowBounds = Bounds;
    }

    private async void InitializeWebView()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userDataFolder = Path.Combine(localAppData, "FloatView", "WebView2");
            Directory.CreateDirectory(userDataFolder);
            
            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await _webView.EnsureCoreWebView2Async(env);
            
            if (_webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                _webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                _webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                _webView.DefaultBackgroundColor = Color.Transparent;
            }
            
            _webView.Source = new Uri(_config.Url);
            
            // Don't override movability here - let the SourceManager control it
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize browser source:\n{ex.Message}", 
                "FloatView", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void SetMovable(bool movable)
    {
        _isMovable = movable;
        
        // Ensure handle is created before applying changes
        if (!IsHandleCreated)
        {
            CreateHandle();
        }
        
        if (movable)
        {
            // Transform into a normal, movable window
            FormBorderStyle = FormBorderStyle.Sizable;
            ShowInTaskbar = true;
            Text = $"Browser Source - {_config.Url}";
            TopMost = false; // Allow normal window behavior
            
            // Remove transparency for normal window behavior
            TransparencyKey = Color.Empty;
            BackColor = SystemColors.Control;
            
            // Ensure no click-through
            RemoveClickThroughCompletely();
        }
        else
        {
            // Transform into transparent overlay
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            Text = "";
            TopMost = true; // Always on top for overlay
            
            // Restore transparency for overlay mode
            BackColor = Color.Black;
            TransparencyKey = Color.Black;
            
            // Enable click-through for overlay
            ApplyClickThrough(true);
        }
        
        // Force complete window refresh
        RecreateHandle();
    }

    private void RemoveClickThroughCompletely()
    {
        if (!IsHandleCreated) return;
        
        try
        {
            // Get current style and remove ALL transparency-related flags
            int currentStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            int newStyle = currentStyle & ~(WS_EX_LAYERED | WS_EX_TRANSPARENT);
            
            if (newStyle != currentStyle)
            {
                SetWindowLong(Handle, GWL_EXSTYLE, newStyle);
                SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, 
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Remove click-through error: {ex.Message}");
        }
    }

    private void ApplyClickThrough(bool enable)
    {
        // Prevent recursive calls
        if (_applyingClickThrough) return;
        _applyingClickThrough = true;
        
        try
        {
            if (!IsHandleCreated) 
            {
                CreateHandle();
            }
            
            int currentStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            int newStyle;
            
            if (enable) 
            {
                // Enable click-through: add transparent flag
                newStyle = currentStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT;
            }
            else 
            {
                // Disable click-through: remove transparent flag but keep layered
                newStyle = currentStyle | WS_EX_LAYERED;
                newStyle = newStyle & ~WS_EX_TRANSPARENT;
            }
            
            // Only apply if there's actually a change
            if (newStyle != currentStyle)
            {
                SetWindowLong(Handle, GWL_EXSTYLE, newStyle);
                
                // Force window to refresh
                SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, 
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
            }
        }
        catch (Exception ex)
        {
            // Silent error handling to avoid spam
            System.Diagnostics.Debug.WriteLine($"Click-through error: {ex.Message}");
        }
        finally
        {
            _applyingClickThrough = false;
        }
    }

    public void ReloadSource()
    {
        _webView.Reload();
    }
}

// Main source manager with hotkey handling
public class SourceManager : Form
{
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_NONE = 0x0000;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const int HOTKEY_ID_TOGGLE = 1;

    private readonly List<BrowserSourceWindow> _sourceWindows = new();
    private ConfigWindow _configWindow;
    private AppConfig _config = new();
    private readonly string _configPath;

    public SourceManager()
    {
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "overlay.config.json");
        LoadConfig();
        
        // Hidden form for hotkey handling
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
        Visible = false;
        
        _configWindow = new ConfigWindow(this);
        _configWindow.Bounds = _config.ConfigWindowBounds;
        _configWindow.LocationChanged += (s, e) => _config.ConfigWindowBounds = _configWindow.Bounds;
        _configWindow.SizeChanged += (s, e) => _config.ConfigWindowBounds = _configWindow.Bounds;
        
        // Load existing sources
        foreach (var sourceConfig in _config.Sources)
        {
            CreateSourceWindow(sourceConfig);
        }
    }

    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(false); // Keep this form hidden
        if (!IsHandleCreated)
        {
            CreateHandle();
            RegisterHotKeys();
        }
    }

    private void RegisterHotKeys()
    {
        // Register global hotkey for toggle config
        RegisterHotkeyFromString(_config.Keybinds.ToggleConfig, HOTKEY_ID_TOGGLE);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            HandleHotKey(m.WParam.ToInt32());
            return;
        }
        base.WndProc(ref m);
    }

    public void ShowConfigWindow()
    {
        _configWindow.Show();
        _configWindow.BringToFront();
    }

    public void AddSource(BrowserSourceConfig config)
    {
        _config.Sources.Add(config);
        CreateSourceWindow(config);
    }

    public void RemoveSource(string sourceId)
    {
        var window = _sourceWindows.FirstOrDefault(w => w.SourceId == sourceId);
        if (window != null)
        {
            _sourceWindows.Remove(window);
            window.Close();
            window.Dispose();
        }
        
        _config.Sources.RemoveAll(s => s.Id == sourceId);
    }

    public IEnumerable<BrowserSourceConfig> GetSources()
    {
        return _config.Sources;
    }
    
    public void ReorderSources(List<BrowserSourceConfig> newOrder)
    {
        _config.Sources.Clear();
        _config.Sources.AddRange(newOrder);
    }
    
    public AppConfig GetConfig()
    {
        return _config;
    }
    
    public void UpdateHotkeys()
    {
        // Unregister old hotkey
        UnregisterHotKey(Handle, HOTKEY_ID_TOGGLE);
        
        // Register new hotkey based on config
        RegisterHotkeyFromString(_config.Keybinds.ToggleConfig, HOTKEY_ID_TOGGLE);
    }
    
    private void RegisterHotkeyFromString(string keyString, int hotkeyId)
    {
        var parts = keyString.Split('+');
        uint modifiers = 0;
        Keys key = Keys.None;
        
        foreach (var part in parts)
        {
            switch (part.Trim())
            {
                case "Ctrl":
                    modifiers |= MOD_CONTROL;
                    break;
                case "Alt":
                    modifiers |= MOD_ALT;
                    break;
                case "Shift":
                    modifiers |= MOD_SHIFT;
                    break;
                default:
                    if (Enum.TryParse<Keys>(part.Trim(), out var parsedKey))
                    {
                        key = parsedKey;
                    }
                    break;
            }
        }
        
        if (key != Keys.None)
        {
            RegisterHotKey(Handle, hotkeyId, modifiers, (uint)key);
        }
    }

    public void SetWindowsMovable(bool movable)
    {
        foreach (var window in _sourceWindows)
        {
            window.SetMovable(movable);
        }
    }

    private void CreateSourceWindow(BrowserSourceConfig config)
    {
        var window = new BrowserSourceWindow(config);
        _sourceWindows.Add(window);
        
        // Set initial movability based on config window visibility
        window.SetMovable(_configWindow.Visible);
        
        window.Show();
    }

    public void SaveConfig()
    {
        try
        {
            // Update window bounds from current windows
            foreach (var window in _sourceWindows)
            {
                var config = _config.Sources.FirstOrDefault(s => s.Id == window.SourceId);
                if (config != null)
                {
                    config.WindowBounds = window.Bounds;
                }
            }
            
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save configuration:\n{ex.Message}", 
                "Reactive Overlay Host", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    _config = config;
                }
            }
        }
        catch
        {
            // Use default config if loading fails
        }
    }

    private void HandleHotKey(int hotkeyId)
    {
        switch (hotkeyId)
        {
            case HOTKEY_ID_TOGGLE:
                try
                {
                    if (_configWindow.Visible)
                    {
                        System.Diagnostics.Debug.WriteLine("TOGGLE: Hiding config window and switching to overlay mode");
                        _configWindow.Hide();
                        // Switch browser source windows to overlay mode (non-movable, click-through)
                        SetWindowsMovable(false);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("TOGGLE: Showing config window and switching to edit mode");
                        _configWindow.Show();
                        _configWindow.BringToFront();
                        _configWindow.Activate();
                        // Switch browser source windows to edit mode (movable, not click-through)
                        SetWindowsMovable(true);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Config window toggle error: {ex.Message}");
                    // Try to recreate the config window if it's been disposed
                    if (_configWindow.IsDisposed)
                    {
                        System.Diagnostics.Debug.WriteLine("Config window was disposed, recreating...");
                        // This shouldn't happen, but let's handle it gracefully
                        _configWindow = new ConfigWindow(this);
                        _configWindow.Show();
                        SetWindowsMovable(true);
                    }
                }
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnregisterHotKey(Handle, HOTKEY_ID_TOGGLE);
            
            foreach (var window in _sourceWindows)
            {
                window.Dispose();
            }
            _configWindow?.Dispose();
        }
        base.Dispose(disposing);
    }
}

// Program entry point
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        
        var sourceManager = new SourceManager();
        
        // Show config window initially
        sourceManager.ShowConfigWindow();
        
        Application.Run(sourceManager);
    }    
}
