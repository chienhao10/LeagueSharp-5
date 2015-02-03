using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace Sharpshooter.Champions
{
    public static class Tristana
    {
        static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        static Orbwalking.Orbwalker Orbwalker { get { return SharpShooter.Orbwalker; } }

        static Spell Q, W, E, R;

        static int GetExtraRange { get { return 543 + (7 * Player.Level); } }
        
        public static void Load()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 900f);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);
            
            W.SetSkillshot(0.5f, 270f, 1500f, false, SkillshotType.SkillshotCircle);

            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseW", "Use W", true).SetValue(false));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseE", "Use E", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseR", "Use R", true).SetValue(true));

            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassUseE", "Use E", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassMana", "if Mana % >", true).SetValue(new Slider(50, 0, 100)));

            SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearUseQ", "Use Q", true).SetValue(true));
            //SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearMana", "if Mana % >", true).SetValue(new Slider(50, 0, 100)));

            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearUseQ", "Use Q", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearUseE", "Use E", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearMana", "if Mana % >", true).SetValue(new Slider(20, 0, 100)));

            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("antigapcloser", "Use Anti-Gapcloser", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("autointerrupt", "Use Auto-Interrupt", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("jump", "Jump to mouse", true).SetValue(new KeyBind('G', KeyBindType.Press)));

            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingAA", "Real AA Range", true).SetValue(new Circle(true, Color.FromArgb(255, 178, 217))));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingW", "W Range", true).SetValue(new Circle(true, Color.FromArgb(255, 178, 217))));

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
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
                if (W.IsReady())
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                    W.Cast(Game.CursorPos);
                }
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            var drawingAA = SharpShooter.Menu.Item("drawingAA", true).GetValue<Circle>();
            var drawingW = SharpShooter.Menu.Item("drawingW", true).GetValue<Circle>();

            if (drawingAA.Active)
                Render.Circle.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), drawingAA.Color);

            if (W.IsReady() && drawingW.Active)
                Render.Circle.DrawCircle(Player.Position, W.Range, drawingW.Color);

            if (SharpShooter.Menu.Item("jump", true).GetValue<KeyBind>().Active)
            {
                Render.Circle.DrawCircle(Game.CursorPos, W.Width, Color.Gold);
                var targetpos = Drawing.WorldToScreen(Game.CursorPos);
                Drawing.DrawText(targetpos[0] - 30, targetpos[1] + 40, Color.Gold, "W Jump");
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

            if (R.IsReady() && gapcloser.Sender.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                R.Cast(gapcloser.Sender);
        }

        static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!SharpShooter.Menu.Item("autointerrupt", true).GetValue<Boolean>() || Player.IsDead)
                return;

            if (sender.IsValidTarget(1000))
            {
                Render.Circle.DrawCircle(sender.Position, sender.BoundingRadius, Color.Gold, 5);
                var targetpos = Drawing.WorldToScreen(sender.Position);
                Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, Color.Gold, "Interrupt");
            }

            if (R.IsReady() && sender.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                R.Cast(sender);
        }
        static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "TristanaE")
                Orbwalking.ResetAutoAttackTimer();
        }

        static void Combo()
        {
            if (SharpShooter.Menu.Item("comboUseQ", true).GetValue<Boolean>())
            {
                var Qtarget = Orbwalker.GetTarget();

                if (Q.IsReady() && Qtarget != null && Qtarget.IsEnemy)
                    Q.Cast();
            }

            if (!Orbwalking.CanMove(1))
                return;

            if (SharpShooter.Menu.Item("comboUseW", true).GetValue<Boolean>() && W.IsReady())
            {
                foreach (Obj_AI_Hero Wtarget in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget(W.Range) && !Player.HasBuffOfType(BuffType.SpellShield) && !Player.HasBuffOfType(BuffType.Invulnerability)))
                {
                    var Wpred = W.GetPrediction(Wtarget);

                    if(!Wpred.CastPosition.UnderTurret(true) && Utility.CountEnemiesInRange(Wpred.CastPosition, 1000) <= 2)
                    {
                        foreach (var buff in Wtarget.Buffs)
                        {
                            if (buff.Name == "tristanaecharge" && buff.Count >= 3)
                            {
                                W.Cast(Wtarget);
                                break;
                            }
                        }

                        if (Player.Health + Player.HPRegenRate <= W.GetDamage(Wtarget) + Player.GetAutoAttackDamage(Wtarget, true))
                            W.Cast(Wtarget);
                    }
                }
            }

            if (SharpShooter.Menu.Item("comboUseE", true).GetValue<Boolean>())
            {
                var Etarget = Orbwalker.GetTarget();

                if (E.IsReady() && Etarget != null && Etarget.IsEnemy)
                    E.Cast((Obj_AI_Hero)Etarget);
            }

            if (SharpShooter.Menu.Item("comboUseR", true).GetValue<Boolean>() && R.IsReady())
            {
                foreach (Obj_AI_Hero Rtarget in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && !Player.HasBuffOfType(BuffType.SpellShield) && !Player.HasBuffOfType(BuffType.Invulnerability)))
                {
                    if (Rtarget.Health + Rtarget.HPRegenRate <= R.GetDamage(Rtarget))
                    {
                        R.Cast(Rtarget);
                        break;
                    }
                }
            }
        }

        static void Harass()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("harassMana", true).GetValue<Slider>().Value))
                return;

            if (SharpShooter.Menu.Item("harassUseE", true).GetValue<Boolean>())
            {
                var Etarget = Orbwalker.GetTarget();

                if (E.IsReady() && Etarget != null && Etarget.IsEnemy)
                    E.Cast((Obj_AI_Hero)Etarget);
            }
        }

        static void Laneclear()
        {
            if (!Orbwalking.CanMove(1)) //|| !(Player.ManaPercentage() > SharpShooter.Menu.Item("laneclearMana", true).GetValue<Slider>().Value))
                return;

            var Minions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy);

            if (Minions.Count <= 0)
                return;

            if (Q.IsReady() && SharpShooter.Menu.Item("laneclearUseQ", true).GetValue<Boolean>())
            {
                if (Minions.Count >= 3)
                    Q.Cast();
            }
        }

        static void Jungleclear()
        {
            if (Q.IsReady())
                Q.Cast();

            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("jungleclearMana", true).GetValue<Slider>().Value))
                return;

            var Mobs = MinionManager.GetMinions(Player.ServerPosition, Orbwalking.GetRealAutoAttackRange(Player), MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (Mobs.Count <= 0)
                return;

            if (E.IsReady() && SharpShooter.Menu.Item("jungleclearUseE", true).GetValue<Boolean>())
                E.Cast(Mobs[0]);
        }
    }
}
