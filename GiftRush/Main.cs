using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GiftRush
{
    public class Main : MelonMod
    {
        public override void OnApplicationLateStart()
        {
            GameDataManager.powerPrefs.dontUploadToLeaderboard = true;
            PatchGame();

            Game game = Singleton<Game>.Instance;

            if (game == null)
                return;

            game.OnLevelLoadComplete += OnLevelLoadComplete;

            if (RM.drifter)
                OnLevelLoadComplete();
        }

        private void OnLevelLoadComplete()
        {
            LevelData currentLevel = Singleton<Game>.Instance.GetCurrentLevel();

            if (SceneManager.GetActiveScene().name.Equals("Heaven_Environment") || !LevelRush.IsLevelRush() &&
                !(LevelRush.GetCurrentLevelRushType() == LevelRush.LevelRushType.WhiteRush || LevelRush.GetCurrentLevelRushType() == LevelRush.LevelRushType.MikeyRush) ||
                currentLevel.isBossFight)
                return;

            if (currentLevel.collectibleGiftForCharacter.ID == "GREEN")
            {
                GameObject darkInsightObject = Singleton<Game>.Instance.GetCurrentLevel().levelID switch
                {
                    "GRID_TRAPS2" => GameObject.Find("Level Art/DarkInsightGate/Spawn_DarkInsight"),
                    "GRID_ESCALATE" => GameObject.Find("DarkInsightGate/Spawn_DarkInsight"),
                    "GRID_ZIPRAP" => GameObject.Find("DarkInsightGate/Spawn_DarkInsight"),
                    _ => null
                };

                if (darkInsightObject != null)
                {
                    GameObject darkInsight = Utils.CreateObjectFromResources("DarkInsight", "Dark Insight", null);
                    darkInsight.transform.position = darkInsightObject.transform.position;
                    darkInsight.transform.rotation = darkInsightObject.transform.rotation;
                }

                GameObject portalSpawner = Singleton<Game>.Instance.GetCurrentLevel().levelID switch
                {
                    "GRID_GODTEMPLE_ENTRY" => GameObject.Find("Spawn_Collectible_Card_Item_LoreCollectibleSmall"),
                    "GRID_ZIPRAP" => GameObject.Find("Items/Spawn_Collectible"),
                    _ => GameObject.Find("Items/Spawn_Collectible_Card_Item_LoreCollectibleSmall")
                };
                if (portalSpawner == null) return;   

                GameObject portal = Utils.CreateObjectFromResources("EnvironmentPortal", "Environment Portal", null);
                portal.GetComponent<EnvironmentPortal>().SetData(currentLevel.collectiblePortalData);
                Vector3 newPos = portalSpawner.transform.position;
                if (portalSpawner.GetComponent<CardPickupSpawner>().offsetHeightFromGround)
                    newPos += Vector3.up * 3f;
                portal.transform.position = newPos;
                portal.transform.rotation = portalSpawner.transform.rotation;
                return;
            }

            GameObject spawnerObject = null;
            spawnerObject = Singleton<Game>.Instance.GetCurrentLevel().levelID switch
            {
                "TUT_SHOCKER2" => GameObject.Find("Items/Spawn_Collectible"),
                "GRID_VERTICAL" => GameObject.Find("Spawn_Collectible_Card_Weapon_Pistol"),
                _ => GameObject.Find("Spawn_Collectible_Card_Item_LoreCollectibleSmall"),
            };
            CardPickupSpawner spawner = spawnerObject.GetComponent<CardPickupSpawner>();
            Vector3 vector = spawnerObject.transform.position;


            if (spawner.offsetHeightFromGround)
                vector += Vector3.up * 3f;

            CardPickup pickup = CardPickup.SpawnPickupCollectible(currentLevel.collectibleGiftForCharacter, vector, spawnerObject.transform.rotation);
            pickup.GetComponent<AudioObjectAmbience>().UpdateSFXRadiusOverride(spawner.collectibleSFXRadiusOverride);
            pickup.SetPickupAction(Singleton<Game>.Instance.OnLevelWin);
        }

        private void PatchGame()
        {
            HarmonyLib.Harmony harmony = new("de.MOPSKATER.BoofOfMemes");

            MethodInfo target = typeof(LevelStats).GetMethod("UpdateTimeMicroseconds");
            HarmonyMethod patch = new(typeof(Main).GetMethod("PreventNewScore"));
            harmony.Patch(target, patch);

            target = typeof(Game).GetMethod("OnLevelWin");
            patch = new(typeof(Main).GetMethod("PreventNewGhost"));
            harmony.Patch(target, patch);

            target = typeof(LevelRush).GetMethod("IsCurrentLevelRushScoreBetter", BindingFlags.NonPublic | BindingFlags.Static);
            patch = new(typeof(Main).GetMethod("PreventNewBestLevelRush"));
            harmony.Patch(target, patch);

            target = typeof(LevelGate).GetMethod("SetUnlocked");
            patch = new(typeof(Main).GetMethod("UnlockGate"));
            harmony.Patch(target, patch);

            target = typeof(BookOfLife).GetMethod("SetBookOpen", BindingFlags.NonPublic | BindingFlags.Instance);
            patch = new(typeof(Main).GetMethod("PreSetBookOpen"));
            harmony.Patch(target, patch);

            target = typeof(Achievements).GetMethod("SyncCharacterQuests", BindingFlags.Public | BindingFlags.Static);
            patch = new(typeof(Main).GetMethod("PreSyncCharacterQuests"));
            harmony.Patch(target, patch);

            target = typeof(MechController).GetMethod("DoCardPickup", BindingFlags.NonPublic | BindingFlags.Instance);
            patch = new(typeof(Main).GetMethod("PreDoCardPickup"));
            harmony.Patch(target, patch);

            target = typeof(GameDataManager).GetMethod("SaveGame", BindingFlags.Public | BindingFlags.Static);
            patch = new(typeof(Main).GetMethod("PreSaveGame"));
            harmony.Patch(target, patch);

            target = typeof(GameDataManager).GetMethod("SaveLevelStats", BindingFlags.Public | BindingFlags.Static);
            patch = new(typeof(Main).GetMethod("PreSaveLevelStats"));
            harmony.Patch(target, patch);

            target = typeof(MenuScreenLevelRushComplete).GetMethod("OnSetVisible");
            patch = new(typeof(Main).GetMethod("PostOnSetVisible"));
            harmony.Patch(target, null, patch);

            target = typeof(EnvironmentPortal).GetMethod("OnPlayerConnect");
            patch = new(typeof(Main).GetMethod("PreOnPlayerConnect"));
            harmony.Patch(target, patch);
        }

        public static bool PreventNewScore(LevelStats __instance, ref long newTime)
        {
            if (newTime < __instance._timeBestMicroseconds)
            {
                if (__instance._timeBestMicroseconds == 999999999999L)
                    __instance._timeBestMicroseconds = 600000000;
                __instance._newBest = true;
            }
            else
                __instance._newBest = false;
            // __instance._timeLastMicroseconds = newTime;
            return false;
        }

        public static bool PreventNewGhost(Game __instance)
        {
            __instance.winAction = null;
            return true;
        }

        public static bool PreventNewBestLevelRush(ref bool __result)
        {
            __result = false;
            return false;
        }

        public static bool UnlockGate(ref bool u)
        {
            u = false;
            return true;
        }

        public static bool PreSetBookOpen(ref bool open) => !open;

        public static bool PreSyncCharacterQuests(ref ActorData actor)
        {
            return actor.displayName != "Interface/ACTORNAME_NEONGREEN";
        }

        public static bool PreDoCardPickup(ref PlayerCardData card)
        {
            if (!(Singleton<Game>.Instance.GetCurrentLevel().isSidequest && card.consumableType == PlayerCardData.ConsumableType.GreenMemoryItem))
                return true;

            GS.WinLevel();
            return false;
        }

        public static bool PreSaveGame() => false;

        public static bool PreSaveLevelStats() => false;

        public static void PostOnSetVisible(ref MenuScreenLevelRushComplete __instance)
        {
            string text = LevelRush.GetCurrentLevelRushType() switch
            {
                LevelRush.LevelRushType.WhiteRush => "White's",
                LevelRush.LevelRushType.MikeyRush => "Mikeys's",
                _ => "Error"
            };

            if (text == "Error") return;

            text += (LevelRush.IsHellRush() ? " Hell " : " Heaven ") + "Gift Rush";
            __instance._rushName.textMeshProUGUI.text = text;
        }

        public static void PreOnPlayerConnect() => LevelRush.UpdateLevelRushTimerMicroseconds(Singleton<Game>.Instance.GetCurrentLevelTimerMicroseconds());
    }
}