using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace InventoryStatusDisplay
{
    public sealed class Program : MyGridProgram
    {
        // start

        // CONFIGURATION - START

        Group[] groups = {
            // g("label", "display block name", display number, "item name", "source item name", conversion rate),
            g("Cobalt", "LCD Panel Status Cobalt (SB)", 0, "MyObjectBuilder_Ingot/Cobalt", "MyObjectBuilder_Ore/Cobalt", 0.3f),
            g("Gold", "LCD Panel Status Gold (SB)", 0, "MyObjectBuilder_Ingot/Gold", "MyObjectBuilder_Ore/Gold", 0.01f),
            g("Iron", "LCD Panel Status Iron (SB)", 0, "MyObjectBuilder_Ingot/Iron", "MyObjectBuilder_Ore/Iron", 0.7f),
            g("Magnesium", "LCD Panel Status Magnesium (SB)", 0, "MyObjectBuilder_Ingot/Magnesium", "MyObjectBuilder_Ore/Magnesium", 0.007f),
            g("Nickel", "LCD Panel Status Nickel (SB)", 0, "MyObjectBuilder_Ingot/Nickel", "MyObjectBuilder_Ore/Nickel", 0.4f),
            g("Platinum", "LCD Panel Status Platinum (SB)", 0, "MyObjectBuilder_Ingot/Platinum", "MyObjectBuilder_Ore/Platinum", 0.005f),
            g("Silicon", "LCD Panel Status Silicon (SB)", 0, "MyObjectBuilder_Ingot/Silicon", "MyObjectBuilder_Ore/Silicon", 0.7f),
            g("Silver", "LCD Panel Status Silver (SB)", 0, "MyObjectBuilder_Ingot/Silver", "MyObjectBuilder_Ore/Silver", 0.1f),
            g("Gravel", "LCD Panel Status Gravel (SB)", 0, "MyObjectBuilder_Ingot/Stone", "MyObjectBuilder_Ore/Stone", 0.014f),
            g("Uranium", "LCD Panel Status Uranium (SB)", 0, "MyObjectBuilder_Ingot/Uranium", "MyObjectBuilder_Ore/Uranium", 0.01f),

            g("Hydrogen", "LCD Panel Status Hydrogen (SB)", 0, "Hydrogen", "MyObjectBuilder_Ore/Ice", 15f),
            g("Oxygen", "LCD Panel Status Oxygen (SB)", 0, "Oxygen", "MyObjectBuilder_Ore/Ice", 7.5f),
        };

        // for item/bp names, see:
        // https://www.reddit.com/r/spaceengineers/comments/adbzhf/comment/edgjo48

        // CONFIGURATION - END

        class Group
        {
            public string label;
            public string displayName;
            public int displayNumber;
            public string itemName;
            public string sourceItemName;
            public float conversionRate;
            public Group(string label, string displayName, int displayNumber, string itemName, string sourceItemName, float conversionRate)
            {
                this.label = label;
                this.displayName = displayName;
                this.displayNumber = displayNumber;
                this.itemName = itemName;
                this.sourceItemName = sourceItemName;
                this.conversionRate = conversionRate;
            }
        }

        static Group g(string label, string displayName, int displayNumber, string itemName, string sourceItemName, float conversionRate)
        {
            return new Group(label, displayName, displayNumber, itemName, sourceItemName, conversionRate);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var stock = getStock();

            foreach (Group group in groups)
            {
                print(group, stock);
            }
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        Dictionary<string, double> getStock()
        {
            var stock = new Dictionary<string, double>();

            var inventories = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(inventories, (IMyTerminalBlock x) => x.HasInventory);
            foreach (IMyTerminalBlock inv in inventories)
            {
                var items = new List<MyInventoryItem>();
                for (var i = 0; i < inv.InventoryCount; i++)
                {
                    inv.GetInventory(i).GetItems(items);
                }
                foreach (MyInventoryItem item in items)
                {
                    var key = item.Type.TypeId + "/" + item.Type.SubtypeId;
                    double amount = (double)item.Amount.RawValue / 1000000d;
                    if (stock.ContainsKey(key))
                    {
                        stock[key] = stock[key] + amount;
                    }
                    else
                    {
                        stock.Add(key, amount);
                    }
                }
            }

            var tanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType(tanks);
            foreach (IMyGasTank tank in tanks)
            {
                var amount = tank.FilledRatio * tank.Capacity;
                var key = tank.DetailedInfo.Contains("Oxygen") ? "Oxygen" : tank.DetailedInfo.Contains("Hydrogen") ? "Hydrogen" : "Gas";
                if (stock.ContainsKey(key))
                {
                    stock[key] = stock[key] + amount;
                }
                else
                {
                    stock.Add(key, amount);
                }
            }

            return stock;
        }

        string formatAmount(double amount)
        {
            var unit = "";
            if (amount >= 1000000)
            {
                unit = "M";
                amount /= 1000000;
            }
            else if (amount >= 1000)
            {
                unit = "k";
                amount /= 1000;
            }
            return "" + (int)amount + unit;
        }

        double getAmount(string itemName, Dictionary<string, double> stock)
        {
            var amount = 0d;
            if (stock.ContainsKey(itemName))
            {
                amount = stock[itemName];
            }
            return amount;
        }

        void print(Group g, Dictionary<string, double> stock)
        {
            var block = GridTerminalSystem.GetBlockWithName(g.displayName);
            if (block == null)
            {
                Echo("cannot find block " + g.displayName);
                return;
            }
            if (block is not IMyTextSurfaceProvider)
            {
                Echo("block " + g.displayName + " is not a valid display");
                return;
            }
            var display = ((IMyTextSurfaceProvider)block).GetSurface(g.displayNumber);

            if (display == null)
            {
                Echo("cannot get display " + g.displayNumber + " of block " + g.displayName);
                return;
            }

            double amount = getAmount(g.itemName, stock);
            double futureAmount = g.conversionRate * getAmount(g.sourceItemName, stock);

            string text = g.label + "\n\n" + formatAmount(amount) + "\n";
            if (futureAmount > 0)
            {
                text += "(+" + formatAmount(futureAmount) + ")";
            }

            display.WriteText(text);
            Echo("printed " + g.label);
        }
        // end
    }
}
