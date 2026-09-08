using System.Management;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace StatsScreen.Services.Hardware.GraniteRidge;

public sealed record CpuIdentity(string Name, string Vendor, int Family, int Model, int Cores, int Packages)
{
    public bool IsSupported => Vendor == "AuthenticAMD" && Family == 0x1A && Model == 0x44 &&
        Cores == 8 && Packages == 1 && Name.Trim() == "AMD Ryzen 7 9800X3D 8-Core Processor";

    public static CpuIdentity Detect()
    {
        if (!X86Base.IsSupported) return new("Unknown", "Unknown", 0, 0, 0, 0);
        var vendor = X86Base.CpuId(0, 0);
        byte[] bytes = new byte[12];
        BitConverter.GetBytes(vendor.Ebx).CopyTo(bytes, 0);
        BitConverter.GetBytes(vendor.Edx).CopyTo(bytes, 4);
        BitConverter.GetBytes(vendor.Ecx).CopyTo(bytes, 8);
        int eax = X86Base.CpuId(1, 0).Eax;
        int family = (eax >> 8) & 15;
        int model = (eax >> 4) & 15;
        if (family is 6 or 15) model |= (eax >> 12) & 0xF0;
        if (family == 15) family += (eax >> 20) & 255;
        using var query = new ManagementObjectSearcher("SELECT Name, NumberOfCores FROM Win32_Processor");
        using var results = query.Get();
        string name = "Unknown";
        int cores = 0, packages = 0;
        foreach (ManagementObject cpu in results)
        {
            using (cpu) { name = Convert.ToString(cpu["Name"]) ?? "Unknown"; cores += Convert.ToInt32(cpu["NumberOfCores"]); packages++; }
        }
        return new(name.Trim(), Encoding.ASCII.GetString(bytes), family, model, cores, packages);
    }
}
