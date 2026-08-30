using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

internal sealed class ScannerService : IAsyncDisposable
{
	private const string ExpeditionMetadata = "Metadata/MiscellaneousObjects/Expedition2/Expedition2Encounter";

	private readonly object _gate = new object();

	private CancellationTokenSource? _cts;

	private Task? _worker;

	public bool IsRunning
	{
		get
		{
			lock (_gate)
			{
				Task worker = _worker;
				return worker != null && !worker.IsCompleted;
			}
		}
	}

	public event Action<ScannerStatus>? StatusChanged;

	public event Action<string>? LogAdded;

	public event Action<IReadOnlyList<RewardRow>>? ResultsChanged;

	public event Action<ScannerMetadata>? MetadataChanged;

	public void Start()
	{
		lock (_gate)
		{
			Task worker = _worker;
			if (worker == null || worker.IsCompleted)
			{
				_cts = new CancellationTokenSource();
				_worker = Task.Run(() => RunAsync(_cts.Token));
			}
		}
	}

	public async Task StopAsync()
	{
		Task worker;
		lock (_gate)
		{
			_cts?.Cancel();
			worker = _worker;
		}
		if (worker != null)
		{
			try
			{
				await worker;
			}
			catch (OperationCanceledException)
			{
			}
		}
		PublishStatus(ScannerState.Stopped, "Scanner stopped");
	}

	public async Task ReconnectAsync()
	{
		await StopAsync();
		Start();
	}

