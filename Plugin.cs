using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
//StartScreen to modify NDA popup
using Assets.Scripts.Components.StartScreen;
using Systems.PersistentProgress;
using System;
using UI.Components.StartScreen;

namespace WanderingQoL
{
    [BepInPlugin(PluginGuid, PluginName, PluginVer)]
    [BepInProcess(PluginTargetExe)]
    [HarmonyPatch]

    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "org.Kupie.WanderingQoL";
        public const string PluginName = "WanderingQoL";
        public const string PluginVer = "1.0.4";
        public const string PluginTargetExe = "WanderingVillage.exe";
        internal static ManualLogSource Log;
        

        // Config Bindings
        internal static ConfigEntry<bool> SkipWelcome;
        internal static ConfigEntry<bool> RemoveSocials;
        internal static ConfigEntry<bool> SkipSurvey;
        internal static ConfigEntry<bool> MuteOnUnfocus;
        internal static ConfigEntry<KeyCode> MinusWorker;
        internal static ConfigEntry<KeyCode> PlusWorker;
        internal static ConfigEntry<KeyCode> MuteHotkey;
        internal static ConfigEntry<int> MaxSpeed;
        internal static ConfigEntry<bool> ExtraWorkerLimits;
        internal static ConfigEntry<float> WorkerSpeedModVar;
        internal static ConfigEntry<bool> UnlockHostilityModifiers;
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

            UnlockHostilityModifiers = Config.Bind("1. General", "Force Unlock Hostility modifiers", false, "Unlocks hostility modifiers even if you haven't beat the game");

            //Cheats
            ExtraWorkerLimits = Config.Bind("2. Cheats", "Extra Workers", false, "Allow up to 13 workers per building");
            WorkerSpeedModVar = Config.Bind("2. Cheats", "Worker Move Speed Multiplier", 1.0f, "Modifier worker move speed by this amount. '2' would make them move 2x as fast.");
            
            //Debug
            DebugLogging = Config.Bind("3. Debug", "Debug Logging", false, "Enables Debug Logging");

            Harmony.PatchAll();
            //Skip intros... some day :'(
            //UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/Startscreen", UnityEngine.SceneManagement.LoadSceneMode.Single);

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
            // Some day maybe I'll figure out having this work, that way we
            // won't do the check above a bazillion times per frame
            //           void OnApplicationFocus(bool hasFocus)
            //           {
            //               AudioListener.pause = !hasFocus;
            //               Console.WriteLine("Game Lost/Gained Focus");
            //           }
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
        class AutoAcceptNomads
        {
            static void Postfix()
            {
                try
                {
                    UpkeepSystem.TryAcceptReproductionSpawnedWorkers(Game.Data.World);
                }
                catch (Exception e) { }
            }
        }

        // Trying to implement "cheats" portion into GUI...
        //[HarmonyPatch(typeof(WorkerSection), nameof(WorkerSection.Init))]

        //class WorkerOverride
        //{
        //    
        //    static void Postfix(World world, FieldOccupant occupant, DetailDisplaySectionOccupant thatBase, WorkerSection thatThis)
        //    {
        //        
        //    thatBase.Init(world, occupant);
        //        WorkerIcon[] componentsInChildren = thatThis.WorkerIconContainer.GetComponentsInChildren<WorkerIcon>();
        //        for (int i = 0; i < componentsInChildren.Length; i++)
        //        {
        //            UnityEngine.Object.Destroy(componentsInChildren[i].gameObject);
        //        }
        //        int minWorkerCount = (int)occupant.Settings.Building.MinWorkerCount;
        //        int maxWorkerCount = (int)occupant.Settings.Building.MaxWorkerCount;
        //
        //        thatThis._workerIcons = new List<WorkerIcon>();
        //        for (int j = 0; j < maxWorkerCount; j++)
        //        {
        //            WorkerIcon workerIcon = UnityEngine.Object.Instantiate<WorkerIcon>(this.WorkerIconPrefab, this.WorkerIconContainer);
        //            workerIcon.Init();
        //            thatThis._workerIcons.Add(workerIcon);
        //        }
        //        thatThis.FullStaffRequiredWarning.SetActive(minWorkerCount >= maxWorkerCount);
        //        thatThis._focusWorkedIndex = 0;
        //    }
        //
        //}

        // PlusMinus hotkeys stuff
        [HarmonyPatch(typeof(TitleSectionOccupant), nameof(TitleSectionOccupant.UpdateUi))]
        class DrawWorkerCount
        {
            static void Postfix(ref FieldOccupant occupant, TitleSectionOccupant __instance)
            {
                if (Plugin.ExtraWorkerLimits.Value && occupant.TargetWorkerCount > 1) {
                    string stringToAdd = " (" + occupant.WorkerCount + "/" + occupant.TargetWorkerCount.ToString() + ")";
                    if (occupant.HasFunction(BuildingFunction.RadioTower) && occupant.State == FieldOccupantState.Complete)
                    {
                        __instance.Name.SetLocalizedString(occupant.Settings.Building.RadioTower.CompletedNameOverride.ToString() + stringToAdd);
                    }
                    else
                    {
                        __instance.Name.SetLocalizedString(occupant.Settings.Name.ToString() + stringToAdd);
                    }
            
                }
            }
        }
        [HarmonyPatch(typeof(WorkerSection), nameof(WorkerSection.UpdateUi))]
        class PlusMinusHotkeys
        {
            static void Postfix(ref FieldOccupant occupant, WorkerSection __instance)
            {
                //bool flag = occupant.FullStaffRequired();
                bool flag = __instance._fullStaff;
                int num = flag ? __instance._maxWorkerCount : 1;
                int maxWorkers = __instance._maxWorkerCount;
                bool BlacklistedExtraWorkerBuilding = occupant.HasFunction(BuildingFunction.Research) |
                    occupant.HasFunction(BuildingFunction.Research
                    );
                // Set limit to 13 if using the cheat for it
                if (Plugin.ExtraWorkerLimits.Value && maxWorkers > 1 && !BlacklistedExtraWorkerBuilding)
                {
                    maxWorkers = 13;

                }
                if (Input.GetKeyUp(Plugin.PlusWorker.Value))
                {
                    occupant.TargetWorkerCount = (byte)Mathf.Min((int)occupant.TargetWorkerCount + num, maxWorkers);
                    D.L("Updated Max Workers: " + occupant.TargetWorkerCount.ToString(), "info");
                    __instance.UpdateButtonVisuals(occupant, occupant.TargetWorkerCount, false, true, occupant.IsOnMission());
                }
                if (Input.GetKeyUp(Plugin.MinusWorker.Value))
                {
                    occupant.TargetWorkerCount = (byte)Mathf.Max((int)occupant.TargetWorkerCount - num, 0);
                    D.L("Updated Max Workers: " + occupant.TargetWorkerCount.ToString(), "info");
                    __instance.UpdateButtonVisuals(occupant, occupant.TargetWorkerCount, false, true, occupant.IsOnMission());
                }

            }
        }

        //=====================================
        //=======INTRO SKIP SOME DAY T.T
        //
        //    [HarmonyPatch(typeof(VideoPanel), nameof(VideoPanel.Init))]
        //    class IntroSkip
        //    {
        //        static void Postfix(VideoPanel __instance, StartGamePanel startGamePanel)
        //        {
        //            UnityEngine.Object.Destroy(__instance.VideoPlayer);
        //            Cursor.visible = true;
        //            startGamePanel.StartNewGame();
        //            
        //
        //        }
        //    }
        //=====================================


    }
}
