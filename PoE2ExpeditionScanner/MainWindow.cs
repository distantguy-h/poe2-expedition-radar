using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PoE2ExpeditionScanner;

public class MainWindow : Window, INotifyPropertyChanged, IComponentConnector
{
	private readonly ScannerService _scanner = new ScannerService();

	private readonly ObservableCollection<RewardRow> _rows = new ObservableCollection<RewardRow>();

	private ICollectionView _resultsView;

	private bool _viewCleared;

	private readonly LicenseStateService _license = new LicenseStateService(Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0");

	private readonly RemoteConfigurationService _remoteConfiguration = new RemoteConfigurationService();

	internal TextBlock AppVersionText;

	internal TextBlock DataVersionText;

	internal TextBlock GameBuildText;

	internal TextBlock LicenseStatusText;

	internal TextBlock LicenseDetailText;

	internal PasswordBox LicenseKeyBox;

	internal Button ActivateButton;

	internal Ellipse StatusDot;

	internal TextBlock StatusTitle;

	internal TextBlock StatusMessage;

	internal TextBlock PidText;

	internal Button StartButton;

	internal Button StopButton;

	internal TextBox SearchBox;

	internal ComboBox CurrencySelector;

	internal ComboBox SlotFilter;

	internal ComboBox ExpeditionFilter;

	internal DataGrid ResultsGrid;

	internal DataGridTextColumn ValueColumn;

	internal StackPanel EmptyStatePanel;

	internal Expander LogExpander;

	internal ScrollViewer LogScroll;

	internal TextBlock LogText;

	private bool _contentLoaded;

	public ICollectionView ResultsView
	{
		get
		{
			return _resultsView;
		}
		private set
		{
			_resultsView = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ResultsView"));
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public MainWindow()
	{
		_resultsView = CollectionViewSource.GetDefaultView(_rows);
		InitializeComponent();
		base.DataContext = this;
		ResultsView.Filter = FilterRow;
		ResultsView.SortDescriptions.Add(new SortDescription("Value", ListSortDirection.Descending));
		_scanner.StatusChanged += delegate(ScannerStatus status)
		{
			base.Dispatcher.InvokeAsync(delegate
			{
				ApplyStatus(status);
			});
		};
		_scanner.LogAdded += delegate(string line)
		{
			base.Dispatcher.InvokeAsync(delegate
			{
				AddLog(line);
			});
		};
		_scanner.MetadataChanged += delegate(ScannerMetadata metadata)
		{
			base.Dispatcher.InvokeAsync(delegate
			{
				ApplyMetadata(metadata);
			});
		};
		_scanner.ResultsChanged += delegate(IReadOnlyList<RewardRow> rows)
		{
			base.Dispatcher.InvokeAsync(delegate
			{
				ApplyResults(rows);
			});
		};
		_license.Changed += delegate(LicenseSnapshot state)
		{
			base.Dispatcher.InvokeAsync(delegate
			{
				ApplyLicense(state);
			});
		};
		base.Loaded += async delegate
		{
			await _license.InitializeAsync();
			if (_license.Current.CanScan)
			{
				await RefreshDataQuietlyAsync();
			}
		};
		base.Closing += Window_Closing;
	}

	private void ApplyLicense(LicenseSnapshot license)
	{
		LicenseStatusText.Text = license.State.ToString().ToUpperInvariant();
		TextBlock licenseDetailText = LicenseDetailText;
		DateTimeOffset? expiresAt = license.ExpiresAt;
		object text;
		if (expiresAt.HasValue)
		{
			DateTimeOffset valueOrDefault = expiresAt.GetValueOrDefault();
			text = $"{license.Message}  {license.Plan?.ToUpperInvariant()} · expires {valueOrDefault.LocalDateTime:g}";
		}
		else
		{
			text = license.Message;
		}
		licenseDetailText.Text = (string)text;
		ActivateButton.IsEnabled = license.State != LicenseState.Activating;
		StartButton.IsEnabled = license.CanScan;
	}

	private async void Activate_Click(object sender, RoutedEventArgs e)
	{
		string password = LicenseKeyBox.Password;
		if (!string.IsNullOrWhiteSpace(password))
		{
			await _license.ActivateAsync(password);
			if (_license.Current.CanScan)
			{
				LicenseKeyBox.Clear();
				await RefreshDataQuietlyAsync();
			}
		}
	}

	private async Task RefreshDataQuietlyAsync()
	{
		try
		{
			AddLog("[data] signed release " + await _remoteConfiguration.RefreshAsync() + " cached");
		}
		catch (Exception ex)
		{
			AddLog("[data] update unavailable; keeping verified local cache: " + ex.Message);
		}
	}

	private void ApplyMetadata(ScannerMetadata metadata)
	{
		AppVersionText.Text = "v" + metadata.AppVersion;
		DataVersionText.Text = metadata.OffsetVersion + " / " + metadata.RecipeVersion;
		GameBuildText.Text = metadata.GameBuild;
	}

	private unsafe void ApplyStatus(ScannerStatus status)
	{
		object obj = status.State switch
		{
			ScannerState.Scanning => ("SCANNING", Color.FromRgb(35, 240, 229)), 
			ScannerState.ScanComplete => ("SCAN COMPLETE", Color.FromRgb(35, 240, 229)), 
			ScannerState.WaitingForGame => ("WAITING FOR GAME", Color.FromRgb(byte.MaxValue, 181, 71)), 
			ScannerState.Connecting => ("CONNECTING", Color.FromRgb(byte.MaxValue, 181, 71)), 
			ScannerState.Stopped => ("STOPPED", Color.FromRgb(120, 147, 154)), 
			ScannerState.DataDisabled => ("DATA DISABLED", Color.FromRgb(byte.MaxValue, 58, 174)), 
			ScannerState.OffsetsOutdated => ("OFFSETS OUTDATED", Color.FromRgb(byte.MaxValue, 58, 174)), 
			_ => ("ERROR", Color.FromRgb(byte.MaxValue, 58, 174)), 
		};
		string item = ((ValueTuple<string, Color>*)(&obj))->Item1;
		SolidColorBrush solidColorBrush = new SolidColorBrush(((ValueTuple<string, Color>*)(&obj))->Item2);
		StatusTitle.Text = item;
		StatusTitle.Foreground = solidColorBrush;
		StatusDot.Fill = solidColorBrush;
		StatusMessage.Text = status.Message;
		TextBlock pidText = PidText;
		int? pid = status.Pid;
		object text;
		if (pid.HasValue)
		{
			int valueOrDefault = pid.GetValueOrDefault();
			text = $"PID {valueOrDefault}";
		}
		else
		{
			text = "PID —";
		}
		pidText.Text = (string)text;
		Button startButton = StartButton;
		bool flag = _license.Current.CanScan;
		ScannerState state;
		if (flag)
		{
			state = status.State;
			bool flag2 = (uint)(state - 3) <= 4u;
			flag = flag2;
		}
		startButton.IsEnabled = flag;
		startButton = StartButton;
		state = status.State;
		flag = (uint)state <= 2u;
		startButton.Content = (flag ? "… SCANNING" : "⌁ SCAN");
		startButton = StopButton;
		state = status.State;
		flag = (uint)state <= 2u;
		startButton.IsEnabled = flag;
	}

	private void ApplyResults(IReadOnlyList<RewardRow> rows)
	{
		if (_viewCleared && rows.Count > 0)
		{
			return;
		}
		_rows.Clear();
		foreach (RewardRow row in rows)
		{
			ApplySelectedCurrency(row);
			_rows.Add(row);
		}
		EmptyStatePanel.Visibility = ((rows.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		RebuildExpeditionFilter(rows);
		ResultsView.Refresh();
	}

	private void RebuildExpeditionFilter(IReadOnlyList<RewardRow> rows)
	{
		string selected = (ExpeditionFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ALL";
		string[] array = (from x in rows.Select((RewardRow x) => x.Expedition).Distinct()
			orderby x
			select x).ToArray();
		ExpeditionFilter.Items.Clear();
		ExpeditionFilter.Items.Add(new ComboBoxItem
		{
			Content = "All expeditions",
			Tag = "ALL"
		});
		string[] array2 = array;
		foreach (string text in array2)
		{
			ExpeditionFilter.Items.Add(new ComboBoxItem
			{
				Content = text,
				Tag = text
			});
		}
		ExpeditionFilter.SelectedIndex = Math.Max(0, Array.FindIndex(array, (string x) => x == selected) + 1);
	}

	private bool FilterRow(object item)
	{
		if (!(item is RewardRow rewardRow))
		{
			return false;
		}
		string search = SearchBox?.Text?.Trim() ?? "";
		if (search.Length > 0 && !new string[5] { rewardRow.Reward, rewardRow.Anchor, rewardRow.Runes, rewardRow.Address, rewardRow.Expedition }.Any((string x) => x.Contains(search, StringComparison.OrdinalIgnoreCase)))
		{
			return false;
		}
		if (SlotFilter?.SelectedItem is ComboBoxItem { Tag: var tag } && int.TryParse(tag?.ToString(), out var result) && result > 0 && ((result == 6) ? (rewardRow.Slots < 6) : (rewardRow.Slots != result)))
		{
			return false;
		}
		string text = (ExpeditionFilter?.SelectedItem as ComboBoxItem)?.Tag?.ToString();
		if ((text != null && !(text == "ALL")) || 1 == 0)
		{
			return rewardRow.Expedition == text;
		}
		return true;
	}

	private void Filter_Changed(object sender, RoutedEventArgs e)
	{
		ResultsView?.Refresh();
	}

	private void Currency_Changed(object sender, RoutedEventArgs e)
	{
		if (!(CurrencySelector?.SelectedItem is ComboBoxItem { Tag: var tag }))
		{
			return;
		}
		bool flag = string.Equals(tag?.ToString(), "DIV", StringComparison.Ordinal);
		if (ValueColumn != null)
		{
			ValueColumn.Header = (flag ? "VALUE (DIV)" : "VALUE (EX)");
		}
		foreach (RewardRow row in _rows)
		{
			ApplySelectedCurrency(row);
		}
		ResultsView?.Refresh();
	}

	private void ApplySelectedCurrency(RewardRow row)
	{
		double? num = (string.Equals((CurrencySelector?.SelectedItem as ComboBoxItem)?.Tag?.ToString(), "DIV", StringComparison.Ordinal) ? row.PriceDivine : row.PriceExalted);
		double? value;
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			value = valueOrDefault * (double)row.Quantity;
		}
		else
		{
			value = null;
		}
		row.Value = value;
	}

	private void AddLog(string line)
	{
		TextBlock logText = LogText;
		logText.Text = logText.Text + ((LogText.Text.Length == 0) ? "" : Environment.NewLine) + line;
		LogScroll.ScrollToEnd();
	}

	private void Clear_Click(object sender, RoutedEventArgs e)
	{
		_viewCleared = true;
		_rows.Clear();
	}

	private void Start_Click(object sender, RoutedEventArgs e)
	{
		if (!_license.Current.CanScan)
		{
			ApplyLicense(_license.Current with
			{
				Message = "A valid license is required before scanning."
			});
		}
		else
		{
			_viewCleared = false;
			_scanner.Start();
		}
	}

	private async void Stop_Click(object sender, RoutedEventArgs e)
	{
		await _scanner.StopAsync();
	}

	private async void Window_Closing(object? sender, CancelEventArgs e)
	{
		await _scanner.DisposeAsync();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.5.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/PoE2ExpeditionScanner;component/mainwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.5.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			AppVersionText = (TextBlock)target;
			break;
		case 2:
			DataVersionText = (TextBlock)target;
			break;
		case 3:
			GameBuildText = (TextBlock)target;
			break;
		case 4:
			LicenseStatusText = (TextBlock)target;
			break;
		case 5:
			LicenseDetailText = (TextBlock)target;
			break;
		case 6:
			LicenseKeyBox = (PasswordBox)target;
			break;
		case 7:
			ActivateButton = (Button)target;
			ActivateButton.Click += Activate_Click;
			break;
		case 8:
			StatusDot = (Ellipse)target;
			break;
		case 9:
			StatusTitle = (TextBlock)target;
			break;
		case 10:
			StatusMessage = (TextBlock)target;
			break;
		case 11:
			PidText = (TextBlock)target;
			break;
		case 12:
			StartButton = (Button)target;
			StartButton.Click += Start_Click;
			break;
		case 13:
			StopButton = (Button)target;
			StopButton.Click += Stop_Click;
			break;
		case 14:
			SearchBox = (TextBox)target;
			SearchBox.TextChanged += Filter_Changed;
			break;
		case 15:
			CurrencySelector = (ComboBox)target;
			CurrencySelector.SelectionChanged += Currency_Changed;
			break;
		case 16:
			SlotFilter = (ComboBox)target;
			SlotFilter.SelectionChanged += Filter_Changed;
			break;
		case 17:
			ExpeditionFilter = (ComboBox)target;
			ExpeditionFilter.SelectionChanged += Filter_Changed;
			break;
		case 18:
			((Button)target).Click += Clear_Click;
			break;
		case 19:
			ResultsGrid = (DataGrid)target;
			break;
		case 20:
			ValueColumn = (DataGridTextColumn)target;
			break;
		case 21:
			EmptyStatePanel = (StackPanel)target;
			break;
		case 22:
			LogExpander = (Expander)target;
			break;
		case 23:
			LogScroll = (ScrollViewer)target;
			break;
		case 24:
			LogText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
