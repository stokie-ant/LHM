// TODO: Sort out why it sometimes excepts on start

using LibreHardwareMonitor.Hardware;
using System;
using System.Linq;
using System.IO;
using System.Security.Principal;

namespace LHM
{
    public class LCDSmartie
    {
        bool isopen = false;
        static bool IsElevated
        {
            get
            {
                var id = WindowsIdentity.GetCurrent();
                return id.Owner != id.User;
            }
        }

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
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true,
            IsBatteryEnabled = true,
            IsPsuEnabled = true,
        };

        private bool init()
        {
            if (!IsElevated)
                return false;

            try
            {
                computer.Open();
            }
            catch (Exception)
            {
                return false;
            }

            isopen = true;
            computer.Accept(new UpdateVisitor());

            if (!File.Exists("plugins\\LHMreport.txt"))
                GenerateReport();
            return true;

        }

        public LCDSmartie()
        {
            init();
        }

        // Gets names
        public string function1(string param1, string param2)
        {
            if (!IsElevated)
                return "Plugin needs administrator privileges";

            if (!isopen)
                if (!init())
                    return "Can't init library";

            string[] p = param1.Split('#');
                 int pcount = param1.Split('#').Count();
                 if (pcount == 1)
                 {
                     computer.Hardware[Convert.ToInt32(p[0])].Update();
                     return computer.Hardware[Convert.ToInt32(p[0])].Name;
                 }
                 if (pcount == 2)
                 {
                     computer.Hardware[Convert.ToInt32(p[0])].Update();
                     return computer.Hardware[Convert.ToInt32(p[0])].Sensors[Convert.ToInt32(p[1])].Name;
                 }
                 if (pcount == 3)
                 {
                     computer.Hardware[Convert.ToInt32(p[0])].Update();
                     return computer.Hardware[Convert.ToInt32(p[0])].SubHardware[Convert.ToInt32(p[1])].Sensors[Convert.ToInt32(p[2])].Name;
                 }

            return "wrong number of parameters";

        }
        // gets subhw names
        public string function2(string param1, string param2)
        {
            if (!IsElevated)
                return "Plugin needs administrator privileges";

            if (!isopen)
                if (!init())
                    return "Can't init library";

            string[] p = param1.Split('#');
            int pcount = param1.Split('#').Count();
            if (pcount == 1)
            {
                computer.Hardware[Convert.ToInt32(p[0])].Update();
                return computer.Hardware[Convert.ToInt32(p[0])].Name;
            }
            if (pcount == 2)
            {
                computer.Hardware[Convert.ToInt32(p[0])].Update();
                return computer.Hardware[Convert.ToInt32(p[0])].SubHardware[Convert.ToInt32(p[1])].Name;
            }
            if (pcount == 3)
            {
                computer.Hardware[Convert.ToInt32(p[0])].Update();
                return computer.Hardware[Convert.ToInt32(p[0])].SubHardware[Convert.ToInt32(p[1])].SubHardware[Convert.ToInt32(p[2])].Name;
            }

            return "wrong number of parameters";

        }

        // Gets values
        public string function3(string param1, string param2)
        {
            if (!IsElevated)
                return "Plugin needs administrator privileges";

            if (!isopen)
                if (!init())
                    return "Can't init library";

            string result;
            string[] p = param1.Split('#');
            int pcount = param1.Split('#').Count();
            if (pcount == 2)
            {
                computer.Hardware[Convert.ToInt32(p[0])].Update();
                result = computer.Hardware[Convert.ToInt32(p[0])].Sensors[Convert.ToInt32(p[1])].Value.ToString();
            }
            else if (pcount == 3)
            {
                computer.Hardware[Convert.ToInt32(p[0])].Update();
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
                resout = Decimal.Round(resout, Convert.ToInt32(p[2]));
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

        public int GetMinRefreshInterval() => 1000;
    }
}