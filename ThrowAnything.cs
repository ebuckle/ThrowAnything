using CallOfTheWild;
using CallOfTheWild.AooMechanics;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using Kingmaker.View.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CallOfTheWild.Helpers;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;

namespace ThrowAnything
{
    public class ThrowAnything
    {
        static LibraryScriptableObject library => Main.library;

        static WeaponCategory[] throwable_weapon_categories = { WeaponCategory.Dagger, WeaponCategory.Starknife };

        static BlueprintProjectile thrown_weapon_proj;

        static Dictionary<WeaponCategory, BlueprintWeaponType> thrown_type_blueprints;

        public static void create()
        {
            createWeaponTypes();
            createWeaponBlueprints();

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
                                              Helpers.CreateAddFacts(toggle_ability_main, toggle_ability_off),
                                              Create<ToggleThrownOnAoo>(),
                                              Create<AooWithRangedWeapon>(a => { a.weapon_categories = throwable_weapon_categories; a.required_feature = null; }),
                                              Create<DoNotProvokeAooOnAoo>()
                                              );
            throw_feature.HideInCharacterSheetAndLevelUp = true;

            var basic_feat_progression = Main.library.Get<BlueprintProgression>("5b72dd2ca2cb73b49903806ee8986325");

            basic_feat_progression.LevelEntries[0].Features.Add(throw_feature);
            
            Action<UnitDescriptor> save_game_action = delegate (UnitDescriptor u)
            {
                if (!u.HasFact(throw_feature))
                {
                    u.AddFact(throw_feature);
                }
            };
            SaveGameFix.save_game_actions.Add(save_game_action);
        }

        public static void createWeaponTypes()
        {
            var dagger = library.Get<BlueprintWeaponType>("07cc1a7fceaee5b42b3e43da960fe76d");
            var starknife = library.Get<BlueprintWeaponType>("5a939137fc039084580725b2b0845c3f");
            var all_throwable_types = new BlueprintWeaponType[] { dagger, starknife };
            var strength_thrown = library.Get<BlueprintWeaponEnchantment>("c4d213911e9616949937e1520c80aaf3");
            thrown_type_blueprints = new Dictionary<WeaponCategory, BlueprintWeaponType>();
            thrown_weapon_proj = library.CopyAndAdd<BlueprintProjectile>("c8559cabbf082234e80ad8e046bfa1a1", "ThrownWeaponProjectile", "c732ed37c1a3414fafcb959ed5c358ce");
            thrown_weapon_proj.Trajectory = library.Get<BlueprintProjectileTrajectory>("d2b5ccb441b77b045a0d68a18d8f0154");
            var seed_guid = "a8bb69f6793a40f68fe34125f44c7684";

            foreach (var type in all_throwable_types)
            {
                //TODO: Reduce damage on -STR
                var thrown_type = library.CopyAndAdd<BlueprintWeaponType>(type, "Thrown" + type.name, Helpers.MergeIds(type.AssetGuid, seed_guid));

                Helpers.SetField(thrown_type, "m_TypeNameText", Helpers.CreateString(thrown_type.name + "TypeName", thrown_type.TypeName + " (Thrown)"));
                Helpers.SetField(thrown_type, "m_DefaultNameText", Helpers.CreateString(thrown_type.name + "DefaultName", thrown_type.DefaultName + " (Thrown)"));
                Helpers.SetField(thrown_type, "m_AttackType", AttackType.Ranged);
                Helpers.SetField(thrown_type, "m_AttackRange", FeetExtension.Feet(30.0f));

                WeaponVisualParameters new_wp = thrown_type.VisualParameters.CloneObject();
                Helpers.SetField(new_wp, "m_Projectiles", new BlueprintProjectile[] { thrown_weapon_proj });
                if (thrown_type.Category == WeaponCategory.Dagger)
                {
                    Helpers.SetField(new_wp, "m_WeaponAnimationStyle", WeaponAnimationStyle.ThrownStraight);
                }
                
                Helpers.SetField(thrown_type, "m_VisualParameters", new_wp);

                thrown_type_blueprints.Add(thrown_type.Category, thrown_type);

                var thrown_type_enchantments = Helpers.GetField<BlueprintWeaponEnchantment[]>(thrown_type, "m_Enchantments").AddToArray(strength_thrown);
                Helpers.SetField(thrown_type, "m_Enchantments", thrown_type_enchantments);
            }
        }

