using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace StatsScreen.Services.Hardware.GraniteRidge;

// Original wrapper of the documented PawnIO ABI. See docs/GRANITE-RIDGE.md.
internal sealed class PawnIoSmuReader : IRyzenSmuReader
{
    private readonly SafeFileHandle _handle;
    private readonly Mutex _pci = new(false, @"Global\Access_PCI");
    private bool _knownVersion;
    private bool _disposed;

    public PawnIoSmuReader()
    {
        _handle = CreateFile(@"\\?\GLOBALROOT\Device\PawnIO", 3, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
        try
        {
            if (_handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
            using Stream stream = typeof(PawnIoSmuReader).Assembly.GetManifestResourceStream("StatsScreen.Resources.RyzenSMU-0.2.11.bin")
                ?? throw new InvalidOperationException("Signed module missing");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            byte[] module = memory.ToArray();
            if (Convert.ToHexString(SHA256.HashData(module)) != "301D9CA397108E09F31BFBD5AC4C9BB4F352A5DE68532C32DB3BA7DDCDE93450")
                throw new InvalidOperationException("Signed module integrity mismatch");
            Transfer(0xA1B22084, module, 0);
        }
        catch { _handle.Dispose(); _pci.Dispose(); throw; }
    }

    public uint ResolveVersion() => WithPci(() =>
    {
        _knownVersion = false;
        byte[] result = Telemetry("ioctl_resolve_pm_table", 16);
        ulong version = BitConverter.ToUInt64(result, 0);
        if (version > uint.MaxValue || BitConverter.ToUInt64(result, 8) == 0)
            throw new InvalidOperationException("Invalid PM descriptor");
        _knownVersion = version == 0x620105;
        return (uint)version;
    });

    public byte[] RefreshAndReadTable() => WithPci(() =>
    {
        if (!_knownVersion) throw new InvalidOperationException("PM layout not validated");
        Telemetry("ioctl_update_pm_table", 0);
        // PawnIO transfers 64-bit cells: 1828 bytes plus four transport-padding bytes.
        byte[] raw = Telemetry("ioctl_read_pm_table", 1832);
        byte[] confirm = Telemetry("ioctl_read_pm_table", 1832);
        if (!raw.AsSpan(0, 1828).SequenceEqual(confirm.AsSpan(0, 1828)))
            throw new InvalidOperationException("Inconsistent PM table snapshot");
        return raw[..1828];
    });

    private T WithPci<T>(Func<T> action)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        bool held = false;
        try
        {
            try { held = _pci.WaitOne(5000); }
            catch (AbandonedMutexException) { held = true; throw new InvalidOperationException("Abandoned PCI access mutex"); }
            if (!held) throw new TimeoutException("PCI access mutex timeout");
            return action();
        }
        finally { if (held) _pci.ReleaseMutex(); }
    }

    private byte[] Telemetry(string operation, int bytes)
    {
        if (operation is not ("ioctl_resolve_pm_table" or "ioctl_update_pm_table" or "ioctl_read_pm_table"))
            throw new InvalidOperationException("Operation is not a telemetry operation");
        byte[] request = new byte[32];
        Encoding.ASCII.GetBytes(operation).CopyTo(request, 0);
        return Transfer(0xA1B22104, request, bytes);
    }

    private byte[] Transfer(uint code, byte[] input, int length)
    {
        byte[] output = new byte[length];
        if (!DeviceIoControl(_handle, code, input, (uint)input.Length, output, (uint)length, out uint read, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        if (read != length) throw new InvalidOperationException($"Incomplete PawnIO response: {read}/{length}");
        return output;
    }

    public void Dispose() { if (_disposed) return; _disposed = true; _handle.Dispose(); _pci.Dispose(); }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(SafeFileHandle handle, uint code, byte[] input, uint inputSize, byte[] output, uint outputSize, out uint returned, IntPtr overlapped);
}
