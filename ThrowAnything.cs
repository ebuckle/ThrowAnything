using CallOfTheWild;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.View.Animation;
using System;
using System.Linq;
using TinyJson;
using UnityEngine;
using UnityEngine.UI;
using static CallOfTheWild.Helpers;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;

namespace ThrowAnything
{
    public class ThrowAnything
    {
        static LibraryScriptableObject library => Main.library;

        static WeaponCategory[] throwable_weapon_categories = { WeaponCategory.Dagger, WeaponCategory.Club, WeaponCategory.Spear, WeaponCategory.LightHammer, WeaponCategory.Starknife, WeaponCategory.Trident };

        static BlueprintWeaponEnchantment thrown_enchantment;

        static BlueprintProjectile thrown_weapon_proj;

        public static void create()
        {
            var dagger = library.Get<BlueprintWeaponType>("07cc1a7fceaee5b42b3e43da960fe76d");
            var thrown_dagger = library.CopyAndAdd<BlueprintWeaponType>("07cc1a7fceaee5b42b3e43da960fe76d", "ThrownDagger", "a09cd01545d6414c89fe1e99c2adcb91");
            thrown_weapon_proj = library.CopyAndAdd<BlueprintProjectile>("c8559cabbf082234e80ad8e046bfa1a1", "ThrownWeaponProjectile", "c732ed37c1a3414fafcb959ed5c358ce");
            var strength_thrown = library.Get<BlueprintWeaponEnchantment>("c4d213911e9616949937e1520c80aaf3");

            Helpers.SetField(thrown_dagger, "m_TypeNameText", Helpers.CreateString("ThrownDaggerTypeName", "Dagger (Thrown)"));
            Helpers.SetField(thrown_dagger, "m_DefaultNameText", Helpers.CreateString("ThrownDaggerDefaultName", "Dagger (Thrown)"));
            Helpers.SetField(thrown_dagger, "m_AttackType", AttackType.Ranged);
            Helpers.SetField(thrown_dagger, "m_AttackRange", FeetExtension.Feet(30.0f));

            WeaponVisualParameters new_wp = thrown_dagger.VisualParameters.CloneObject();
            Helpers.SetField(new_wp, "m_Projectiles", new BlueprintProjectile[] { thrown_weapon_proj });
            Helpers.SetField(new_wp, "m_WeaponAnimationStyle", WeaponAnimationStyle.ThrownArc);
            Helpers.SetField(thrown_dagger, "m_VisualParameters", new_wp);

            dagger.AddComponent(Create<WeaponBlueprintHolder>(w => w.blueprint_weapon = thrown_dagger));
            thrown_dagger.AddComponent(Create<WeaponBlueprintHolder>(w => w.blueprint_weapon = dagger));


            var toggle_ability_main = CreateAbility("ThrownWeaponMainhandToggleAbility",
                                                    "Toggle Thrown Main Hand",
                                                    "Toggle weapon between thrown and melee.",
                                                    "71a259fe3f044ea5a9fd13f4b35ea887",
                                                    null, //TODO icon
                                                    Kingmaker.UnitLogic.Abilities.Blueprints.AbilityType.Extraordinary,
                                                    CommandType.Free,
                                                    Kingmaker.UnitLogic.Abilities.Blueprints.AbilityRange.Personal,
                                                    "",
                                                    "",
                                                    CreateRunActions(new GameAction[] { Create<ToggleThrowable>(t => t.main_hand = true) }),
                                                    Create<AbilityCasterMainWeaponCheck>(a => a.Category = throwable_weapon_categories)
                                                    );

            var toggle_ability_off = CreateAbility("ThrownWeaponOffhandToggleAbility",
                                                   "Toggle Thrown Off Hand",
                                                   "Toggle weapon between thrown and melee.",
                                                   "42408912991f442fafc22024381ae50d",
                                                   null, //TODO icon
                                                   Kingmaker.UnitLogic.Abilities.Blueprints.AbilityType.Extraordinary,
                                                   CommandType.Free,
                                                   Kingmaker.UnitLogic.Abilities.Blueprints.AbilityRange.Personal,
                                                   "",
                                                   "",
                                                   CreateRunActions(new GameAction[] { Create<ToggleThrowable>(t => t.main_hand = false) }),
                                                   Create<AbilityCasterSecondaryWeaponCheck>(a => a.Category = throwable_weapon_categories)
                                                   );

            var throw_feature = CreateFeature("ThrownWeaponFeature",
                                              "",
                                              "",
                                              "eafc849d8c474f7bac39ec43618f9dcd",
                                              null,
                                              Kingmaker.Blueprints.Classes.FeatureGroup.None,
                                              Helpers.CreateAddFacts(toggle_ability_main, toggle_ability_off)
                                              );
            throw_feature.HideInCharacterSheetAndLevelUp = true;

            thrown_enchantment = Common.createWeaponEnchantment("ThrownWeaponToggle", "Throwing", "This weapon may also be thrown.", "", "", "092b0f5dc5e947e09a614aea81007cc8", 0, null, Create<AddUnitFeatureEquipment>(a => a.Feature = throw_feature));


            var dagger_enchantments = Helpers.GetField<BlueprintWeaponEnchantment[]>(dagger, "m_Enchantments").AddToArray(thrown_enchantment);
            Helpers.SetField(dagger, "m_Enchantments", dagger_enchantments);
            var thrown_dagger_enchantments = Helpers.GetField<BlueprintWeaponEnchantment[]>(thrown_dagger, "m_Enchantments").AddToArray(thrown_enchantment, strength_thrown);
            Helpers.SetField(thrown_dagger, "m_Enchantments", thrown_dagger_enchantments);
        }

