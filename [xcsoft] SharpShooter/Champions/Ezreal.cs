using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace Sharpshooter.Champions
{
    public static class Ezreal
    {
        static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        static Orbwalking.Orbwalker Orbwalker { get { return SharpShooter.Orbwalker; } }

        static Spell Q, W, E, R;

        public static void Load()
        {
            Q = new Spell(SpellSlot.Q, 1180f);
            W = new Spell(SpellSlot.W, 1000f);
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 2500f);

            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            var drawDamageMenu = new MenuItem("Draw_RDamage", "Draw (R) Damage", true).SetValue(true);
            var drawFill = new MenuItem("Draw_Fill", "Draw (R) Damage Fill", true).SetValue(new Circle(true, Color.FromArgb(90, 255, 169, 4)));

            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseW", "Use W", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseR", "Use R", true).SetValue(true));

            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassUseW", "Use W", true).SetValue(false));
            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassMana", "if Mana % >", true).SetValue(new Slider(50, 0, 100)));
            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassAuto", "Auto Harass", true).SetValue(true));
            

            SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearMana", "if Mana % >", true).SetValue(new Slider(60, 0, 100)));

            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearMana", "if Mana % >", true).SetValue(new Slider(20, 0, 100)));

            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("killsteal", "Use Killsteal", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("jump", "Jump to mouse", true).SetValue(new KeyBind('G', KeyBindType.Press)));

            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingAA", "Real AA Range", true).SetValue(new Circle(true, Color.Gold)));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingQ", "Q Range", true).SetValue(new Circle(true, Color.Gold)));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingW", "W Range", true).SetValue(new Circle(false, Color.Gold)));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingE", "E Range", true).SetValue(new Circle(false, Color.Gold)));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingR", "R Range", true).SetValue(new Circle(true, Color.Gold)));

            SharpShooter.Menu.SubMenu("Drawings").AddItem(drawDamageMenu);
            SharpShooter.Menu.SubMenu("Drawings").AddItem(drawFill);

            DamageIndicator.DamageToUnit = GetComboDamage;
            DamageIndicator.Enabled = drawDamageMenu.GetValue<bool>();
            DamageIndicator.Fill = drawFill.GetValue<Circle>().Active;
            DamageIndicator.FillColor = drawFill.GetValue<Circle>().Color;

            drawDamageMenu.ValueChanged +=
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

            Game.OnGameUpdate += Game_OnGameUpdate;
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

            if(SharpShooter.Menu.Item("jump", true).GetValue<KeyBind>().Active)
            {
                if (E.IsReady())
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    E.Cast(Game.CursorPos);
                }
            }

            if (SharpShooter.Menu.Item("harassAuto", true).GetValue<Boolean>())
                Harass();

            Killsteal();
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

            if (Q.IsReady() && drawingQ.Active)
                Render.Circle.DrawCircle(Player.Position, Q.Range, drawingQ.Color);

            if (W.IsReady() && drawingW.Active)
                Render.Circle.DrawCircle(Player.Position, W.Range, drawingW.Color);

            if (E.IsReady() && drawingE.Active)
                Render.Circle.DrawCircle(Player.Position, E.Range, drawingE.Color);

            if (R.IsReady() && drawingR.Active)
                Render.Circle.DrawCircle(Player.Position, R.Range, drawingR.Color);

            if (SharpShooter.Menu.Item("jump", true).GetValue<KeyBind>().Active)
            {
                Render.Circle.DrawCircle(Game.CursorPos, 50, Color.Gold);
                var targetpos = Drawing.WorldToScreen(Game.CursorPos);
                Drawing.DrawText(targetpos[0] - 30, targetpos[1] + 40, Color.Gold, "E Jump");
            }
        }

        static void Killsteal()
        {
            if (!SharpShooter.Menu.Item("killsteal", true).GetValue<Boolean>())
                return;

            foreach (Obj_AI_Hero target in HeroManager.Enemies.Where(x => x.IsValidTarget(R.Range) && !x.HasBuffOfType(BuffType.Invulnerability) && !x.HasBuffOfType(BuffType.SpellShield)))
            {
                if (target != null)
                {
                    if (Q.CanCast(target) && (target.Health + target.HPRegenRate) <= Q.GetDamage(target))
                        Q.Cast(target);

                    if (W.CanCast(target) && (target.Health + target.HPRegenRate) <= W.GetDamage(target))
                        R.Cast(target);

                    if (R.CanCast(target) && (target.Health + target.HPRegenRate) <= R.GetDamage(target) && !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && R.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
                        R.Cast(target);
                }
            }
        }

        static float GetComboDamage(Obj_AI_Base enemy)
        {
            return R.IsReady() ? R.GetDamage(enemy) : 0;
        }

        static void Combo()
        {
            if (!Orbwalking.CanMove(1))
                return;

            if (SharpShooter.Menu.Item("comboUseQ", true).GetValue<Boolean>())
            {
                var Qtarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true);

                if (Q.CanCast(Qtarget) && Q.GetPrediction(Qtarget).Hitchance >= HitChance.VeryHigh)
                    Q.Cast(Qtarget);
            }

            if (SharpShooter.Menu.Item("comboUseW", true).GetValue<Boolean>())
            {
                var Wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical, true);

                if (W.CanCast(Wtarget) && W.GetPrediction(Wtarget).Hitchance >= HitChance.VeryHigh)
                    W.Cast(Wtarget);
            }

            if (SharpShooter.Menu.Item("comboUseR", true).GetValue<Boolean>())
            {
                var Rtarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical, true);

                if (R.CanCast(Rtarget) && !Rtarget.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                    R.CastIfWillHit(Rtarget, 2);
            }
        }

        static void Harass()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("harassMana", true).GetValue<Slider>().Value))
                return;

            if (SharpShooter.Menu.Item("harassUseQ", true).GetValue<Boolean>())
            {
                var Qtarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true);

                if (Q.CanCast(Qtarget) && Q.GetPrediction(Qtarget).Hitchance >= HitChance.VeryHigh)
                    Q.Cast(Qtarget);
            }

            if (SharpShooter.Menu.Item("harassUseW", true).GetValue<Boolean>())
            {
                var Wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical, true);

                if (W.CanCast(Wtarget) && W.GetPrediction(Wtarget).Hitchance >= HitChance.VeryHigh)
                    W.Cast(Wtarget);
            }
        }

        static void Laneclear()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("laneclearMana", true).GetValue<Slider>().Value))
                return;

            var Minions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy);

            if (Minions.Count <= 0)
                return;

            if (Q.IsReady() && SharpShooter.Menu.Item("laneclearUseQ", true).GetValue<Boolean>())
            {
                var Farmloc = Q.GetLineFarmLocation(Minions);

                if (Farmloc.MinionsHit >= 1 && !Player.IsWindingUp)
                    Q.Cast(Farmloc.Position);
            }
        }

        static void Jungleclear()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("jungleclearMana", true).GetValue<Slider>().Value))
                return;

            var Mobs = MinionManager.GetMinions(Player.ServerPosition, Orbwalking.GetRealAutoAttackRange(Player) + 100, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (Mobs.Count <= 0)
                return;

            if (Q.CanCast(Mobs[0]) && SharpShooter.Menu.Item("jungleclearUseQ", true).GetValue<Boolean>())
                Q.Cast(Mobs[0].Position);
        }
    }
}
