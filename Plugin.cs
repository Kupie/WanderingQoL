using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
//StartScreen to modify NDA popup
using Assets.Scripts.Components.StartScreen;

namespace MainNameSpace
{
    [BepInPlugin(PluginGuid, PluginName, PluginVer)]
    [BepInProcess(PluginTargetExe)]
    [HarmonyPatch]

    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "org.Kupie.WanderingQoL";
        public const string PluginName = "WanderingQoL";
        public const string PluginVer = "1.0.0";
        public const string PluginTargetExe = "WanderingVillage.exe";
        internal static ManualLogSource Log;
        private static readonly Harmony Harmony = new(PluginGuid);

        // Config Bindings
        internal static ConfigEntry<bool> SkipWelcome;
        internal static ConfigEntry<bool> RemoveSocials;
        internal static ConfigEntry<bool> MuteOnUnfocus;
        internal static ConfigEntry<KeyCode> MinusWorker;
        internal static ConfigEntry<KeyCode> PlusWorker;
        internal static ConfigEntry<KeyCode> MuteHotkey;
        internal static ConfigEntry<bool> ExtraWorkerLimits;
        internal static ConfigEntry<bool> DebugLogging;


        private void Awake()
        {
            // Plugin startup logic
            Log = new ManualLogSource(PluginName);
            BepInEx.Logging.Logger.Sources.Add(Log);
            Log.LogInfo($"Plugin {PluginName} is loaded!");

            //General
            SkipWelcome = Config.Bind("1. General", "Remove Welcome", true, "Remove Welcome Screen");
            RemoveSocials = Config.Bind("1. General", "Remove Socials", true, "Removes Social Media links");
            MuteOnUnfocus = Config.Bind("1. General", "Mute On Unfocus", true, "Mute when Game Loses Focus");

            MuteHotkey = Config.Bind("1. General", "Game Mute Hotkey with CTRL", KeyCode.M, "Holding CTRL + This key will mute/unmute the game");

            PlusWorker = Config.Bind("1. General", "Hotkey to add a worker", KeyCode.X, "Hotkey to Add a Worker");
            MinusWorker = Config.Bind("1. General", "Hotkey to subtract a worker", KeyCode.Z, "Hotkey to Subtract a Worker");

            //Cheats
            ExtraWorkerLimits = Config.Bind("2. Cheats", "Extra Workers", false, "Allow up to 13 workers per building");

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
        //Hiding Socials Buttons from start screen
        [HarmonyPatch(typeof(StartScreenUi), nameof(StartScreenUi.Update))]
        class HideSocials1
        {

            static void Postfix(StartScreenUi __instance)
            {
                if (RemoveSocials.Value && (__instance.DiscordButton.isActiveAndEnabled || __instance.FollowButton.isActiveAndEnabled || __instance.ForumButton.isActiveAndEnabled))
                {

                    D.L("Removing Socials Buttons off startup...", "info");
                    __instance.ForumButton.SetActive(false);
                    __instance.FollowButton.SetActive(false);
                    __instance.DiscordButton.SetActive(false);
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

        //Skipping welcome screen
        [HarmonyPatch(typeof(StartScreenUi), nameof(StartScreenUi.Awake))]
        class SkipWelcomeClass
        {
            static void Postfix(StartScreenUi __instance)
            {
                if (Plugin.SkipWelcome.Value)
                {
                    D.L("Removing Welcome Screen...", "info");
                    __instance.DemoWelcomeScreen.Show(false);
                }

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
        [HarmonyPatch(typeof(WorkerSection), nameof(WorkerSection.UpdateUi))]
        class PlusMinusHotkeys
        {
            static void Postfix(World world, FieldOccupant occupant, WorkerSection __instance)
            {
                int maxWorkerCount = (int)occupant.Settings.Building.MaxWorkerCount;
                int minWorkerCount = (int)occupant.Settings.Building.MinWorkerCount;
                bool PlusAllowed = ((int)occupant.TargetWorkerCount < maxWorkerCount);
                bool MinusAllowed = ((int)occupant.TargetWorkerCount > minWorkerCount);


                //try
                //{
                //    Log.LogInfo(GameObject.Find("DeconstructButton").transform.position);
                //}
                //catch (Exception e)
                //{
                //    Log.LogError($" {e} ===== trying to find position of DeconstructButton");
                //}
                //if (ExtraWorkerLimits.Value && occupant.TargetWorkerCount < 13)
                //{
                //    __instance.PlusButton.interactable = true;
                //}
                //if (__instance.PlusButton.Pressed())
                //{
                //    occupant.TargetWorkerCount += 1;
                //    Log.LogInfo($"Increased worker count to {occupant.TargetWorkerCount} via button");
                //}
                if (Input.GetKeyUp(PlusWorker.Value))
                {

                    if (Plugin.ExtraWorkerLimits.Value && occupant.TargetWorkerCount < 13)
                    {
                        occupant.TargetWorkerCount += 1;
                        D.L($"Cheat Increased worker count to {occupant.TargetWorkerCount}", "info");
                    }
                    else if (PlusAllowed)
                    {
                        occupant.TargetWorkerCount = (byte)Mathf.Min((int)(occupant.TargetWorkerCount + 1), maxWorkerCount);
                        D.L($"Increased worker count to {occupant.TargetWorkerCount}", "info");
                    }
                    // Some day I hope to draw the number of workers somewhere, since you can't see
                    // how many you add via the "cheat" feature
                    //if (occupant.TargetWorkerCount > maxWorkerCount)
                    //{
                    //    GUI.Label(new Rect(10, 10, 100, 20), occupant.TargetWorkerCount.ToString());
                    //}

                }
                if (Input.GetKeyUp(Plugin.MinusWorker.Value) && MinusAllowed)
                {
                    occupant.TargetWorkerCount = (byte)Mathf.Max((int)(occupant.TargetWorkerCount - 1), 0);
                    D.L($"Decreased worker count to {occupant.TargetWorkerCount}", "info");
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