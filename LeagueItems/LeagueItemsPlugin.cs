using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
        public const string PluginVersion = "0.1.0";

        public static PluginInfo pInfo { get; private set; }

        internal static BepInEx.Logging.ManualLogSource logger;

        public void Awake()
        {
            logger = Logger;

            pInfo = Info;
            Assets.Init();
            DamageColorAPI.Init();

            RoR2.ItemCatalog.availability.CallWhenAvailable(Integrations.Init);

            BladeOfTheRuinedKing.Init();
            Bloodthirster.Init();
            DeadMansPlate.Init();
            NashorsTooth.Init();
            SpearOfShojin.Init();
            TitanicHydra.Init();
            WarmogsArmor.Init();

            logger.LogMessage(nameof(Awake) + " done.");
        }
       
        // The Update() method is run on every frame of the game.
        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.
                logger.LogMessage($"Player pressed F2. Spawning our custom item at coordinates {transform.position}.");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(BladeOfTheRuinedKing.itemDef.itemIndex), transform.position, transform.forward * 20f);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(Bloodthirster.itemDef.itemIndex), transform.position, transform.forward * 20f);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(DeadMansPlate.itemDef.itemIndex), transform.position, transform.forward * 20f);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(NashorsTooth.itemDef.itemIndex), transform.position, transform.forward * 20f);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(SpearOfShojin.itemDef.itemIndex), transform.position, transform.forward * 20f);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(TitanicHydra.itemDef.itemIndex), transform.position, transform.forward * 20f);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(WarmogsArmor.itemDef.itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}
