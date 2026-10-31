using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ChatGptMcpAgent;

internal sealed partial class ChatGptMainForm : Form
{
	private sealed class BrowserTab
	{
		internal string Id = Guid.NewGuid().ToString("N");

		internal readonly WebView2 View = new WebView2
		{
			Dock = DockStyle.Fill,
			DefaultBackgroundColor = Color.FromArgb(37, 37, 35)
		};

		internal readonly WarmChromePanel Chip = new WarmChromePanel
		{
			Width = 211,
			Height = 32,
			Radius = 12,
			Margin = new Padding(3, 0, 0, 0)
		};

		internal readonly Label Title = new Label
		{
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			Padding = new Padding(10, 0, 0, 0),
			AutoEllipsis = true
		};

		internal readonly WarmChromeButton CloseButton = new WarmChromeButton
		{
			Text = "×",
			Width = 29,
			Dock = DockStyle.Right,
			Margin = Padding.Empty,
			Radius = 9
		};

		internal string Address = "https://chatgpt.com/";

		internal string? ToolbarInjection;

		internal string? ContextInjection;

		internal bool Loaded;

		internal bool NavigationOk;
	}

	private const string Home = "https://chatgpt.com/";

	private const int CaptionHeight = 32;

	private readonly Color _warmBackground = Color.FromArgb(37, 37, 35);

	private readonly List<BrowserTab> _pages = new List<BrowserTab>();

	private readonly List<Form> _popups = new List<Form>();

	private readonly TableLayoutPanel _layout = new TableLayoutPanel
	{
		Dock = DockStyle.Fill,
		ColumnCount = 1,
		RowCount = 2,
		Margin = Padding.Empty,
		Padding = Padding.Empty
	};

	private readonly WebView2 _chrome = new WebView2
	{
		Dock = DockStyle.Fill,
		Margin = Padding.Empty,
		DefaultBackgroundColor = Color.FromArgb(37, 37, 35)
	};

	private bool _chromeReady;

	private bool _chromeNavigated;

	private readonly Panel _caption = new Panel
	{
		Dock = DockStyle.Fill
	};

	private readonly Label _captionName = new Label
	{
		Dock = DockStyle.Fill,
		TextAlign = ContentAlignment.MiddleLeft,
		Padding = new Padding(12, 0, 0, 0),
		AutoEllipsis = true
	};

	private readonly WarmChromeButton _minimize = new WarmChromeButton
	{
		Text = "—",
		Dock = DockStyle.Right,
		Width = 43,
		Radius = 10
	};

	private readonly WarmChromeButton _maximize = new WarmChromeButton
	{
		Text = "□",
		Dock = DockStyle.Right,
		Width = 43,
		Radius = 10
	};

	private readonly WarmChromeButton _close = new WarmChromeButton
	{
		Text = "×",
		Dock = DockStyle.Right,
		Width = 43,
		Radius = 10
	};

	private readonly Panel _tabsPanel = new Panel
	{
		Dock = DockStyle.Fill,
		Padding = Padding.Empty
	};

	private readonly FlowLayoutPanel _tabs = new FlowLayoutPanel
	{
		Dock = DockStyle.Fill,
		FlowDirection = FlowDirection.LeftToRight,
		WrapContents = false,
		AutoScroll = true,
		Margin = Padding.Empty,
		Padding = new Padding(8, 3, 0, 0)
	};

	private readonly WarmChromeButton _newTab = new WarmChromeButton
	{
		Text = "+",
		Width = 32,
		Height = 30,
		Radius = 12,
		Margin = new Padding(5, 1, 0, 0)
	};

	private readonly TableLayoutPanel _navigation = new TableLayoutPanel
	{
		Dock = DockStyle.Fill,
		ColumnCount = 5,
		RowCount = 1,
		Margin = Padding.Empty,
		Padding = new Padding(6, 2, 6, 2)
	};

	private readonly WarmChromeButton _back = new WarmChromeButton
	{
		Text = "‹",
		Dock = DockStyle.Fill,
		Margin = new Padding(2),
		Radius = 12
	};

	private readonly WarmChromeButton _forward = new WarmChromeButton
	{
		Text = "›",
		Dock = DockStyle.Fill,
		Margin = new Padding(2),
		Radius = 12
	};

	private readonly WarmChromeButton _reload = new WarmChromeButton
	{
		Text = "⟳",
		Dock = DockStyle.Fill,
		Margin = new Padding(2),
		Radius = 12
	};

	private readonly WarmChromeButton _go = new WarmChromeButton
	{
		Text = "→",
		Dock = DockStyle.Fill,
		Margin = new Padding(2),
		Radius = 12
	};

	private readonly WarmChromePanel _addressShell = new WarmChromePanel
	{
		Dock = DockStyle.Fill,
		Radius = 14,
		ShowOutline = false,
		Padding = new Padding(13, 8, 9, 5),
		Margin = new Padding(6, 4, 6, 4)
	};

	private readonly TextBox _address = new TextBox
	{
		Dock = DockStyle.Fill,
		BorderStyle = BorderStyle.None,
		Margin = Padding.Empty,
		Font = new Font("Segoe UI", 9.5f)
	};

	private readonly Panel _content = new Panel
	{
		Dock = DockStyle.Fill,
		Margin = Padding.Empty,
		Padding = Padding.Empty
	};

	private readonly Label _error = new Label
	{
		Dock = DockStyle.Top,
		Visible = false,
		Height = 32,
		TextAlign = ContentAlignment.MiddleCenter
	};

	private readonly ToolTip _hint = new ToolTip
	{
		ShowAlways = true
	};

	private readonly System.Windows.Forms.Timer _themeReload = new System.Windows.Forms.Timer
	{
		Interval = 320
	};

	private readonly System.Windows.Forms.Timer _toolbarReload = new System.Windows.Forms.Timer
	{
		Interval = 350
	};

	private readonly System.Windows.Forms.Timer _contextReload = new System.Windows.Forms.Timer
	{
		Interval = 350
	};

	private FileSystemWatcher? _themeWatcher;

	private FileSystemWatcher? _toolbarWatcher;

	private FileSystemWatcher? _contextWatcher;

	private CoreWebView2Environment? _environment;
    private readonly TaskCompletionSource<bool> _browserExited =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Task BrowserProcessExitTask => _environment is null ?
        Task.CompletedTask : _browserExited.Task;
    internal VpnConnectionMode BrowserNetworkMode { get; } = VpnModeController.Current;


	private BrowserTab? _active;

	private bool _initializing;

	private bool _exitRequested;

	private bool _lastNavigationSucceeded;

	private static string WarmCssFile => Path.Combine(AppContext.BaseDirectory, "Themes", "ChatGPT-Warm-Dark.css");

	private static string ToolbarScriptFile => Path.Combine(AppContext.BaseDirectory, "Themes", "ChatGPT-Toolbar.js");

	private static string ContextScriptFile => Path.Combine(AppContext.BaseDirectory, "Themes", "ChatGPT-Context.js");

	internal static string ProfileFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatGPTMCPAgent", "WebView2", "ChatGPT");

