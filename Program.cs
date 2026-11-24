using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LayaLagsik
{
    // =========================================================================
    // 1. C# DATA MODELS & ENUMS
    // =========================================================================
    public enum PaceMode
    {
        StandingStill = 0,
        SlowWalk = 80,
        MediumWalk = 105,
        FastBrisk = 125,
        Jogging = 160
    }

    public class HandPortions
    {
        public int Protein { get; set; } = 1; // Palm size
        public int Veggies { get; set; } = 1; // Fist size
        public int Carbs { get; set; } = 1;   // Cupped hand
        public int Fats { get; set; } = 1;    // Thumb size

        public int Total => Protein + Veggies + Carbs + Fats;
    }

    public class StepSessionRecord
    {
        public string PaceLabel { get; set; } = string.Empty;
        public int Steps { get; set; }
        public int DurationSeconds { get; set; }
        public string LoggedAt { get; set; } = string.Empty;
    }

    public class WellnessSnapshot
    {
        public string Mood { get; set; } = "glowing";
        public string? StartedEatingAt { get; set; }
        public string? StoppedFastingAt { get; set; }
        public int MealsToday { get; set; }
        public HandPortions Portions { get; set; } = new HandPortions();
        public int Waters { get; set; }
        public int StepGoal { get; set; }
        public int DailySteps { get; set; }
        public string SelectedPaceKey { get; set; } = "medium";
        public string SelectedPaceLabel { get; set; } = "Medium Walk";
        public int SelectedPaceSpm { get; set; } = 105;
        public bool SessionActive { get; set; }
        public int SessionSteps { get; set; }
        public int SessionSeconds { get; set; }
        public List<StepSessionRecord> Sessions { get; set; } = new List<StepSessionRecord>();
        public int TodayDayIndex { get; set; }
        public Dictionary<string, bool[]> Habits { get; set; } = new Dictionary<string, bool[]>();
        public string StatusMessage { get; set; } = "Connected to C#!";
    }

    // =========================================================================
    // 2. CORE C# WELLNESS ENGINE
    // =========================================================================
    public class LayaLagsikEngine
    {
        private readonly object _sync = new object();

        public string Mood { get; private set; } = "glowing";
        public string? StartedEatingAt { get; private set; }
        public string? StoppedFastingAt { get; private set; }
        public int MealsToday { get; private set; } = 2;

        public HandPortions Portions { get; } = new HandPortions();
        public int Waters { get; private set; } = 4;

        public int StepGoal { get; private set; } = 10000; // 10,000 minimum step goal
        public int DailySteps { get; private set; } = 0;
        public string SelectedPaceKey { get; private set; } = "medium";
        public bool SessionActive { get; private set; } = false;
        public int SessionSteps { get; private set; } = 0;
        public int SessionSeconds { get; private set; } = 0;
        public List<StepSessionRecord> Sessions { get; } = new List<StepSessionRecord>();

        public Dictionary<string, bool[]> Habits { get; } = new Dictionary<string, bool[]>
        {
            ["fast"] = new bool[7],
            ["plate"] = new bool[7],
            ["steps"] = new bool[7],
            ["water"] = new bool[7]
        };

        public string LastMessage { get; private set; } = "Malaya sa pressure, lagsik sa matag adlaw.";

        public static int GetTodayDayIndex()
        {
            int d = (int)DateTime.Now.DayOfWeek;
            return d == 0 ? 6 : d - 1;
        }

        private static (PaceMode mode, string label, int spm) ResolvePace(string key)
        {
            return (key ?? "medium").ToLowerInvariant() switch
            {
                "still" => (PaceMode.StandingStill, "Standing Still", 0),
                "slow" => (PaceMode.SlowWalk, "Slow Walk", 80),
                "fast" => (PaceMode.FastBrisk, "Fast / Brisk", 125),
                "jogging" => (PaceMode.Jogging, "Jogging", 160),
                _ => (PaceMode.MediumWalk, "Medium Walk", 105)
            };
        }

        private void SyncTodayHabits()
        {
            int today = GetTodayDayIndex();
            Habits["plate"][today] = Portions.Total >= 3;
            Habits["steps"][today] = DailySteps >= StepGoal;
            Habits["water"][today] = Waters >= 8;
            if (!string.IsNullOrEmpty(StartedEatingAt) || !string.IsNullOrEmpty(StoppedFastingAt))
            {
                Habits["fast"][today] = true;
            }
        }

        public WellnessSnapshot GetSnapshot()
        {
            lock (_sync)
            {
                SyncTodayHabits();
                var paceInfo = ResolvePace(SelectedPaceKey);
                return new WellnessSnapshot
                {
                    Mood = Mood,
                    StartedEatingAt = StartedEatingAt,
                    StoppedFastingAt = StoppedFastingAt,
                    MealsToday = MealsToday,
                    Portions = new HandPortions
                    {
                        Protein = Portions.Protein,
                        Veggies = Portions.Veggies,
                        Carbs = Portions.Carbs,
                        Fats = Portions.Fats
                    },
                    Waters = Waters,
                    StepGoal = StepGoal,
                    DailySteps = DailySteps,
                    SelectedPaceKey = SelectedPaceKey,
                    SelectedPaceLabel = paceInfo.label,
                    SelectedPaceSpm = paceInfo.spm,
                    SessionActive = SessionActive,
                    SessionSteps = SessionSteps,
                    SessionSeconds = SessionSeconds,
                    Sessions = Sessions.Take(6).ToList(),
                    TodayDayIndex = GetTodayDayIndex(),
                    Habits = Habits.ToDictionary(k => k.Key, v => (bool[])v.Value.Clone()),
                    StatusMessage = LastMessage
                };
            }
        }

        public WellnessSnapshot ApplyAction(string action, Dictionary<string, string> query)
        {
            lock (_sync)
            {
                switch ((action ?? "").ToLowerInvariant())
                {
                    case "start_eating":
                        StartedEatingAt = DateTime.Now.ToString("hh:mm tt");
                        LastMessage = $"Started eating at {StartedEatingAt}";
                        break;

                    case "stop_fasting":
                        StoppedFastingAt = DateTime.Now.ToString("hh:mm tt");
                        LastMessage = $"Stopped fasting at {StoppedFastingAt}";
                        break;

                    case "clear_timer":
                        StartedEatingAt = null;
                        StoppedFastingAt = null;
                        LastMessage = "Timer timestamps cleared";
                        break;

                    case "adjust_portion":
                        string key = query.GetValueOrDefault("key") ?? "";
                        int delta = int.TryParse(query.GetValueOrDefault("delta"), out var pd) ? pd : 0;
                        if (key == "protein") Portions.Protein = Math.Clamp(Portions.Protein + delta, 0, 6);
                        else if (key == "veggies") Portions.Veggies = Math.Clamp(Portions.Veggies + delta, 0, 6);
                        else if (key == "carbs") Portions.Carbs = Math.Clamp(Portions.Carbs + delta, 0, 6);
                        else if (key == "fats") Portions.Fats = Math.Clamp(Portions.Fats + delta, 0, 6);
                        LastMessage = $"Updated {key} portion";
                        break;

                    case "set_water":
                        int count = int.TryParse(query.GetValueOrDefault("count"), out var wc) ? wc : 0;
                        Waters = Math.Clamp(count, 0, 8);
                        LastMessage = $"Hydration droplets: {Waters}/8";
                        break;

                    case "reset_plate":
                        Portions.Protein = 0;
                        Portions.Veggies = 0;
                        Portions.Carbs = 0;
                        Portions.Fats = 0;
                        Waters = 0;
                        LastMessage = "Plate & water reset";
                        break;

                    // Pick pace mode first (does NOT auto-start until start_session is pressed)
                    case "select_pace":
                        string paceKey = query.GetValueOrDefault("pace") ?? "medium";
                        SelectedPaceKey = paceKey;
                        var info = ResolvePace(paceKey);
                        LastMessage = SessionActive
                            ? $"Switched live pace to {info.label}"
                            : $"{info.label} picked — press Start Session when ready!";
                        break;

                    case "start_session":
                        SessionActive = true;
                        SessionSteps = 0;
                        SessionSeconds = 0;
                        var activeInfo = ResolvePace(SelectedPaceKey);
                        LastMessage = $"Session started in {activeInfo.label} mode!";
                        break;

                    case "tick_session":
                        if (SessionActive)
                        {
                            int secs = int.TryParse(query.GetValueOrDefault("seconds"), out var ts) ? Math.Max(1, ts) : 1;
                            var tickInfo = ResolvePace(SelectedPaceKey);
                            SessionSeconds += secs;
                            int added = (int)Math.Round((tickInfo.spm / 60.0) * secs);
                            SessionSteps += added;
                            DailySteps += added;
                        }
                        break;

                    case "add_steps":
                        int manual = int.TryParse(query.GetValueOrDefault("steps"), out var ms) ? Math.Max(1, ms) : 1;
                        DailySteps += manual;
                        if (SessionActive) SessionSteps += manual;
                        LastMessage = $"+{manual} steps added!";
                        break;

                    case "save_session":
                        if (SessionActive)
                        {
                            var savedInfo = ResolvePace(SelectedPaceKey);
                            SessionActive = false;
                            if (SessionSteps > 0 || SessionSeconds > 0)
                            {
                                Sessions.Insert(0, new StepSessionRecord
                                {
                                    PaceLabel = savedInfo.label,
                                    Steps = SessionSteps,
                                    DurationSeconds = SessionSeconds,
                                    LoggedAt = DateTime.Now.ToString("hh:mm tt")
                                });
                            }
                            LastMessage = $"Saved {SessionSteps:N0} steps in C#!";
                            SessionSteps = 0;
                            SessionSeconds = 0;
                        }
                        break;

                    case "restart_steps":
                        SessionActive = false;
                        SessionSteps = 0;
                        SessionSeconds = 0;
                        DailySteps = 0;
                        LastMessage = "Steps restarted back to 0";
                        break;

                    case "toggle_habit":
                        string habitKey = query.GetValueOrDefault("habit") ?? "";
                        int dayIdx = int.TryParse(query.GetValueOrDefault("day"), out var di) ? di : -1;
                        int today = GetTodayDayIndex();
                        if (dayIdx != today)
                        {
                            LastMessage = "Locked! Only Today can be edited.";
                        }
                        else if (Habits.ContainsKey(habitKey))
                        {
                            Habits[habitKey][today] = !Habits[habitKey][today];
                            LastMessage = $"Updated Today's {habitKey} habit";
                        }
                        break;
                }

                SyncTodayHabits();
                return GetSnapshot();
            }
        }
    }

    // =========================================================================
    // 3. BUILT-IN C# WEB SERVER (RUNS DIRECTLY FROM YOUR LAYA FOLDER)
    // =========================================================================
    public static class Program
    {
        private static readonly LayaLagsikEngine Engine = new LayaLagsikEngine();

        public static async Task Main(string[] args)
        {
            string url = "http://localhost:5080/";
            using var listener = new HttpListener();
            listener.Prefixes.Add(url);

            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not start server at {url}: {ex.Message}");
                return;
            }

            Console.WriteLine("=================================================");
            Console.WriteLine("  Laya & Lagsik — Running in C# (laya folder)");
            Console.WriteLine($"  Open in your browser: {url}");
            Console.WriteLine("  Press Ctrl+C to stop.");
            Console.WriteLine("=================================================");

            while (true)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(ctx));
            }
        }

        private static async Task HandleRequestAsync(HttpListenerContext ctx)
        {
            try
            {
                string path = ctx.Request.Url?.AbsolutePath ?? "/";
                var query = ParseQueryString(ctx.Request.Url?.Query ?? "");

                if (path == "/api/state")
                {
                    await WriteJsonAsync(ctx.Response, Engine.GetSnapshot());
                    return;
                }

                if (path == "/api/action")
                {
                    string action = query.GetValueOrDefault("name") ?? "";
                    await WriteJsonAsync(ctx.Response, Engine.ApplyAction(action, query));
                    return;
                }

                byte[] htmlBytes = Encoding.UTF8.GetBytes(BuildHtmlPage());
                ctx.Response.ContentType = "text/html; charset=utf-8";
                ctx.Response.ContentLength64 = htmlBytes.Length;
                await ctx.Response.OutputStream.WriteAsync(htmlBytes);
                ctx.Response.Close();
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = 500;
                byte[] err = Encoding.UTF8.GetBytes($"Server error: {ex.Message}");
                await ctx.Response.OutputStream.WriteAsync(err);
                ctx.Response.Close();
            }
        }

        private static async Task WriteJsonAsync(HttpListenerResponse response, object payload)
        {
            string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes);
            response.Close();
        }

        private static Dictionary<string, string> ParseQueryString(string rawQuery)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(rawQuery)) return dict;
            foreach (string pair in rawQuery.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                dict[Uri.UnescapeDataString(parts[0])] = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
            }
            return dict;
        }

        private static string BuildHtmlPage()
        {
            return """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Laya & Lagsik — Powered by C#</title>
  <script src="https://cdn.tailwindcss.com"></script>
  <link href="https://fonts.googleapis.com/css2?family=Fredoka:wght@600;700&family=Plus+Jakarta+Sans:wght@500;600;700;800&display=swap" rel="stylesheet" />
  <script>
    tailwind.config = {
      theme: {
        extend: {
          fontFamily: {
            script: ['"Fredoka"', 'sans-serif'],
            sans: ['"Plus Jakarta Sans"', 'sans-serif'],
          },
          colors: {
            warmcream: '#FFFDF7',
            sand: '#F5EFE6',
            malachite: '#15484C',
            coral: '#FF7A59',
            marigold: '#FFA20C',
            lagoon: '#30B8B2',
          }
        }
      }
    };
  </script>
</head>
<body class="min-h-screen bg-warmcream text-malachite font-sans pb-24">
  <header class="sticky top-0 z-20 bg-warmcream/90 backdrop-blur border-b border-sand px-4 py-3">
    <div class="max-w-xl mx-auto flex items-center justify-between">
      <div>
        <h1 class="font-script text-2xl text-malachite">🌴 Laya &amp; Lagsik</h1>
        <p class="text-xs opacity-75">C# Prototype (Program.cs)</p>
      </div>
      <span id="status-pill" class="px-3 py-1 rounded-full bg-lagoon/15 text-lagoon text-xs font-extrabold">
        Connected to C#
      </span>
    </div>
  </header>

  <main id="view" class="max-w-xl mx-auto p-4 space-y-4"></main>

  <nav class="fixed bottom-4 left-1/2 -translate-x-1/2 w-[92vw] max-w-md z-30">
    <div class="rounded-full bg-malachite p-1.5 shadow-xl flex gap-1">
      <button onclick="switchTab('timer')" id="tab-timer" class="flex-1 py-2 rounded-full text-xs font-extrabold text-warmcream">⏱️ Timer</button>
      <button onclick="switchTab('plate')" id="tab-plate" class="flex-1 py-2 rounded-full text-xs font-extrabold text-warmcream">🥗 Plate</button>
      <button onclick="switchTab('steps')" id="tab-steps" class="flex-1 py-2 rounded-full text-xs font-extrabold bg-marigold text-malachite">👟 Steps</button>
      <button onclick="switchTab('journey')" id="tab-journey" class="flex-1 py-2 rounded-full text-xs font-extrabold text-warmcream">🌷 Journey</button>
    </div>
  </nav>

  <script>
    let snap = null;
    let activeTab = 'steps';

    async function callApi(name, params = {}) {
      const qs = new URLSearchParams({ name, ...params }).toString();
      const res = await fetch('/api/action?' + qs);
      snap = await res.json();
      render();
    }

    async function boot() {
      const res = await fetch('/api/state');
      snap = await res.json();
      render();
      setInterval(async () => {
        if (snap && snap.sessionActive) {
          await callApi('tick_session', { seconds: 1 });
        }
      }, 1000);
    }

    function switchTab(t) {
      activeTab = t;
      ['timer', 'plate', 'steps', 'journey'].forEach(k => {
        const el = document.getElementById('tab-' + k);
        el.className = k === t
          ? 'flex-1 py-2 rounded-full text-xs font-extrabold bg-marigold text-malachite'
          : 'flex-1 py-2 rounded-full text-xs font-extrabold text-warmcream';
      });
      render();
    }

    function render() {
      if (!snap) return;
      document.getElementById('status-pill').textContent = snap.statusMessage;
      const view = document.getElementById('view');

      if (activeTab === 'timer') {
        view.innerHTML = `
          <div class="rounded-3xl bg-white border border-sand p-5 space-y-4 shadow-sm">
            <h2 class="font-script text-xl">Simple Meal & Rest Log</h2>
            <div class="grid grid-cols-2 gap-3">
              <div class="rounded-2xl bg-sand/70 p-3.5">
                <div class="text-xs opacity-70 font-bold">Started Eating At</div>
                <div class="text-lg font-extrabold mt-1">${snap.startedEatingAt || 'Not recorded'}</div>
              </div>
              <div class="rounded-2xl bg-sand/70 p-3.5">
                <div class="text-xs opacity-70 font-bold">Stopped Fasting At</div>
                <div class="text-lg font-extrabold mt-1">${snap.stoppedFastingAt || 'Not recorded'}</div>
              </div>
            </div>
            <div class="grid grid-cols-3 gap-2">
              <button onclick="callApi('start_eating')" class="py-3 rounded-2xl bg-malachite text-warmcream text-xs font-extrabold">Start Eating</button>
              <button onclick="callApi('stop_fasting')" class="py-3 rounded-2xl bg-lagoon text-white text-xs font-extrabold">Stop Fasting</button>
              <button onclick="callApi('clear_timer')" class="py-3 rounded-2xl bg-sand text-xs font-extrabold">Clear</button>
            </div>
          </div>`;
      } else if (activeTab === 'plate') {
        const items = [
          ['protein', '🍗 Protein (Palm)', snap.portions.protein],
          ['veggies', '🥬 Veggies (Fist)', snap.portions.veggies],
          ['carbs', '🍚 Carbs (Cupped)', snap.portions.carbs],
          ['fats', '🥑 Fats (Thumb)', snap.portions.fats],
        ];
        view.innerHTML = `
          <div class="rounded-3xl bg-white border border-sand p-5 space-y-4 shadow-sm">
            <div class="flex justify-between items-center">
              <h2 class="font-script text-xl">Hand-Portion Plate</h2>
              <button onclick="callApi('reset_plate')" class="px-3 py-1 rounded-xl bg-sand text-xs font-extrabold">Reset</button>
            </div>
            <div class="grid grid-cols-2 gap-3">
              ${items.map(([k, label, val]) => `
                <div class="rounded-2xl bg-sand/70 p-3.5 flex items-center justify-between">
                  <div>
                    <div class="text-xs font-extrabold">${label}</div>
                    <div class="font-script text-2xl">${val}</div>
                  </div>
                  <div class="flex gap-1.5">
                    <button onclick="callApi('adjust_portion', { key: '${k}', delta: -1 })" class="w-8 h-8 rounded-xl bg-white font-extrabold">−</button>
                    <button onclick="callApi('adjust_portion', { key: '${k}', delta: 1 })" class="w-8 h-8 rounded-xl bg-marigold font-extrabold">+</button>
                  </div>
                </div>
              `).join('')}
            </div>
            <div>
              <div class="text-xs font-extrabold mb-2">Hydration Droplets (${snap.waters}/8)</div>
              <div class="grid grid-cols-8 gap-1.5">
                ${Array.from({ length: 8 }, (_, i) => `
                  <button onclick="callApi('set_water', { count: ${i + 1 === snap.waters ? i : i + 1} })"
                    class="py-2 rounded-xl text-xs font-extrabold ${i < snap.waters ? 'bg-lagoon text-white' : 'bg-sand'}">
                    💧
                  </button>
                `).join('')}
              </div>
            </div>
          </div>`;
      } else if (activeTab === 'steps') {
        const paces = [
          ['still', '🧍 Still (0)'],
          ['slow', '🌿 Slow (80)'],
          ['medium', '🧭 Medium (105)'],
          ['fast', '⚡ Fast (125)'],
          ['jogging', '🏃 Jog (160)'],
        ];
        view.innerHTML = `
          <div class="rounded-3xl bg-white border border-sand p-5 space-y-4 shadow-sm">
            <div class="flex justify-between items-center">
              <div>
                <h2 class="font-script text-xl">10,000+ Steps</h2>
                <p class="text-xs opacity-75">Goal: ${snap.stepGoal.toLocaleString()} steps</p>
              </div>
              <button onclick="callApi('restart_steps')" class="px-3 py-1.5 rounded-xl bg-marigold/25 text-xs font-extrabold">Restart Steps</button>
            </div>
            <div class="grid grid-cols-2 gap-3 text-center">
              <div class="rounded-2xl bg-sand/70 p-3">
                <div class="text-xs font-bold opacity-70">Today's Steps</div>
                <div class="font-script text-3xl">${snap.dailySteps.toLocaleString()}</div>
              </div>
              <div class="rounded-2xl bg-sand/70 p-3">
                <div class="text-xs font-bold opacity-70">Session (${snap.sessionActive ? 'ACTIVE' : 'STANDBY'})</div>
                <div class="font-script text-3xl text-lagoon">${snap.sessionSteps.toLocaleString()}</div>
              </div>
            </div>
            <div class="space-y-1.5">
              <div class="text-[11px] font-extrabold uppercase opacity-75">1. Pick Mode First (Stays Standby)</div>
              <div class="grid grid-cols-5 gap-1.5">
                ${paces.map(([k, label]) => `
                  <button onclick="callApi('select_pace', { pace: '${k}' })"
                    class="p-2 rounded-xl text-[11px] font-extrabold border ${snap.selectedPaceKey === k ? 'bg-marigold/25 border-marigold' : 'bg-sand/60 border-transparent'}">
                    ${label}
                  </button>
                `).join('')}
              </div>
            </div>
            <div class="grid grid-cols-2 gap-2.5">
              <button onclick="callApi('start_session')" class="py-3 rounded-2xl bg-malachite text-warmcream text-xs font-extrabold">Start Session</button>
              <button onclick="callApi('save_session')" class="py-3 rounded-2xl bg-lagoon text-white text-xs font-extrabold">Save Session</button>
            </div>
            <div class="flex gap-2 justify-end">
              <button onclick="callApi('add_steps', { steps: 25 })" class="px-3 py-1.5 rounded-xl bg-sand text-xs font-extrabold">+25 Steps</button>
              <button onclick="callApi('add_steps', { steps: 250 })" class="px-3 py-1.5 rounded-xl bg-marigold/25 text-xs font-extrabold">+250 Steps</button>
            </div>
          </div>`;
      } else {
        const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
        const habits = [
          ['fast', '🌙 Rest / Fast'],
          ['plate', '🥗 Plate'],
          ['steps', '👟 10k+ Steps'],
          ['water', '💧 8 Waters'],
        ];
        view.innerHTML = `
          <div class="rounded-3xl bg-white border border-sand p-5 space-y-3 shadow-sm">
            <h2 class="font-script text-xl">Journey (Today = ${days[snap.todayDayIndex]} Unlocked)</h2>
            ${habits.map(([hk, label]) => `
              <div class="space-y-1">
                <div class="text-xs font-extrabold">${label}</div>
                <div class="grid grid-cols-7 gap-1.5">
                  ${days.map((d, idx) => {
                    const done = snap.habits[hk][idx];
                    const isToday = idx === snap.todayDayIndex;
                    return `<button onclick="callApi('toggle_habit', { habit: '${hk}', day: ${idx} })"
                      class="py-2 rounded-xl text-xs font-extrabold ${done ? 'bg-lagoon text-white' : isToday ? 'bg-sand border border-lagoon' : 'bg-sand/40 opacity-50'}">
                      ${d}<br/>${done ? '✓' : isToday ? '+' : '🔒'}
                    </button>`;
                  }).join('')}
                </div>
              </div>
            `).join('')}
          </div>`;
      }
    }

    boot();
  </script>
</body>
</html>
""";
        }
    }
}