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

            Dfg = new Items.Item((int)ItemId.Deathfire_Grasp, Orbwalking.GetRealAutoAttackRange(Player));

            Menu = new Menu("[xcsoft] Twisted Fate", "xcoft_TF", true);

            Menu orbwalkerMenu = Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

            Menu ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector"));
            TargetSelector.AddToMenu(ts);

            var wMenu = new Menu("Pick Card [You maybe not use it (ComboMode OP)]", "pickcard");
            wMenu.AddItem(new MenuItem("selectgold", "Select Gold").SetValue(new KeyBind("W".ToCharArray()[0], KeyBindType.Press)));
            wMenu.AddItem(new MenuItem("selectblue", "Select Blue").SetValue(new KeyBind("E".ToCharArray()[0], KeyBindType.Press)));
            wMenu.AddItem(new MenuItem("selectred", "Select Red").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Menu.AddSubMenu(wMenu);

            var comboMenu  = new Menu("ComboMode Option", "comboop");
            comboMenu.AddItem(new MenuItem("cconly", "Q Cast to CC state enemy only (Not recommended)").SetValue(false));
            Menu.AddSubMenu(comboMenu);

            var AdditionalsMenu = new Menu("Additional Option", "additionals");
            AdditionalsMenu.AddItem(new MenuItem("goldR", "Select Gold When Using Ultimate").SetValue(true));
            Menu.AddSubMenu(AdditionalsMenu);

            var lasthitMenu = new Menu("Lasthit Settings", "lasthitset");
            lasthitMenu.AddItem(new MenuItem("lasthitUseW", "Use W (Blue only)").SetValue(false));
            lasthitMenu.AddItem(new MenuItem("lasthitbluemana", "Lasthit with blue if mana % <").SetValue(new Slider(20, 0, 100)));
            Menu.AddSubMenu(lasthitMenu);

            var laneclearMenu = new Menu("LaneClear Settings", "laneclearset");
            laneclearMenu.AddItem(new MenuItem("laneclearUseW", "Use W").SetValue(true));
            laneclearMenu.AddItem(new MenuItem("laneclearbluemana", "Blue instead of red if mana % <").SetValue(new Slider(20, 0, 100)));
            laneclearMenu.AddItem(new MenuItem("laneclearmc", "Red if Minions count >=").SetValue(new Slider(3, 2, 5)));
            Menu.AddSubMenu(laneclearMenu);

            var Drawings = new Menu("Drawings Settings", "Drawings");
            Drawings.AddItem(new MenuItem("AAcircle", "AA Range").SetValue(true));
            Drawings.AddItem(new MenuItem("FAAcircle", "Flash + AA Range").SetValue(new Circle(true, Color.LightGray)));
            Drawings.AddItem(new MenuItem("Qcircle", "Q Range").SetValue(new Circle(true, Color.Gold)));
            Drawings.AddItem(new MenuItem("Rcircle", "R Range").SetValue(new Circle(true, Color.LightSkyBlue)));
            Drawings.AddItem(new MenuItem("RcircleMap", "R Range (minimap)").SetValue(new Circle(true, Color.White)));
            Drawings.AddItem(new MenuItem("drawMinionLastHit", "Minion Last Hit").SetValue(new Circle(true, Color.GreenYellow)));
            Drawings.AddItem(new MenuItem("drawMinionNearKill", "Minion Near Kill").SetValue(new Circle(true, Color.Gray)));
            Menu.AddSubMenu(Drawings);

            var predMenu = new Menu("Prediction", "pred");
            predMenu.AddItem(new MenuItem("kappa", "Maybe Best"));
            Menu.AddSubMenu(predMenu);

            var havefun = new MenuItem("Have fun!", "Have fun!");
            Menu.AddItem(havefun);

            Menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            Orbwalking.BeforeAttack += OrbwalkingOnBeforeAttack;

            Game.PrintChat("<font color = \"#33CCCC\">[xcsoft] Twisted Fate -</font> Loaded");
        }

        private static void OrbwalkingOnBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                args.Process = CardSelector.Status != SelectStatus.Selecting && Environment.TickCount - CardSelector.LastWSent > 300;
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harras();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                Lasthit();

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
            if (sender.IsMe && args.SData.Name == "gate" && Menu.Item("goldR").GetValue<bool>())
                CardSelector.StartSelecting(Cards.Yellow);
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Q.IsReady() && Menu.Item("Qcircle").GetValue<Circle>().Active)
                Utility.DrawCircle(Player.Position, Q.Range, Color.Yellow);

            Color temp = Color.Gold;

            if (Menu.Item("AAcircle").GetValue<bool>())
            {
                if (W.IsReady())
                {
                    var wName = Player.Spellbook.GetSpell(SpellSlot.W).Name;

                    if (wName == "goldcardlock") temp = Color.Gold;
                    else if (wName == "bluecardlock") temp = Color.Blue;
                    else if (wName == "redcardlock") temp = Color.Red;
                    else if (wName == "PickACard") temp = Color.LightGreen;

                    Utility.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), temp);
                }
                else
                    Utility.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), Color.Gray);
            }

            var FAAcircle = Menu.Item("FAAcircle").GetValue<Circle>();

            if (FAAcircle.Active)
                Utility.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player) + 400, FAAcircle.Color);//AA+Flash Range

            var drawMinionLastHit = Menu.Item("drawMinionLastHit").GetValue<Circle>();
            var drawMinionNearKill = Menu.Item("drawMinionNearKill").GetValue<Circle>();
            if (drawMinionLastHit.Active || drawMinionNearKill.Active)
            {
                var xMinions =
                    MinionManager.GetMinions(ObjectManager.Player.Position, ObjectManager.Player.AttackRange + ObjectManager.Player.BoundingRadius + 300, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);

                foreach (var xMinion in xMinions)
                {
                    if (drawMinionLastHit.Active && ObjectManager.Player.GetAutoAttackDamage(xMinion, true) >= xMinion.Health)
                    {
                        Utility.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionLastHit.Color);
                    }
                    else if (drawMinionNearKill.Active && ObjectManager.Player.GetAutoAttackDamage(xMinion, true) * 2 >= xMinion.Health)
                    {
                        Utility.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionNearKill.Color);
                    }
                }
            }
        }

        static void Drawing_OnEndScene(EventArgs args)
        {
            if (Player.IsDead)
                return;

            var Rcircle = Menu.Item("Rcircle").GetValue <Circle>();

            if (Rcircle.Active) 
                Utility.DrawCircle(Player.Position, 5500, Rcircle.Color);

            var Rcirclemap = Menu.Item("RcircleMap").GetValue<Circle>();

            if (Rcirclemap.Active) 
            Utility.DrawCircle(Player.Position, 5500, Rcirclemap.Color, 1, 21, true);
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

                    if (Menu.Item("cconly").GetValue<bool>())
                    {
                        if (pred.Hitchance == HitChance.VeryHigh || pred.Hitchance == HitChance.Immobile || pred.Hitchance == HitChance.Dashing)
                        {
                            foreach (var buff in target.Buffs)
                            {
                                if (buff.Type == BuffType.Stun || buff.Type == BuffType.Taunt || buff.Type == BuffType.Snare || buff.Type == BuffType.Suppression || buff.Type == BuffType.Charm || buff.Type == BuffType.Fear || buff.Type == BuffType.Flee || buff.Type == BuffType.Slow)
                                    Q.Cast(target);
                            }
                        } 
                    }
                    else if (pred.Hitchance == HitChance.High || pred.Hitchance == HitChance.Dashing || pred.Hitchance == HitChance.Immobile)
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

        static void Lasthit()
        {
            if (W.IsReady())
            {
                if (Menu.Item("lasthitUseW").GetValue<bool>())
                {
                    if (Utility.ManaPercentage(Player) < Menu.Item("lasthitbluemana").GetValue<Slider>().Value)
                    {
                        var xMinions = MinionManager.GetMinions(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);

                        foreach (var xMinion in xMinions)
                        {
                            if (Player.GetAutoAttackDamage(xMinion, true) * 3 >= xMinion.Health)
                            {
                                CardSelector.StartSelecting(Cards.Blue);
                            }
                        }
                    }
                }
            }
        }

        static void LaneClear()
        {
            if (W.IsReady())
            {
                int minionsInWRange = MinionManager.GetMinions(Player.Position, W.Range).Count;

                if (Menu.Item("laneclearUseW").GetValue<bool>())
                {
                    if (Utility.ManaPercentage(Player) > Menu.Item("laneclearbluemana").GetValue<Slider>().Value)
                    {
                        if (minionsInWRange >= Menu.Item("laneclearmc").GetValue<Slider>().Value)
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
}