	internal static bool ProfileValid
	{
		get
		{
			if (Path.IsPathFullyQualified(ProfileFolder))
			{
				return ProfileFolder.Contains("ChatGPTMCPAgent", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
	}

	internal bool BrowserInitialized => _active?.Loaded ?? false;

	internal bool LastNavigationSucceeded => _active?.NavigationOk ?? false;

	internal WebView2 BrowserControl => _active?.View ?? throw new InvalidOperationException("No active WebView2 tab");

	internal string? CurrentPage => _active?.View.CoreWebView2?.Source;

	internal int TabCount => _pages.Count;

	internal string? CurrentAddress => _active?.Address;

	internal bool HasCustomCaption => base.FormBorderStyle == FormBorderStyle.None;

	internal bool HasBrowserChrome
	{
		get
		{
			if (_layout.RowCount == 2 && _layout.Controls.Contains(_chrome))
			{
				return _layout.Controls.Contains(_content);
			}
			return false;
		}
	}

	internal bool ChromeInitialized
	{
		get
		{
			if (_chromeReady)
			{
				return _chromeNavigated;
			}
			return false;
		}
	}

	internal Color CaptionColor => _caption.BackColor;

	internal async Task<bool> SmokeChromeAsync()
	{
		if (!ChromeInitialized || _chrome.CoreWebView2 == null || _pages.Count != 1)
		{
			return false;
		}
		CoreWebView2 browser = _chrome.CoreWebView2;
		if (await browser.ExecuteScriptAsync("(() => {\n    const strip = document.querySelector('#tabs-scroll');\n    const plus = document.querySelector('#add-tab');\n    const tab = strip?.querySelector('.tab');\n    const body = getComputedStyle(document.body);\n    const address = document.querySelector('#address');\n    return Boolean(strip && plus && tab && address &&\n        strip.parentElement?.lastElementChild === plus &&\n        body.backgroundColor === 'rgb(37, 37, 35)' &&\n        document.querySelector('#navigation .address-pill') &&\n        document.querySelector('#reload[title*=\"Ctrl+F5\"]'));\n})()") != "true")
		{
			return false;
		}
		if (!(await SmokeAddressGeometryAsync(browser)))
		{
			return false;
		}
		if (!(await SmokeTabRenameAsync(browser)))
		{
			return false;
		}
		BrowserTab original = _active;
		await browser.ExecuteScriptAsync("document.querySelector('#add-tab')?.click()");
		for (int i = 0; i < 30; i++)
		{
			if (_pages.Count != 1)
			{
				break;
			}
			await Task.Delay(100);
		}
		if (_pages.Count != 2 || _active == original)
		{
			return false;
		}
		await browser.ExecuteScriptAsync("document.querySelector('#tabs-scroll .tab.selected .tab-close')?.click()");
		for (int i = 0; i < 30; i++)
		{
			if (_pages.Count != 2)
			{
				break;
			}
			await Task.Delay(100);
		}
		return _pages.Count == 1 && _active == original;
	}

	private async Task<bool> SmokeAddressGeometryAsync(CoreWebView2 browser)
	{
		Size previous = base.Size;
		try
		{
			int[] array = new int[3] { 820, 900, 1370 };
			foreach (int width in array)
			{
				base.Width = width;
				await Task.Delay(160);
				if (await browser.ExecuteScriptAsync("(() => {\n  const box=id=>document.getElementById(id).getBoundingClientRect();\n  const navigation=box('navigation'), pill=document.querySelector('.address-pill').getBoundingClientRect();\n  const input=box('address'), lock=box('address-icon'), go=box('go');\n  const reload=box('reload'), tabs=box('tabs-row'), plus=box('add-tab');\n  return [\n    navigation.height===44,\n    pill.left>=reload.right+3,\n    pill.right<=navigation.right-3,\n    pill.top>=navigation.top-1,\n    pill.bottom<=navigation.bottom,\n    lock.right<=input.left+1,\n    input.right<=go.left+2,\n    input.width>=80,\n    plus.right<=tabs.right+1,\n    plus.left>=tabs.left,\n    tabs.right<=innerWidth+1\n  ].every(Boolean);\n})()") != "true")
				{
					return false;
				}
			}
			return true;
		}
		finally
		{
			base.Size = previous;
		}
	}

	private static async Task<bool> SmokeTabRenameAsync(CoreWebView2 browser)
	{
		return await browser.ExecuteScriptAsync("(() => {\n  const label=document.querySelector('.tab-title');\n  if(!label)return false;\n  label.dispatchEvent(new MouseEvent('dblclick',{bubbles:true}));\n  let edit=document.querySelector('.tab-rename-input');\n  if(!edit)return false;\n  edit.value='Тестовое имя вкладки';\n  edit.dispatchEvent(new Event('input',{bubbles:true}));\n  edit.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',bubbles:true}));\n  const title=()=>document.querySelector('.tab-title')?.textContent;\n  if(title()!=='Тестовое имя вкладки')return false;\n  window.applyChromeState({address:'https://chatgpt.com/test-rename'});\n  if(title()!=='Тестовое имя вкладки')return false;\n  document.querySelector('.tab-title').dispatchEvent(\n    new MouseEvent('dblclick',{bubbles:true}));\n  edit=document.querySelector('.tab-rename-input');\n  if(!edit)return false;\n  edit.value='НЕ СОХРАНЯТЬ';\n  edit.dispatchEvent(new Event('input',{bubbles:true}));\n  edit.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}));\n  return title()==='Тестовое имя вкладки';\n})()") == "true";
	}

	internal async Task<bool> SmokeTabsAsync()
	{
		BrowserTab before = _active;
		CoreWebView2 coreWebView = before?.View.CoreWebView2;
		if (coreWebView == null || _pages.Count != 1)
		{
			return false;
		}
		uint firstBrowser = coreWebView.BrowserProcessId;
		BrowserTab browserTab = await CreateTabAsync("https://chatgpt.com/");
		if (browserTab?.View.CoreWebView2 == null)
		{
			return false;
		}
		bool num = firstBrowser == browserTab.View.CoreWebView2.BrowserProcessId;
		bool flag = _active == browserTab && _pages.Count == 2;
		ActivateTab(before);
		bool flag2 = _active == before;
		CloseTab(browserTab);
		return num && flag && flag2 && _pages.Count == 1 && _active == before;
	}

	internal ChatGptMainForm()
	{
		Text = "ChatGPT MCP Tunnel v" + Application.ProductVersion.Split('+')[0];
		base.Icon = TrayIcons.AppIcon();
		Font = new Font("Segoe UI", 9.5f);
		base.Size = new Size(1370, 900);
		MinimumSize = new Size(820, 560);
		base.StartPosition = FormStartPosition.CenterScreen;
		base.FormBorderStyle = FormBorderStyle.None;
		base.KeyPreview = true;
		DoubleBuffered = true;
		BackColor = _warmBackground;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		_layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 106f));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		_layout.Controls.Add(_chrome, 0, 0);
		_layout.Controls.Add(_content, 0, 1);
		base.Controls.Add(_layout);
		_captionName.Text = Text;
		WarmChromeButton[] array = new WarmChromeButton[3] { _minimize, _maximize, _close };
		foreach (WarmChromeButton obj in array)
		{
			obj.FlatStyle = FlatStyle.Flat;
			obj.FlatAppearance.BorderSize = 0;
			obj.Font = new Font("Segoe UI", 11f);
			obj.TabStop = false;
		}
		_caption.Controls.Add(_captionName);
		_caption.Controls.Add(_minimize);
		_caption.Controls.Add(_maximize);
		_caption.Controls.Add(_close);
		_captionName.MouseDown += DragTitle;
		_caption.MouseDown += DragTitle;
		_captionName.DoubleClick += delegate
		{
			ToggleMaximize();
		};
		_caption.DoubleClick += delegate
		{
			ToggleMaximize();
		};
		_minimize.Click += delegate
		{
			base.WindowState = FormWindowState.Minimized;
		};
		_maximize.Click += delegate
		{
			ToggleMaximize();
		};
		_close.Click += delegate
		{
			Close();
		};
		_tabsPanel.Controls.Add(_tabs);
		_tabs.Controls.Add(_newTab);
		_newTab.Click += async delegate
		{
			await OpenTabAsync("https://chatgpt.com/");
		};
		_hint.SetToolTip(_newTab, "Новая вкладка (Ctrl+T)");
		float[] array2 = new float[3] { 39f, 39f, 42f };
		foreach (float width in array2)
		{
			_navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, width));
		}
		_navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		_navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42f));
		_navigation.Controls.Add(_back, 0, 0);
		_navigation.Controls.Add(_forward, 1, 0);
		_navigation.Controls.Add(_reload, 2, 0);
		_addressShell.Controls.Add(_address);
		_navigation.Controls.Add(_addressShell, 3, 0);
		_navigation.Controls.Add(_go, 4, 0);
		_hint.SetToolTip(_back, "Назад (Alt+←)");
		_hint.SetToolTip(_forward, "Вперёд (Alt+→)");
		_hint.SetToolTip(_reload, "Принудительное обновление без кэша (Ctrl+F5)");
		_hint.SetToolTip(_go, "Перейти по адресу (Enter)");
		_back.Click += delegate
		{
			BrowserTab? active = _active;
			if (active != null && active.View.CoreWebView2?.CanGoBack == true)
			{
				_active.View.CoreWebView2.GoBack();
			}
		};
		_forward.Click += delegate
		{
			BrowserTab? active = _active;
			if (active != null && active.View.CoreWebView2?.CanGoForward == true)
			{
				_active.View.CoreWebView2.GoForward();
			}
		};
		_reload.Click += async delegate
		{
			await HardReloadAsync();
		};
		_go.Click += async delegate
		{
			await NavigateAddressAsync();
		};
		_address.KeyDown += async delegate(object? _, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return)
			{
				e.SuppressKeyPress = true;
				await NavigateAddressAsync();
			}
		};
		_address.Enter += delegate
		{
			_address.SelectAll();
		};
		_content.Controls.Add(_error);
		_error.BringToFront();
		_themeReload.Tick += async delegate
		{
			_themeReload.Stop();
			await ApplyAllPalettesAsync();
		};
		_toolbarReload.Tick += async delegate
		{
			_toolbarReload.Stop();
			await ReloadAllToolbarScriptsAsync();
		};
		_contextReload.Tick += async delegate
		{
			_contextReload.Stop();
			await ReloadAllContextScriptsAsync();
		};
		base.Shown += async delegate
		{
			ApplyBrowserAppearance();
			await InitializeAsync();
		};
		base.FormClosing += delegate(object? _, FormClosingEventArgs e)
		{
			if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
			{
				e.Cancel = true;
				Hide();
			}
		};
		base.FormClosed += delegate
		{
			_themeWatcher?.Dispose();
			_toolbarWatcher?.Dispose();
			_contextWatcher?.Dispose();
			_themeReload.Dispose();
			_toolbarReload.Dispose();
			_contextReload.Dispose();
			ShutdownVoice();
			_chrome.Dispose();
			BrowserTab[] array3 = _pages.ToArray();
			for (int j = 0; j < array3.Length; j++)
			{
				array3[j].View.Dispose();
			}
			_hint.Dispose();
		};
		base.KeyDown += async delegate(object? _, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.T)
			{
				e.SuppressKeyPress = true;
				await OpenTabAsync("https://chatgpt.com/");
			}
			else if (e.Control && e.KeyCode == Keys.W)
			{
				e.SuppressKeyPress = true;
				CloseActiveTab();
			}
			else if (e.Control && e.KeyCode == Keys.Tab)
			{
				e.SuppressKeyPress = true;
				CycleTab((!e.Shift) ? 1 : (-1));
			}
			else if (e.Control && e.KeyCode == Keys.L)
			{
				e.SuppressKeyPress = true;
				_address.Focus();
				_address.SelectAll();
			}
			else if (e.Control && e.KeyCode == Keys.F5)
			{
				e.SuppressKeyPress = true;
				await HardReloadAsync();
			}
			else if (e.Alt && e.KeyCode == Keys.Left)
			{
				e.SuppressKeyPress = true;
				_back.PerformClick();
			}
			else if (e.Alt && e.KeyCode == Keys.Right)
			{
				e.SuppressKeyPress = true;
				_forward.PerformClick();
			}
		};
		ApplyBrowserAppearance();
	}

	[DllImport("user32.dll")]
	private static extern bool ReleaseCapture();

	[DllImport("user32.dll")]
	private static extern nint SendMessage(nint hwnd, int msg, nint wparam, nint lparam);

	private void DragTitle(object? sender, MouseEventArgs e)
	{
		if (e.Button == MouseButtons.Left && base.WindowState != FormWindowState.Maximized)
		{
			ReleaseCapture();
			SendMessage(base.Handle, 161, new IntPtr(2), IntPtr.Zero);
		}
	}

	private void ToggleMaximize()
	{
		if (base.WindowState == FormWindowState.Maximized)
		{
			base.WindowState = FormWindowState.Normal;
		}
		else
		{
			base.MaximizedBounds = Screen.FromHandle(base.Handle).WorkingArea;
			base.WindowState = FormWindowState.Maximized;
		}
		_maximize.Text = ((base.WindowState == FormWindowState.Maximized) ? "❐" : "□");
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == 132 && base.WindowState == FormWindowState.Normal)
		{
			base.WndProc(ref m);
			long num = ((IntPtr)m.LParam).ToInt64();
			Point point = PointToClient(new Point((short)(num & 0xFFFF), (short)((num >> 16) & 0xFFFF)));
			bool flag = point.X < 7;
			bool flag2 = point.X >= base.Width - 7;
			bool flag3 = point.Y < 7;
			bool flag4 = point.Y >= base.Height - 7;
			if (flag3 && flag)
			{
				m.Result = new IntPtr(13);
			}
			else if (flag3 && flag2)
			{
				m.Result = new IntPtr(14);
			}
			else if (flag4 && flag)
			{
				m.Result = new IntPtr(16);
			}
			else if (flag4 && flag2)
			{
				m.Result = new IntPtr(17);
			}
			else if (flag)
			{
				m.Result = new IntPtr(10);
			}
			else if (flag2)
			{
				m.Result = new IntPtr(11);
			}
			else if (flag3)
			{
				m.Result = new IntPtr(12);
			}
			else if (flag4)
			{
				m.Result = new IntPtr(15);
			}
		}
		else
		{
			base.WndProc(ref m);
		}
	}

	internal void ShowChat()
	{
		if (!base.IsDisposed && !base.Disposing)
		{
			if (base.WindowState == FormWindowState.Minimized)
			{
				base.WindowState = FormWindowState.Normal;
			}
			if (!base.Visible)
			{
				Show();
			}
			BringToFront();
			Activate();
		}
	}

	internal void ExitApplication()
	{
		_exitRequested = true;
		Close();
	}

	private void BuildTabChip(BrowserTab tab)
	{
		tab.Title.Text = "Новая вкладка";
		tab.Chip.Controls.Add(tab.Title);
		tab.Chip.Controls.Add(tab.CloseButton);
		tab.CloseButton.FlatStyle = FlatStyle.Flat;
		tab.CloseButton.FlatAppearance.BorderSize = 0;
		tab.CloseButton.Font = new Font("Segoe UI", 10f);
		tab.Title.Click += delegate
		{
			ActivateTab(tab);
		};
		tab.Chip.Click += delegate
		{
			ActivateTab(tab);
		};
		tab.CloseButton.Click += delegate
		{
			CloseTab(tab);
		};
		tab.Title.MouseDown += delegate(object? _, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Middle)
			{
				CloseTab(tab);
			}
		};
		ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
		contextMenuStrip.Items.Add("Закрыть вкладку", null, delegate
		{
			CloseTab(tab);
		});
		contextMenuStrip.Items.Add("Закрыть другие", null, delegate
		{
			BrowserTab[] array = _pages.ToArray();
			foreach (BrowserTab browserTab in array)
			{
				if (browserTab != tab)
				{
					CloseTab(browserTab, suppressNew: true);
				}
			}
			ActivateTab(tab);
		});
		contextMenuStrip.Items.Add("Дублировать", null, async delegate
		{
			await OpenTabAsync(tab.Address);
		});
		tab.Chip.ContextMenuStrip = contextMenuStrip;
		tab.Title.ContextMenuStrip = contextMenuStrip;
		_tabs.Controls.Add(tab.Chip);
		_tabs.Controls.SetChildIndex(_newTab, _tabs.Controls.Count - 1);
		StyleTabChip(tab);
	}

	private void StyleTabChip(BrowserTab tab)
	{
		bool flag = tab == _active;
		bool dark = AppTheme.Dark;
		tab.Chip.BackColor = ((!dark) ? (flag ? Color.White : Color.FromArgb(235, 235, 235)) : (flag ? Color.FromArgb(57, 57, 52) : Color.FromArgb(48, 48, 45)));
		tab.Title.BackColor = tab.Chip.BackColor;
		tab.Title.ForeColor = (flag ? AppTheme.Text : AppTheme.Secondary);
		tab.CloseButton.BackColor = tab.Chip.BackColor;
		tab.CloseButton.ForeColor = AppTheme.Secondary;
		tab.CloseButton.Radius = 9;
		tab.Chip.Invalidate(invalidateChildren: true);
	}

	private async Task<BrowserTab?> CreateTabAsync(string? initialAddress, bool navigate = true)
	{
		if (_environment == null || base.IsDisposed || _pages.Count >= 25)
		{
			return null;
		}
		BrowserTab tab = new BrowserTab
		{
			Address = (initialAddress ?? "https://chatgpt.com/")
		};
		_pages.Add(tab);
		tab.View.Visible = false;
		_content.Controls.Add(tab.View);
		ActivateTab(tab);
		try
		{
			await tab.View.EnsureCoreWebView2Async(_environment);
			if (base.IsDisposed || tab.View.IsDisposed)
			{
				return null;
			}
			CoreWebView2 core = tab.View.CoreWebView2 ?? throw new InvalidOperationException("WebView2 tab not initialized");
			tab.Loaded = true;
			core.Settings.AreDefaultContextMenusEnabled = true;
			core.Settings.AreDevToolsEnabled = false;
			core.Profile.PreferredColorScheme = ((!AppTheme.Dark) ? CoreWebView2PreferredColorScheme.Light : CoreWebView2PreferredColorScheme.Dark);
			core.NavigationCompleted += delegate(object? _, CoreWebView2NavigationCompletedEventArgs e)
			{
				if (!tab.View.IsDisposed)
				{
					tab.NavigationOk = e.IsSuccess;
					if (tab == _active)
					{
						_error.Visible = !e.IsSuccess;
						_error.Text = (e.IsSuccess ? "" : ("Ошибка загрузки: " + e.WebErrorStatus));
						UpdateNavigation();
					}
					if (e.IsSuccess)
					{
						_ = ApplySitePaletteAsync(tab);
						_ = RegisterToolbarAsync(tab);
						_ = RegisterContextScriptAsync(tab);
					}
				}
			};
			core.SourceChanged += delegate
			{
				tab.Address = core.Source ?? tab.Address;
				if (tab == _active)
				{
					UpdateNavigation();
				}
			};
			core.DocumentTitleChanged += delegate
			{
				string documentTitle = core.DocumentTitle;
				tab.Title.Text = (string.IsNullOrWhiteSpace(documentTitle) ? "Новая вкладка" : ((documentTitle.Length > 48) ? (documentTitle.Substring(0, 47) + "…") : documentTitle));
				_hint.SetToolTip(tab.Title, documentTitle);
				PublishChromeState();
			};
			core.HistoryChanged += delegate
			{
				if (tab == _active)
				{
					UpdateNavigation();
				}
			};
			core.NewWindowRequested += async delegate(object? _, CoreWebView2NewWindowRequestedEventArgs args)
			{
				await HandlePopupAsync(args);
			};
			core.ContextMenuRequested += delegate(object? _, CoreWebView2ContextMenuRequestedEventArgs args)
			{
				HandleContextMenu(tab, args);
			};
			core.WebMessageReceived += delegate(object? _, CoreWebView2WebMessageReceivedEventArgs args)
			{
				OnClickableContext(tab, args);
			};
			tab.View.KeyDown += delegate(object? _, KeyEventArgs e)
			{
				if (e.Control && e.KeyCode == Keys.F5)
				{
					KeyEventArgs e2 = e;
					bool handled = (e.SuppressKeyPress = true);
					e2.Handled = handled;
					BeginInvoke(async delegate
					{
						await HardReloadAsync(tab);
					});
				}
				else if (e.Control && e.KeyCode == Keys.T)
				{
					KeyEventArgs e3 = e;
					bool handled = (e.SuppressKeyPress = true);
					e3.Handled = handled;
					BeginInvoke(async delegate
					{
						await OpenTabAsync("https://chatgpt.com/");
					});
				}
				else if (e.Control && e.KeyCode == Keys.W)
				{
					KeyEventArgs e4 = e;
					bool handled = (e.SuppressKeyPress = true);
					e4.Handled = handled;
					BeginInvoke(delegate
					{
						CloseTab(tab);
					});
				}
				else if (e.Control && e.KeyCode == Keys.L)
				{
					KeyEventArgs e5 = e;
					bool handled = (e.SuppressKeyPress = true);
					e5.Handled = handled;
					BeginInvoke(delegate
					{
						_address.Focus();
						_address.SelectAll();
					});
				}
				else if (e.Control && e.KeyCode == Keys.Tab)
				{
					KeyEventArgs e6 = e;
					bool handled = (e.SuppressKeyPress = true);
					e6.Handled = handled;
					BeginInvoke(delegate
					{
						CycleTab((!e.Shift) ? 1 : (-1));
					});
				}
				else if (e.Alt && e.KeyCode == Keys.Left)
				{
					KeyEventArgs e7 = e;
					bool handled = (e.SuppressKeyPress = true);
					e7.Handled = handled;
					BeginInvoke(delegate
					{
						_back.PerformClick();
					});
				}
				else if (e.Alt && e.KeyCode == Keys.Right)
				{
					KeyEventArgs e8 = e;
					bool handled = (e.SuppressKeyPress = true);
					e8.Handled = handled;
					BeginInvoke(delegate
					{
						_forward.PerformClick();
					});
				}
			};
			await RegisterToolbarAsync(tab);
			await RegisterContextScriptAsync(tab);
			if (navigate)
			{
				core.Navigate(tab.Address);
			}
			return tab;
		}
		catch (Exception ex)
		{
			AgentStorage.Log("WebView2 tab initialization: " + ex.GetType().Name);
			if (tab == _active)
			{
				_error.Text = ex.Message;
				_error.Visible = true;
			}
			return null;
		}
	}

	private async Task OpenTabAsync(string address)
	{
		await CreateTabAsync(address);
	}

	private void ActivateTab(BrowserTab tab)
	{
		if (base.IsDisposed || !_pages.Contains(tab))
		{
			return;
		}
		if (_active != null && !_active.View.IsDisposed)
		{
			_active.View.Visible = false;
		}
		_active = tab;
		if (!tab.View.IsDisposed)
		{
			tab.View.Visible = true;
			tab.View.BringToFront();
		}
		foreach (BrowserTab page in _pages)
		{
			StyleTabChip(page);
		}
		UpdateNavigation();
		PublishChromeState();
	}

	private void CycleTab(int step)
	{
		if (_pages.Count >= 2 && _active != null)
		{
			int num = _pages.IndexOf(_active);
			ActivateTab(_pages[(num + step + _pages.Count) % _pages.Count]);
		}
	}

	private void CloseActiveTab()
	{
		if (_active != null)
		{
			CloseTab(_active);
		}
	}

	private void CloseTab(BrowserTab tab, bool suppressNew = false)
	{
		int num = _pages.IndexOf(tab);
		if (num < 0)
		{
			return;
		}
		_pages.RemoveAt(num);
		_tabs.Controls.Remove(tab.Chip);
		tab.Chip.ContextMenuStrip?.Dispose();
		tab.Chip.Dispose();
		_content.Controls.Remove(tab.View);
		tab.View.Dispose();
		if (_active == tab)
		{
			_active = null;
			if (_pages.Count > 0)
			{
				ActivateTab(_pages[Math.Min(num, _pages.Count - 1)]);
			}
		}
		if (!suppressNew && _pages.Count == 0 && !base.IsDisposed)
		{
			OpenTabAsync("https://chatgpt.com/");
		}
		PublishChromeState();
	}

	private async Task NavigateAddressAsync()
	{
		if (_active?.View.CoreWebView2 == null)
		{
			return;
		}
		string text = _address.Text.Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			if (!text.Contains("://", StringComparison.Ordinal))
			{
				text = "https://" + text;
			}
			Uri result;
			bool flag = !Uri.TryCreate(text, UriKind.Absolute, out result);
			if (!flag)
			{
				string scheme = result.Scheme;
				bool flag2 = ((scheme == "https" || scheme == "http") ? true : false);
				flag = !flag2;
			}
			if (flag)
			{
				MessageBox.Show(this, "Введите корректный адрес HTTP или HTTPS.", "Адрес страницы");
				return;
			}
			_active.View.CoreWebView2.Navigate(result.AbsoluteUri);
			_active.View.Focus();
		}
	}

	private void UpdateNavigation()
	{
		CoreWebView2 coreWebView = _active?.View.CoreWebView2;
		BrowserTab active = _active;
		if (active != null && !_address.Focused)
		{
			_address.Text = coreWebView?.Source ?? active.Address;
		}
		_back.Enabled = coreWebView?.CanGoBack ?? false;
		_forward.Enabled = coreWebView?.CanGoForward ?? false;
		_reload.Enabled = coreWebView != null;
		_go.Enabled = coreWebView != null;
		Label error = _error;
		BrowserTab active2 = _active;
		error.Visible = active2 != null && active2.Loaded && !active2.NavigationOk && coreWebView != null && coreWebView.Source != "https://chatgpt.com/";
		PublishChromeState();
	}

	private async Task HardReloadAsync(BrowserTab? tab = null)
	{
		if (tab == null)
		{
			tab = _active;
		}
		CoreWebView2 core = tab?.View.CoreWebView2;
		if (core == null)
		{
			return;
		}
		try
		{
			await core.CallDevToolsProtocolMethodAsync("Page.reload", "{\"ignoreCache\":true}");
		}
		catch (Exception ex) when (((ex is InvalidOperationException || ex is COMException || ex is ArgumentException) ? 1 : 0) != 0)
		{
			AgentStorage.Log("Hard reload fallback: " + ex.GetType().Name);
			try
			{
				await core.CallDevToolsProtocolMethodAsync("Network.clearBrowserCache", "{}");
				core.Reload();
			}
			catch (COMException)
			{
				core.Reload();
			}
		}
	}

	private async void HandleContextMenu(BrowserTab tab, CoreWebView2ContextMenuRequestedEventArgs args)
	{
		try
		{
			CoreWebView2ContextMenuItem coreWebView2ContextMenuItem = tab.View.CoreWebView2.Environment.CreateContextMenuItem(args.ContextMenuTarget.HasLinkUri ? "Открыть ссылку в новой вкладке" : "Открыть элемент в новой вкладке", null, CoreWebView2ContextMenuItemKind.Command);
			string link = (args.ContextMenuTarget.HasLinkUri ? args.ContextMenuTarget.LinkUri : "");
			Point point = args.Location;
			coreWebView2ContextMenuItem.CustomItemSelected += async delegate
			{
				if (!base.IsDisposed && !tab.View.IsDisposed)
				{
					await OpenContextTargetAsync(tab, link, point);
				}
			};
			args.MenuItems.Insert(0, coreWebView2ContextMenuItem);
		}
		catch (Exception ex) when (((ex is InvalidOperationException || ex is COMException || ex is ArgumentException) ? 1 : 0) != 0)
		{
			AgentStorage.Log("Context menu: " + ex.GetType().Name);
		}
	}

	private async Task OpenContextTargetAsync(BrowserTab from, string link, Point point, string overrideSelector = "")
	{
		if (from.View.IsDisposed)
		{
			return;
		}
		string activeAddress = from.View.CoreWebView2?.Source ?? "https://chatgpt.com/";
		if (TryHttpUri(link, out string valid))
		{
			await OpenTabAsync(valid);
			return;
		}
		string selector = overrideSelector;
		try
		{
			if (selector.Length == 0)
			{
				string javaScript = $"(() => {{\n  const hit = document.elementFromPoint({point.X}, {point.Y});\n  const click = hit?.closest('a,button,[role=\"link\"],[role=\"button\"],[onclick],[tabindex]');\n  if (!click) return '';\n  if (click.id) return '#' + CSS.escape(click.id);\n  const test = click.getAttribute('data-testid');\n  if (test) {{\n      const q='[data-testid=\"'+CSS.escape(test)+'\"]';\n      if (document.querySelectorAll(q).length===1) return q;\n  }}\n  const aria=click.getAttribute('aria-label');\n  if (aria) {{\n      const q=click.tagName.toLowerCase()+'[aria-label=\"'+CSS.escape(aria)+'\"]';\n      if (document.querySelectorAll(q).length===1) return q;\n  }}\n  const result=[];\n  for (let node=click;node&&node.nodeType===1&&node!==document.body;node=node.parentElement) {{\n      const tag=node.tagName.toLowerCase();\n      const siblings=[...node.parentElement.children].filter(n=>n.tagName===node.tagName);\n      const pos=siblings.indexOf(node)+1;\n      result.unshift(tag+':nth-of-type('+pos+')');\n  }}\n  return 'body>'+result.join('>');\n}})()";
				selector = JsonSerializer.Deserialize<string>(await from.View.CoreWebView2.ExecuteScriptAsync(javaScript)) ?? "";
			}
		}
		catch (Exception ex) when (((ex is InvalidOperationException || ex is JsonException || ex is COMException) ? 1 : 0) != 0)
		{
		}
		if (!TryHttpUri(activeAddress, out string valid2))
		{
			valid2 = "https://chatgpt.com/";
		}
		BrowserTab tab = await CreateTabAsync(valid2);
		if (tab == null || selector.Length == 0)
		{
			return;
		}
		string code = "(async () => {\n  const selector = " + JsonSerializer.Serialize(selector) + ";\n  for (let i=0;i<30;i++){\n    const target=document.querySelector(selector);\n    if (target) {\n      target.click();\n      return true;\n    }\n    await new Promise(resolve=>setTimeout(resolve,180));\n  }\n  return false;\n})()";
		Task.Run(async delegate
		{
			await Task.Delay(850);
			if (!base.IsDisposed && !tab.View.IsDisposed && tab.View.IsHandleCreated)
			{
				tab.View.BeginInvoke(async delegate
				{
					try
					{
						await tab.View.CoreWebView2.ExecuteScriptAsync(code);
					}
					catch (Exception ex2) when (((ex2 is InvalidOperationException || ex2 is COMException) ? 1 : 0) != 0)
					{
					}
				});
			}
		});
	}

	private void OnClickableContext(BrowserTab tab, CoreWebView2WebMessageReceivedEventArgs args)
	{
		if (tab.View.IsDisposed || base.IsDisposed || !TryHttpUri(args.Source, out string valid) || !string.Equals(new Uri(valid).Host, new Uri(tab.View.CoreWebView2.Source).Host, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(args.WebMessageAsJson);
			JsonElement rootElement = jsonDocument.RootElement;
			if (!rootElement.TryGetProperty("mcp_kind", out var value))
			{
				return;
			}
			if (value.GetString() == "voice_reply")
			{
				_ = HandleVoiceWebMessageAsync(tab,rootElement);
				return;
			}
			if (value.GetString() == "browser_shortcut")
			{
				JsonElement value2;
				switch (rootElement.TryGetProperty("action", out value2) ? value2.GetString() : null)
				{
				case "hard_reload":
					HardReloadAsync(tab);
					break;
				case "new_tab":
					OpenTabAsync("https://chatgpt.com/");
					break;
				case "close_tab":
					CloseTab(tab);
					break;
				case "focus_address":
					_address.Focus();
					_address.SelectAll();
					break;
				case "next_tab":
					CycleTab(1);
					break;
				case "previous_tab":
					CycleTab(-1);
					break;
				}
			}
			else
			{
				if (value.GetString() != "clickable_context")
				{
					return;
				}
				JsonElement value3;
				string href = (rootElement.TryGetProperty("href", out value3) ? (value3.GetString() ?? "") : "");
				JsonElement value4;
				string selector = (rootElement.TryGetProperty("selector", out value4) ? (value4.GetString() ?? "") : "");
				JsonElement value5;
				int x = (rootElement.TryGetProperty("x", out value5) ? value5.GetInt32() : 0);
				JsonElement value6;
				int y = (rootElement.TryGetProperty("y", out value6) ? value6.GetInt32() : 0);
				if (href.Length > 4096 || selector.Length > 4096 || x < 0 || y < 0 || x >= tab.View.Width || y >= tab.View.Height)
				{
					return;
				}
				ContextMenuStrip menu = new ContextMenuStrip
				{
					ShowImageMargin = false,
					BackColor = AppTheme.Surface,
					ForeColor = AppTheme.Text
				};
				ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("Открыть в новой вкладке");
				toolStripMenuItem.Click += async delegate
				{
					await OpenContextTargetAsync(tab, href, new Point(x, y), selector);
				};
				menu.Items.Add(toolStripMenuItem);
				if (TryHttpUri(href, out string link))
				{
					ToolStripMenuItem toolStripMenuItem2 = new ToolStripMenuItem("Копировать адрес ссылки");
					toolStripMenuItem2.Click += delegate
					{
						try
						{
							Clipboard.SetText(link);
						}
						catch (ExternalException)
						{
						}
					};
					menu.Items.Add(toolStripMenuItem2);
				}
				menu.Closed += delegate
				{
					menu.Dispose();
				};
				menu.Show(tab.View, new Point(x, y));
			}
		}
		catch (Exception ex) when (((ex is JsonException || ex is InvalidOperationException || ex is ArgumentException || ex is COMException) ? 1 : 0) != 0)
		{
			AgentStorage.Log("Clickable context: " + ex.GetType().Name);
		}
	}

	private static bool TryHttpUri(string? value, out string valid)
	{
		valid = "";
		Uri result;
		bool flag = !Uri.TryCreate(value, UriKind.Absolute, out result);
		if (!flag)
		{
			string scheme = result.Scheme;
			bool flag2 = ((scheme == "http" || scheme == "https") ? true : false);
			flag = !flag2;
		}
		if (flag)
		{
			return false;
		}
		valid = result.AbsoluteUri;
		return true;
	}

	private async Task HandlePopupAsync(CoreWebView2NewWindowRequestedEventArgs args)
	{
		CoreWebView2Deferral deferral = args.GetDeferral();
		try
		{
			BrowserTab browserTab = await CreateTabAsync(null, navigate: false);
			if (browserTab != null)
			{
				args.NewWindow = browserTab.View.CoreWebView2;
			}
			else
			{
				args.Handled = true;
			}
		}
		catch (Exception ex)
		{
			AgentStorage.Log("WebView2 new tab: " + ex.GetType().Name);
			args.Handled = true;
		}
		finally
		{
			deferral.Complete();
		}
	}

	private static string ReadWarmCss()
	{
		if (!File.Exists(WarmCssFile))
		{
			return ":root { --main-surface-primary: #252523 !important; }html,body {background-color:#252523!important;color:#E5D9BF!important;}";
		}
		return File.ReadAllText(WarmCssFile);
	}

	private static string ReadToolbarScript()
	{
		if (!File.Exists(ToolbarScriptFile))
		{
			return "";
		}
		return File.ReadAllText(ToolbarScriptFile);
	}

	private static string ReadContextScript()
	{
		if (!File.Exists(ContextScriptFile))
		{
			return "";
		}
		return File.ReadAllText(ContextScriptFile);
	}

	private void WatchFiles()
	{
		Watch(WarmCssFile, delegate
		{
			_themeReload.Stop();
			_themeReload.Start();
		}, out _themeWatcher);
		Watch(ToolbarScriptFile, delegate
		{
			_toolbarReload.Stop();
			_toolbarReload.Start();
		}, out _toolbarWatcher);
		Watch(ContextScriptFile, delegate
		{
			_contextReload.Stop();
			_contextReload.Start();
		}, out _contextWatcher);
	}

	private void Watch(string file, Action callback, out FileSystemWatcher? watcher)
	{
		watcher = null;
		string directoryName = Path.GetDirectoryName(file);
		if (Directory.Exists(directoryName))
		{
			FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(directoryName, Path.GetFileName(file))
			{
				NotifyFilter = (NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite),
				EnableRaisingEvents = true
			};
			fileSystemWatcher.Changed += delegate
			{
				Schedule();
			};
			fileSystemWatcher.Created += delegate
			{
				Schedule();
			};
			fileSystemWatcher.Renamed += delegate
			{
				Schedule();
			};
			watcher = fileSystemWatcher;
		}
		void Schedule()
		{
			if (base.IsDisposed || base.Disposing || !base.IsHandleCreated)
			{
				return;
			}
			try
			{
				BeginInvoke(delegate
				{
					if (!base.IsDisposed)
					{
						callback();
					}
				});
			}
			catch (InvalidOperationException)
			{
			}
		}
	}

	private async Task ReloadAllToolbarScriptsAsync()
	{
		BrowserTab[] array = _pages.ToArray();
		foreach (BrowserTab tab in array)
		{
			await RegisterToolbarAsync(tab);
		}
	}

	private async Task ReloadAllContextScriptsAsync()
	{
		BrowserTab[] array = _pages.ToArray();
		foreach (BrowserTab tab in array)
		{
			await RegisterContextScriptAsync(tab);
		}
	}

	private async Task RegisterContextScriptAsync(BrowserTab tab)
	{
		if (base.IsDisposed || tab.View.IsDisposed)
		{
			return;
		}
		CoreWebView2 core = tab.View.CoreWebView2;
		if (core == null)
		{
			return;
		}
		string voiceFile=Path.Combine(AppContext.BaseDirectory,"Themes","ChatGPT-Voice.js");
		string script=ReadContextScript() + "\n" +
		    (File.Exists(voiceFile)?File.ReadAllText(voiceFile):"");
		if (string.IsNullOrWhiteSpace(script))
		{
			return;
		}
		try
		{
			string contextInjection = tab.ContextInjection;
			if (contextInjection != null && contextInjection.Length > 0)
			{
				core.RemoveScriptToExecuteOnDocumentCreated(tab.ContextInjection);
			}
			contextInjection = (tab.ContextInjection = await core.AddScriptToExecuteOnDocumentCreatedAsync(script));
			if (TryHttpUri(core.Source, out contextInjection))
			{
				await core.ExecuteScriptAsync(script);
				await core.ExecuteScriptAsync("window.__mcpVoiceBridge?.setEnabled("+
				    (_voicePreferences.AutoSpeak?"true":"false")+")");
			}
		}
		catch (Exception ex) when (((ex is InvalidOperationException || ex is IOException || ex is COMException) ? 1 : 0) != 0)
		{
			AgentStorage.Log("ChatGPT context script: " + ex.GetType().Name);
		}
	}

	private async Task RegisterToolbarAsync(BrowserTab tab)
	{
		if (base.IsDisposed || tab.View.IsDisposed)
		{
			return;
		}
		CoreWebView2 core = tab.View.CoreWebView2;
		if (core == null)
		{
			return;
		}
		string script = ReadToolbarScript();
		if (string.IsNullOrWhiteSpace(script))
		{
			return;
		}
		try
		{
			string toolbarInjection = tab.ToolbarInjection;
			if (toolbarInjection != null && toolbarInjection.Length > 0)
			{
				core.RemoveScriptToExecuteOnDocumentCreated(tab.ToolbarInjection);
			}
			tab.ToolbarInjection = await core.AddScriptToExecuteOnDocumentCreatedAsync(script);
			if (core.Source.StartsWith("https://chatgpt.com/", StringComparison.Ordinal))
			{
				await core.ExecuteScriptAsync(script);
			}
		}
		catch (Exception ex) when (((ex is InvalidOperationException || ex is IOException || ex is COMException) ? 1 : 0) != 0)
		{
			AgentStorage.Log("ChatGPT toolbar patch: " + ex.GetType().Name);
		}
	}

	private async Task ApplyAllPalettesAsync()
	{
		BrowserTab[] array = _pages.ToArray();
		foreach (BrowserTab tab in array)
		{
			await ApplySitePaletteAsync(tab);
		}
	}

	private async Task ApplySitePaletteAsync(BrowserTab tab)
	{
		if (tab.View.IsDisposed)
		{
			return;
		}
		CoreWebView2 coreWebView = tab.View.CoreWebView2;
		if (coreWebView == null || !coreWebView.Source.StartsWith("https://chatgpt.com/", StringComparison.Ordinal))
		{
			return;
		}
		string value = (AppTheme.Dark ? ReadWarmCss() : ":root {\n  --main-surface-primary: #ffffff !important;\n  --main-surface-secondary: #f1f1f1 !important;\n  --main-surface-tertiary: #e5e5e5 !important;\n  --sidebar-surface-primary: #f7f7f7 !important;\n  --sidebar-surface-secondary: #eaeaea !important;\n  --text-primary: #202020 !important;\n  --text-secondary: #666666 !important;\n  color-scheme: light !important;\n}\nhtml,body,main,[role=\"main\"] {background-color:#ffffff!important;}");
		string javaScript = "(() => {\n  if(location.hostname!=='chatgpt.com')return;\n  let style=document.getElementById('mcp-graphite-palette');\n  if(!style){\n    style=document.createElement('style');\n    style.id='mcp-graphite-palette';\n    (document.head||document.documentElement).appendChild(style);\n  }\n  style.textContent=" + JsonSerializer.Serialize(value) + ";})()";
		try
		{
			await coreWebView.ExecuteScriptAsync(javaScript);
		}
		catch (Exception ex) when (((ex is InvalidOperationException || ex is COMException) ? 1 : 0) != 0)
		{
		}
	}

	internal async Task<bool> ToolbarScriptLoadedAsync()
	{
		CoreWebView2 coreWebView = _active?.View.CoreWebView2;
		if (coreWebView == null)
		{
			return false;
		}
		try
		{
			return await coreWebView.ExecuteScriptAsync("Boolean(window.__mcpConversationToolbar && typeof window.__mcpConversationToolbar.stop === 'function')") == "true";
		}
		catch (Exception ex) when (((ex is InvalidOperationException || ex is COMException) ? 1 : 0) != 0)
		{
			return false;
		}
	}

	internal async Task<bool> PaletteAppliedAsync()
	{
		CoreWebView2 coreWebView = _active?.View.CoreWebView2;
		if (coreWebView == null || !LastNavigationSucceeded)
		{
			return false;
		}
		try
		{
			string text = JsonSerializer.Deserialize<string>(await coreWebView.ExecuteScriptAsync("getComputedStyle(document.body).backgroundColor"));
			return AppTheme.Dark ? (text == "rgb(37, 37, 35)") : (text == "rgb(255, 255, 255)");
		}
		catch (Exception ex) when (((ex is InvalidOperationException || ex is JsonException || ex is COMException) ? 1 : 0) != 0)
		{
			return false;
		}
	}

	internal void ApplyBrowserAppearance()
	{
		BackColor = AppTheme.Background;
		bool dark = AppTheme.Dark;
		Color color = (dark ? Color.FromArgb(37, 37, 35) : Color.FromArgb(247, 247, 247));
		Color color2 = (dark ? Color.FromArgb(48, 48, 45) : Color.White);
		_caption.BackColor = color;
		_captionName.BackColor = color;
		_captionName.ForeColor = AppTheme.Text;
		_tabsPanel.BackColor = color;
		_tabs.BackColor = color;
		_navigation.BackColor = color2;
		_content.BackColor = (dark ? _warmBackground : Color.White);
		_error.BackColor = color2;
		_error.ForeColor = AppTheme.Text;
		_newTab.BackColor = color;
		_addressShell.BackColor = (dark ? Color.FromArgb(48, 48, 45) : Color.White);
		_addressShell.ForeColor = AppTheme.Text;
		_address.BackColor = _addressShell.BackColor;
		_address.ForeColor = AppTheme.Text;
		WarmChromeButton[] array = new WarmChromeButton[8] { _back, _forward, _reload, _go, _newTab, _minimize, _maximize, _close };
		foreach (WarmChromeButton warmChromeButton in array)
		{
			warmChromeButton.BackColor = ((warmChromeButton == _newTab || warmChromeButton == _close || warmChromeButton == _minimize || warmChromeButton == _maximize) ? color : color2);
			warmChromeButton.ForeColor = AppTheme.Text;
			warmChromeButton.Invalidate();
		}
		foreach (BrowserTab page in _pages)
		{
			StyleTabChip(page);
			page.View.DefaultBackgroundColor = (dark ? _warmBackground : Color.White);
			if (page.View.CoreWebView2 != null)
			{
				page.View.CoreWebView2.Profile.PreferredColorScheme = ((!dark) ? CoreWebView2PreferredColorScheme.Light : CoreWebView2PreferredColorScheme.Dark);
			}
		}
		_chrome.DefaultBackgroundColor = (dark ? _warmBackground : Color.White);
		PublishChromeState();
		ApplyAllPalettesAsync();
		Invalidate(invalidateChildren: true);
	}

	private async Task InitializeChromeAsync()
	{
		if (_environment == null)
		{
			throw new InvalidOperationException("WebView2 environment unavailable");
		}
		if (_chrome.CoreWebView2 != null)
		{
			return;
		}
		await _chrome.EnsureCoreWebView2Async(_environment);
		if (!base.IsDisposed)
		{
			CoreWebView2 obj = _chrome.CoreWebView2 ?? throw new InvalidOperationException("Browser header unavailable");
			obj.Settings.AreDefaultContextMenusEnabled = false;
			obj.Settings.AreDevToolsEnabled = false;
			string text = Path.Combine(AppContext.BaseDirectory, "Themes");
			if (!File.Exists(Path.Combine(text, "BrowserChrome.html")))
			{
				throw new FileNotFoundException("Themes\\BrowserChrome.html");
			}
			obj.SetVirtualHostNameToFolderMapping("mcp-tunnel.local", text, CoreWebView2HostResourceAccessKind.DenyCors);
			obj.WebMessageReceived += ChromeMessageReceived;
			obj.NavigationCompleted += delegate(object? _, CoreWebView2NavigationCompletedEventArgs e)
			{
				_chromeNavigated = e.IsSuccess;
				PublishChromeState();
				PublishVoiceState();
			};
			_chromeReady = true;
			obj.Navigate("https://mcp-tunnel.local/BrowserChrome.html");
		}
	}

	private BrowserTab? FindTab(string? id)
	{
		return _pages.FirstOrDefault((BrowserTab t) => t.Id == id);
	}

	private void PublishChromeState()
	{
		if (base.IsDisposed || !_chromeNavigated || _chrome.CoreWebView2 == null)
		{
			return;
		}
		try
		{
			CoreWebView2 coreWebView = _active?.View.CoreWebView2;
			string text = JsonSerializer.Serialize(new
			{
				kind = "browser_state",
				tabs = _pages.Select((BrowserTab t) => new
				{
					id = t.Id,
					title = (string.IsNullOrWhiteSpace(t.Title.Text) ? "Новая вкладка" : t.Title.Text)
				}).ToArray(),
				activeId = _active?.Id,
				address = (coreWebView?.Source ?? _active?.Address ?? "https://chatgpt.com/"),
				version = "v" + Application.ProductVersion.Split('+')[0],
				theme = (AppTheme.Dark ? "dark" : "light"),
				maximized = (base.WindowState == FormWindowState.Maximized),
				networkMode = VpnModeController.Current == VpnConnectionMode.FreeReality ? "free" : "happ",
				canGoBack = (coreWebView?.CanGoBack ?? false),
				canGoForward = (coreWebView?.CanGoForward ?? false)
			});
			_chrome.CoreWebView2.ExecuteScriptAsync("window.applyChromeState?.(" + text + ")");
		}
		catch (Exception ex) when (((ex is InvalidOperationException || ex is COMException) ? 1 : 0) != 0)
		{
			AgentStorage.Log("Browser chrome state: " + ex.GetType().Name);
		}
	}

	private async void ChromeMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
	{
		if (base.IsDisposed || base.Disposing || !args.Source.StartsWith("https://mcp-tunnel.local/BrowserChrome.html", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		try
		{
			string webMessageAsJson = args.WebMessageAsJson;
			if (webMessageAsJson.Length > 8192)
			{
				return;
			}
			using JsonDocument doc = JsonDocument.Parse(webMessageAsJson);
			JsonElement rootElement = doc.RootElement;
			if (!rootElement.TryGetProperty("command", out var value))
			{
				return;
			}
			string text = value.GetString();
			if (text == "vpn-switch")
			{
				HandleVpnChromeMessage(rootElement);
				return;
			}
			if (text is not null && text.StartsWith("voice-",StringComparison.Ordinal))
			{
				await HandleVoiceChromeCommandAsync(text,rootElement);
				return;
			}
			JsonElement value2;
			string id = (rootElement.TryGetProperty("id", out value2) ? value2.GetString() : null);
			BrowserTab browserTab = FindTab(id);
			if (text == null)
			{
				return;
			}
			switch (text.Length)
			{
			case 8:
				switch (text[2])
				{
				case 'n':
					if (text == "minimize")
					{
						base.WindowState = FormWindowState.Minimized;
					}
					break;
				case 'x':
					if (text == "maximize")
					{
						ToggleMaximize();
						PublishChromeState();
					}
					break;
				case 'b':
					if (text == "tab-next")
					{
						CycleTab(1);
					}
					break;
				case 'v':
				{
					if (!(text == "navigate"))
					{
						break;
					}
					JsonElement value3;
					string text2 = ((!rootElement.TryGetProperty("address", out value3)) ? null : value3.GetString()?.Trim());
					if (text2 != null)
					{
						int i = text2.Length;
						if (i > 0 && i < 4096)
						{
							_address.Text = text2;
							await NavigateAddressAsync();
						}
					}
					break;
				}
				}
				break;
			case 4:
				switch (text[0])
				{
				case 'd':
					if (text == "drag" && base.WindowState != FormWindowState.Maximized)
					{
						ReleaseCapture();
						SendMessage(base.Handle, 161, new IntPtr(2), IntPtr.Zero);
					}
					break;
				case 'b':
					if (text == "back")
					{
						BrowserTab? active2 = _active;
						if (active2 != null && active2.View.CoreWebView2?.CanGoBack == true)
						{
							_active.View.CoreWebView2.GoBack();
						}
					}
					break;
				}
				break;
			case 12:
				switch (text[4])
				{
				case 'o':
					if (text == "window-close")
					{
						Close();
					}
					break;
				case 'a':
					if (text == "tab-activate" && browserTab != null)
					{
						ActivateTab(browserTab);
					}
					break;
				case 'p':
					if (text == "tab-previous")
					{
						CycleTab(-1);
					}
					break;
				}
				break;
			case 7:
				switch (text[0])
				{
				case 't':
					if (text == "tab-new")
					{
						await OpenTabAsync("https://chatgpt.com/");
					}
					break;
				case 'f':
					if (text == "forward")
					{
						BrowserTab? active = _active;
						if (active != null && active.View.CoreWebView2?.CanGoForward == true)
						{
							_active.View.CoreWebView2.GoForward();
						}
					}
					break;
				}
				break;
			case 5:
				if (text == "ready")
				{
					PublishChromeState();
				}
				break;
			case 9:
				if (text == "tab-close" && browserTab != null)
				{
					CloseTab(browserTab);
				}
				break;
			case 13:
				if (text == "tab-duplicate" && browserTab != null)
				{
					await OpenTabAsync(browserTab.Address);
				}
				break;
			case 15:
			{
				if (!(text == "tab-close-other") || browserTab == null)
				{
					break;
				}
				BrowserTab[] array = _pages.ToArray();
				foreach (BrowserTab browserTab2 in array)
				{
					if (browserTab2 != browserTab)
					{
						CloseTab(browserTab2, suppressNew: true);
					}
				}
				ActivateTab(browserTab);
				break;
			}
			case 11:
				if (text == "hard-reload")
				{
					await HardReloadAsync();
				}
				break;
			}
		}
		catch (Exception ex) when (((ex is JsonException || ex is InvalidOperationException || ex is ArgumentException || ex is COMException) ? 1 : 0) != 0)
		{
			AgentStorage.Log("Browser chrome action: " + ex.GetType().Name);
		}
	}

	private async Task InitializeAsync()
	{
		if (_initializing || base.IsDisposed || _environment != null)
		{
			return;
		}
		_initializing = true;
		try
		{
			string text = Environment.GetEnvironmentVariable("CHATGPT_MCP_WEBVIEW_TEST_PROFILE") ?? VpnModeController.BrowserProfileFolder(ProfileFolder);
			Directory.CreateDirectory(text);
			_environment = await CoreWebView2Environment.CreateAsync(null, text, new CoreWebView2EnvironmentOptions
			{
				AdditionalBrowserArguments = VpnModeController.BrowserArguments()
			});
            _environment.BrowserProcessExited += (_, _) => _browserExited.TrySetResult(true);
			WatchFiles();
			await InitializeChromeAsync();
			await OpenTabAsync("https://chatgpt.com/");
			AgentStorage.Log("ChatGPT tabbed WebView2 initialized");
		}
		catch (Exception ex)
		{
			AgentStorage.Log("Tabbed WebView2 init error " + ex.GetType().Name);
			_error.Text = "Ошибка WebView2: " + ex.Message;
			_error.Visible = true;
		}
		finally
		{
			_initializing = false;
		}
	}
}
