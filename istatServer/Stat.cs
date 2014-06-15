using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using OpenHardwareMonitor.GUI;
using OpenHardwareMonitor.Hardware;

namespace istatServer
{
    /// <summary>
    /// Handles collection of data for reporting (cpu, network, etc.)
    /// </summary>
    internal class Stat : IDisposable
    {
        private const int REFRESH_INTERVAL = 1000; //time in msec to update stats;



        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer); 

       
        public long FirstUptime { get; set; }
        public long CurrentUptime { get; set; }
        public FixedSizeQueue<CpuStat> CPU { get; set; }
        public MemStat MEM { get; set; }
        public LoadStat LOAD { get; set; }
        public DiskStat[] DISKS { get; set; }

        public TempItem[] TEMPS { get; set; }
        public FanItem[] FANS { get; set; }
        public FixedSizeQueue<NetStat> NET { get; set; }
        public string NetworkInterfaceName { get; set; }
        


        private readonly PerformanceCounter _cpuPrivCounter = new PerformanceCounter { CategoryName = "Processor", CounterName = "% Privileged Time", InstanceName = "_Total" };
        private readonly PerformanceCounter _cpuUserCounter = new PerformanceCounter { CategoryName = "Processor", CounterName = "% User Time", InstanceName = "_Total" };
        private readonly PerformanceCounter _uptimeCounter = new PerformanceCounter { CategoryName = "System", CounterName = "System Up Time" };
        private readonly PerformanceCounter _poolNonPagedBytesCounter = new PerformanceCounter { CategoryName = "Memory", CounterName = "Pool Nonpaged Bytes" };
        private readonly PerformanceCounter _cachedBytesCounter = new PerformanceCounter { CategoryName = "Memory", CounterName = "Cache Bytes" };
        private readonly PerformanceCounter _pageInSec = new PerformanceCounter { CategoryName = "Memory", CounterName = "Pages Input/sec" };
        private readonly PerformanceCounter _pageOutSec = new PerformanceCounter { CategoryName = "Memory", CounterName = "Pages Output/sec" };
      
        private readonly Computer _computer = new Computer();
        private readonly UpdateVisitor _visitor = new UpdateVisitor();

        private readonly Timer _statUpdateTimer;

        public Stat()
        {
            _computer.HDDEnabled = true;
            _computer.Open();
            _computer.Accept(_visitor);

            FANS = new FanItem[0];
            TEMPS = new TempItem[0];
            
             // first call always seems to return 0;
            _cpuPrivCounter.NextValue();
            _cpuUserCounter.NextValue();
            _uptimeCounter.NextValue();
            _poolNonPagedBytesCounter.NextValue();
            _pageInSec.NextValue();
            _pageOutSec.NextValue();
            _cachedBytesCounter.NextValue();

            SetNetworkInterface();
            CPU = new FixedSizeQueue<CpuStat>{MaxSize = 250};
            NET = new FixedSizeQueue<NetStat> {MaxSize = 250};
            MEM = new MemStat();
            LOAD = new LoadStat();

            //not currently working
           

            PopulateUptime();
            FirstUptime = CurrentUptime;
            PopulateTemps();
            PopulateFans();
            AddCpu();
            AddNet();
            PopulateDisks();
            PopulateMemory();

            _statUpdateTimer = new Timer(TimerTick, null, 0, REFRESH_INTERVAL);
        }

        
        private double GetUnixSeconds()
        {
            return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }



        private bool _processing;
        public void TimerTick(object sender)
        {
            if (_processing) return;
            _processing = true;
            _computer.Accept(_visitor); //Update mechanism for OpenHardwareMonitorLib
            PopulateUptime();
            PopulateTemps();
            PopulateFans();
            AddCpu();
            AddNet();
            PopulateDisks();
            PopulateMemory();
            _processing = false;
        }

