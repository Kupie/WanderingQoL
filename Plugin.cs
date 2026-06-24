using System;
using System.IO;
using System.Text;
//StartScreen to modify NDA popup
using Assets.Scripts.Components.StartScreen;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Systems.PersistentProgress;
using UI.Components.StartScreen;
using UnityEngine;

namespace WanderingQoL
{
	[BepInPlugin(PluginGuid, PluginName, PluginVer)]
	[BepInProcess(PluginTargetExe)]
	[HarmonyPatch]

	public class Plugin : BaseUnityPlugin
	{
		public const string PluginGuid = "org.Kupie.WanderingQoL";
		public const string PluginName = "WanderingQoL";
		public const string PluginVer = "1.0.5";
		public const string PluginTargetExe = "WanderingVillage.exe";
		internal static ManualLogSource Log;


		// Config Bindings
		internal static ConfigEntry<bool> SkipWelcome;
		internal static ConfigEntry<bool> RemoveSocials;
		internal static ConfigEntry<bool> SkipSurvey;
		internal static ConfigEntry<bool> MuteOnUnfocus;
		internal static ConfigEntry<bool> AutoAcceptNomads;
		internal static ConfigEntry<KeyCode> MinusWorker;
		internal static ConfigEntry<KeyCode> PlusWorker;
		internal static ConfigEntry<KeyCode> MuteHotkey;
		internal static ConfigEntry<int> MaxSpeed;
		internal static ConfigEntry<int> ExtraWorkerLimits;
		internal static ConfigEntry<float> WorkerSpeedModVar;
		internal static ConfigEntry<bool> UnlockHostilityModifiers;
		internal static ConfigEntry<bool> AutoHarvestMushrooms;
		internal static ConfigEntry<bool> AutoHarvestBushes;
		internal static ConfigEntry<bool> AutoHarvestBushesUproot;
		internal static ConfigEntry<bool> AllBuildingsMovable;
		internal static ConfigEntry<bool> DebugLogging;


		private void Awake()
		{
			var Harmony = new Harmony(PluginGuid);
			// Plugin startup logic
			Log = new ManualLogSource(PluginName);
			BepInEx.Logging.Logger.Sources.Add(Log);
			Log.LogInfo($"Plugin {PluginName} is loaded!");

			//General
			SkipWelcome = Config.Bind("1. General", "Remove Welcome", true, "Remove Welcome Screen");
			RemoveSocials = Config.Bind("1. General", "Remove Socials", true, "Removes Social Media links");
			SkipSurvey = Config.Bind("1. General", "Skip Survey on Quit", true, "Skips the 'take a survey' screen upon quitting");
			MuteOnUnfocus = Config.Bind("1. General", "Mute On Unfocus", true, "Mute when Game Loses Focus");

			MuteHotkey = Config.Bind("1. General", "Game Mute Hotkey with CTRL", KeyCode.M, "Holding CTRL + This key will mute/unmute the game");

			PlusWorker = Config.Bind("1. General", "Hotkey to add a worker", KeyCode.X, "Hotkey to Add a Worker");
			MinusWorker = Config.Bind("1. General", "Hotkey to subtract a worker", KeyCode.Z, "Hotkey to Subtract a Worker");


			MaxSpeed = Config.Bind("1. General", "Max Speed", 4, "Maximum Speed the game will let you run it at");

			AutoAcceptNomads = Config.Bind("1. General", "Auto Accept Nomads", true, "Automatically accept nomads without prompting.");
			AutoHarvestMushrooms = Config.Bind("1. General", "Auto Harvest Mushrooms", true, "Automatically mark fully grown wild mushrooms (outside farming zones) for harvesting.");
			AutoHarvestBushes = Config.Bind("1. General", "Auto Harvest Bushes", true, "Automatically mark fully grown wild bushes (outside farming zones) for harvesting.");
			AutoHarvestBushesUproot = Config.Bind("1. General", "Also Uproot Autoharvest Bushes", true, "Also Uproot the auto-harvested bushes for seeds/replanting");

			UnlockHostilityModifiers = Config.Bind("1. General", "Force Unlock Hostility modifiers", false, "Unlocks hostility modifiers even if you haven't beat the game");

			//Cheats
			ExtraWorkerLimits = Config.Bind("2. Cheats", "Extra Workers Max", 0, "Allow this many extra workers per building. Set to 0 to disable.");
			WorkerSpeedModVar = Config.Bind("2. Cheats", "Worker Move Speed Multiplier", 1.0f, "Modifier worker move speed by this amount. '2' would make them move 2x as fast.");


			AllBuildingsMovable = Config.Bind("1. General", "All Buildings Movable", true, "Allows all buildings to be moved, even ones that normally cannot be.");

			//Debug
			DebugLogging = Config.Bind("3. Debug", "Debug Logging", false, "Enables Debug Logging");

			Harmony.PatchAll();
			if (AutoHarvestMushrooms.Value || AutoHarvestBushes.Value)
				InvokeRepeating(nameof(AutoHarvestUpdate), 5f, 1f);
			//Skip intros... some day :'(
			//UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/Startscreen", UnityEngine.SceneManagement.LoadSceneMode.Single);

		}

