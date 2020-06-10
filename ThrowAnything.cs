using CallOfTheWild;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using Kingmaker.View.Animation;
using System;
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

        public static void create()
        {
            var dagger = library.Get<BlueprintWeaponType>("07cc1a7fceaee5b42b3e43da960fe76d");
            var thrown_dagger = library.CopyAndAdd<BlueprintWeaponType>("07cc1a7fceaee5b42b3e43da960fe76d", "ThrownDagger", "a09cd01545d6414c89fe1e99c2adcb91");
            var throwing_axe_projectile = library.Get<BlueprintProjectile>("dbcc51cfd11fc1441a495daf9df9b340");

            thrown_dagger.name = thrown_dagger.name + " (Thrown)";
            Helpers.SetField(thrown_dagger, "m_AttackType", AttackType.Ranged);
            Helpers.SetField(thrown_dagger, "m_AttackRange", FeetExtension.Feet(30.0f));

            WeaponVisualParameters new_wp = thrown_dagger.VisualParameters.CloneObject();
            Helpers.SetField(new_wp, "m_Projectiles", new BlueprintProjectile[] { throwing_axe_projectile });
            Helpers.SetField(new_wp, "m_WeaponAnimationStyle", WeaponAnimationStyle.ThrownArc);
            Helpers.SetField(thrown_dagger, "m_VisualParameters", new_wp);

            dagger.AddComponent(Create<WeaponBlueprintHolder>(w => w.blueprint_weapon = thrown_dagger));
            thrown_dagger.AddComponent(Create<WeaponBlueprintHolder>(w => w.blueprint_weapon = dagger));


            var toggle_ability = CreateAbility("ThrownWeaponToggleAbility",
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

            var throw_feature = CreateFeature("ThrownWeaponFeature",
                                              toggle_ability.Name,
                                              toggle_ability.Description,
                                              "eafc849d8c474f7bac39ec43618f9dcd",
                                              null,
                                              Kingmaker.Blueprints.Classes.FeatureGroup.None,
                                              Helpers.CreateAddFact(toggle_ability)
                                              );
            throw_feature.HideInCharacterSheetAndLevelUp = true;

            thrown_enchantment = Common.createWeaponEnchantment("ThrownWeaponToggle", "Throwing", "This weapon may also be thrown.", "", "", "092b0f5dc5e947e09a614aea81007cc8", 0, null, Create<AddUnitFeatureEquipment>(a => a.Feature = throw_feature));


            var dagger_enchantments = Helpers.GetField<BlueprintWeaponEnchantment[]>(dagger, "m_Enchantments").AddToArray(thrown_enchantment);
            Helpers.SetField(dagger, "m_Enchantments", dagger_enchantments);
            var thrown_dagger_enchantments = Helpers.GetField<BlueprintWeaponEnchantment[]>(thrown_dagger, "m_Enchantments").AddToArray(thrown_enchantment);
            Helpers.SetField(thrown_dagger, "m_Enchantments", thrown_dagger_enchantments);
        }

        [AllowedOn(typeof(BlueprintUnitFact))]
        public class ToggleThrowable : ContextAction
        {
            public bool main_hand;
            private BlueprintProjectile throwing_axe_projectile = library.Get<BlueprintProjectile>("dbcc51cfd11fc1441a495daf9df9b340");

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
                BlueprintWeaponType new_blueprint = weapon.Blueprint.GetComponent<WeaponBlueprintHolder>().blueprint_weapon;
                if (new_blueprint == null)
                {
                    Main.logger.Log("Blueprint was null");
                    return;
                }
                var blueprint = weapon.Blueprint.CloneObject();
                blueprint.name = blueprint.name + " (Thrown)";
                Helpers.SetField(blueprint, "m_AttackType", AttackType.Ranged);
                Helpers.SetField(blueprint, "m_AttackRange", FeetExtension.Feet(30.0f));

                WeaponVisualParameters new_wp = blueprint.VisualParameters.CloneObject();
                Helpers.SetField(new_wp, "m_Projectiles", new BlueprintProjectile[] { throwing_axe_projectile });
                Helpers.SetField(new_wp, "m_WeaponAnimationStyle", WeaponAnimationStyle.ThrownArc);
                Helpers.SetField(blueprint, "m_VisualParameters", new_wp);
                Helpers.SetField(weapon, "m_Blueprint", blueprint);
                weapon.OnDidEquipped(unitEntityData.Descriptor);
            }
        }

        public class WeaponBlueprintHolder : BlueprintComponent
        {
            public BlueprintWeaponType blueprint_weapon;
        }
    }
}
