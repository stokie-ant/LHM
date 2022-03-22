/*
 * LCD Smartie plugin to interact with Libre Hardware Monitor library
 * 
 * Copyright (C) 2022  Stokie-Ant
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using LibreHardwareMonitor.Hardware;
using System;
using System.Linq;
using System.IO;
using System.Security.Principal;
using System.Threading;

namespace LHM
{
    public class LCDSmartie
    {
        static bool started = false;
        static bool broken = false;
        static bool isAdmin = false;
        static bool enableGpu = false;
        static string emessage;

        public class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }

        readonly Computer computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = enableGpu,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true,
            IsBatteryEnabled = true,
            IsPsuEnabled = true,
        };

        public void ThreadLoop ()
        {
            if (!File.Exists("plugins\\LHMDisableGPU.txt"))
                enableGpu = true;

            try
            {
                computer.Open();
            }
            catch (Exception e)
            {
                broken = true;
                emessage = e.ToString();
                return;
            }

            started = true;
            computer.Accept(new UpdateVisitor());

            if (!File.Exists("plugins\\LHMreport.txt"))
                GenerateReport();

            while (true)
                foreach (IHardware hardware in computer.Hardware)
                {
                    hardware.Update();
                    Thread.Sleep(100);
                }
        }

        public LCDSmartie()
        {
            var id = WindowsIdentity.GetCurrent();
            if (id.Owner == id.User)
                return;

            isAdmin = true;

            // we run open() and update() in a thread so smartie doesn't soft lock waiting for the library
            Thread loop = new Thread(new ThreadStart(ThreadLoop));
            loop.Start();
        }

        // Gets names
        public string function1(string param1, string param2)
        {
            if (!isAdmin)
                return "Plugin needs administrator privileges";

            if (broken)
                return emessage;

            if (!started)
                return "waiting for data";

            string[] p = param1.Split('#');
                 int pcount = param1.Split('#').Count();
                 if (pcount == 1)
                 {
                     return computer.Hardware[Convert.ToInt32(p[0])].Name;
                 }
                 if (pcount == 2)
                 {
                     return computer.Hardware[Convert.ToInt32(p[0])].Sensors[Convert.ToInt32(p[1])].Name;
                 }
                 if (pcount == 3)
                 {
                     return computer.Hardware[Convert.ToInt32(p[0])].SubHardware[Convert.ToInt32(p[1])].Sensors[Convert.ToInt32(p[2])].Name;
                 }

            return "wrong number of parameters";

        }

        // gets subhw names
        public string function2(string param1, string param2)
        {
            if (!isAdmin)
                return "Plugin needs administrator privileges";

            if (broken)
                return emessage;

            if (!started)
                return "waiting for data";

            string[] p = param1.Split('#');
            int pcount = param1.Split('#').Count();
            if (pcount == 1)
            {
                return computer.Hardware[Convert.ToInt32(p[0])].Name;
            }
            if (pcount == 2)
            {
                return computer.Hardware[Convert.ToInt32(p[0])].SubHardware[Convert.ToInt32(p[1])].Name;
            }
            if (pcount == 3)
            {
                return computer.Hardware[Convert.ToInt32(p[0])].SubHardware[Convert.ToInt32(p[1])].SubHardware[Convert.ToInt32(p[2])].Name;
            }

            return "wrong number of parameters";

        }

        // Gets values
        public string function3(string param1, string param2)
        {
            if (!isAdmin)
                return "Plugin needs administrator privileges";

            if (broken)
                return emessage;

            if (!started)
                return "waiting for data";

            string result;
            string[] p = param1.Split('#');
            int pcount = param1.Split('#').Count();
            if (pcount == 2)
            {
                result = computer.Hardware[Convert.ToInt32(p[0])].Sensors[Convert.ToInt32(p[1])].Value.ToString();
            }
            else if (pcount == 3)
            {
                result = computer.Hardware[Convert.ToInt32(p[0])].SubHardware[Convert.ToInt32(p[1])].Sensors[Convert.ToInt32(p[2])].Value.ToString();
            }
            else
                return "wrong number of parameters";

            decimal x;
            decimal y;
            decimal resout = Convert.ToDecimal(result);
            p = param2.Split('#');
            pcount = param2.Split('#').Count();
            if (pcount >= 2) // operator#operand
            {                
                x = Convert.ToDecimal(result);
                y = Convert.ToDecimal(p[1]);
                if (Convert.ToInt32(p[0]) == 1)
                    resout = x + y;
                else if (Convert.ToInt32(p[0]) == 2)
                    resout = x - y;
                else if (Convert.ToInt32(p[0]) == 3)
                    resout = x / y;
                else if (Convert.ToInt32(p[0]) == 4)
                    resout = x * y;
                result = resout.ToString();
            }

            if (pcount == 3)// operator#operand#decimal.round
            {
                resout = decimal.Round(resout, Convert.ToInt32(p[2]));
                result = resout.ToString();
            }
        
            return result;

        }

        private void GenerateReport()
        {
            string string1 = "Name";
            string1 = string1.PadRight(50) + "Value";
            string1 = string1.PadRight(65) + "Get Name";
            string1 = string1.PadRight(105) + "Get Value\r\n";
            string string2;
            int idhw = 0;
            int idsubhw = 0;
            int idsen = 0;
            int idsubsen = 0;
            foreach (IHardware hardware in computer.Hardware)
            {
                hardware.Update();

                string1 = string1 + hardware.Name.PadRight(65) + "$dll(LHM.dll,1," + idhw + ",0)" + "\r\n";

                foreach (IHardware subhardware in hardware.SubHardware)
                {
                    string1 = string1 + subhardware.Name.PadRight(65) + "$dll(LHM.dll,2," + idhw + "#" + idsubhw + ",0)" + "\r\n";

                    foreach (ISensor sensor in subhardware.Sensors)
                    {
                        string1 = string1 + sensor.Name.PadRight(50) + sensor.Value.ToString().PadRight(15);
                        string2 = "$dll(LHM.dll,1," + idhw + "#" + idsubhw + "#" + idsubsen + ",0)";
                        string1 = string1 + string2.PadRight(40) + "$dll(LHM.dll,3," + idhw + "#" + idsubhw + "#" + idsubsen + ",0)\r\n";
                        idsubsen++;
                    }
                    idsubhw++;
                    idsubsen = 0;
                }

                foreach (ISensor sensor in hardware.Sensors)
                {
                    string1 = string1 + sensor.Name.PadRight(50) + sensor.Value.ToString().PadRight(15);
                    string2 = "$dll(LHM.dll,1," + idhw + "#" + idsen + ",0)";
                    string1 = string1 + string2.PadRight(40) + "$dll(LHM.dll,3," + idhw + "#" + idsen + ",0)\r\n";
                    idsen++;
                }

                idhw++;
                idsubhw = 0;
                idsen = 0;
                idsubsen = 0;
            }
            File.WriteAllText("plugins\\LHMreport.txt", string1);
        }

        public int GetMinRefreshInterval() => 300;
    }
}