		unsafe void AutoHarvestUpdate()
		{
			World world = Game.Data.World;
			if (world == null) return;

			for (int i = 0; i < world.FieldOccupants.Count; i++)
			{
				FieldOccupant occupant = world.FieldOccupants.GetByIndex(i);
				if (occupant.Type != OccupantType.Plant) continue;
				if (!occupant.HasFunction(PlantFunction.Collectable)) continue;
				if (!occupant.Settings.Plant.PlantType.Has(PlantFilterType.Crop)) continue;
				string plantName = occupant.Settings.Name.ToString();
				if (occupant.State != FieldOccupantState.Complete) continue;
				if (occupant.Data->MarkedForCollection) continue;
				if (occupant.Data->CollectionJobTaken) continue;
				if (world.Field(occupant.Position)->PlotID != FieldOccupantID.None) continue;
				
				if ((plantName == "Mushroom" || plantName == "Decayed Mushroom") &&  AutoHarvestMushrooms.Value) {
					FieldOccupantSystem.MarkForCollection(world, occupant, true, 3, false, true);
					D.L("Auto-harvesting mushroom at " + occupant.Position.ToString(), "info");
				}
				if (plantName == "Berry Bush" &&  AutoHarvestBushes.Value) {
					FieldOccupantSystem.MarkForCollection(world, occupant, true, 3, AutoHarvestBushesUproot.Value, true);
					D.L("Auto-harvesting berry bush at " + occupant.Position.ToString(), "info");
				}

				
			}
		}

		// Debug Logging system
		public class D
		{
			public static void L(string message, string type)
			{
				if (DebugLogging.Value && (type == "info"))
				{
					Log.LogInfo(message);
				}
				if (DebugLogging.Value && (type == "error"))
				{
					Log.LogError(message);
				}
				if (DebugLogging.Value && (type == "warn"))
				{
					Log.LogWarning(message);
				}
			}
		}


		// Reload Config patch
		//[HarmonyPatch(typeof(Game), nameof(Game.Update))]
		//class Reloadconfig
		//{
		//    static void Postfix()
		//    {
		//        // If F5 hotkey is pressed, toggle Forced Mute
		//        if (Input.GetKeyDown(KeyCode.F5))
		//        {
		//            ConfigFile.Reload();
		//        }
		//    }
		//}


		//Automute patch. This postfix runs after the main "Game.Update" method, which runs every frame (I think) so it's a good one to hook into.
		[HarmonyPatch(typeof(Game), nameof(Game.Update))]
		class GameMuter
		{
			public static bool MuteForced = false;
			static void Postfix()
			{
				// If Ctrl + Mute hotkey is pressed, toggle Forced Mute
				if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(MuteHotkey.Value))
				{
					MuteForced = !MuteForced;
					D.L($"Forced Mute set to {MuteForced}", "info");

				}