	private async Task RunAsync(CancellationToken token)
	{
		PublishStatus(ScannerState.Connecting, "Loading local data for one-shot scan...");
		ConfigurationBundle config;
		try
		{
			Log("Loading offsets.json and recipes.json...");
			config = await ConfigurationLoader.LoadAsync(AppContext.BaseDirectory);
			this.MetadataChanged?.Invoke(new ScannerMetadata(config.AppVersion, config.OffsetDocument.DataVersion, config.Catalog.DataVersion, config.OffsetDocument.GameBuild));
			Log($"Data ready: offsets {config.OffsetDocument.DataVersion}, recipes {config.Catalog.DataVersion} ({config.Catalog.RecipeCount} recipes)");
		}
		catch (DataDisabledException ex)
		{
			PublishStatus(ScannerState.DataDisabled, ex.Message);
			Log("DATA DISABLED: " + ex.Message);
			return;
		}
		catch (Exception ex2)
		{
			PublishStatus(ScannerState.Error, ex2.Message);
			Log("CONFIG ERROR: " + ex2.Message);
			return;
		}
		var (priceBook, value) = await RewardPriceService.LoadAsync(token);
		Log($"Prices: {priceBook.Count} rewards from {value}.");
		while (!token.IsCancellationRequested)
		{
			PublishStatus(ScannerState.WaitingForGame, "Waiting for Path of Exile 2...");
			GameProcess process = null;
			while (process == null && !token.IsCancellationRequested)
			{
				process = GameProcess.Attach();
				if (process == null)
				{
					PublishStatus(ScannerState.WaitingForGame, GameProcess.IsGameRunning() ? "Game detected but memory access failed. Restart this app as Administrator." : "Waiting for Path of Exile 2...");
					await Task.Delay(1500, token);
				}
			}
			if (process == null)
			{
				break;
			}
			using (process)
			{
				using (MemoryReader mem = new MemoryReader(process.Handle))
				{
					PublishStatus(ScannerState.Connecting, "Resolving GameState and validating offsets...", process.Pid);
					Log($"Attached to {process.Name} (PID {process.Pid})");
					nint slot;
					try
					{
						slot = Bootstrap.ResolveGameStateSlot(process, mem, config.Offsets, config.Patterns);
					}
					catch (Exception ex3)
					{
						PublishStatus(ScannerState.Error, "AOB scan failed: " + ex3.Message, process.Pid);
						Log("AOB ERROR: " + ex3.Message);
						await Task.Delay(2000, token);
						goto end_IL_03f2;
					}
					if (slot == 0)
					{
						PublishStatus(ScannerState.Error, "Could not resolve the live chain. Wait until the map finishes loading, then press SCAN again.", process.Pid);
						Log("SCAN FAILED: no valid in-game chain. This can be transient during area loading.");
						break;
					}
					if (!Bootstrap.TryResolveInGame(mem, slot, config.Offsets, out var _, out var area, out var localPlayer))
					{
						PublishStatus(ScannerState.OffsetsOutdated, "The in-game pointer chain is invalid.", process.Pid);
						break;
					}
					PublishStatus(ScannerState.Scanning, "Live scan active", process.Pid);
					Log($"GameState slot 0x{slot:X}; scanner is live.");
					nint cachedArea = 0;
					Dictionary<long, List<RewardRow>> monolithRows = new Dictionary<long, List<RewardRow>>();
					Dictionary<long, string> monolithLabels = new Dictionary<long, string>();
					while (!token.IsCancellationRequested && IsProcessAlive(process.Pid))
					{
						if (!Bootstrap.TryResolveInGame(mem, slot, config.Offsets, out localPlayer, out var area2, out area))
						{
							PublishStatus(ScannerState.Connecting, "Area loading; waiting for a valid map...", process.Pid);
							await Task.Delay(500, token);
							continue;
						}
						if (area2 != cachedArea)
						{
							monolithRows.Clear();
							monolithLabels.Clear();
							this.ResultsChanged?.Invoke(Array.Empty<RewardRow>());
							Log($"Entered area 0x{area2:X}; cleared RuneStation cache.");
						}
						int areaLevel = mem.ReadInt32(area2 + config.Offsets.AreaLevel);
						List<EntityInfo> first = EntityReader.ReadAwakeEntities(mem, area2, config.Offsets.AreaAwakeEntities);
						List<EntityInfo> second = EntityReader.ReadAwakeEntities(awakeOffset: (config.Offsets.AreaSleepingEntities > 0) ? config.Offsets.AreaSleepingEntities : (config.Offsets.AreaAwakeEntities + 16), mem: mem, area: area2);
						List<EntityInfo> list = (from x in first.Concat(second)
							group x by x.Address into x
							select x.First()).ToList();
						DateTime now = DateTime.Now;
						int num = 0;
						foreach (EntityInfo item in list)
						{
							bool num2 = item.Metadata.Contains("Expedition2Encounter", StringComparison.OrdinalIgnoreCase);
							bool flag = !item.Metadata.StartsWith("Metadata/Characters/", StringComparison.Ordinal) && !item.Metadata.StartsWith("Metadata/Monsters/", StringComparison.Ordinal);
							if (!num2 && !flag)
							{
								continue;
							}
							num++;
							ExpeditionState s = MonolithReader.Read(mem, item.Address, config.Offsets, config.Catalog.RuneNames);
							if (!s.Resolved)
							{
								continue;
							}
							long num3 = item.Address;
							if (!monolithLabels.TryGetValue(num3, out string value2))
							{
								value2 = (monolithLabels[num3] = $"RUNE-{monolithLabels.Count + 1:00}");
								Log($"RuneStation resolved: {value2} 0x{num3:X} ({item.Metadata})");
							}
							IOrderedEnumerable<RuneMonolithCatalog.Offer> orderedEnumerable = from x in config.Catalog.Offers(s, areaLevel)
								orderby x.Size descending, x.Count descending, x.Name
								select x;
							List<RewardRow> list2 = new List<RewardRow>();
							foreach (RuneMonolithCatalog.Offer item2 in orderedEnumerable)
							{
								RewardPrice rewardPrice = priceBook.Find(item2.Name);
								list2.Add(new RewardRow(now, value2, num3, areaLevel, s.HoleCount, s.AnchorName, s.AnchorPos, item2.Name, item2.Count, item2.Size, item2.Runes, rewardPrice?.Exalted, rewardPrice?.Divine));
							}
							monolithRows[num3] = list2;
						}
						List<RewardRow> list3 = monolithRows.OrderBy<KeyValuePair<long, List<RewardRow>>, string>((KeyValuePair<long, List<RewardRow>> x) => x.Value.FirstOrDefault()?.Expedition).SelectMany((KeyValuePair<long, List<RewardRow>> x) => x.Value).ToList();
						int count = monolithRows.Count;
						this.ResultsChanged?.Invoke(list3);
						PublishStatus(ScannerState.ScanComplete, (count == 0) ? $"Scan complete · no RuneStation · {list.Count} entities · {num} objects probed" : $"Scan complete · {count} RuneStation(s) · {list3.Count} reward(s)", process.Pid);
						Log($"One-shot scan complete: {count} RuneStation(s), {list3.Count} reward(s).");
						return;
					}
					this.ResultsChanged?.Invoke(Array.Empty<RewardRow>());
					Log("Game process closed or changed; reconnecting.");
					goto end_IL_03cd;
					end_IL_03f2:;
				}
				end_IL_03cd:;
			}
		}
	}

	private static bool IsProcessAlive(int pid)
	{
		try
		{
			using Process process = Process.GetProcessById(pid);
			return !process.HasExited;
		}
		catch
		{
			return false;
		}
	}

	private void PublishStatus(ScannerState state, string message, int? pid = null)
	{
		this.StatusChanged?.Invoke(new ScannerStatus(state, message, pid));
	}

	private void Log(string message)
	{
		this.LogAdded?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
	}

	public async ValueTask DisposeAsync()
	{
		await StopAsync();
		_cts?.Dispose();
	}
}
