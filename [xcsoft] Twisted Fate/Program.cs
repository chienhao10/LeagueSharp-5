using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace xc_TwistedFate
{
    internal class Program
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static Orbwalking.Orbwalker Orbwalker;
        private static Spell Q, W;
        private static Items.Item Dfg;
        private static Menu Menu;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "TwistedFate")
                return;

            Q = new Spell(SpellSlot.Q, 1450);
            Q.SetSkillshot(0.25f, 40f, 1000f, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 1000);

            Dfg = new Items.Item((int)ItemId.Deathfire_Grasp, 550);

            Menu = new Menu("[xcsoft] Twisted Fate", "xcoft_TF", true);

            Menu orbwalkerMenu = Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

            Menu ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector"));
            TargetSelector.AddToMenu(ts);

            var wMenu = new Menu("Pick Card", "pickcard");
            wMenu.AddItem(new MenuItem("selectgold", "Select Gold").SetValue(new KeyBind("W".ToCharArray()[0], KeyBindType.Press)));
            wMenu.AddItem(new MenuItem("selectblue", "Select Blue").SetValue(new KeyBind("E".ToCharArray()[0], KeyBindType.Press)));
            wMenu.AddItem(new MenuItem("selectred", "Select Red").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Menu.AddSubMenu(wMenu);

            var comboMenu  = new Menu("ComboMode Option", "comboset");
            comboMenu.AddItem(new MenuItem("stunonly", "Q Cast stunned enemy only").SetValue(false));
            Menu.AddSubMenu(comboMenu);

            var predMenu = new Menu("Prediction", "comboset");
            predMenu.AddItem(new MenuItem("kappa", "Maybe Best"));
            Menu.AddSubMenu(predMenu);

            var havefun = new MenuItem("Have fun!", "Have fun!");
            Menu.AddItem(havefun);

            Menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;

            Game.PrintChat("<font color = \"#33CCCC\">[xcsoft] Twisted Fate -</font> Loaded");
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harras();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                LaneClear();

            if (Menu.Item("selectgold").GetValue<KeyBind>().Active)
                CardSelector.StartSelecting(Cards.Yellow);

            if (Menu.Item("selectblue").GetValue<KeyBind>().Active)
                CardSelector.StartSelecting(Cards.Blue);

            if (Menu.Item("selectred").GetValue<KeyBind>().Active)
                CardSelector.StartSelecting(Cards.Red);
        }

        static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "gate")
                CardSelector.StartSelecting(Cards.Yellow);
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Q.IsReady())
                Utility.DrawCircle(Player.Position, Q.Range, Color.Yellow);

            Color temp = Color.Gold;

            if (W.IsReady())
            {
                var wName = Player.Spellbook.GetSpell(SpellSlot.W).Name;

                if (wName == "goldcardlock") temp = Color.Gold;
                else if (wName == "bluecardlock") temp = Color.Blue;
                else if (wName == "redcardlock") temp = Color.Red;
                else if (wName == "PickACard") temp = Color.LightGreen;

                Utility.DrawCircle(Player.Position, 550, temp);
            }
            else
                Utility.DrawCircle(Player.Position, 550, Color.Gray);

            Utility.DrawCircle(Player.Position, 550 + 400, Color.LightGray);//AA+Flash Range
        }

        static void Drawing_OnEndScene(EventArgs args)
        {
            if (Player.IsDead)
                return;

            Utility.DrawCircle(Player.Position, 5500, Color.LightSkyBlue);
            Utility.DrawCircle(Player.Position, 5500, Color.White, 1, 21, true);
        }

        static void Combo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(1450, TargetSelector.DamageType.Magical);

            if (Dfg.IsReady())
            {
                if (target.IsValidTarget(Dfg.Range))
                    Dfg.Cast(target);
            }

            if (W.IsReady())
            {
                if (target.IsValidTarget(W.Range) && target is Obj_AI_Hero)
                    CardSelector.StartSelecting(Cards.Yellow);
            }

            if (Q.IsReady())
            {
                if (target.IsValidTarget(Q.Range) && target is Obj_AI_Hero)
                {
                    var pred = Q.GetPrediction(target);

                    if (Menu.Item("stunonly").GetValue<bool>() && pred.Hitchance == HitChance.Immobile)
                        Q.Cast(target);
                    else if (pred.Hitchance == HitChance.High || pred.Hitchance == HitChance.Dashing)
                        Q.Cast(target);
                    
                }
            }
        }

        static void Harras()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical, true);

            if (Q.IsReady())
            {
                if (target.IsValidTarget(Q.Range) && Q.GetPrediction(target).Hitchance == HitChance.High)
                    Q.Cast(target);
            }
        }

        static void LaneClear()
        {
            if (W.IsReady())
            {
                int minionsInWRange = MinionManager.GetMinions(Player.Position, W.Range).Count;

                if (Utility.ManaPercentage(Player) >= 20)
                {
                    if (minionsInWRange >= 3)
                        CardSelector.StartSelecting(Cards.Red);
                    else
                        CardSelector.StartSelecting(Cards.Blue);
                }
                else
                    CardSelector.StartSelecting(Cards.Blue);
            }
        }
    }
}
