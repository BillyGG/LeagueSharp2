﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using xSLx_Orbwalker;

namespace ForeverTeemo
{
    class Program
    {

        private static String ChampName = "Teemo";
        public static Menu Menu;
        public static Spell Q, W, E, R;
        private static xSLxOrbwalker xSLx;
        private static Obj_AI_Hero Player;

        public static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.BaseSkinName != ChampName) return;

            Menu = new Menu("Forever Teemo", "ForeverTeemo", true);
            var xSLxMenu = new Menu("Orbwalker", "Orbwalker1");
            xSLxOrbwalker.AddToMenu(xSLxMenu);
            Menu.AddSubMenu(xSLxMenu);
            var ts = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(ts);
            Menu.AddSubMenu(ts);

            //Combo Menu
            Menu.AddSubMenu(new Menu("[Teemo]Combo", "Combo"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseQC", "Use Q - Combo").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseWC", "Use W - Combo").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("UseRC", "Use R - Combo").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("QManaC", "Q Mana - Combo").SetValue(new Slider(30, 1, 100)));
            Menu.SubMenu("Combo").AddItem(new MenuItem("WManaC", "W Mana - Combo").SetValue(new Slider(30, 1, 100)));
            Menu.SubMenu("Combo").AddItem(new MenuItem("RManaC", "R Mana - Combo").SetValue(new Slider(30, 1, 100)));
            Menu.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            //Harass Menu
            Menu.AddSubMenu(new Menu("[Teemo]Harass", "Harass"));
            Menu.SubMenu("Harass").AddItem(new MenuItem("UseQH", "Use Q - Harass").SetValue(true));
            //--Auto Harass--
            Menu.SubMenu("Harass").AddItem(new MenuItem("UseQAH", "Auto Harass with Q").SetValue(true));
            Menu.SubMenu("Harass").AddItem(new MenuItem("QManaAH", "Q Mana - Auto Harass").SetValue(new Slider(50, 1, 100)));

            //Farm
            Menu.AddSubMenu(new Menu("[Teemo]Farm", "Farm"));
            Menu.SubMenu("Farm").AddItem(new MenuItem("UseQF", "Use Q - Farm").SetValue(false));
            Menu.SubMenu("Farm").AddItem(new MenuItem("UseRF", "Use R - Farm").SetValue(false));
            Menu.SubMenu("Farm").AddItem(new MenuItem("QManaF", "Q Mana - Farm").SetValue(new Slider(50, 1, 100)));
            Menu.SubMenu("Farm").AddItem(new MenuItem("RManaF", "R Mana - Farm").SetValue(new Slider(50, 1, 100)));

            //Items
            Menu.AddSubMenu(new Menu("[Teemo]Items", "Items"));
            Menu.SubMenu("Items").AddItem(new MenuItem("BotrkC", "Botrk - Combo").SetValue(true));
            Menu.SubMenu("Items").AddItem(new MenuItem("BotrkH", "Botrk - Harrass").SetValue(false));
            Menu.SubMenu("Items").AddItem(new MenuItem("YoumuuC", "Youmuu - Combo").SetValue(true));
            Menu.SubMenu("Items").AddItem(new MenuItem("YoumuuH", "Youmuu - Harrass").SetValue(false));
            Menu.SubMenu("Items").AddItem(new MenuItem("BilgeC", "Cutlass - Combo").SetValue(true));
            Menu.SubMenu("Items").AddItem(new MenuItem("BilgeH", "Cutlass - Harrass").SetValue(false));
            Menu.SubMenu("Items").AddItem(new MenuItem("OwnHPercBotrk", "Min Own H. % Botrk").SetValue(new Slider(50, 1, 100)));
            Menu.SubMenu("Items").AddItem(new MenuItem("EnHPercBotrk", "Min Enemy H. % Botrk").SetValue(new Slider(20, 1, 100)));

