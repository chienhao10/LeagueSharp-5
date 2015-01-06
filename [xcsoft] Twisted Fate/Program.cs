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
        private static Items.Item Dfg, Bft;
        private static Menu Menu;
        private static SpellSlot SFlash;
        private static SpellSlot SIgnite;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "TwistedFate")
                return;

            SFlash = Player.GetSpellSlot("SummonerFlash");
            SIgnite = Player.GetSpellSlot("SummonerIgnite");

            Dfg = new Items.Item((int)ItemId.Deathfire_Grasp, Orbwalking.GetRealAutoAttackRange(Player));
            Bft = new Items.Item((int)ItemId.Blackfire_Torch, Orbwalking.GetRealAutoAttackRange(Player));

            Q = new Spell(SpellSlot.Q, 1450);
            Q.SetSkillshot(0.25f, 40f, 1000f, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 1000);

            Menu = new Menu("[xcsoft] Twisted Fate", "xcoft_TF", true);

            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Menu.AddSubMenu(orbwalkerMenu);

            var ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector"));
            TargetSelector.AddToMenu(ts);

            var wMenu = new Menu("Pick a Card", "pickcard");
            wMenu.AddItem(new MenuItem("selectgold", "Select Gold").SetValue(new KeyBind('W', KeyBindType.Press)));
            wMenu.AddItem(new MenuItem("selectblue", "Select Blue").SetValue(new KeyBind('E', KeyBindType.Press)));
            wMenu.AddItem(new MenuItem("selectred", "Select Red").SetValue(new KeyBind('T', KeyBindType.Press)));
            wMenu.AddItem(new MenuItem("plz1", "Do not use the W key"));
            wMenu.AddItem(new MenuItem("plz2", "W key is sometimes random selection :("));
            Menu.AddSubMenu(wMenu);

            var comboMenu  = new Menu("ComboMode Settings", "comboop");
            comboMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("useW", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("qrange", "Use Q if target is in selected range").SetValue(new Slider(1200, (int)Orbwalking.GetRealAutoAttackRange(Player), 1450)));
            comboMenu.AddItem(new MenuItem("cconly", "Q Cast to CC state enemy only").SetValue(false));
            comboMenu.AddItem(new MenuItem("ignoreshield", "Ignore shield target").SetValue(false));
            comboMenu.AddItem(new MenuItem("useblue", "Blue instead of gold if low mana(<20%)").SetValue(false));
            comboMenu.AddItem(new MenuItem("usepacket", "Packet casting for Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("usedfg", "Use Deathfire Grasp").SetValue(true));
            comboMenu.AddItem(new MenuItem("usebft", "Use Blackfire Torch").SetValue(true));
            Menu.AddSubMenu(comboMenu);

            var AdditionalsMenu = new Menu("Additional Options", "additionals");
            AdditionalsMenu.AddItem(new MenuItem("goldR", "Select Gold when using ultimate(gate)").SetValue(true));
            AdditionalsMenu.AddItem(new MenuItem("killsteal", "Use Killsteal").SetValue(true));
            AdditionalsMenu.AddItem(new MenuItem("gapcloser", "Use Anti-gapcloser").SetValue(true));
            AdditionalsMenu.AddItem(new MenuItem("interrupt", "Use Auto-interrupt").SetValue(true));
            Menu.AddSubMenu(AdditionalsMenu);

            var harrasMenu = new Menu("Harras Settings", "harassop");
            harrasMenu.AddItem(new MenuItem("harrasUseQ", "Use Q").SetValue(true));
            harrasMenu.AddItem(new MenuItem("harrasrange", "Harras Range").SetValue(new Slider(1200, (int)Orbwalking.GetRealAutoAttackRange(Player), 1450)));
            Menu.AddSubMenu(harrasMenu);

            var lasthitMenu = new Menu("Lasthit Settings", "lasthitset");
            lasthitMenu.AddItem(new MenuItem("lasthitUseW", "Use W (Blue only)").SetValue(true));
            lasthitMenu.AddItem(new MenuItem("lasthitbluemana", "Lasthit with blue if mana % <").SetValue(new Slider(20, 0, 100)));
            Menu.AddSubMenu(lasthitMenu);

            var laneclearMenu = new Menu("LaneClear Settings", "laneclearset");
            laneclearMenu.AddItem(new MenuItem("laneclearUseQ", "Use Q").SetValue(true));
            laneclearMenu.AddItem(new MenuItem("laneclearQmana", "Q cast if mana % >").SetValue(new Slider(20, 0, 100)));
            laneclearMenu.AddItem(new MenuItem("laneclearQmc", "Q cast if Hit possible Minions count >=").SetValue(new Slider(5, 2, 7)));
            laneclearMenu.AddItem(new MenuItem("laneclearUseW", "Use W").SetValue(true));
            laneclearMenu.AddItem(new MenuItem("laneclearredmc", "Red instead of blue if Minions count >=").SetValue(new Slider(3, 2, 5)));
            laneclearMenu.AddItem(new MenuItem("laneclearbluemana", "Blue instead of red if mana % <").SetValue(new Slider(20, 0, 100)));
            Menu.AddSubMenu(laneclearMenu);

            var Drawings = new Menu("Drawings Settings", "Drawings");
            Drawings.AddItem(new MenuItem("AAcircle", "AA Range").SetValue(true));
            Drawings.AddItem(new MenuItem("FAAcircle", "Flash + AA Range").SetValue(true));
            Drawings.AddItem(new MenuItem("Qcircle", "Q Range").SetValue(new Circle(true, Color.LightSkyBlue)));
            Drawings.AddItem(new MenuItem("Rcircle", "R Range").SetValue(new Circle(true, Color.LightSkyBlue)));
            Drawings.AddItem(new MenuItem("RcircleMap", "R Range (minimap)").SetValue(new Circle(true, Color.White)));
            Drawings.AddItem(new MenuItem("drawMinionLastHit", "Minion Last Hit").SetValue(new Circle(true, Color.GreenYellow)));
            Drawings.AddItem(new MenuItem("drawMinionNearKill", "Minion Near Kill").SetValue(new Circle(true, Color.Gray)));
            
            var drawComboDamageMenu = new MenuItem("Draw_ComboDamage", "Draw Combo Damage").SetValue(true);
            var drawFill = new MenuItem("Draw_Fill", "Draw Combo Damage Fill").SetValue(new Circle(true, Color.FromArgb(90, 255, 169, 4)));
            Drawings.AddItem(drawComboDamageMenu);
            Drawings.AddItem(drawFill);
            DamageIndicator.DamageToUnit = GetComboDamage;
            DamageIndicator.Enabled = drawComboDamageMenu.GetValue<bool>();
            DamageIndicator.Fill = drawFill.GetValue<Circle>().Active;
            DamageIndicator.FillColor = drawFill.GetValue<Circle>().Color;
            drawComboDamageMenu.ValueChanged +=
            delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                DamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };
            drawFill.ValueChanged +=
            delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                DamageIndicator.Fill = eventArgs.GetNewValue<Circle>().Active;
                DamageIndicator.FillColor = eventArgs.GetNewValue<Circle>().Color;
            };

            Drawings.AddItem(new MenuItem("jgpos", "JunglePosition").SetValue(true));

            Menu.AddSubMenu(Drawings);

            var predMenu = new Menu("Prediction", "pred");
            predMenu.AddItem(new MenuItem("kappa", "Maybe Best"));
            Menu.AddSubMenu(predMenu);

            var havefun = new MenuItem("Have fun!", "Have fun!");
            Menu.AddItem(havefun);

            var movement = new MenuItem("movement", "Disable orbwalk movement").SetValue(false);
            movement.ValueChanged +=
            delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Orbwalker.SetMovement(!eventArgs.GetNewValue<bool>());
            };

            Menu.AddItem(movement);

            Menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;

            Game.PrintChat("<font color = \"#33CCCC\">[xcsoft] Twisted Fate -</font> Loaded");
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!Menu.Item("gapcloser").GetValue<bool>())
                return;

            if(gapcloser.Sender.IsValidTarget(W.Range))
            {
                CardSelector.StartSelecting(Cards.Yellow);

                Utility.DrawCircle(gapcloser.Sender.Position, 50, Color.Gold);

                var targetpos = Drawing.WorldToScreen(gapcloser.Sender.Position);

                Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, Color.Gold, "Gapcloser");
            }

            if (Player.HasBuff("goldcardpreattack" , true) && gapcloser.Sender.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player) + 10) && gapcloser.Sender.IsTargetable)
                Player.IssueOrder(GameObjectOrder.AttackUnit, gapcloser.Sender);
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Menu.Item("interrupt").GetValue<bool>())
                return;

            if (spell.BuffName == "Destiny")
                return;

            if (unit.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player) + 400))
            {
                CardSelector.StartSelecting(Cards.Yellow);

                Utility.DrawCircle(unit.Position, 50, Color.Gold);

                var targetpos = Drawing.WorldToScreen(unit.Position);

                Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, Color.Gold, "Interrupt");
            }
                
            if (Player.HasBuff("goldcardpreattack", true) && unit.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player) + 10) && unit.IsTargetable)
                Player.IssueOrder(GameObjectOrder.AttackUnit, unit);
            
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
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

            killsteal();
        }

        static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "gate" && Menu.Item("goldR").GetValue<bool>())
            {
                CardSelector.StartSelecting(Cards.Yellow);
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            var Qcircle = Menu.Item("Qcircle").GetValue<Circle>();

            if (Q.IsReady() && Qcircle.Active)
                Utility.DrawCircle(Player.Position, Menu.Item("qrange").GetValue<Slider>().Value, Qcircle.Color);

            if (Menu.Item("AAcircle").GetValue<bool>())
            {
                if (W.IsReady())
                {
                    Color temp = Color.Gold;
                    var wName = Player.Spellbook.GetSpell(SpellSlot.W).Name;

                    if (wName == "goldcardlock") temp = Color.Gold;
                    else if (wName == "bluecardlock") temp = Color.Blue;
                    else if (wName == "redcardlock") temp = Color.Red;
                    else if (wName == "PickACard") temp = Color.LightGreen;

                    Utility.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), temp);
                }
                else
                {
                    Color temp = Color.Gold;

                    if (Player.HasBuff("goldcardpreattack", true)) temp = Color.Gold;
                    else if (Player.HasBuff("bluecardpreattack", true)) temp = Color.Blue;
                    else if (Player.HasBuff("redcardpreattack", true)) temp = Color.Red;
                    else temp = Color.Gray;

                    Utility.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), temp);
                }
            }

            if (Menu.Item("FAAcircle").GetValue<bool>())
            {
                Obj_AI_Hero target = TargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(Player) + 400, TargetSelector.DamageType.Magical, false);

                if (target != null && SFlash != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(SFlash) == SpellState.Ready)
                {
                    Utility.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player) + 400, Color.Gold);//AA+Flash Range

                    if (!target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                    {
                        Utility.DrawCircle(target.Position, 50, Color.Gold);

                        var targetpos = Drawing.WorldToScreen(target.Position);

                        Drawing.DrawText(targetpos[0] - 70, targetpos[1] + 20, Color.Gold, "Flash+AA possible");
                    }

                }
                else
                    Utility.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player) + 400, Color.Gray);//AA+Flash Range
            }

            var drawMinionLastHit = Menu.Item("drawMinionLastHit").GetValue<Circle>();
            var drawMinionNearKill = Menu.Item("drawMinionNearKill").GetValue<Circle>();
            if (drawMinionLastHit.Active || drawMinionNearKill.Active)
            {
                var xMinions =
                    MinionManager.GetMinions(Player.Position, Player.AttackRange + Player.BoundingRadius + 300, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);

                foreach (var xMinion in xMinions)
                {
                    if (drawMinionLastHit.Active && Player.GetAutoAttackDamage(xMinion, true) >= xMinion.Health)
                    {
                        Utility.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionLastHit.Color);
                    }
                    else if (drawMinionNearKill.Active && Player.GetAutoAttackDamage(xMinion, true) * 2 >= xMinion.Health)
                    {
                        Utility.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionNearKill.Color);
                    }
                }
            }

            //Drawing JunglePosition part of Marksman# copy
            if (Game.MapId == (GameMapId)11 && Menu.Item("jgpos").GetValue<bool>())
            {
                const float circleRange = 100f;

                Utility.DrawCircle(new Vector3(7461.018f, 3253.575f, 52.57141f), circleRange, Color.Blue); // blue team :red
                Utility.DrawCircle(new Vector3(3511.601f, 8745.617f, 52.57141f), circleRange, Color.Blue); // blue team :blue
                Utility.DrawCircle(new Vector3(7462.053f, 2489.813f, 52.57141f), circleRange, Color.Blue); // blue team :golems
                Utility.DrawCircle(new Vector3(3144.897f, 7106.449f, 51.89026f), circleRange, Color.Blue); // blue team :wolfs
                Utility.DrawCircle(new Vector3(7770.341f, 5061.238f, 49.26587f), circleRange, Color.Blue); // blue team :wariaths

                Utility.DrawCircle(new Vector3(10930.93f, 5405.83f, -68.72192f), circleRange, Color.Yellow); // Dragon

                Utility.DrawCircle(new Vector3(7326.056f, 11643.01f, 50.21985f), circleRange, Color.Red); // red team :red
                Utility.DrawCircle(new Vector3(11417.6f, 6216.028f, 51.00244f), circleRange, Color.Red); // red team :blue
                Utility.DrawCircle(new Vector3(7368.408f, 12488.37f, 56.47668f), circleRange, Color.Red); // red team :golems
                Utility.DrawCircle(new Vector3(10342.77f, 8896.083f, 51.72742f), circleRange, Color.Red); // red team :wolfs
                Utility.DrawCircle(new Vector3(7001.741f, 9915.717f, 54.02466f), circleRange, Color.Red); // red team :wariaths                    
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
            Utility.DrawCircle(Player.Position, 5500, Rcirclemap.Color, 1, 30, true);
        }

        static void Combo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical, Menu.Item("ignoreshield").GetValue<bool>());

            if (Dfg.IsReady() && Menu.Item("usedfg").GetValue<bool>())
            {
                if (target.IsValidTarget(Dfg.Range))
                    Dfg.Cast(target);
            }

            if (Bft.IsReady() && Menu.Item("usebft").GetValue<bool>())
            {
                if (target.IsValidTarget(Bft.Range))
                    Bft.Cast(target);
            }

            if (W.IsReady() && Menu.Item("useW").GetValue<bool>())
            {
                if (target.IsValidTarget(W.Range))
                {
                    if (Menu.Item("useblue").GetValue<bool>())
                    {
                        if (Utility.ManaPercentage(Player) < 20)
                        {
                            CardSelector.StartSelecting(Cards.Blue);
                        }
                        else
                            CardSelector.StartSelecting(Cards.Yellow);
                    }
                    else
                        CardSelector.StartSelecting(Cards.Yellow);
                }
            }

            if (Q.IsReady() && Menu.Item("useQ").GetValue<bool>())
            {
                if (target.IsValidTarget(Menu.Item("qrange").GetValue<Slider>().Value))
                {
                    var pred = Q.GetPrediction(target);

                    if (Menu.Item("cconly").GetValue<bool>())
                    {
                        if (pred.Hitchance >= HitChance.VeryHigh)
                        {
                            foreach (var buff in target.Buffs)
                            {
                                if (buff.Type == BuffType.Stun || buff.Type == BuffType.Taunt || buff.Type == BuffType.Snare || buff.Type == BuffType.Suppression || buff.Type == BuffType.Charm || buff.Type == BuffType.Fear || buff.Type == BuffType.Flee || buff.Type == BuffType.Slow)
                                    Q.Cast(target, Menu.Item("usepacket").GetValue<bool>());
                            }
                        }
                    }
                    else if (pred.Hitchance >= HitChance.VeryHigh)
                        Q.Cast(target, Menu.Item("usepacket").GetValue<bool>());
                }
            }
        }

        static void Harras()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (Q.IsReady() && Menu.Item("harrasUseQ").GetValue<bool>())
            {
                if (target.IsValidTarget(Menu.Item("harrasrange").GetValue<Slider>().Value) && Q.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
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
                        var xMinions = MinionManager.GetMinions(Player.Position, 700, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);

                        foreach (var xMinion in xMinions)
                        {
                            if (Player.GetAutoAttackDamage(xMinion, false) * 3 >= xMinion.Health)
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
            if (Q.IsReady() && Menu.Item("laneclearUseQ").GetValue<bool>() && Utility.ManaPercentage(Player) > Menu.Item("laneclearQmana").GetValue<Slider>().Value)
            {
                var hitpossible = 0;

                Obj_AI_Base minion = null;
                foreach (Obj_AI_Base minions in MinionManager.GetMinions(Player.Position, Q.Range))
                {
                    if (Q.GetPrediction(minions).Hitchance >= HitChance.High)
                    {
                        hitpossible++;
                        minion = minions;
                    }
                }

                if (hitpossible >= Menu.Item("laneclearQmc").GetValue<Slider>().Value)
                    Q.Cast(minion, Menu.Item("usepacket").GetValue<bool>());
            }

            if (W.IsReady() && Menu.Item("laneclearUseW").GetValue<bool>())
            {
                var minioncount = MinionManager.GetMinions(Player.Position, W.Range).Count;

                if (minioncount > 0)
                {
                    if (Utility.ManaPercentage(Player) > Menu.Item("laneclearbluemana").GetValue<Slider>().Value)
                    {
                        if (minioncount >= Menu.Item("laneclearredmc").GetValue<Slider>().Value)
                            CardSelector.StartSelecting(Cards.Red);
                        else
                            CardSelector.StartSelecting(Cards.Blue);
                    }
                    else
                        CardSelector.StartSelecting(Cards.Blue);
                }
            }
        }

        static float GetComboDamage(Obj_AI_Base enemy)
        {
            var APdmg = 0d;
            var ADdmg = 0d;
            var Truedmg = 0d;
            bool card = false;

            //AP데미지
            if(Q.IsReady())
                APdmg += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (W.IsReady())//카드 돌리고있을때
                APdmg += Player.GetSpellDamage(enemy, SpellSlot.W, 2);//골드카드데미지추가
            else//카드뽑았나?
            {
                card = true;//넌 카드를 들고있다고 생각한다.
                foreach (var buff in Player.Buffs)//패건들지마손모가지날아가붕게
                {//버프이름 JeonHelperForDev 어셈으로 찾음
                    if (buff.Name == "bluecardpreattack")//블루카드들고있네
                        APdmg += Player.GetSpellDamage(enemy, SpellSlot.W);//블루카드데미지추가
                    else if (buff.Name == "redcardpreattack")//레드카드들고있네
                        APdmg += Player.GetSpellDamage(enemy, SpellSlot.W, 1);//레드카드데미지추가
                    else if (buff.Name == "goldcardpreattack")//골드카드들고있네
                        APdmg += Player.GetSpellDamage(enemy, SpellSlot.W, 2);//골드카드데미지추가
                    else card = false;//카드없네
                }
            }

            bool passive = false;
            foreach (var buff in Player.Buffs)
            {
                if (buff.Name == "cardmasterstackparticle")//E패시브있네
                {
                    APdmg += Player.GetSpellDamage(enemy, SpellSlot.E);//패시브딜추가
                    passive = true;
                }

                if (buff.Name == "lichbane")//리치베인패시브있네?
                {
                    APdmg += Damage.CalcDamage(Player, enemy, Damage.DamageType.Magical, (Player.BaseAttackDamage * 0.75) + ((Player.BaseAbilityDamage + Player.FlatMagicDamageMod) * 0.5));//리치베인딜 추가
                    passive = true;
                }

                if (buff.Name == "sheen")//광휘의검(=삼위일체) 패시브있네?
                {
                    ADdmg += Player.GetAutoAttackDamage(enemy, false);//광휘의검딜추가
                    passive = true;
                }
            }

            if (!card && passive)//카드없네 평타로 패시브터트릴건가보네
                ADdmg += Player.GetAutoAttackDamage(enemy, false);//평타딜추가

            if (Dfg.IsReady() && Menu.Item("usedfg").GetValue<bool>())
            {
                APdmg += Player.GetItemDamage(enemy, Damage.DamageItems.Dfg);//데파딜추가
                APdmg = APdmg * 1.2;//20%추가피해
            }
            else if (Bft.IsReady() && Menu.Item("usebft").GetValue<bool>())
            {
                APdmg += Player.GetItemDamage(enemy, Damage.DamageItems.BlackFireTorch);//어둠불꽃횃불딜추가(뒤틀린숲전용)
                APdmg = APdmg * 1.2;//20%추가피해
            }

            //true데미지
            //if (SIgnite != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(SIgnite) == SpellState.Ready)//점화있음?
            //    Truedmg += Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);//점화딜추가

            return (float)ADdmg + (float)APdmg + (float)Truedmg;
        }

        static void killsteal()
        {
            if (!Menu.Item("killsteal").GetValue<bool>())
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(Q.Range) && x.IsEnemy && !x.IsDead && !x.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (target != null)
                {
                    if (Q.IsReady())
                    {
                        if (Q.GetDamage(target) > target.Health + 20 & Q.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
                            Q.Cast(target, Menu.Item("usepacket").GetValue<bool>());
                    }
                }
            }
        }
    }
}
