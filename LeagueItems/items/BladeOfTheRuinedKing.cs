﻿using System;
using System.Collections.Generic;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

using static LeagueItems.ConfigManager;

namespace LeagueItems
{
    internal class BladeOfTheRuinedKing
    {
        public static ItemDef itemDef;

        public static Color32 botrkColor = new(40, 179, 191, 255);

        // Deals 1% (+1% per stack) current health damage on-hit, up to 100% of the initial damage.
        public static ConfigurableValue<float> onHitDamageNumber = new(
            "Item: Blade of the Ruined King",
            "On-Hit Damage",
            1.0f,
            "Percent of current health done on-hit for each stack of BotRK.",
            new List<string>()
            {
                "ITEM_BLADEOFTHERUINEDKING_DESC"
            }
        );
        public static float onHitDamagePercent = onHitDamageNumber / 100f;

        public static ConfigurableValue<bool> shouldCapBorkDamage = new(
            "Item: Blade of the Ruined King",
            "Cap On-Hit Damage",
            true,
            "Whether or not the on-hit damage should be capped at a certain ratio of the attack's base damage. If false, BotRK on-hit damage has no cap.",
            new List<string>()
            {
                "ITEM_BLADEOFTHERUINEDKING_DESC"
            }
        );

        public static ConfigurableValue<float> maximumDamageCap = new(
            "Item: Blade of the Ruined King",
            "Max Damage Cap",
            100f,
            "Maximum damage per proc as a percentage of the initial damage (e.g. if your attack does 12 damage, BotRK does a max of 12 damage).",
            new List<string>()
            {
                "ITEM_BLADEOFTHERUINEDKING_DESC"
            }
        );
        public static float maximumDamageCapPercent = maximumDamageCap / 100f;

        private static DamageAPI.ModdedDamageType botrkDamageType;
        public static DamageColorIndex botrkDamageColor = DamageColorAPI.RegisterDamageColor(botrkColor);

        public class BladeStatistics : MonoBehaviour
        {
            private float _totalDamageDealt;
            public float TotalDamageDealt
            {
                get { return _totalDamageDealt; }
                set
                {
                    _totalDamageDealt = value;
                    if (NetworkServer.active)
                    {
                        new BladeSync(gameObject.GetComponent<NetworkIdentity>().netId, value).Send(NetworkDestination.Clients);
                    }
                }
            }

            public class BladeSync : INetMessage
            {
                NetworkInstanceId objId;
                float totalDamageDealt;

                public BladeSync()
                {
                }

                public BladeSync(NetworkInstanceId objId, float totalDamage)
                {
                    this.objId = objId;
                    this.totalDamageDealt = totalDamage;
                }

                public void Deserialize(NetworkReader reader)
                {
                    objId = reader.ReadNetworkId();
                    totalDamageDealt = reader.ReadSingle();
                }

                public void OnReceived()
                {
                    if (NetworkServer.active) return;

                    GameObject obj = Util.FindNetworkObject(objId);
                    if (obj)
                    {
                        BladeStatistics component = obj.GetComponent<BladeStatistics>();
                        if (component) component.TotalDamageDealt = totalDamageDealt;
                    }
                }

                public void Serialize(NetworkWriter writer)
                {
                    writer.Write(objId);
                    writer.Write(totalDamageDealt);
                }
            }
        }

        internal static void Init()
        {
            GenerateItem();
            AddTokens();

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(itemDef, displayRules));

            NetworkingAPI.RegisterMessageType<BladeStatistics.BladeSync>();

            botrkDamageType = DamageAPI.ReserveDamageType();

            Hooks();
        }

        private static void GenerateItem()
        {
            itemDef = ScriptableObject.CreateInstance<ItemDef>();
            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            itemDef.name = "Botrk";
            itemDef.nameToken = "BotrkToken";
            itemDef.pickupToken = "BotrkPickup";
            itemDef.descriptionToken = "BotrkDesc";
            itemDef.loreToken = "BotrkLore";

            ItemTierCatalog.availability.CallWhenAvailable(() =>
            {
                if (itemDef) itemDef.tier = ItemTier.Tier3;
            });

            itemDef.pickupIconSprite = LeagueItemsPlugin.MainAssets.LoadAsset<Sprite>("BotRK.png");
            itemDef.pickupModelPrefab = LeagueItemsPlugin.MainAssets.LoadAsset<GameObject>("bladeoftheruinedking.prefab");
            itemDef.canRemove = true;
            itemDef.hidden = false;
        }