				if (MuteForced && !AudioListener.pause) // If mute is forced but audio not paused, then pause audio
				{
					AudioListener.pause = true;
					D.L($"Audio Force Paused", "info");

				}
				else if (!MuteForced && Plugin.MuteOnUnfocus.Value) // Otherwise go through auto-unmute logic if feature is enabled
				{
					// If application is focused but Audio is paused then unpause audio
					if (Application.isFocused && AudioListener.pause)
					{
						//Console.WriteLine("Focused");
						AudioListener.pause = false;
						D.L("Audio Auto Unpaused", "info");

					}
					// If app is unfocused but Audio is not paused, then pause audio
					else if (!Application.isFocused && !AudioListener.pause)
					{
						//Console.WriteLine("Not Focused");
						AudioListener.pause = true;
						D.L("Audio Auto Paused", "info");
					}
				}
				else if (!MuteForced && AudioListener.pause)// Else if audio is paused but forced mute is false, then unpause
				{
					AudioListener.pause = false;
					D.L("Audio force Unpaused", "info");
				}


			}

		}

		// Disabling bug report button. 
		// No I'm not disabling this feature. If a bug happens while using mods, I don't want to bother the
		// game devs when it could be the mod's fault

		[HarmonyPatch(typeof(TopBarDisplay), nameof(TopBarDisplay.UpdateUi))]
		class HideBugReport
		{
			static void Postfix(TopBarDisplay __instance)
			{
				if (__instance.UserReportButton.isActiveAndEnabled)
				{
					D.L("Disabling User Report Button", "info");
					__instance.UserReportButton.SetActive(false);
				}
			}
		}