            //Drawings
            Menu.AddSubMenu(new Menu("[Teemo]Drawings", "Drawing"));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("DrawQ", "Draw Q Range").SetValue(new Circle(true, Color.Green)));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("DrawR", "Draw R Range").SetValue(new Circle(true, Color.Green)));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("DrawIgnite", "Draw Ignite Range").SetValue(new Circle(true, Color.Red)));

            Menu.AddToMainMenu();

            Q = new Spell(SpellSlot.Q, 680f, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 230f, TargetSelector.DamageType.Magical);
            Q.SetTargetted(0f, 2000f);
            R.SetSkillshot(0.1f, 75f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Game.OnGameUpdate += Game_OnGameUpdate;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
            xSLxOrbwalker.AfterAttack += xSLxOrbwalker_AfterAttack;
            Game.PrintChat("<font color='#FF0000'>DZDraven</font> ReLoaded!");
            Game.PrintChat("By <font color='#FF0000'>DZ</font><font color='#FFFFFF'>191</font>. Special Thanks to: Lexxes");
            Game.PrintChat("If you like my assemblies feel free to donate me (link on the forum :) )");
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            var Sender = (Obj_AI_Hero)unit;
            if (!isMenuEnabled("Interrupt") || !E.IsReady() || !Sender.IsValidTarget()) return;
            CastRHitchance(Sender);
        }

        private static void xSLxOrbwalker_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            if (!(target is Obj_AI_Hero)) return;
            if (unit.IsMe && target.IsValidTarget())
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                castItems((Obj_AI_Hero)target);
            }
        }

        static void castItems(Obj_AI_Hero tar)
        {
            var ownH = getPerValue(false);
            if ((Menu.Item("BotrkC").GetValue<bool>() && xSLxOrbwalker.CurrentMode == xSLxOrbwalker.Mode.Combo) && (Menu.Item("OwnHPercBotrk").GetValue<Slider>().Value <= ownH) &&
                ((Menu.Item("EnHPercBotrk").GetValue<Slider>().Value <= getPerValueTarget(tar, false))))
            {
                UseItem(3153, tar);
            }
            if ((Menu.Item("BotrkH").GetValue<bool>() && xSLxOrbwalker.CurrentMode == xSLxOrbwalker.Mode.Harass) && (Menu.Item("OwnHPercBotrk").GetValue<Slider>().Value <= ownH) &&
               ((Menu.Item("EnHPercBotrk").GetValue<Slider>().Value <= getPerValueTarget(tar, false))))
            {
                UseItem(3153, tar);
            }
            if (Menu.Item("YoumuuC").GetValue<bool>() && xSLxOrbwalker.CurrentMode == xSLxOrbwalker.Mode.Combo)
            {
                UseItem(3142);
            }
            if (Menu.Item("YoumuuH").GetValue<bool>() && xSLxOrbwalker.CurrentMode == xSLxOrbwalker.Mode.Harass)
            {
                UseItem(3142);
            }
            if (Menu.Item("BilgeC").GetValue<bool>() && xSLxOrbwalker.CurrentMode == xSLxOrbwalker.Mode.Combo)
            {
                UseItem(3144, tar);
            }
            if (Menu.Item("BilgeH").GetValue<bool>() && xSLxOrbwalker.CurrentMode == xSLxOrbwalker.Mode.Harass)
            {
                UseItem(3144, tar);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            var target = TargetSelector.GetTarget(xSLxOrbwalker.GetAutoAttackRange(), TargetSelector.DamageType.Physical);
            var Qtarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var Rtarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);

            if (Qtarget.IsValidTarget()) CastQ(Qtarget);
            //if (Rtarget.IsValidTarget())CastR(Rtarget);
            if (xSLxOrbwalker.CurrentMode == xSLxOrbwalker.Mode.LaneClear) { QFarmCheck(); }
        }

        private static void QFarmCheck()
        {
            if (!isMenuEnabled("UseQF")) return;
            List<Obj_AI_Base> MinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,
                MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
        }

        //private static void CastR(Obj_AI_Hero Rtarget)
        //{
        //throw new NotImplementedException();
        //}

        private static void Drawing_OnDraw(EventArgs args)
        {
            var DrawQ = Menu.Item("DrawQ").GetValue<Circle>();
            var DrawR = Menu.Item("DrawR").GetValue<Circle>();
            //var QRadius = Playe
            if (DrawQ.Active)
            {
                Utility.DrawCircle(Player.Position, 680f, Color.Green);
            }
            if (DrawR.Active)
            {
                Utility.DrawCircle(Player.Position, 230f, Color.GreenYellow);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var GPSender = (Obj_AI_Hero)gapcloser.Sender;
            if (!isMenuEnabled("antiGP") || !R.IsReady() || !GPSender.IsValidTarget()) return;
            //CastRHitchance(GPSender);
        }

        private static void CastQ(Obj_AI_Base target)
        {
            switch (xSLxOrbwalker.CurrentMode)
            {
                case xSLxOrbwalker.Mode.Combo:
                    if (!isMenuEnabled("UseQC")) return;
                    var ManaQCombo = Menu.Item("QManaC").GetValue<Slider>().Value;
                    if (getPerValue(true) >= ManaQCombo) Q.Cast(isMenuEnabled("Packets"));
                    break;
                case xSLxOrbwalker.Mode.Harass:
                    if (!isMenuEnabled("UseQH")) return;
                    var ManaQHarass = Menu.Item("QManaH").GetValue<Slider>().Value;
                    if (getPerValue(true) >= ManaQHarass) Q.Cast(isMenuEnabled("Packets"));
                    break;
                case xSLxOrbwalker.Mode.LaneClear:
                    if (!isMenuEnabled("UseQF")) return;
                    var ManaQLC = Menu.Item("QManaLC").GetValue<Slider>().Value;
                    if (getPerValue(true) >= ManaQLC && minionThere()) Q.Cast(isMenuEnabled("Packets"));
                    break;
            }
        }
        /*#region FixMe

        private static void CastW()
        {
            if (hasWBuff() || !W.IsReady()) return;

            switch (xSLxOrbwalker.CurrentMode)
            {
                case xSLxOrbwalker.Mode.Combo:
                    if (!isMenuEnabled("UseWC")) return;
                    var MWC = Menu.Item("WManaC").GetValue<Slider>().Value;
                    if (getPerValue(true) >= MWC) W.Cast(isMenuEnabled("Packets"));
                    break;
            }
        } 
        #endregion*/

        private static bool hasWBuff()
        {
            return Player.HasBuff("MoveQuick", true) || Player.HasBuff("movequickbuff", true);
        }

        public static bool minionThere()
        {
            var List = MinionManager.GetMinions(Player.Position, xSLxOrbwalker.GetAutoAttackRange())
                .Where(m => HealthPrediction.GetHealthPrediction(m,
                    (int)(Player.Distance(m, false) / Orbwalking.GetMyProjectileSpeed()) * 1000) <=
                            Q.GetDamage(m) + Player.GetAutoAttackDamage(m)
                        ).ToList();
            // Game.PrintChat("QDmg "+Q.GetDamage(List.FirstOrDefault()));
            return List.Count > 0;
        }
        public static Vector3 PosAfterRange(Vector3 p1, Vector3 finalp2, float range)
        {
            var Pos2 = Vector3.Normalize(finalp2 - p1);
            return p1 + (Pos2 * range);
        }

        public static void CastRHitchance(Obj_AI_Hero target)
        {
            var Pred = R.GetPrediction(target);
            if (Pred.Hitchance >= HitChance.VeryHigh)
            {
                R.Cast(target, isMenuEnabled("Packets"));
            }
        }

        #region Utility Methods
        public static bool isMenuEnabled(String val)
        {
            return Menu.Item(val).GetValue<bool>();
        }
        static float getPerValue(bool mana)
        {
            if (mana) return (Player.Mana / Player.MaxMana) * 100;
            return (Player.Health / Player.MaxHealth) * 100;
        }
        static float getPerValueTarget(Obj_AI_Hero target, bool mana)
        {
            if (mana) return (target.Mana / target.MaxMana) * 100;
            return (target.Health / target.MaxHealth) * 100;
        }
        public static void UseItem(int id, Obj_AI_Hero target = null)
        {
            if (Items.HasItem(id) && Items.CanUseItem(id))
            {
                Items.UseItem(id, target);
            }
        }
        public static bool isUnderEnTurret(Vector3 Position)
        {
            foreach (var tur in ObjectManager.Get<Obj_AI_Turret>().Where(turr => turr.IsEnemy && (turr.Health != 0)))
            {
                if (tur.Distance(Position) <= 975f) return true;
            }
            return false;
        }
        private static bool getUnitsInPath(Obj_AI_Hero player, Obj_AI_Hero target, Spell spell)
        {
            float distance = player.Distance(target, false);
            List<Obj_AI_Base> minionList = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, spell.Range,
                MinionTypes.All, MinionTeam.NotAlly);
            int numberOfMinions = (from Obj_AI_Minion minion in minionList
                                   let skillshotPosition =
                                       V2E(player.Position,
                                           V2E(player.Position, target.Position,
                                               Vector3.Distance(player.Position, target.Position) - spell.Width + 1).To3D(),
                                           Vector3.Distance(player.Position, minion.Position))
                                   where skillshotPosition.Distance(minion) < spell.Width
                                   select minion).Count();
            int numberOfChamps = (from minion in ObjectManager.Get<Obj_AI_Hero>()
                                  let skillshotPosition =
                                      V2E(player.Position,
                                          V2E(player.Position, target.Position,
                                              Vector3.Distance(player.Position, target.Position) - spell.Width + 1).To3D(),
                                          Vector3.Distance(player.Position, minion.Position))
                                  where skillshotPosition.Distance(minion) < spell.Width && minion.IsEnemy
                                  select minion).Count();
            int totalUnits = numberOfChamps + numberOfMinions - 1;
            // total number of champions and minions the projectile will pass through.
            if (totalUnits == -1) return false;
            double damageReduction = 0;
            damageReduction = ((totalUnits > 7)) ? 0.4 : (totalUnits == 0) ? 1.0 : (1 - ((totalUnits) / 12.5));
            // the damage reduction calculations minus percentage for each unit it passes through!
            return spell.GetDamage(target) * damageReduction >= (target.Health + (distance / 2000) * target.HPRegenRate);
            // - 15 is a safeguard for certain kill.
        }
        private static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
        }
        #endregion
    }
}
