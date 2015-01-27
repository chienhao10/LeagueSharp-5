using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace Sharpshooter.Champions
{
    public static class Ashe
    {
        static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        static Orbwalking.Orbwalker Orbwalker { get { return SharpShooter.Orbwalker; } }

        static Spell Q, W, E, R;

        static Obj_AI_Hero rTarget;

        static bool QisActive { get { return Player.HasBuff("FrostShot", true); } }

        public static void Load()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1200f);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 20000f);

            W.SetSkillshot(0.25f, (float)(24.32f * Math.PI / 180), 1600f, true, SkillshotType.SkillshotCone);
            R.SetSkillshot(0.25f, 130f, 1600f, false, SkillshotType.SkillshotLine);

            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseW", "Use W", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseR", "Use R", true).SetValue(true));

            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassUseW", "Use W", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassMana", "If Mana % >", true).SetValue(new Slider(50, 0, 100)));

            SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearUseW", "Use W", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearMana", "if Mana % >", true).SetValue(new Slider(60, 0, 100)));

            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearUseW", "Use W", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearMana", "If Mana % >", true).SetValue(new Slider(20, 0, 100)));

            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("antigapcloser", "Use Anti-Gapcloser", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("autointerrupt", "Use Auto-Interrupt", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("CastR", "R Manually Cast", true).SetValue(new KeyBind('T', KeyBindType.Press)));

            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingAA", "Real AA Range", true).SetValue(new Circle(true, Color.DodgerBlue)));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingW", "W Range", true).SetValue(new Circle(true, Color.DodgerBlue)));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingE", "E Range", true).SetValue(new Circle(true, Color.DodgerBlue)));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingR", "R Range", true).SetValue(new Circle(true, Color.DodgerBlue)));

            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }

            if (SharpShooter.Menu.Item("CastR", true).GetValue<KeyBind>().Active)
            {
                Vector3 searchPos;

                if (Player.Distance(Game.CursorPos) < R.Range - 300)
                    searchPos = Game.CursorPos;
                else
                    searchPos = Player.Position +
                                Vector3.Normalize(Game.CursorPos - Player.Position) * (R.Range - 300);

                var rTargettemp = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(R.Range) && hero.Distance(searchPos) < 300).OrderByDescending(TargetSelector.GetPriority);

                if (rTargettemp.Count<Obj_AI_Hero>() > 0)
                {
                    rTarget = rTargettemp.First<Obj_AI_Hero>();

                    if (R.IsReady() && R.GetPrediction(rTarget).Hitchance >= HitChance.VeryHigh)
                        R.Cast(rTarget);
                }
                else
                    rTarget = null;
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            var drawingAA = SharpShooter.Menu.Item("drawingAA", true).GetValue<Circle>();
            var drawingW = SharpShooter.Menu.Item("drawingW", true).GetValue<Circle>();
            var drawingE = SharpShooter.Menu.Item("drawingE", true).GetValue<Circle>();
            var drawingR = SharpShooter.Menu.Item("drawingR", true).GetValue<Circle>();

            if (drawingAA.Active)
                Render.Circle.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), drawingAA.Color);

            if (W.IsReady() && drawingW.Active)
                Render.Circle.DrawCircle(Player.Position, W.Range, drawingW.Color);

            if (E.IsReady() && drawingE.Active)
            {
                Render.Circle.DrawCircle(Player.Position, 1750 + (E.Level * 750), drawingE.Color);
            }
                
            if (R.IsReady() && drawingR.Active)
                Render.Circle.DrawCircle(Player.Position, 1500, drawingR.Color);

            if (SharpShooter.Menu.Item("CastR", true).GetValue<KeyBind>().Active)
            {
                Vector3 DrawPosition;

                if (Player.Distance(Game.CursorPos) < R.Range - 300)
                    DrawPosition = Game.CursorPos;
                else
                    DrawPosition = ObjectManager.Player.Position +
                                   Vector3.Normalize(Game.CursorPos - Player.Position) * (R.Range - 300);

                Render.Circle.DrawCircle(DrawPosition, 300, Color.White);

                if (rTarget != null)
                {
                    var pred = R.GetPrediction(rTarget);

                    var colortemp = Color.WhiteSmoke;

                    if (pred.Hitchance >= HitChance.VeryHigh)
                        colortemp = Color.Gold;

                    Render.Circle.DrawCircle(rTarget.Position, rTarget.BoundingRadius, colortemp);
                    Render.Circle.DrawCircle(pred.CastPosition, R.Width, Color.LightSkyBlue);

                    var targetpos = Drawing.WorldToScreen(rTarget.Position);
                    Drawing.DrawText(targetpos[0] - 30, targetpos[1] + 20, colortemp, "Target: " + rTarget.ChampionName);
                    Drawing.DrawText(targetpos[0] - 30, targetpos[1] + 40, colortemp, "Hitchance: " + pred.Hitchance);
                }
            }
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero && !QisActive)
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && SharpShooter.Menu.Item("comboUseQ", true).GetValue<Boolean>())
                    Q.Cast();

                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && SharpShooter.Menu.Item("harassUseQ", true).GetValue<Boolean>())
                    Q.Cast();
            }

            if (!(args.Target is Obj_AI_Hero) && QisActive)
                Q.Cast();

        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!SharpShooter.Menu.Item("antigapcloser", true).GetValue<Boolean>() || Player.IsDead)
                return;

            if (gapcloser.Sender.IsValidTarget(1000))
            {
                if (R.CanCast(gapcloser.Sender))
                    R.Cast(gapcloser.Sender);

                Render.Circle.DrawCircle(gapcloser.Sender.Position, gapcloser.Sender.BoundingRadius, Color.Gold, 5);
                var targetpos = Drawing.WorldToScreen(gapcloser.Sender.Position);
                Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, Color.Gold, "Gapcloser");
            }
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!SharpShooter.Menu.Item("autointerrupt", true).GetValue<Boolean>() || Player.IsDead)
                return;

            if (unit.IsValidTarget(1500))
            {
                if (R.CanCast(unit))
                    R.Cast(unit);

                Render.Circle.DrawCircle(unit.Position, unit.BoundingRadius, Color.Gold, 5);
                var targetpos = Drawing.WorldToScreen(unit.Position);
                Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, Color.Gold, "Interrupt");
            }
        }

        static void Combo()
        {
            if (!Orbwalking.CanMove(1))
                return;

            var Wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical, true);
            var Rtarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical, true);

            if (W.CanCast(Wtarget) && W.GetPrediction(Wtarget).Hitchance >= HitChance.High && SharpShooter.Menu.Item("comboUseW", true).GetValue<Boolean>())
                W.Cast(Wtarget);

            if (R.IsReady() && Rtarget.IsValidTarget(1000) && R.GetPrediction(Rtarget).Hitchance >= HitChance.VeryHigh && SharpShooter.Menu.Item("comboUseR", true).GetValue<Boolean>())
                R.Cast(Rtarget);
        }

        static void Harass()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("harassMana", true).GetValue<Slider>().Value))
                return;

            var Wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical, true);

            if (W.CanCast(Wtarget) && W.GetPrediction(Wtarget).Hitchance >= HitChance.VeryHigh && SharpShooter.Menu.Item("harassUseW", true).GetValue<Boolean>())
                W.Cast(Wtarget);
        }

        static void Laneclear()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("laneclearMana", true).GetValue<Slider>().Value))
                return;

            var Minions = MinionManager.GetMinions(Player.ServerPosition, W.Range, MinionTypes.All, MinionTeam.Enemy);

            if (Minions.Count <= 0)
                return;

            if (W.IsReady() && SharpShooter.Menu.Item("laneclearUseW", true).GetValue<Boolean>())
            {
                var Farmloc = W.GetLineFarmLocation(Minions);

                if (Farmloc.MinionsHit >= 3)
                    W.Cast(Farmloc.Position);
            }
        }

        static void Jungleclear()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("jungleclearMana", true).GetValue<Slider>().Value))
                return;

            var Mobs = MinionManager.GetMinions(Player.ServerPosition, Orbwalking.GetRealAutoAttackRange(Player) + 100, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (Mobs.Count < 1)
                return;

            if (W.IsReady() && SharpShooter.Menu.Item("jungleclearUseW", true).GetValue<Boolean>())
            {
                W.Cast(Mobs[0].Position);
            }
        }
    }
}