        /// <summary>
        /// Finds all temps on the motherboard, hard drives, nvidia & ati graphics cards, and cpu cores
        /// </summary>
        private void PopulateTemps()
        {
            var moboTempSensors = from h in _computer.Hardware
                              where h.HardwareType == HardwareType.Mainboard
                              from sh in h.SubHardware
                              where sh.HardwareType == HardwareType.SuperIO
                              from ts in sh.Sensors
                              where ts.SensorType == SensorType.Temperature
                              select ts;


            var gpuTempSensors = from h in _computer.Hardware
                                 where
                                     (h.HardwareType == HardwareType.GpuAti | h.HardwareType == HardwareType.GpuNvidia |h.HardwareType == HardwareType.HDD | h.HardwareType == HardwareType.CPU)
                                 from s in h.Sensors
                                 where s.SensorType == SensorType.Temperature
                                 select s;
            var allTemps = moboTempSensors.Concat(gpuTempSensors);

            int index = 1;
            
            TEMPS = (from s in allTemps.Where(s => s.Value != null)
                        select new TempItem {Index = index++, Name = s.Name, TemperatureC = s.Value.Value}).OrderBy(s => s.Name).ToArray();
        }

        /// <summary>
        /// Finds all temps from the motherboard and from nvidia & ati graphics cards
        /// </summary>
        private void PopulateFans()
        {
            var moboFanSensors = from h in _computer.Hardware
                                  where h.HardwareType == HardwareType.Mainboard
                                  from sh in h.SubHardware
                                  where sh.HardwareType == HardwareType.SuperIO
                                  from ts in sh.Sensors
                                  where ts.SensorType == SensorType.Fan
                                  select ts;

            var gpuFanSensors = from h in _computer.Hardware
                                 where
                                     (h.HardwareType == HardwareType.GpuAti || h.HardwareType == HardwareType.GpuNvidia)
                                 from s in h.Sensors
                                 where s.SensorType == SensorType.Fan
                                 select s;
            var allTemps = moboFanSensors.Concat(gpuFanSensors);

            int index = 1;

            FANS = (from s in allTemps.Where(s => s.Value != null)
                     select new FanItem { Index = index++, Name = s.Name, RPM = (int)s.Value.Value }).OrderBy(s => s.Name).ToArray();
        }

