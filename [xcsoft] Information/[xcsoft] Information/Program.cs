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

namespace _xcsoft__Information
{
    internal class Program
    {
        static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        static Obj_AI_Hero Target;

        static Menu Menu;

        static Render.Text Text;

        static string Font = "monospace";

        static readonly string NewLine = "\n";

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Menu = new Menu("[xcsoft] Information", "xcsoft_information", true);
            TargetSelector.AddToMenu(Menu.AddSubMenu(new Menu("Target Selector", "Target Selector")));
            Menu.AddToMainMenu();

            Menu.AddItem(new MenuItem("switch", "Switch")).SetValue<Boolean>(true);
            Menu.AddItem(new MenuItem("x", "X")).SetValue<Slider>(new Slider(150 ,0 , Drawing.Width));
            Menu.AddItem(new MenuItem("y", "Y")).SetValue<Slider>(new Slider(30 ,0 , Drawing.Height));
            Menu.AddItem(new MenuItem("size", "Size")).SetValue<Slider>(new Slider(13, 10, 20));

            Menu.Item("x").ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Text.X = eventArgs.GetNewValue<Slider>().Value;
                };

            Menu.Item("y").ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Text.Y = eventArgs.GetNewValue<Slider>().Value;
                };

            Menu.Item("size").ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Text = new Render.Text(Menu.Item("x").GetValue<Slider>().Value, Menu.Item("y").GetValue<Slider>().Value, "", Menu.Item("size").GetValue<Slider>().Value, SharpDX.Color.White, Font);
                };

            Text = new Render.Text(Menu.Item("x").GetValue<Slider>().Value, Menu.Item("y").GetValue<Slider>().Value, "", Menu.Item("size").GetValue<Slider>().Value, SharpDX.Color.White, Font);

            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (!Menu.Item("switch").GetValue<Boolean>())
                return;

            Target = TargetSelector.GetSelectedTarget() != null ? TargetSelector.GetSelectedTarget() : Player;
            
            var buffs = "";

            foreach (var buff in Target.Buffs)
            {
                buffs += "\n" + buff.Name + "(Count:" + buff.Count + "/Duration:" + (buff.EndTime - Game.ClockTime).ToString("0.00") +"),";
            }

            Text.text =
                "Name: " + Target.Name + NewLine +
                "ChampionName: " + Target.ChampionName + NewLine +
                "SkinName: " + Target.SkinName + NewLine +
                "Gold: " + Target.Gold + NewLine +
                "Level: " + Target.Level + NewLine +
                "TotalAttackDamage: " + Utility.TotalAttackDamage(Target) + NewLine +
                "TotalMagicalDamage: " + Utility.TotalMagicalDamage(Target) + NewLine +
                "Armor: " + Target.Armor + NewLine +
                "Health: " + Target.Health + " / " + Target.MaxHealth + " (" + Target.HealthPercentage() + "%)" + NewLine +
                "Mana: " + Target.Mana + " / " + Target.MaxMana + " (" + Target.ManaPercentage() + "%)" + NewLine +
                "HPRegenRate: " + Target.HPRegenRate + NewLine +
                "PARRegenRate: " + Target.PARRegenRate + NewLine +
                "Experience: " + Target.Experience + NewLine +
                "Position: " + Target.Position + NewLine +
                "ServerPosition: " + Target.ServerPosition + NewLine +
                "Team: " + Target.Team + NewLine +
                "NetworkId: " + Target.NetworkId + NewLine +
                "MoveSpeed: " + Target.MoveSpeed + NewLine +
                "AttackRange: " + Target.AttackRange + NewLine +
                "RealAutoAttackRange: " + Orbwalking.GetRealAutoAttackRange(Target) + NewLine +
                "DeathDuration: " + Target.DeathDuration + NewLine +
                "BoundingRadius: " + Target.BoundingRadius + NewLine +
                NewLine +
                "Buffs: " + buffs + NewLine +
                NewLine +
                "IsDead: " + Target.IsDead + NewLine +
                "IsImmovable: " + Target.IsImmovable + NewLine +
                "IsInvulnerable: " + Target.IsInvulnerable + NewLine +
                "IsMoving: " + Target.IsMoving + NewLine +
                "IsPacified: " + Target.IsPacified + NewLine +
                "IsTargetable: " + Target.IsTargetable + NewLine +
                "IsWindingUp: " + Target.IsWindingUp + NewLine +
                "IsZombie: " + Target.IsZombie + NewLine +
                "IsRecalling: " + Target.IsRecalling() + NewLine +
                "IsStunned: " + Target.IsStunned + NewLine +
                "IsRooted: " + Target.IsRooted + NewLine +
                "IsMelee: " + Target.IsMelee() + NewLine +
                "IsDashing: " + Target.IsDashing() + NewLine +
                "IsValid: " + Target.IsValid + NewLine +
                "IsMovementImpaired: " + Target.IsMovementImpaired() + NewLine +
                "IsBot: " + Target.IsBot + NewLine +
                "IsAlly: " + Target.IsAlly + NewLine +
                "IsEnemy: " + Target.IsEnemy + NewLine +
                "CanAttack: " + Target.CanAttack + NewLine +
                "InFountain: " + Target.InFountain() + NewLine +
                "InShop: " + Target.InShop() + NewLine +
                NewLine +
                "Game_CursorPos: " + Game.CursorPos + NewLine +
                "Game_ClockTime: " + Game.ClockTime + NewLine +
                "Game_Time: " + Game.Time + NewLine +
                "Game_Type: " + Game.Type + NewLine +
                "Game_Version: " + Game.Version + NewLine +
                "Game_Region: " + Game.Region + NewLine +
                "Game_IP: " + Game.IP + NewLine +
                "Game_Port: " + Game.Port + NewLine +
                "Game_Ping: " + Game.Ping + NewLine +
                "Game_Mode: " + Game.Mode + NewLine +
                "Game_MapId: " + Game.MapId
                ;


            Text.OnEndScene();
        }
    }
}
