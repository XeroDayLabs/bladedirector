using System;
using System.Data;

namespace bladeDirectorWCF
{
    public class VMHardwareSpec
    {
        public int memoryMB;
        public int cpuCount;

        public VMHardwareSpec()
        {
            // For XML de/ser
        }

        public VMHardwareSpec(int newMemoryMB, int newCPUCount)
        {
            memoryMB = newMemoryMB;
            cpuCount = newCPUCount;
        }

        public VMHardwareSpec(IDataRecord reader)
        {
            if (!(reader["memoryMB"] is DBNull))
                memoryMB = Convert.ToInt32((long) reader["memoryMB"]);
            if (!(reader["cpuCount"] is DBNull))
                cpuCount = Convert.ToInt32((long)reader["cpuCount"]);
        }
    }

    public class VMSpec
    {
        public VMHardwareSpec hw;
        public VMSoftwareSpec sw;
    }
}