        /// <summary>
        /// Selects interface with most traffic since only one can be reported
        /// </summary>
        private void SetNetworkInterface()
        {

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                long max = long.MinValue;
                string name = null;
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    long total = ni.GetIPv4Statistics().BytesReceived + ni.GetIPv4Statistics().BytesSent;
                    if (total > max)
                    {
                        max = total;
                        name = ni.Name;
                    }
                }
                NetworkInterfaceName = name;
            }
        }

        /// <summary>
        /// Handles CPU and LOAD population
        /// </summary>
        private void AddCpu()
        {

            var usr = _cpuUserCounter.NextValue();
            var priv = _cpuPrivCounter.NextValue();
            LOAD.AddValue((usr + priv)/100d);
            var cs = new CpuStat
                         {
                             Uptime = CurrentUptime,
                             Idle = clamp(0,100,(int)(100 - (usr + priv))),
                             System = (int)priv,
                             User = (int)usr,
                             Nice = 0
                         };
            CPU.Enqueue(cs);
        }

        private int clamp(int min, int max, int value)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        /// <summary>
        /// Adds a new network interface measurement
        /// </summary>
        private void AddNet()
        {
            if (NetworkInterfaceName == null) return;
            NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.Name == NetworkInterfaceName).FirstOrDefault();
            if (ni == null) return;
            var ns = new NetStat
                             {
                                 Uptime = CurrentUptime,
                                 Upload = ni.GetIPv4Statistics().BytesSent,
                                 Download = ni.GetIPv4Statistics().BytesReceived,
                                 UnixTime = GetUnixSeconds()
                             };
            NET.Enqueue(ns);
        }

        

        private void PopulateMemory()
        {
            var status = new MEMORYSTATUSEX();
            GlobalMemoryStatusEx(status);
            UInt64 poolNonPaged = Convert.ToUInt64(_poolNonPagedBytesCounter.NextValue());
            long pageInSec = Convert.ToInt64(_pageInSec.NextValue());
            long pageOutSec = Convert.ToInt64(_pageOutSec.NextValue());
            UInt64 cachedBytes = Convert.ToUInt64(_cachedBytesCounter.NextValue());

            //Total physical = (kernel used + user used) + Cached + Free

            lock (MEM)
            {
                MEM.Free = Convert.ToInt64(status.ullAvailPhys)/1000000;
                MEM.Active = Convert.ToInt64(status.ullTotalPhys - status.ullAvailPhys - poolNonPaged - cachedBytes)/1000000;
                MEM.Inactive = Convert.ToInt64(cachedBytes/1000000);
                MEM.Total = Convert.ToInt64(status.ullTotalPhys)/1000000;
                MEM.Wired = poolNonPaged/1000000;
                MEM.PageInCount = pageInSec; //sample time is 1 second so should be ~
                MEM.PageOutCount = pageOutSec; //sample time is 1 second so should be ~
                MEM.SwapTotal = Convert.ToInt64(status.ullTotalPageFile)/1000000;
                MEM.SwapUsed = Convert.ToInt64(status.ullTotalPageFile - status.ullAvailPageFile)/1000000;
            }
        }

        private void PopulateUptime()
        {
                CurrentUptime = Convert.ToInt64(_uptimeCounter.NextValue());
        }

        private void PopulateDisks()
        {
            DISKS = (from d in DriveInfo.GetDrives()
                     where d.DriveType == DriveType.Fixed
                     select
                         new DiskStat
                             {
                                 Free = d.AvailableFreeSpace / 1000000,
                                 Uuid = d.Name.GetHashCode().ToString(),
                                 Name = d.Name,
                                 PercentUsed = 100- (int) ((d.TotalFreeSpace*100/d.TotalSize)),
                                 Total = (int)d.TotalSize / 1000000
                             }).ToArray();
        }


        public void Dispose()
        {
            _statUpdateTimer.Dispose();
            _computer.Close();

            _cpuPrivCounter.Close();
            _cpuPrivCounter.Dispose();

            _cpuUserCounter.Close();
            _cpuUserCounter.Dispose();

            _cachedBytesCounter.Close();
            _cachedBytesCounter.Dispose();

            
            _poolNonPagedBytesCounter.Close();
            _poolNonPagedBytesCounter.Dispose();

            _pageInSec.Close();
            _pageInSec.Dispose();

            _pageOutSec.Close();
            _pageOutSec.Dispose();

            PerformanceCounter.CloseSharedResources();
        }
    }


    /// <summary>
    /// One network measurement sample
    /// </summary>
    internal class NetStat
    {
        /// <summary>
        /// Uptime - used as a time index by client
        /// </summary>
        public long Uptime { get; set; }

        /// <summary>
        /// Bytes downloaded
        /// </summary>
        public long Download { get; set; }

        /// <summary>
        /// Bytes uploaded
        /// </summary>
        public long Upload { get; set; }

        /// <summary>
        /// Time since 1,1,1970 in seconds
        /// </summary>
        public double UnixTime { get; set; }
    }


    /// <summary>
    /// Disk information
    /// </summary>
    internal class DiskStat
    {
        /// <summary>
        /// Name of disk ("C:\", etc.)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Unique ID for disk
        /// </summary>
        public string Uuid { get; set; }

        /// <summary>
        /// Space free
        /// </summary>
        public long Free { get; set; }

        /// <summary>
        /// Percent used
        /// </summary>
        public int PercentUsed { get; set; }

        /// <summary>
        /// Total space
        /// </summary>
        public long Total { get; set; }
    }


    /// <summary>
    /// Temp item - unique index for each named temperature zone
    /// </summary>
    internal class TempItem
    {
        /// <summary>
        /// Name of temperature zone
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Unique index for each temperature zone
        /// </summary>
        public int Index { get; set; }


        /// <summary>
        /// Temperature in centigrade
        /// </summary>
        public double TemperatureC { get; set; }
    }
 
    internal class FanItem
    {
        /// <summary>
        /// Name of fan
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// Unique index for each fan
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Current RPM value of fan
        /// </summary>
        public int RPM { get; set; }
    }


    /// <summary>
    /// Class which calculates a 0-1.0 based load factor for one, five, and ten minute intervals
    /// </summary>
    internal class LoadStat
    {

        private readonly FixedSizeQueue<double> _loadVals = new FixedSizeQueue<double> { MaxSize = 10 * 60 }; //one sample per second

        /// <summary>
        /// One minute load average
        /// </summary>
        public double OneMinuteAverage
        {
            get
            {
                if (_loadVals.Count() < 1 * 60)
                    return 0; // not enough data
                return _loadVals.Slice(0, 1*60).Average();
            }
        }

        /// <summary>
        /// 5 minute load average
        /// </summary>
        public double FiveMinuteAverage
        {
            get
            {
                if (_loadVals.Count() < 10 * 60)
                    return 0; // not enough data
                return _loadVals.Slice(0, 10 * 60).Average();
            }
        }


        /// <summary>
        /// 10 minute load average
        /// </summary>
        public double TenMinuteAverage
        {
            get
            {
                if (_loadVals.Count() < 10*60)
                    return 0; // not enough data
                return _loadVals.Slice(0, 10*60).Average();
            }
        }

        /// <summary>
        /// Add a new load item (0.0 to 1.0)
        /// </summary>
        /// <param name="val">New load value</param>
        public void AddValue(double val)
        {
            _loadVals.Enqueue(val);
        }
    }

    

    internal class MemStat
    {
        //string.Format("<MEM w=\"{1}\" a=\"{2}\" i=\"{3}\" f=\"{4}\" t=\"{5}\" su=\"{6}\" st=\"{7}\" pi=\"{8}\" po=\"{9}\"></MEM>") +
        /*
                     * Memory
            Wired memory
            This is memory that applications or the system needs immediate access to, so it can’t be cached to disk. It will vary depending on what applications you’re using.
            Active memory
            This is memory that is actively being used.
            Inactive memory
            This memory is no longer being used and has been cached to disk. It’ll remain in RAM until another application needs the space.
            Free memory
            This memory is not being used.
         */
     
        /// <summary>
        /// This is memory that applications or the system needs immediate access to, so it can’t be cached to disk
        /// </summary>
        public UInt64 Wired { get; set; }

        /// <summary>
        /// This is memory that is actively being used.
        /// </summary>
        public long Active { get; set; }

        /// <summary>
        /// This memory is no longer being used and has been cached to disk. It’ll remain in RAM until another application needs the space.
        /// </summary>
        public long Inactive { get; set; }

        /// <summary>
        /// This memory is not being used.
        /// </summary>
        public long Free { get; set; }

        /// <summary>
        /// Total available memory
        /// </summary>
        public long Total { get; set; }

        /// <summary>
        /// Amount of pagefile used
        /// </summary>
        public long SwapUsed { get; set; }

        /// <summary>
        /// Pagefile total
        /// </summary>
        public long SwapTotal { get; set; }

        /// <summary>
        /// Page In Rate (per second)
        /// </summary>
        public long PageInCount { get; set; }

        /// <summary>
        /// Page Out Rate (per second)
        /// </summary>
        public long PageOutCount { get; set; }
    }

    /// <summary>
    /// Holds one CPU measurement stat
    /// </summary>
    internal class CpuStat
    {
        /// <summary>
        /// Uptime value when this was taken
        /// </summary>
        public long Uptime { get; set; }

        /// <summary>
        /// Percentage of time that the CPU or CPUs were idle.
        /// </summary>
        public int Idle { get; set; }

        /// <summary>
        ///  Percentage of CPU time used by the current user.
        /// </summary>
        public int User { get; set; }

        /// <summary>
        /// Percentage of CPU time used by tasks that belong to the system (eg. processes owned by root, windowserver etc).
        /// </summary>
        public int System { get; set; }

        /// <summary>
        ///  Percentage of CPU time used by tasks that are running using nice. These processes are using a non standard priority level to give them more or less priority.
        /// </summary>
        public int Nice { get; set; }
    }
}
