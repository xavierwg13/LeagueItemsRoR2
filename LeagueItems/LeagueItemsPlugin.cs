using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace LeagueItems
{
    // Dependencies
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(DamageAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(RecalculateStatsAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class LeagueItemsPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.shirograhm.leagueitems";
        public const string PluginName = "LeagueItems";
        public const string PluginVersion = "0.1.1";

        public static PluginInfo PInfo { get; private set; }

        public static AssetBundle MainAssets;

        internal static BepInEx.Logging.ManualLogSource logger;

        public void Awake()
        {
            logger = Logger;

            PInfo = Info;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LeagueItems.assetbundle"))
            {
                if (stream != null)
                {
                    MainAssets = AssetBundle.LoadFromStream(stream);

                    logger.LogMessage("Successfully loaded assets.");
                }
                else
                {
                    logger.LogError("ERROR: Assets failed to load.");
                }
            }

            On.RoR2.Items.ContagiousItemManager.Init += InjectVoidItems;

            GenericGameEvents.Init();
            DamageColorAPI.Init();

            RoR2.ItemCatalog.availability.CallWhenAvailable(Integrations.Init);

            BladeOfTheRuinedKing.Init();
            Bloodthirster.Init();
            DeadMansPlate.Init();
            DeathsDance.Init();
            GuinsoosRageblade.Init();
            Heartsteel.Init();
            InfinityEdge.Init();
            NashorsTooth.Init();
            SpearOfShojin.Init();
            TitanicHydra.Init();
            WarmogsArmor.Init();
            WitsEnd.Init();

            logger.LogMessage(nameof(Awake) + " done.");
        }

        private void InjectVoidItems(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            List<ItemDef.Pair> newVoidPairs = new List<ItemDef.Pair>();

            Debug.Log("Injecting LeagueMod item corruption...");
            // Nashor's => Guinsoo's
            newVoidPairs.Add(new ItemDef.Pair() {
                itemDef1 = NashorsTooth.itemDef,
                itemDef2 = GuinsoosRageblade.itemDef
            });
            
            var key = DLC1Content.ItemRelationshipTypes.ContagiousItem;
            Debug.Log(key);
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            var voidPairs = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem];
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = voidPairs.Union(newVoidPairs).ToArray();
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            Debug.Log("Finished injecting LeagueMod item transformations.");

            orig();
        }

        // The Update() method is run on every frame of the game.
        private void Update()
        {
            if (!NetworkServer.active) { return; }

            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.
                logger.LogMessage($"Player pressed F2. Spawning a random League item at coordinates {transform.position}.");

                // All Items Array
                ItemIndex[] allItems = new ItemIndex[]
                {
                    BladeOfTheRuinedKing.itemDef.itemIndex,
                    Bloodthirster.itemDef.itemIndex,
                    DeadMansPlate.itemDef.itemIndex,
                    DeathsDance.itemDef.itemIndex,
                    GuinsoosRageblade.itemDef.itemIndex,
                    Heartsteel.itemDef.itemIndex,
                    InfinityEdge.itemDef.itemIndex,
                    NashorsTooth.itemDef.itemIndex,
                    SpearOfShojin.itemDef.itemIndex,
                    TitanicHydra.itemDef.itemIndex,
                    WarmogsArmor.itemDef.itemIndex,
                    WitsEnd.itemDef.itemIndex
                };

                var random = new System.Random();
                int arrayIdx = random.Next(0, allItems.Length);

                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(allItems[arrayIdx]), transform.position, transform.forward * 20f);   
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.
                logger.LogMessage($"Player pressed F3. Spawning Guinsoo's Rageblade at coordinates {transform.position}.");

                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(GuinsoosRageblade.itemDef.itemIndex), transform.position, transform.forward * 20f);
            }
        }

        public struct GenericCharacterInfo
        {
            public GameObject gameObject;
            public CharacterBody body;
            public CharacterMaster master;
            public TeamComponent teamComponent;
            public HealthComponent healthComponent;
            public Inventory inventory;
            public TeamIndex teamIndex;
            public Vector3 aimOrigin;

            public GenericCharacterInfo(CharacterBody body)
            {
                this.body = body;
                gameObject = body ? body.gameObject : null;
                master = body ? body.master : null;
                teamComponent = body ? body.teamComponent : null;
                healthComponent = body ? body.healthComponent : null;
                inventory = master ? master.inventory : null;
                teamIndex = teamComponent ? teamComponent.teamIndex : TeamIndex.Neutral;
                aimOrigin = body ? body.aimOrigin : UnityEngine.Random.insideUnitSphere.normalized;
            }
        }
    }
}