		//Hiding Socials Buttons from start screen
		[HarmonyPatch(typeof(StartScreenUi), nameof(StartScreenUi.Update))]
		class HideSocials1
		{
			static bool alreadyHidWelcomeScreen;
			static void Postfix(StartScreenUi __instance)
			{
				if (RemoveSocials.Value && (__instance.DiscordButton.isActiveAndEnabled || __instance.FollowButton.isActiveAndEnabled || __instance.RoadmapButton.isActiveAndEnabled))
				{

					D.L("Removing Socials Buttons off startup...", "info");
					__instance.RoadmapButton.SetActive(false);
					__instance.FollowButton.SetActive(false);
					__instance.DiscordButton.SetActive(false);
					D.L("Finding ButtonGuide_set...", "info");

					if (Plugin.SkipWelcome.Value && !alreadyHidWelcomeScreen)
					{
						GameObject welcomePanelFind = GameObject.Find("WelcomePanel");
						if (welcomePanelFind != null && welcomePanelFind.activeSelf)
						{
							Plugin.D.L("Hiding ButtonGuide_set!", "info");
							welcomePanelFind.SetActive(false);
							alreadyHidWelcomeScreen = true;
						}

					}
				}
				if (SkipSurvey.Value && Game.Settings.ActiveDemoSettings.ShowSurveyOnLeave)
				{
					Plugin.D.L("Setting ShowSurveyOnLeave to false", "info");
					Game.Settings.ActiveDemoSettings.ShowSurveyOnLeave = false;
				}


			}
		}
		//Hiding Socials off Pause Menu
		[HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.UpdateUi))]
		class HideSocials2
		{

			static void Postfix(PauseMenu __instance)
			{
				if (RemoveSocials.Value && (__instance.DiscordButton.isActiveAndEnabled || __instance.FeedbackButton.isActiveAndEnabled))
				{
					D.L("Removing Socials Buttons off pause menu...", "info");
					__instance.DiscordButton.SetActive(false);
					__instance.FeedbackButton.SetActive(false);
				}
			}
		}

		////Unlock Hostility Modifiers
		//[HarmonyPatch(typeof(DifficultyModifiersPanel), nameof(DifficultyModifiersPanel.AbsoluteMaxDifficulty), MethodType.Getter)]
		//class UnlockHostilityModifiersClass
		//{
		//    static void Postfix(ref int __result)
		//    {
		//        D.L("Current Max Difficulty: " + __result, "info");
		//        __result = 100;
		//    }
		//}

		//[HarmonyPatch(typeof(PersistentProgressSaveDataExtensions), nameof(PersistentProgressSaveDataExtensions.ShouldAutoUnlockDifficultyModifiers))]
		//class UnlockHostilityModifiersClass2
		//{
		//
		//    static void Prefix()
		//    {
		//        if (Plugin.UnlockHostilityModifiers.Value && !PersistentProgressSaveSystem.SaveData.AreDifficultyModifiersUnlocked)
		//        {
		//            D.L("Patching ShouldAutoUnlockDifficultyModifiers", "info");
		//            PersistentProgressSaveSystem.SaveData.AreDifficultyModifiersUnlocked = true;
		//        }
		//    }
		//}

		// Game speed mod
		[HarmonyPatch(typeof(TimeSystem), nameof(TimeSystem.GetMaxSpeed))]
		class TimeSystemMod
		{
			static void Postfix(ref int __result)
			{
				if (!Game.Data.DebugData.UnlockTimeScale)
				{
					__result = MaxSpeed.Value;
				}
			}
		}

		// Worker Movespeed modifier
		[HarmonyPatch(typeof(Worker), nameof(Worker.GetMovementSpeed))]
		class WorkerSpeedMod
		{
			static void Postfix(ref float __result)
			{
				__result *= WorkerSpeedModVar.Value;
			}
		}

		[HarmonyPatch(typeof(Game), nameof(Game.Update))]
		class AutoAcceptNomadsMod
		{
			static void Postfix()
			{
				if (!Plugin.AutoAcceptNomads.Value) return;
				try
				{
					UpkeepSystem.TryAcceptReproductionSpawnedWorkers(Game.Data.World);
				}
				catch (Exception e) { }
			}
		}

		[HarmonyPatch(typeof(WorkerSection), nameof(WorkerSection.UpdateUi))]
		class PlusMinusHotkeys
		{
			static void Postfix(ref FieldOccupant occupant, WorkerSection __instance)
			{
				bool flag = __instance._fullStaff;
				int num = flag ? __instance._maxWorkerCount : 1;
				if (Input.GetKeyUp(Plugin.PlusWorker.Value))
				{
					occupant.TargetWorkerCount = (byte)Mathf.Min((int)occupant.TargetWorkerCount + num, __instance._maxWorkerCount);
					D.L("Updated Max Workers: " + occupant.TargetWorkerCount.ToString(), "info");
				}
				if (Input.GetKeyUp(Plugin.MinusWorker.Value))
				{
					occupant.TargetWorkerCount = (byte)Mathf.Max((int)occupant.TargetWorkerCount - num, 0);
					D.L("Updated Max Workers: " + occupant.TargetWorkerCount.ToString(), "info");
				}
			}
		}

		// Skips the startup splash video (studio logo on launch)
		[HarmonyPatch(typeof(VideoPanel), nameof(VideoPanel.ShowStartUp))]
		class SkipStartupVideo
		{
			static bool Prefix(Action onComplete)
			{
				if (!Plugin.SkipWelcome.Value) return true;
				onComplete?.Invoke();
				return false;
			}
		}
		[HarmonyPatch(typeof(Game), nameof(Game.Awake))]
		class PatchBuildingSettings
		{
			static bool _patched = false;
			static void Postfix()
			{
				if (_patched) return;
				_patched = true;
				foreach (OccupantSettings occupant in Game.Settings.OccupantSettings)
				{
					if (occupant.Type != OccupantType.Building) continue;
					if (Plugin.AllBuildingsMovable.Value)
						occupant.Building.CanBeMoved = true;
				}
			}
		}

		[HarmonyPatch(typeof(Game), nameof(Game.Awake))]
		class PatchMaxWorkerCounts
		{
			static bool _patched = false;
			static void Postfix()
			{
				if (_patched) return;
				_patched = true;
				if (Plugin.ExtraWorkerLimits.Value <= 0) return;
				foreach (OccupantSettings occupant in Game.Settings.OccupantSettings)
				{
					if (occupant.Type != OccupantType.Building) continue;
					BuildingWorkerCountSettings current = occupant.Building.WorkerCountSettings.DefaultValue;
					if (current.MaxWorkerCount <= 1) continue;
					bool blacklisted = occupant.Building.HasFunction(BuildingFunction.Research) ||
									   occupant.Building.HasFunction(BuildingFunction.RadioTower) ||
									   occupant.Building.FullStaffRequired();
					if (blacklisted) continue;

					occupant.Building.WorkerCountSettings.DefaultValue = new BuildingWorkerCountSettings
					{
						MinWorkerCount = current.MinWorkerCount,
						MaxWorkerCount = (byte)Mathf.Min(current.MaxWorkerCount + Plugin.ExtraWorkerLimits.Value, 13)
					};
				}
			}
		}

		// Dump occupants so I know how to analyze them in other code
		//		[HarmonyPatch(typeof(Game), nameof(Game.Update))]
		//		class DumpOccupantSettingsCSV
		//		{
		//			static bool _dumped = false;
		//
		//			static void Postfix()
		//			{
		//				if (_dumped) return;
		//				if (Game.Data.World == null) return;
		//				_dumped = true;
		//
		//				try
		//				{
		//					string path = Path.Combine(Paths.BepInExRootPath, "OccupantDump.csv");
		//					StringBuilder sb = new StringBuilder();
		//					sb.AppendLine("Name,OccupantType,PlantFilterType,PlantFunctions,BuildingFunctions,MinWorkers,MaxWorkers,FullStaffRequired,Collectable,HasLifeCycle,HasMultipleCollectCycles,CanBeDeconstructed,CanBeMoved");
		//
		//					foreach (OccupantSettings settings in Game.Settings.OccupantSettings)
		//					{
		//						string name = settings.Name.ToString();
		//						string occupantType = settings.Type.ToString();
		//						string plantFilterType = "";
		//						string plantFunctions = "";
		//						string buildingFunctions = "";
		//						string minWorkers = "";
		//						string maxWorkers = "";
		//						string fullStaffRequired = "";
		//						string collectable = "";
		//						string hasLifeCycle = "";
		//						string hasMultipleCollectCycles = "";
		//						string canBeDeconstructed = "";
		//						string canBeMoved = "";
		//
		//						if (settings.Type == OccupantType.Plant)
		//						{
		//							plantFilterType = settings.Plant.PlantType.ToString();
		//							plantFunctions = settings.Plant.Function.ToString();
		//							collectable = settings.Plant.HasFunction(PlantFunction.Collectable).ToString();
		//							hasLifeCycle = settings.Plant.HasFunction(PlantFunction.LifeCycle).ToString();
		//							if (settings.Plant.HasFunction(PlantFunction.Collectable))
		//								hasMultipleCollectCycles = settings.Plant.Collectable.HasMultipleCollectCycles.ToString();
		//						}
		//						else if (settings.Type == OccupantType.Building)
		//						{
		//							buildingFunctions = settings.Building.Function.ToString();
		//							BuildingWorkerCountSettings workerCount = settings.Building.WorkerCountSettings.DefaultValue;
		//							minWorkers = workerCount.MinWorkerCount.ToString();
		//							maxWorkers = workerCount.MaxWorkerCount.ToString();
		//							fullStaffRequired = settings.Building.FullStaffRequired().ToString();
		//							canBeDeconstructed = settings.Building.CanBeDeconstructed.ToString();
		//							canBeMoved = settings.Building.CanBeMoved.ToString();
		//						}
		//
		//						sb.AppendLine(string.Join(",", new[]
		//						{
		//					"\"" + name + "\"",
		//					occupantType,
		//					plantFilterType,
		//					"\"" + plantFunctions + "\"",
		//					"\"" + buildingFunctions + "\"",
		//					minWorkers,
		//					maxWorkers,
		//					fullStaffRequired,
		//					collectable,
		//					hasLifeCycle,
		//					hasMultipleCollectCycles,
		//					canBeDeconstructed,
		//					canBeMoved
		//				}));
		//					}
		//
		//					File.WriteAllText(path, sb.ToString());
		//					Log.LogInfo("Occupant dump written to: " + path);
		//				}
		//				catch (Exception e)
		//				{
		//					Log.LogError("OccupantDump failed: " + e);
		//				}
		//			}
		//		}
	}
}