#region

using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace Marksman
{
    internal class Sivir : Champion
    {
        public Spell Q;
        public Spell W;

        public Sivir()
        {
            Utils.PrintMessage("Sivir loaded.");

            Q = new Spell(SpellSlot.Q, 1250);
            Q.SetSkillshot(0.25f, 90f, 1350f, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 593);
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (GetValue<bool>("AutoQ"))
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(Q.Range)))
                {
                    Q.CastIfHitchanceEquals(enemy, HitChance.Immobile);
                }
            }


            if (ComboActive || HarassActive)
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));

                if (Orbwalking.CanMove(100))
                {
                    if (useQ)
                    {
                        var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

                        if (Q.GetPrediction(t).Hitchance >= HitChance.VeryHigh)
                        {
                            if (t != null && Q.IsReady())
                                Q.Cast(t);
                        }
                    }
                }
            }
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t != null && (ComboActive || HarassActive) && unit.IsMe)
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

                if (W.IsReady() && useW)
                {
                    W.Cast();
                }
                else if (Q.IsReady() && useQ)
                {
                    var pred = Q.GetPrediction(t);

                    if (pred.Hitchance >= HitChance.VeryHigh)
                        Q.Cast(t);
                }
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { Q };
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }

            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            var pred = Q.GetPrediction(t);

            if (pred.Hitchance >= HitChance.VeryHigh)
                Render.Circle.DrawCircle(pred.CastPosition, 90, Color.Gold, 5, true);
            else
                Render.Circle.DrawCircle(pred.CastPosition, 90, Color.Gray, 5, true);
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(false));
            config.AddItem(new MenuItem("UseWH" + Id, "Use W").SetValue(false));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("AutoQ" + Id, "Auto Q on stunned targets").SetValue(true));

            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            return true;
        }

        public override bool ExtrasMenu(Menu config)
        {

            return true;
        }
        public override bool LaneClearMenu(Menu config)
        {
            return true;
        }
    }
}
