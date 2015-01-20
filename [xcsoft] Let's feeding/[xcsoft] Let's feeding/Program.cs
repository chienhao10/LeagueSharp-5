using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace _xcsoft__Let_s_feeding
{
    internal class Program
    {
        static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        static readonly Vector3 SummonersRift_PurpleFountain = new Vector3(14400f, 14376f, 171.9777f);
        static readonly Vector3 SummonersRift_BlueFountain = new Vector3(420f, 422f, 183.5748f);

        private static SpellSlot Revive;

        private static Menu Menu;

        static float lasttime; 

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (Game.MapId != (GameMapId)11)
                return;

            Revive = Player.GetSpellSlot("SummonerRevive");

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            Menu = new Menu("[xcsoft] Let's Feeding", "xcoft_feeder", true);
            Menu.AddItem(new MenuItem("switch", "Switch").SetValue(false));
            Menu.AddItem(new MenuItem("enjoy", "Enjoy!"));
            Menu.AddToMainMenu();

            Game.PrintChat("<font color = \"#33CCCC\">[xcsoft] Let's feeding -</font> Loaded");
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (!Menu.Item("switch").GetValue<bool>())
                return;

            if(Player.InShop() || Player.IsDead)
            {
                if (Player.InventoryItems.Length < 6)
                {
                    if (Player.Gold >= 475 && Player.InventoryItems.Any(i => i.Id == ItemId.Boots_of_Mobility))
                        Player.BuyItem(ItemId.Boots_of_Mobility_Enchantment_Homeguard);

                    if (Player.Gold >= 475 && Player.InventoryItems.Any(i => i.Id == ItemId.Boots_of_Speed))
                        Player.BuyItem(ItemId.Boots_of_Mobility);

                    if (Player.Gold >= 325 && !Player.InventoryItems.Any(i => i.Id == ItemId.Boots_of_Mobility))
                        Player.BuyItem(ItemId.Boots_of_Speed);
                }
            }

            if (Player.IsDead && Revive != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(Revive) == SpellState.Ready)
                Player.Spellbook.CastSpell(Revive);

            if (Player.IsDead || Game.Time <= lasttime + 0.5)
            return;

            lasttime = Game.Time;

            Player.IssueOrder(GameObjectOrder.MoveTo, Player.Team == GameObjectTeam.Chaos ? SummonersRift_BlueFountain : SummonersRift_PurpleFountain);
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (!Menu.Item("switch").GetValue<bool>())
                return;

            var centerpos = Drawing.WorldToScreen(new Vector3(7350f, 7400f, 53.96267f));
            Drawing.DrawText(centerpos[0], centerpos[1], Color.White, "the way to hell");

            if (Player.IsDead)
                return;

            var playerpos = Drawing.WorldToScreen(Player.Position);
            Drawing.DrawText(playerpos[0] - 80, playerpos[1] + 50, Color.Gold, "Feeding in progress..");
            Render.Circle.DrawCircle(Player.Position, Player.BoundingRadius, Color.Gold);
        }
    }
}
