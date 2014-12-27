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

            W = new Spell(SpellSlot.W, 800);

            Dfg = new Items.Item((int)ItemId.Deathfire_Grasp, 750);

            Menu = new Menu("[xcsoft] Twisted Fate", "xcoft_TF", true);

            Menu orbwalkerMenu = Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

            Menu ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector"));
            TargetSelector.AddToMenu(ts);

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
            {
                // combo to kill the enemy
                Combo();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                // lasthit and harass
                Harras();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                // fast minion farming
                LaneClear();
            }
        }

        static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "gate")
            {
                CardSelector.StartSelecting(Cards.Yellow);
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Q.IsReady())
                Utility.DrawCircle(Player.Position, Q.Range, Color.LightSkyBlue, 2, 21);

            if (W.IsReady())
                Utility.DrawCircle(Player.Position, W.Range, Color.LightSkyBlue, 2, 21);
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
                {
                    Dfg.Cast(target);
                }
            }

            if (W.IsReady())
            {
                if (target.IsValidTarget(W.Range) && target is Obj_AI_Hero)
                {
                    CardSelector.StartSelecting(Cards.Yellow);
                }
            }

            if (Q.IsReady())
            {
                if (target.IsValidTarget(Q.Range) && target is Obj_AI_Hero)
                {
                    Q.Cast(target);
                }
            }
        }

        static void Harras()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical, true);

            if (Q.IsReady())
            {
                if (target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target);
                }
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
                    {
                        CardSelector.StartSelecting(Cards.Red);
                    }
                    else
                    {
                        CardSelector.StartSelecting(Cards.Blue);
                    }
                        
                }
                else
                {
                    CardSelector.StartSelecting(Cards.Blue);
                }
            }
        }
    }
}
