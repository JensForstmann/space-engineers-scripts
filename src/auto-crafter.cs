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

namespace AutoCrafter
{
    public sealed class Program : MyGridProgram
    {
        // start

        // CONFIGURATION - START

        string assembler = "Assembler (Cube MK-1)";
        Group[] groups = {
            // g("item name", "bp name", minimum, maximum)
            g("MyObjectBuilder_Component/BulletproofGlass", "MyObjectBuilder_BlueprintDefinition/BulletproofGlass", 1000, 2000),
            g("MyObjectBuilder_Component/Computer", "MyObjectBuilder_BlueprintDefinition/ComputerComponent", 1000, 2000),
            g("MyObjectBuilder_Component/Construction", "MyObjectBuilder_BlueprintDefinition/ConstructionComponent", 1000, 2000),
            g("MyObjectBuilder_Component/Detector", "MyObjectBuilder_BlueprintDefinition/DetectorComponent", 100, 200),
            g("MyObjectBuilder_Component/Display", "MyObjectBuilder_BlueprintDefinition/Display", 100, 200),
            g("MyObjectBuilder_Component/Girder", "MyObjectBuilder_BlueprintDefinition/GirderComponent", 1000, 2000),
            g("MyObjectBuilder_Component/InteriorPlate", "MyObjectBuilder_BlueprintDefinition/InteriorPlate", 1000, 2000),
            g("MyObjectBuilder_Component/LargeTube", "MyObjectBuilder_BlueprintDefinition/LargeTube", 1000, 2000),
            g("MyObjectBuilder_Component/MetalGrid", "MyObjectBuilder_BlueprintDefinition/MetalGrid", 1000, 2000),
            g("MyObjectBuilder_Component/Motor", "MyObjectBuilder_BlueprintDefinition/MotorComponent", 1000, 2000),
            g("MyObjectBuilder_Component/PowerCell", "MyObjectBuilder_BlueprintDefinition/PowerCell", 500, 1000),
            g("MyObjectBuilder_Component/RadioCommunication", "MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent", 100, 200),
            g("MyObjectBuilder_Component/SmallTube", "MyObjectBuilder_BlueprintDefinition/SmallTube", 1000, 2000),
            g("MyObjectBuilder_Component/SolarCell", "MyObjectBuilder_BlueprintDefinition/SolarCell", 1000, 2000),
            g("MyObjectBuilder_Component/SteelPlate", "MyObjectBuilder_BlueprintDefinition/SteelPlate", 20000, 30000),
            g("MyObjectBuilder_Component/Superconductor", "MyObjectBuilder_BlueprintDefinition/Superconductor", 400, 500),
            g("MyObjectBuilder_Component/Explosives", "MyObjectBuilder_BlueprintDefinition/ExplosivesComponent", 100, 200),
            g("MyObjectBuilder_Component/GravityGenerator", "MyObjectBuilder_BlueprintDefinition/GravityGeneratorComponent", 10, 20),
            g("MyObjectBuilder_Component/Medical", "MyObjectBuilder_BlueprintDefinition/MedicalComponent", 10, 20),
            g("MyObjectBuilder_Component/Reactor", "MyObjectBuilder_BlueprintDefinition/ReactorComponent", 2000, 3000),
            g("MyObjectBuilder_Component/Thrust", "MyObjectBuilder_BlueprintDefinition/ThrustComponent", 2000, 4000),
            g("MyObjectBuilder_AmmoMagazine/NATO_25x184mm", "MyObjectBuilder_BlueprintDefinition/NATO_25x184mmMagazine", 1000, 2000),
        };

        // for item/bp names, see:
        // https://www.reddit.com/r/spaceengineers/comments/adbzhf/comment/edgjo48

        // CONFIGURATION - END

        class Group
        {
            public string itemName;
            public string bpName;
            public int min;
            public int max;
            public Group(string itemName, string bpName, int min, int max)
            {
                this.itemName = itemName;
                this.bpName = bpName;
                this.min = min;
                this.max = max;
            }
        }

        static Group g(string itemName, string bpName, int min, int max)
        {
            return new Group(itemName, bpName, min, max);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var myAssembler = GridTerminalSystem.GetBlockWithName(assembler) as IMyAssembler;
            if (myAssembler == null)
            {
                Echo("Assembler " + assembler + " not found");
                return;
            }

            var stock = getStock(myAssembler.GetInventory());
            var queue = getQueue(myAssembler);

            foreach (Group group in groups)
            {
                Check(group, myAssembler, stock, queue);
            }
        }

        Dictionary<string, float> getStock(IMyInventory oneInventory)
        {
            var connectedInventories = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(connectedInventories, (IMyTerminalBlock x) => x.HasInventory && x.GetInventory().IsConnectedTo(oneInventory));

            var stock = new Dictionary<string, float>();
            foreach (IMyTerminalBlock inv in connectedInventories)
            {
                var items = new List<MyInventoryItem>();
                for (var i = 0; i < inv.InventoryCount; i++)
                {
                    inv.GetInventory(i).GetItems(items);
                }
                foreach (MyInventoryItem item in items)
                {
                    var key = item.Type.TypeId + "/" + item.Type.SubtypeId;
                    float amount = (float)item.Amount.RawValue / 1000000f;
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

            Echo("found " + connectedInventories.Count + " inventories");

            return stock;
        }

        Dictionary<string, float> getQueue(IMyAssembler mainAssembler)
        {
            var queue = new Dictionary<string, float>();

            var connectedAssemblers = new List<IMyAssembler>();
            GridTerminalSystem.GetBlocksOfType(connectedAssemblers, (IMyAssembler x) => x.HasInventory && x.GetInventory().IsConnectedTo(mainAssembler.GetInventory()) && (mainAssembler == x || x.CooperativeMode));
            foreach (IMyAssembler assembler in connectedAssemblers)
            {
                var assemblerQueue = new List<MyProductionItem>();
                assembler.GetQueue(assemblerQueue);
                foreach (MyProductionItem item in assemblerQueue)
                {
                    var key = item.BlueprintId.TypeId + "/" + item.BlueprintId.SubtypeId;
                    float amount = (float)item.Amount.RawValue / 1000000f;
                    if (queue.ContainsKey(key))
                    {
                        queue[key] = queue[key] + amount;
                    }
                    else
                    {
                        queue.Add(key, amount);
                    }
                }
            }

            Echo("found " + connectedAssemblers.Count + " assembler");

            return queue;
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        void Check(Group g, IMyAssembler mainAssembler, Dictionary<string, float> stock, Dictionary<string, float> queue)
        {
            MyDefinitionId bp;
            if (!MyDefinitionId.TryParse(g.bpName, out bp))
            {
                Echo("Failed parsing " + g.bpName);
                return;
            }

            var stockAmount = stock.ContainsKey(g.itemName) ? stock[g.itemName] : 0;
            var queueAmount = queue.ContainsKey(g.bpName) ? queue[g.bpName] : 0;
            var shortName = g.itemName.Split('/')[1];

            var stockAndQueue = stockAmount + queueAmount;
            var msg = shortName + ": " + stockAmount + "s, " + queueAmount + "q";

            if (stockAndQueue < g.min)
            {
                var toCraft = g.max - stockAndQueue;
                if (!mainAssembler.CanUseBlueprint(bp))
                {
                    Echo("Assembler " + assembler + " cannot craft " + g.bpName);
                    Echo(bp + "");
                    return;
                }
                msg += " -> add " + toCraft + " to queue";
                mainAssembler.AddQueueItem(bp, toCraft);
            }

            Echo(msg);
            return;
        }

        // end
    }
}