        private static void Hooks()
        {
            CharacterMaster.onStartGlobal += (obj) =>
            {
                if (obj.inventory) obj.inventory.gameObject.AddComponent<BladeStatistics>();
            };

            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                orig(self, damageInfo, victim);

                if (!NetworkServer.active) return;

                if (damageInfo.attacker == null || victim == null)
                {
                    return;
                }

                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody victimBody = victim.GetComponent<CharacterBody>();

                if (attackerBody?.inventory)
                {
                    int itemCount = attackerBody.inventory.GetItemCount(itemDef);
                    // Calculate scaled percentage value based on hyperbolic curve
                    float hyperbolicPercentage = 1 - (1 / (1 + (onHitDamagePercent * itemCount)));

                    if (itemCount > 0 && damageInfo.procCoefficient > 0)
                    {
                        float botrkDamage = victimBody.healthComponent.health * damageInfo.procCoefficient * hyperbolicPercentage;

                        if (shouldCapBorkDamage)
                        {
                            // If capped, damage cannot exceed maxDamageCap% of the initial attack's damage
                            botrkDamage = Mathf.Clamp(botrkDamage, 0.0f, maximumDamageCapPercent * damageInfo.damage);
                        }

                        DamageInfo botrkProc = new()
                        {
                            damage = botrkDamage,
                            attacker = damageInfo.attacker,
                            inflictor = damageInfo.attacker,
                            procCoefficient = 0f,
                            position = damageInfo.position,
                            crit = false,
                            damageColorIndex = botrkDamageColor,
                            procChainMask = damageInfo.procChainMask,
                            damageType = DamageType.Silent
                        };
                        DamageAPI.AddModdedDamageType(botrkProc, botrkDamageType);

                        victimBody.healthComponent.TakeDamage(botrkProc);

                        var itemStats = attackerBody.inventory.GetComponent<BladeStatistics>();
                        itemStats.TotalDamageDealt += botrkDamage;
                    }
                }
            };

            On.RoR2.UI.ScoreboardStrip.SetMaster += (orig, self, characterMaster) =>
            {
                orig(self, characterMaster);
                if (Integrations.itemStatsEnabled)
                {
                    // Let other mods handle stat tracking if installed
                    return;
                }

                if (self.itemInventoryDisplay && characterMaster)
                {
#pragma warning disable Publicizer001
                    var itemStats = self.itemInventoryDisplay.inventory.GetComponent<BladeStatistics>();

                    if (itemStats)
                    {
                        self.itemInventoryDisplay.itemIcons.ForEach(delegate (RoR2.UI.ItemIcon item)
                        {
                            string valueDamageText = String.Format("{0:#}", itemStats.TotalDamageDealt);

                            if (item.itemIndex == itemDef.itemIndex)
                            {
                                item.tooltipProvider.overrideBodyText =
                                    Language.GetString(itemDef.descriptionToken)
                                    + "<br><br>Total Damage Done: " + valueDamageText;
                            }
                        });
                    }
#pragma warning restore Publicizer001
                }
            };
        }

        // This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // The Name should be self explanatory
            LanguageAPI.Add("Botrk", "Blade of the Ruined King");

            // Name Token
            LanguageAPI.Add("BotrkToken", "Blade of the Ruined King");

            // The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("BotrkPickup", "Deal a percentage of enemies current health as bonus damage on-hit.");

            // The Description is where you put the actual numbers and give an advanced description.
            string desc = "Deal <style=cIsDamage>" + onHitDamageNumber + "%</style> <style=cStack>(+" + onHitDamageNumber + "% per stack)</style> " +
                "of enemy current health as bonus damage on-hit";

            if (shouldCapBorkDamage)
            {
                desc += ", up to a maximum of <style=cIsDamage>" + maximumDamageCap + "%</style> of the initial damage.";
            }
            else
            {
                desc += ".";
            }
            LanguageAPI.Add("BotrkDesc", desc);

            // The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("BotrkLore", "");
        }
    }
}

// Styles
// <style=cIsHealth>" + exampleValue + "</style>
// <style=cIsDamage>" + exampleValue + "</style>
// <style=cIsHealing>" + exampleValue + "</style>
// <style=cIsUtility>" + exampleValue + "</style>
// <style=cIsVoid>" + exampleValue + "</style>
// <style=cHumanObjective>" + exampleValue + "</style>
// <style=cLunarObjective>" + exampleValue + "</style>
// <style=cStack>" + exampleValue + "</style>
// <style=cWorldEvent>" + exampleValue + "</style>
// <style=cArtifact>" + exampleValue + "</style>
// <style=cUserSetting>" + exampleValue + "</style>
// <style=cDeath>" + exampleValue + "</style>
// <style=cSub>" + exampleValue + "</style>
// <style=cMono>" + exampleValue + "</style>
// <style=cShrine>" + exampleValue + "</style>
// <style=cEvent>" + exampleValue + "</style>