        [AllowedOn(typeof(BlueprintUnitFact))]
        public class ToggleThrowable : ContextAction
        {
            public bool main_hand;

            public override string GetCaption()
            {
                return "Toggle melee weapon to thrown.";
            }

            public override void RunAction()
            {
                MechanicsContext.Data data = ElementsContext.GetData<MechanicsContext.Data>();
                MechanicsContext mechanicsContext = (data != null) ? data.Context : null;
                if (mechanicsContext == null)
                {
                    Main.logger.Log("Unable to toggle weapon: no context found");
                    return;
                }
                UnitEntityData unitEntityData = mechanicsContext.MaybeCaster;
                if (unitEntityData == null)
                {
                    Main.logger.Log("Can't apply buff: target is null");
                    return;
                }

                ItemEntityWeapon weapon = main_hand ? unitEntityData.Body.PrimaryHand.MaybeWeapon : unitEntityData.Body.SecondaryHand.MaybeWeapon;
                weapon.OnWillUnequip();
                BlueprintWeaponType new_type = weapon.Blueprint.Type.GetComponent<WeaponBlueprintHolder>().blueprint_weapon;
                var new_blueprint = weapon.Blueprint.CloneObject();
                Helpers.SetField(new_blueprint, "m_Type", new_type);

                WeaponVisualParameters new_wp = new_blueprint.VisualParameters.CloneObject();
                Helpers.SetField(new_wp, "m_Projectiles", new_type.VisualParameters.Projectiles);
                Helpers.SetField(new_wp, "m_WeaponAnimationStyle", new_type.VisualParameters.AnimStyle);
                Helpers.SetField(new_blueprint, "m_VisualParameters", new_wp);

                Helpers.SetField(weapon, "m_Blueprint", new_blueprint);
                weapon.OnDidEquipped(unitEntityData.Descriptor);
                Main.logger.Log(weapon.Blueprint.VisualParameters.AnimStyle.ToString());
            }
        }

        public class WeaponBlueprintHolder : BlueprintComponent
        {
            public BlueprintWeaponType blueprint_weapon;
        }

        [AllowedOn(typeof(BlueprintAbility))]
        [AllowMultipleComponents]
        public class AbilityCasterSecondaryWeaponCheck : BlueprintComponent, IAbilityCasterChecker
        {
            public bool CorrectCaster(UnitEntityData caster)
            {
                return caster.Body.SecondaryHand.HasWeapon && this.Category.Contains(caster.Body.SecondaryHand.Weapon.Blueprint.Type.Category);
            }

            public string GetReason()
            {
                return LocalizedTexts.Instance.Reasons.SpecificWeaponRequired;
            }

            public WeaponCategory[] Category;
        }


        [Harmony12.HarmonyPatch(typeof(ProjectileController))]
        [Harmony12.HarmonyPatch("Launch", Harmony12.MethodType.Normal)]
        [Harmony12.HarmonyPatch(new Type[] { typeof(UnitEntityData), typeof(TargetWrapper), typeof(BlueprintProjectile), typeof(RuleAttackRoll), typeof(RulebookEvent) })]
        class ProjectileController__Launch__Patch
        {
            private BlueprintProjectile throwing_axe_proj = library.Get<BlueprintProjectile>("dbcc51cfd11fc1441a495daf9df9b340");
            static void Postfix(ProjectileController __instance, UnitEntityData launcher, TargetWrapper target, BlueprintProjectile projectileBlueprint, RuleAttackRoll attackRoll, RulebookEvent ruleOnHit, Projectile __result)
            {
                if (projectileBlueprint == thrown_weapon_proj)
                {
                    var weapon = attackRoll.Weapon;
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(weapon.Blueprint.VisualParameters.Model, __result.View.transform);
                    gameObject.transform.localPosition = Vector3.zero;
                    gameObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    gameObject.transform.localScale = Vector3.one;
                }
            }
        }
    }
}