        public static void createWeaponBlueprints()
        {
            var all_throwable_weapons = library.GetAllBlueprints().OfType<BlueprintItemWeapon>().Where(b => throwable_weapon_categories.Contains(b.Category)).ToArray();
            var seed_guid = "2ec9a69b2e6041e285b4005ffc47efd2";

            foreach (var weapon in all_throwable_weapons)
            {
                var thrown_weapon = library.CopyAndAdd(weapon, "Thrown" + weapon.name, Helpers.MergeIds(weapon.AssetGuid, seed_guid));
                var new_type = thrown_type_blueprints[thrown_weapon.Category];

                Helpers.SetField(thrown_weapon, "m_DisplayNameText", Helpers.CreateString(thrown_weapon.Name + "ThrownName", thrown_weapon.Name + " (Thrown)"));
                Helpers.SetField(thrown_weapon, "m_Type", new_type);

                thrown_weapon.VisualParameters.Prototype = new_type.VisualParameters;

                weapon.AddComponent(Create<WeaponBlueprintHolder>(w => w.blueprint_weapon = thrown_weapon));
                thrown_weapon.AddComponent(Create<WeaponBlueprintHolder>(w => w.blueprint_weapon = weapon));
            }
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
                var new_blueprint = weapon.Blueprint.GetComponent<WeaponBlueprintHolder>().blueprint_weapon;
                Helpers.SetField(weapon, "m_Blueprint", new_blueprint);
                weapon.OnDidEquipped(unitEntityData.Descriptor);
            }
        }

        public class WeaponBlueprintHolder : BlueprintComponent
        {
            public BlueprintItemWeapon blueprint_weapon;
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
            static void Postfix(ProjectileController __instance, UnitEntityData launcher, TargetWrapper target, BlueprintProjectile projectileBlueprint, RuleAttackRoll attackRoll, RulebookEvent ruleOnHit, Projectile __result)
            {
                if (projectileBlueprint == thrown_weapon_proj)
                {
                    var weapon = attackRoll.Weapon;
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(weapon.Blueprint.VisualParameters.Model, __result.View.transform);
                    gameObject.transform.localPosition = Vector3.zero;
                    gameObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    gameObject.transform.localScale = Vector3.one;
                    __result.MaxRange = weapon.AttackRange.Meters;
                }
            }
        }

        class ToggleThrownOnAoo : RuleInitiatorLogicComponent<RuleAttackWithWeapon>
        {
            private bool has_toggled = false;

            public override void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
            {
                has_toggled = false;
                if (evt.IsAttackOfOpportunity && throwable_weapon_categories.Contains(evt.Weapon.Blueprint.Category) && evt.Weapon.Blueprint.IsRanged)
                {
                    Main.logger.Log("Toggling AOO weapon before attack");
                    evt.Weapon.OnWillUnequip();
                    var new_blueprint = evt.Weapon.Blueprint.GetComponent<WeaponBlueprintHolder>().blueprint_weapon;
                    Helpers.SetField(evt.Weapon, "m_Blueprint", new_blueprint);
                    evt.Weapon.OnDidEquipped(evt.Initiator.Descriptor);
                    has_toggled = true;
                }
            }

            public override void OnEventDidTrigger(RuleAttackWithWeapon evt)
            {
                if (has_toggled)
                {
                    Main.logger.Log("Toggling AOO weapon after attack");
                    evt.Weapon.OnWillUnequip();
                    var new_blueprint = evt.Weapon.Blueprint.GetComponent<WeaponBlueprintHolder>().blueprint_weapon;
                    Helpers.SetField(evt.Weapon, "m_Blueprint", new_blueprint);
                    evt.Weapon.OnDidEquipped(evt.Initiator.Descriptor);
                }
                has_toggled = false;
            }
        }
    }
}
