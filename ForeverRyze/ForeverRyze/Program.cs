using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace ForeverRyze
{
    internal class Program
    {

        public const string ChampName = "Ryze";
        public static Obj_AI_Hero Player = ObjectManager.Player;
        private static Orbwalking.Orbwalker _orbwalker;

        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        private static SpellSlot IgniteSlot;
        
        public static Menu _menu;
        public static Menu TargetedItems;
        public static Menu NoTargetedItems;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            #region Check Name
            Player = ObjectManager.Player;
            if (Player.BaseSkinName != ChampName)
                return; 
            #endregion

            #region Abilities and Summoners
            Q = new Spell(SpellSlot.Q, 625f, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 600f, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 600f, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R);

            SpellList.AddRange(new[] { Q, W, E, R });

            IgniteSlot = Player.GetSpellSlot("SummonerDot"); 
            #endregion

            #region Menu
            #region OrbwalkerMenu
            _menu = new Menu("Forever Ryze", "forever.ryze", true);
            var orbwalkerMenu = new Menu("Orbwalker", "forever.ryze.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

            _menu.AddSubMenu(orbwalkerMenu);
            #endregion OrbwalkerMenu
            #region TargetSelectorMenu
            var targetSelectorMenu = new Menu("Target Selector", "forever.ryze.targetselecter");
            {
                TargetSelector.AddToMenu(_menu.AddSubMenu(targetSelectorMenu));
            }
            #endregion TargetSelectorMenu
            #region ComboMenu
            var comboMenu = new Menu("Combo", "forever.ryze.combo");
            {
                comboMenu.AddItem(new MenuItem("forever.ryze.combo.useq", "Combo - Q").SetValue(true));
                comboMenu.AddItem(new MenuItem("forever.ryze.combo.usew", "Combo - W").SetValue(true));
                comboMenu.AddItem(new MenuItem("forever.ryze.combo.usee", "Combo - E").SetValue(true));
                comboMenu.AddItem(new MenuItem("forever.ryze.combo.user", "Combo - R").SetValue(true));
                comboMenu.AddItem(new MenuItem("forever.ryze.combo.userhp", "Use Ult Below % HP").SetValue(new Slider(60, 1, 100)));
                comboMenu.AddItem(new MenuItem("forever.ryze.combo.active", "Combo - Active").SetValue(new KeyBind(32, KeyBindType.Press)));
            }
            _menu.AddSubMenu(comboMenu);
            #endregion ComboMenu
            #region HarassMenu
            var harassMenu = new Menu("Harass", "forever.ryze.harass");
            {
                harassMenu.AddItem(new MenuItem("forever.ryze.harass.useq", "Harass - Q").SetValue(true));
                harassMenu.AddItem(new MenuItem("forever.ryze.harass.auto", "Harass - Auto Q").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)));
                harassMenu.AddItem(new MenuItem("forever.ryze.harass.usew", "Harass - W").SetValue(true));
                harassMenu.AddItem(new MenuItem("forever.ryze.harass.usee", "Harass - E").SetValue(true));
                harassMenu.AddItem(new MenuItem("forever.ryze.harass.mana", "Harass - Min Mana %").SetValue(new Slider(50, 0, 100)));
                harassMenu.AddItem(new MenuItem("forever.ryze.harass.active", "Harass - Active").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            }
            _menu.AddSubMenu(harassMenu);
            #endregion HarassMenu
            #region FarmMenu
            var farmMenu = new Menu("Farm", "forever.ryze.farm");
            {
                farmMenu.AddItem(new MenuItem("forever.ryze.farm.useq", "Farm - Q").SetValue(true));
                farmMenu.AddItem(new MenuItem("forever.ryze.farm.usee", "Farm - E").SetValue(true));
                farmMenu.AddItem(new MenuItem("forever.ryze.farm.mana", "Farm - Min Mana %").SetValue(new Slider(50, 0, 100)));
                farmMenu.AddItem(new MenuItem("forever.ryze.farm.active", "Farm - Active").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            }
            _menu.AddSubMenu(farmMenu);
            #endregion FarmMenu
            #region MiscMenu
            var miscMenu = new Menu("Misc", "forever.ryze.misc");
            {
                miscMenu.AddItem(new MenuItem("forever.ryze.misc.ks", "Killsteal").SetValue(true));
                miscMenu.AddItem(new MenuItem("forever.ryze.misc.addmore", "More to be Added"));
            }
            _menu.AddSubMenu(miscMenu);
            #endregion MiscMenu
            #region DrawingsMenu
            var drawingsMenu = new Menu("Drawings", "forever.ryze.drawings");
            {
                drawingsMenu.AddItem(new MenuItem("forever.ryze.drawings.q", "Draw Q Range").SetValue(true));
                drawingsMenu.AddItem(new MenuItem("forever.ryze.drawings.w", "Draw W Range").SetValue(true));
                drawingsMenu.AddItem(new MenuItem("forever.ryze.drawings.e", "Draw E Range").SetValue(true));
            }
            _menu.AddSubMenu(drawingsMenu);
            #endregion DrawingsMenu

            _menu.AddToMainMenu();
            #endregion Menu

            #region Run
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Game.PrintChat("Loading Forever Ryze - Billy GG");

            #endregion Run
        }
        
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;
            
            if (_menu.Item("forever.ryze.combo.active").GetValue<KeyBind>().Active)
            {
                Overload();
                RunePrison();
                SpellFlux();
                DespPower();
            }
            else
            {
                if (_menu.Item("forever.ryze.harass.active").GetValue<KeyBind>().Active)
                {
                    Overload();
                    RunePrison();
                    SpellFlux();
                }
                if (_menu.Item("forever.ryze.harass.auto").GetValue<KeyBind>().Active)
                {
                    OverloadAutoHarass();
                }
                if (_menu.Item("forever.ryze.farm.active").GetValue<KeyBind>().Active)
                {
                    Overload();
                    SpellFlux();
                }
            }
        }

        private static void OverloadAutoHarass()
        {
            if (!_menu.Item("forever.ryze.harass.auto").GetValue<Bool>())
                return;

            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (Q.IsReady())
            {
                int sliderValue = _menu.Item("forever.ryze.harass.mana").GetValue<Slider>().Value;
                float manaPercent = Player.Mana / Player.MaxMana * 100;
                if (target.IsValidTarget(Q.Range) && manaPercent < sliderValue)
                {
                    Q.CastOnUnit(target);
                }
            }
        }

        private static void DespPower()
        {
            if (!_menu.Item("forever.ryze.combo.user").GetValue<Bool>())
                return;
            if (R.IsReady())
            {
                int sliderValue = _menu.Item("forever.ryze.combo.userhp").GetValue<Slider>().Value;
                float healthPercent = Player.Health / Player.MaxHealth * 100;
                if (healthPercent < sliderValue)
                    R.CastOnUnit(Player);
            }
        }

        private static void Overload()
        {
            if (!_menu.Item("forever.ryze.combo.useq").GetValue<Bool>())
                return;

            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (Q.IsReady())
            {
                if (target.IsValidTarget(Q.Range))
                {
                    Q.CastOnUnit(target);
                }
            }
        }

        private static void RunePrison()
        {
            if (!_menu.Item("forever.ryze.combo.usew").GetValue<Bool>())
                return;
            Obj_AI_Hero target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            if (W.IsReady())
            {
                if (target.IsValidTarget())
                {
                    W.CastOnUnit(target);
                }
            }
        }

        private static void SpellFlux()
        {
            if (!_menu.Item("forever.ryze.combo.usee").GetValue<Bool>())
                return;
            Obj_AI_Hero target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (E.IsReady())
            {
                if (target.IsValidTarget())
                {
                    E.CastOnUnit(target);
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (_menu.Item("forever.ryze.drawings.q").GetValue<Bool>())
            {
                if (Q.IsReady())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Color.AliceBlue);
                }
            }
            if (_menu.Item("forever.ryze.drawings.w").GetValue<Bool>())
            {
                if (W.IsReady())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, Color.AliceBlue);
                }
            }
            if (_menu.Item("forever.ryze.drawings.e").GetValue<Bool>())
            {
                if (E.IsReady())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, Color.AliceBlue);
                }
            }

        }
    }
}
