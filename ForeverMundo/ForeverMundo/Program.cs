#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace ForeverMundo
{
    internal class Program
    {
        public const string ChampionName = "DrMundo";
        public static Obj_AI_Hero Player;
        private static Menu _menu;
        private static Orbwalking.Orbwalker _orbwalker;

        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        private static SpellSlot IgniteSlot;
        private static SpellSlot SmiteSlot;

        public static bool WActive = false;

        public static Menu Config;
        public static Menu TargetedItems;
        public static Menu NoTargetedItems;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
                
        static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.BaseSkinName != ChampionName) return;

            //Spells and Summoners
            Q = new Spell(SpellSlot.Q, 1000);
            W = new Spell(SpellSlot.W, Player.AttackRange + 25);
            E = new Spell(SpellSlot.E, Player.AttackRange + 25);
            R = new Spell(SpellSlot.R, Player.AttackRange + 25);

            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            SpellList.AddRange(new[] { Q, W, E, R });

            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            SmiteSlot = Player.GetSpellSlot("SummonerSmite");

            //Menu Begin
            _menu = new Menu("Forever Mundo", "forever.mundo", true);
            var orbwalkerMenu = new Menu("Orbwalker", "forever.mundo.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

            _menu.AddSubMenu(orbwalkerMenu);

            var targetSelectorMenu = new Menu("Target Selector", "forever.mundo.targetselector");
            {
                TargetSelector.AddToMenu(_menu.AddSubMenu(targetSelectorMenu));
            }

            var comboMenu = new Menu("Combo", "forever.mundo.combo");
            {
                comboMenu.AddItem(new MenuItem("forever.mundo.combo.useq", "Use Q").SetValue(true));
                comboMenu.AddItem(new MenuItem("forever.mundo.combo.usew", "Use W").SetValue(true));
                comboMenu.AddItem(new MenuItem("forever.mundo.combo.usewat", "-> If Hp Above %").SetValue(new Slider(30, 100, 0)));
                comboMenu.AddItem(new MenuItem("forever.mundo.combo.usee", "Use E").SetValue(true));
                comboMenu.AddItem(new MenuItem("forever.mundo.combo.user", "Use R").SetValue(true));
                comboMenu.AddItem(new MenuItem("forever.mundo.combo.active", "Combo Active").SetValue(new KeyBind(32, KeyBindType.Press)));
            }
            _menu.AddSubMenu(comboMenu);

            var harrassMenu = new Menu("Harrass", "forever.mundo.harrass");
            {
                harrassMenu.AddItem(new MenuItem("forever.mundo.harrass.useq", "Use Q").SetValue(true));
                harrassMenu.AddItem(new MenuItem("forever.mundo.harrass.useE", "Use E").SetValue(true));
                harrassMenu.AddItem(new MenuItem("forever.mundo.harrass.auto", "Auto Harrass Q?").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Toggle)));
                harrassMenu.AddItem(new MenuItem("forever.mundo.harrass.life", "Don't Harrass If < HP%").SetValue(new Slider(60, 100, 0)));
            }
            _menu.AddSubMenu(harrassMenu);

            var farmMenu = new Menu("Farm", "forever.mundo.farm");
            {
                farmMenu.AddItem(new MenuItem("forever.mundo.farm.useq", "Use Q").SetValue(true));
                farmMenu.AddItem(new MenuItem("forever.mundo.farm.usew", "Use W").SetValue(true));
                farmMenu.AddItem(new MenuItem("forever.mundo.farm.usee", "Use E").SetValue(true));
                farmMenu.AddItem(new MenuItem("forever.mundo.farm.active", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                farmMenu.AddItem(new MenuItem("forever.mundo.farm.freeze", "Freeze!").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            }
            _menu.AddSubMenu(farmMenu);

            var jungleMenu = new Menu("Jungle", "forever.mundo.jungle");
            {
                jungleMenu.AddItem(new MenuItem("forever.mundo.jungle.useq", "Use Q").SetValue(true));
                jungleMenu.AddItem(new MenuItem("forever.mundo.jungle.usew", "Use W").SetValue(true));
                jungleMenu.AddItem(new MenuItem("forever.mundo.jungle.useq", "Use Q").SetValue(true));
            }
            _menu.AddSubMenu(jungleMenu);

            var miscMenu = new Menu("Misc", "forever.mundo.misc");
            {
                miscMenu.AddItem(new MenuItem("forever.mundo.misc.ks", "Killsteal using Q").SetValue(true));
                miscMenu.AddItem(new MenuItem("forever.mundo.misc.ignite", "Auto Ignite").SetValue(true));
                miscMenu.AddItem(new MenuItem("forever.mundo.misc.qrange", "Q Range").SetValue(new Slider(980, 1000, 0)));
                miscMenu.AddItem(new MenuItem("forever.mundo.misc.user", "Use R To Save Life").SetValue(true));
                miscMenu.AddItem(new MenuItem("forever.mundo.misc.userat", "-> If Hp Lower Than %").SetValue(new Slider(30, 100, 0)));
            }
            _menu.AddSubMenu(miscMenu);

            var drawingMenu = new Menu("Drawing", "forever.mundo.drawings");
            {
                drawingMenu.AddItem(new MenuItem("forever.mundo.drawings.q", "Draw Q").SetValue(new Circle(false, System.Drawing.Color.White)));
                drawingMenu.AddItem(new MenuItem("forever.mundo.drawings.linewidth", "Line Width").SetValue(new Slider(5, 30, 0)));
                drawingMenu.AddItem(new MenuItem("forever.mundo.drawings.linequality", "Line Quality").SetValue(new Slider(30, 100, 0)));
            }
            _menu.AddSubMenu(drawingMenu);

            _menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;

            Game.PrintChat("<font color=\"#00BFFF\">Forever" + ChampionName + " -</font> <font color=\"#FFFFFF\">Loaded!</font>");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            if (Player.HasBuff("BurningAgony"))
            {
                WActive = true;
            }
            else
            {
                WActive = false;
            }

            if (WActive && W.IsReady())
            {
                int inimigos = Utility.CountEnemiesInRange(600);

                if (!Config.Item("LaneClearActive").GetValue<KeyBind>().Active && !Config.Item("JungleFarmActive").GetValue<KeyBind>().Active && inimigos == 0)
                {
                    W.Cast();
                }
            }


            if (_menu.Item("forever.mundo.combo.active").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();

                if (Config.Item("harassToggle").GetValue<KeyBind>().Active)
                    ToggleHarass();

                if (Config.Item("FreezeActive").GetValue<KeyBind>().Active)
                {
                    FreezeFarm();
                }
                if (_menu.Item("forever.mundo.farm.active").GetValue<KeyBind>().Active)
                {
                    LaneClear();
                }

                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                    JungleFarm();
            }

            if (Config.Item("lifesave").GetValue<bool>())
                LifeSave();

            if (Config.Item("KS").GetValue<bool>())
                Killsteal();

            if (Config.Item("AutoSmite").GetValue<KeyBind>().Active)
                AutoSmite();

            if (Config.Item("AutoI").GetValue<bool>())
                AutoIgnite();
        }

        private static void AutoIgnite()
        {
            throw new NotImplementedException();
        }

        private static void AutoSmite()
        {
            throw new NotImplementedException();
        }

        private static void Killsteal()
        {
            throw new NotImplementedException();
        }

        private static void LifeSave()
        {
            throw new NotImplementedException();
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                if (Q.IsReady())
                {
                    Q.Cast(mobs[0].Position);
                }
                if (!WActive && W.IsReady())
                {
                    W.Cast();
                }
                if (E.IsReady())
                {
                    E.Cast();
                }
            }
        }

        private static void LaneClear()
        {
            var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range + Q.Width + 30, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
            var allMinionsW = MinionManager.GetMinions(Player.ServerPosition, 350, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
        
            foreach (var vMinion in allMinionsQ)
            {
                var Qdamage = ObjectManager.Player.GetSpellDamage(vMinion, SpellSlot.Q) * 0.85;

                if (_menu.Item("forever.mundo.farm.useq").GetValue<bool>() && Q.IsReady() && Qdamage >= Q.GetHealthPrediction(vMinion))
                {
                    Q.Cast(vMinion.Position);
                }

                if (_menu.Item("forever.mundo.farm.usew").GetValue<bool>() && W.IsReady() && !WActive && allMinionsW.Count > 2)
                {
                    W.Cast();
                }

                if (_menu.Item("forever.mundo.farm.usee").GetValue<bool>() && E.IsReady())
                {
                    E.Cast();
                }
            }
        }

        private static void FreezeFarm()
        {
            throw new NotImplementedException();
        }

        private static void ToggleHarass()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range + Q.Width, TargetSelector.DamageType.Magical);

            int qRange = _menu.Item("forever.mundo.misc.qrange").GetValue<Slider>().Value;

            var RLife = _menu.Item("forever.mundo.harrass.life").GetValue<Slider>().Value;
            var LPercentR = Player.Health * 100 / Player.MaxHealth;

            if (qTarget != null && Q.IsReady() && LPercentR >= RLife && Player.Distance(qTarget) <= qRange)
            {
                PredictionOutput qPred = Q.GetPrediction(qTarget);
                if (qPred.Hitchance >= HitChance.High)
                    Q.Cast(qPred.CastPosition);
            }
        }

        private static void Harass()
        {
            throw new NotImplementedException();
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (_menu.Item("forever.mundo.drawings.q").GetValue<Circle>().Active)
            {
                Render.Circle.DrawCircle(Player.Position, _menu.Item("forever.mundo.misc.qrange").GetValue<Slider>().Value, _menu.Item("forever.mundo.drawings.q").GetValue<Circle>().Color, _menu.Item("forever.mundo.drawings.linewidth").GetValue<Slider>().Value);
                //Utility.DrawCircle(Player.Position, _menu.Item("forever.mundo.misc.qrange").GetValue<Slider>().Value, _menu.Item("forever.mundo.drawings.q").GetValue<Circle>().Color, _menu.Item("forever.mundo.drawings.linewidth").GetValue<Slider>().Value, _menu.Item("forever.mundo.drawings.linequality").GetValue<Slider>().Value);
            }
        }

        private static void Combo()
        {
            int qRange = _menu.Item("forever.mundo.misc.qrange").GetValue<Slider>().Value;

            var qTarget = TargetSelector.GetTarget(Q.Range + Q.Width, TargetSelector.DamageType.Magical);

            var wHealth = _menu.Item("forever.mundo.combo.usewat").GetValue<Slider>().Value;

            if (qTarget != null && _menu.Item("forever.mundo.combo.useq").GetValue<bool>() && Q.IsReady() && Player.Distance(qTarget) <= qRange)
            {
                PredictionOutput qPred = Q.GetPrediction(qTarget);
                if (qPred.Hitchance >= HitChance.High)
                    Q.Cast(qPred.CastPosition);
            }
            if (!WActive && qTarget != null && _menu.Item("forever.mundo.combo.usew").GetValue<bool>() && W.IsReady() && Player.Distance(qTarget) <= 300)
            {
                W.Cast();
            }
            if (qTarget != null && Config.Item("UseECombo").GetValue<bool>() && E.IsReady() && Player.Distance(qTarget) <= qRange)
            {
                E.Cast();
            }
        }
    }
}
