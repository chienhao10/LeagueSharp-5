using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace Sharpshooter.Champions
{
    public static class Caitlyn
    {
        static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        static Orbwalking.Orbwalker Orbwalker { get { return SharpShooter.Orbwalker; } }

        static Spell Q, W, E, R;

        static Obj_AI_Hero eqTarget;

        public static void Load()
        {
            Q = new Spell(SpellSlot.Q, 1250);
            W = new Spell(SpellSlot.W, 800);
            E = new Spell(SpellSlot.E, 950);
            R = new Spell(SpellSlot.R, 2000);

            Q.SetSkillshot(0.7f, 60f, 2200f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 80f, 1600f, true, SkillshotType.SkillshotLine);

            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseR", "Use R", true).SetValue(true));

            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassMana", "if Mana % >", true).SetValue(new Slider(50, 0, 100)));

            SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearMana", "if Mana % >", true).SetValue(new Slider(50, 0, 100)));

            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearMana", "if Mana % >", true).SetValue(new Slider(20, 0, 100)));

            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("antigapcloser", "Use Anti-Gapcloser", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("AutoW", "Autocast W on immobile targets", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("CastEQ", "EQ Combo", true).SetValue(new KeyBind('T', KeyBindType.Press)));
            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("dash", "Dash to Mouse", true).SetValue(new KeyBind('G', KeyBindType.Press)));

            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingAA", "Real AA Range", true).SetValue(new Circle(true, Color.FromArgb(255, 94, 0))));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingQ", "Q Range", true).SetValue(new Circle(true, Color.FromArgb(255,94,0))));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingW", "W Range", true).SetValue(new Circle(false, Color.FromArgb(255, 94, 0))));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingE", "E Range", true).SetValue(new Circle(true, Color.FromArgb(255, 94, 0))));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingR", "R Range", true).SetValue(new Circle(true, Color.FromArgb(255, 94, 0))));

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
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

            AutoW();

            if(SharpShooter.Menu.Item("CastEQ", true).GetValue<KeyBind>().Active)
            {
               Vector3 searchPos;

                if (Player.Distance(Game.CursorPos) < Q.Range - 150)
                    searchPos = Game.CursorPos;
                else
                    searchPos = Player.Position +
                                Vector3.Normalize(Game.CursorPos - Player.Position) * (Q.Range - 150);

                var rTargettemp = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(Q.Range) && hero.Distance(searchPos) < 150).OrderByDescending(TargetSelector.GetPriority);

                if (rTargettemp.Count<Obj_AI_Hero>() > 0)
                {
                    eqTarget = rTargettemp.First<Obj_AI_Hero>();

                    if (Q.IsReady() && E.IsReady() && E.GetPrediction(eqTarget).Hitchance >= HitChance.High)
                    {
                        E.Cast(eqTarget);
                        Q.Cast(eqTarget);
                    }
                }
                else
                    eqTarget = null;
            }

            if (SharpShooter.Menu.Item("dash", true).GetValue<KeyBind>().Active)
            {
                if(E.IsReady())
                    E.Cast(Game.CursorPos.Extend(Player.Position, 5000));
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            var drawingAA = SharpShooter.Menu.Item("drawingAA", true).GetValue<Circle>();
            var drawingQ = SharpShooter.Menu.Item("drawingQ", true).GetValue<Circle>();
            var drawingW = SharpShooter.Menu.Item("drawingW", true).GetValue<Circle>();
            var drawingE = SharpShooter.Menu.Item("drawingE", true).GetValue<Circle>();
            var drawingR = SharpShooter.Menu.Item("drawingR", true).GetValue<Circle>();

            if (drawingAA.Active)
                Render.Circle.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), drawingAA.Color);

            if (drawingQ.Active && Q.IsReady())
                Render.Circle.DrawCircle(Player.Position, Q.Range, drawingQ.Color);

            if (drawingW.Active && W.IsReady())
                Render.Circle.DrawCircle(Player.Position, W.Range, drawingW.Color);

            if (drawingE.Active && E.IsReady())
                Render.Circle.DrawCircle(Player.Position, E.Range, drawingE.Color);

            if (drawingR.Active && R.IsReady())
                Render.Circle.DrawCircle(Player.Position, R.Range, drawingR.Color);

            if (SharpShooter.Menu.Item("CastEQ", true).GetValue<KeyBind>().Active)
            {
                Vector3 DrawPosition;

                if (Player.Distance(Game.CursorPos) < Q.Range - 150)
                    DrawPosition = Game.CursorPos;
                else
                    DrawPosition = ObjectManager.Player.Position +
                                   Vector3.Normalize(Game.CursorPos - Player.Position) * (Q.Range - 150);

                Render.Circle.DrawCircle(DrawPosition, 150, Color.White);
                var targetpos1 = Drawing.WorldToScreen(DrawPosition);

                Drawing.DrawText(targetpos1[0] - 40, targetpos1[1] + 40, Color.White, "EQ Combo");

                if (eqTarget != null)
                {
                    var pred = E.GetPrediction(eqTarget);

                    var colortemp = Color.WhiteSmoke;

                    if (pred.Hitchance >= HitChance.High)
                        colortemp = Color.Gold;

                    Render.Circle.DrawCircle(eqTarget.Position, eqTarget.BoundingRadius, colortemp);
                    Render.Circle.DrawCircle(pred.CastPosition, Q.Width, Color.Gold);

                    var targetpos2 = Drawing.WorldToScreen(eqTarget.Position);
                    Drawing.DrawText(targetpos2[0] - 30, targetpos2[1] + 40, colortemp, "Target: " + eqTarget.ChampionName);
                    Drawing.DrawText(targetpos2[0] - 30, targetpos2[1] + 60, colortemp, "Hitchance: " + pred.Hitchance);
                }
            }

            if (SharpShooter.Menu.Item("dash", true).GetValue<KeyBind>().Active)
            {
                Render.Circle.DrawCircle(Game.CursorPos, 100, Color.Gold);
                var targetpos = Drawing.WorldToScreen(Game.CursorPos);
                Drawing.DrawText(targetpos[0] - 30, targetpos[1] + 40, Color.Gold, "E Dash");
            }
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!SharpShooter.Menu.Item("antigapcloser", true).GetValue<Boolean>() || Player.IsDead)
                return;

            if (gapcloser.Sender.IsValidTarget(1000))
            {
                Render.Circle.DrawCircle(gapcloser.Sender.Position, gapcloser.Sender.BoundingRadius, Color.Gold, 5);
                var targetpos = Drawing.WorldToScreen(gapcloser.Sender.Position);
                Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, Color.Gold, "Gapcloser");
            }

            if (E.CanCast(gapcloser.Sender))
                E.Cast(gapcloser.Sender.Position);
            else if (W.CanCast(gapcloser.Sender))
                W.Cast(gapcloser.End);
        }

        static void AutoW()
        {
            if (!SharpShooter.Menu.Item("AutoW", true).GetValue<Boolean>())
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(W.Range) && x.IsEnemy && !x.IsDead))
            {
                if (target != null)
                {
                    if (W.CanCast(target) && W.GetPrediction(target).Hitchance >= HitChance.Dashing)
                        W.Cast(target);
                }
            }
        }

        static void Combo()
        {
            if (!Orbwalking.CanMove(1))
                return;

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true);

            if (Q.CanCast(target) && SharpShooter.Menu.Item("comboUseQ", true).GetValue<Boolean>() && Q.GetPrediction(target).Hitchance >= HitChance.High)
                Q.Cast(target);

            if (R.IsReady() && SharpShooter.Menu.Item("comboUseR", true).GetValue<Boolean>())
            {
                foreach (Obj_AI_Hero rtarget in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(R.Range) && x.IsEnemy && !x.IsDead && !x.HasBuffOfType(BuffType.Invulnerability)))
                {
                    if (rtarget != null)
                    {
                        if (R.CanCast(rtarget) && (rtarget.Health + rtarget.HPRegenRate * 2) <= R.GetDamage(rtarget) && !rtarget.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                            R.Cast(rtarget);
                    }
                }
            }
        }

        static void Harass()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("harassMana", true).GetValue<Slider>().Value))
                return;

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true);

            if (Q.CanCast(target) && SharpShooter.Menu.Item("harassUseQ", true).GetValue<Boolean>() && Q.GetPrediction(target).Hitchance >= HitChance.High)
                Q.Cast(target);
        }

        static void Laneclear()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("laneclearMana", true).GetValue<Slider>().Value))
                return;

            var Minions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy);

            if (Minions.Count <= 0)
                return;

            if (SharpShooter.Menu.Item("laneclearUseQ", true).GetValue<Boolean>())
            {
                var farmloc = Q.GetLineFarmLocation(Minions);

                if (farmloc.MinionsHit >= 3)
                    Q.Cast(farmloc.Position);
            }
        }

        static void Jungleclear()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("jungleclearMana", true).GetValue<Slider>().Value))
                return;

            var Mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (Mobs.Count <= 0)
                return;

            if (Q.CanCast(Mobs[0]) && SharpShooter.Menu.Item("jungleclearUseQ", true).GetValue<Boolean>())
                Q.Cast(Mobs[0]);
        }
    }
}
