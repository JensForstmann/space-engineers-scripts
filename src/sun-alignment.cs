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

namespace SunAlignment
{
    public sealed class Program : MyGridProgram
    {
        // start

        // CONFIGURATION - START

        Group[] groups = {
            // g("name of solar panel that should not get sunlight", "name of rotor to rotate solar panel")
            g("Solar Panel Left Reference", "Rotor Solar Left"),
            g("Solar Panel Right Reference", "Rotor Solar Right"),
            g("Solar Panel Ground Reference", "Rotor Solar Ground"),
            
            g("Solar Panel Oxyfarm Left Reference", "Advanced Rotor Oxyfarm Left"),
            g("Solar Panel Oxyfarm Right Reference", "Advanced Rotor Oxyfarm Right"),
            g("Solar Panel Oxyfarm Ground Reference", "Advanced Rotor Oxyfarm Ground"),
        };

        // CONFIGURATION - END

        int thresholdKw = 10;
        float velocityRpm = 0.25f;

        class Group
        {
            public string solarName;
            public string rotorName;
            public int lastPowerKw = 0;
            public bool reverse = false;
            public bool alreadyReversed = false;
            public Group(string sn, string rn)
            {
                solarName = sn;
                rotorName = rn;
            }
        }

        static Group g(string sn, string rn)
        {
            return new Group(sn, rn);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            velocityRpm = Math.Abs(velocityRpm);
            var i = 1;
            foreach (Group group in groups)
            {
                Echo("-- group " + (i++));
                Align(group);
            }
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        void Align(Group g)
        {
            var solarBlock = GridTerminalSystem.GetBlockWithName(g.solarName) as IMySolarPanel;

            if (solarBlock == null)
            {
                Echo("Solar panel " + g.solarName + " not found");
                return;
            }

            var rotorBlock = GridTerminalSystem.GetBlockWithName(g.rotorName) as IMyMotorStator;
            if (rotorBlock == null)
            {
                Echo("Rotor panel " + g.rotorName + " not found");
                return;
            }

            var currentVelocityAbs = Math.Abs(rotorBlock.GetValueFloat("Velocity"));
            var rotorIsMoving = currentVelocityAbs != 0f;
            var rotorIsMovingManual = rotorIsMoving && currentVelocityAbs != velocityRpm;
            var powerKw = Convert.ToInt32(solarBlock.MaxOutput * 1000);
            var strSolar = g.solarName + " (" + powerKw + "kW)";
            var strRotor = g.rotorName + " (" + (rotorIsMoving ? "on" : "off") + ")";

            if (rotorIsMoving && currentVelocityAbs != velocityRpm)
            {
                Echo("Rotor " + g.rotorName + " is moving manual");
                return;
            }

            if (powerKw > thresholdKw)
            {
                if (!rotorIsMoving)
                {
                    Echo(strSolar + " -> start " + g.rotorName);
                    g.alreadyReversed = false;
                    g.lastPowerKw = powerKw;
                    rotorBlock.TargetVelocityRPM = (g.reverse ? -1 : 1) * velocityRpm;
                    return;
                }
                if (powerKw > g.lastPowerKw && !g.alreadyReversed)
                {
                    Echo(strSolar + " -> reverse " + g.rotorName);
                    g.reverse = !g.reverse;
                    g.alreadyReversed = true;
                    g.lastPowerKw = powerKw;
                    rotorBlock.TargetVelocityRPM = (g.reverse ? -1 : 1) * velocityRpm;
                    return;
                }
                Echo(strSolar + " -> keep turning " + g.rotorName);
                return;
            }

            if (powerKw <= thresholdKw && rotorIsMoving)
            {
                Echo(strSolar + " -> stop " + g.rotorName);
                rotorBlock.TargetVelocityRPM = 0;
                g.lastPowerKw = 0;
                return;
            }

            Echo(strSolar + ", " + strRotor);
            return;
        }

        // end
    }
}
