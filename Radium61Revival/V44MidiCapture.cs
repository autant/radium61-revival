using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

// Strictly input-only capture. Requires user to associate PID_1015 with WinUSB beforehand.
internal static class V44MidiCapture
{
    private const string InstanceNeedle = "vid_0763&pid_1015";
    private static readonly Guid WinUsbInterfaceGuid = new("E924A51A-A87F-4099-8066-FE2EE7E60361");
    [StructLayout(LayoutKind.Sequential)]
    private struct InterfaceDescriptor
    {
        public byte Length, DescriptorType, InterfaceNumber, AlternateSetting, NumEndpoints, InterfaceClass, InterfaceSubClass, InterfaceProtocol, Interface;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PipeInformation
    {
        public int PipeType;
        public byte PipeId;
        public ushort MaximumPacketSize;
        public byte Interval;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct InterfaceData { public int Size; public Guid InterfaceClassGuid; public uint Flags; public IntPtr Reserved; }
    private const uint DIGCF_PRESENT=2, DIGCF_DEVICEINTERFACE=16;
    [DllImport("setupapi.dll",EntryPoint="SetupDiGetClassDevsW",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid guid,string? enumerator,IntPtr parent,uint flags);
    [DllImport("setupapi.dll",SetLastError=true)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set,IntPtr data,ref Guid guid,uint index,ref InterfaceData info);
    [DllImport("setupapi.dll",EntryPoint="SetupDiGetDeviceInterfaceDetailW",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set,ref InterfaceData info,IntPtr detail,uint detailSize,out uint required,IntPtr devinfo);
    [DllImport("setupapi.dll",SetLastError=true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("kernel32.dll",EntryPoint="CreateFileW",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern SafeFileHandle CreateFile(string name,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
    [DllImport("winusb.dll",SetLastError=true)] private static extern bool WinUsb_Initialize(SafeFileHandle handle,out IntPtr usb);
    [DllImport("winusb.dll",SetLastError=true)] private static extern bool WinUsb_Free(IntPtr usb);
    [DllImport("winusb.dll",SetLastError=true)] private static extern bool WinUsb_QueryInterfaceSettings(IntPtr usb,byte alternate,out InterfaceDescriptor descriptor);
    [DllImport("winusb.dll",SetLastError=true)] private static extern bool WinUsb_QueryPipe(IntPtr usb,byte alternate,byte index,out PipeInformation info);
    [DllImport("winusb.dll",SetLastError=true)] private static extern bool WinUsb_SetPipePolicy(IntPtr usb,byte pipe,uint policy,uint size,ref uint value);
    [DllImport("winusb.dll",SetLastError=true)] private static extern bool WinUsb_ReadPipe(IntPtr usb,byte pipe,byte[] data,uint size,out uint read,IntPtr overlapped);

    private static string[] FindPaths()
    {
        var result=new System.Collections.Generic.List<string>();
        var guid=WinUsbInterfaceGuid;
        IntPtr set=SetupDiGetClassDevs(ref guid,null,IntPtr.Zero,DIGCF_PRESENT|DIGCF_DEVICEINTERFACE);
        if(set==new IntPtr(-1)) throw new Win32Exception(Marshal.GetLastPInvokeError(),"SetupDiGetClassDevs");
        try
        {
            for(uint i=0;;i++)
            {
                var info=new InterfaceData { Size=Marshal.SizeOf<InterfaceData>() };
                if(!SetupDiEnumDeviceInterfaces(set,IntPtr.Zero,ref guid,i,ref info))
                {
                    int e=Marshal.GetLastPInvokeError();
                    if(e==259)break;
                    throw new Win32Exception(e,"SetupDiEnumDeviceInterfaces");
                }
                SetupDiGetDeviceInterfaceDetail(set,ref info,IntPtr.Zero,0,out uint required,IntPtr.Zero);
                if(required<8 || required>65536) throw new InvalidDataException("Taille detail anormale");
                IntPtr memory=Marshal.AllocHGlobal((int)required);
                try
                {
                    // SP_DEVICE_INTERFACE_DETAIL_DATA_W.cbSize == 8 on 64-bit Windows.
                    Marshal.WriteInt32(memory,8);
                    if(!SetupDiGetDeviceInterfaceDetail(set,ref info,memory,required,out _,IntPtr.Zero))
                        throw new Win32Exception(Marshal.GetLastPInvokeError(),"SetupDiGetDeviceInterfaceDetail");
                    string path=Marshal.PtrToStringUni(IntPtr.Add(memory,4))??"";
                    if(path.Contains(InstanceNeedle,StringComparison.OrdinalIgnoreCase))result.Add(path);
                }
                finally { Marshal.FreeHGlobal(memory); }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
    public static int Run(string endpoint,string destination, Action<byte[],int>? onPacket=null, int seconds=15, Func<bool>? keepRunning=null, bool quietPackets=false, int maxPackets=0)
    {
        if(!OperatingSystem.IsWindows() || Environment.Is64BitProcess==false)
        { Console.Error.WriteLine("Windows x64 uniquement.");return 2; }
        if(endpoint!="81" && endpoint!="82")
        { Console.Error.WriteLine("Endpoint autorise : 81 ou 82 (IN uniquement).");return 2; }
        byte pipe=Convert.ToByte(endpoint,16);
        string output=Path.GetFullPath(destination);
        if(File.Exists(output)) { Console.Error.WriteLine("Fichier existe deja : aucun ecrasement.");return 2; }
        string[] paths;
        try { paths=FindPaths(); }
        catch(Exception ex) { Console.Error.WriteLine("Decouverte : "+ex.Message);return 3; }
        if(paths.Length!=1)
        { Console.Error.WriteLine($"PID_1015 interfaces WinUSB trouvees : {paths.Length}; attendu : 1. STOP. Aucun changement de pilote automatise.");return 3; }
        using var handle=CreateFile(paths[0],0xC0000000,3,IntPtr.Zero,3,0x40000000,IntPtr.Zero); // GENERIC_READ | GENERIC_WRITE requis pour WinUsb_Initialize ; aucune ecriture envoyee
        if(handle.IsInvalid) { Console.Error.WriteLine("Ouverture lecture seule : "+new Win32Exception(Marshal.GetLastPInvokeError()).Message);return 4; }
        if(!WinUsb_Initialize(handle,out IntPtr usb))
        { int e=Marshal.GetLastPInvokeError(); Console.Error.WriteLine($"WinUsb_Initialize : Win32={e}, {new Win32Exception(e).Message}");return 4; }
        try
        {
            if(!WinUsb_QueryInterfaceSettings(usb,0,out var descriptor) || descriptor.InterfaceClass!=0xFF || descriptor.NumEndpoints!=3)
            { Console.Error.WriteLine("Interface attendue FF avec 3 endpoints absente. STOP.");return 4; }
            bool found=false;
            for(byte i=0;i<descriptor.NumEndpoints;i++)
            {
                if(!WinUsb_QueryPipe(usb,0,i,out var item)) { Console.Error.WriteLine("WinUsb_QueryPipe a echoue.");return 4; }
                if(item.PipeId==pipe && item.MaximumPacketSize==32 &&
                   ((pipe==0x81 && item.PipeType==3) || (pipe==0x82 && item.PipeType==2))) found=true;
            }
            if(!found) { Console.Error.WriteLine("Endpoint inattendu. STOP.");return 4; }
            uint timeoutMs=500;
            if(!WinUsb_SetPipePolicy(usb,pipe,3,4,ref timeoutMs))
            { Console.Error.WriteLine("Impossible de fixer le timeout de lecture. STOP : "+Marshal.GetLastPInvokeError());return 4; }
            Console.WriteLine($"Lecture endpoint 0x{pipe:X2}, {(seconds<=0?"continue":seconds+" s")}. Aucune ecriture USB OUT.");
            using var writer=new StreamWriter(new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read),new UTF8Encoding(false)) { AutoFlush=true };
            writer.WriteLine($"# Radium61 v4.4 PID1015 endpoint 0x{pipe:X2} UTC={DateTimeOffset.UtcNow:O}");
            var until=DateTime.UtcNow.AddSeconds(seconds);
            int packets=0,failures=0;
            while((seconds<=0 || DateTime.UtcNow<until) && (keepRunning is null || keepRunning()))
            {
                var buffer=new byte[32];
                if(WinUsb_ReadPipe(usb,pipe,buffer,(uint)buffer.Length,out uint count,IntPtr.Zero))
                {
                    if(count>0)
                    {
                        string line=$"{DateTimeOffset.UtcNow:O} len={count} {Convert.ToHexString(buffer.AsSpan(0,(int)count))}";
                        writer.WriteLine(line);if(onPacket is null && !quietPackets)Console.WriteLine(line);packets++;
                        onPacket?.Invoke(buffer,(int)count);
                        if(maxPackets>0 && packets>=maxPackets)break;
                        if(seconds>0 && packets>=1000) { Console.WriteLine("Limite 1000 paquets atteinte.");break; }
                    }
                }
                else
                {
                    int error=Marshal.GetLastPInvokeError();
                    if(error==121 || error==1460) continue; // expected timeout
                    writer.WriteLine($"# READ_ERROR Win32={error}");
                    Console.Error.WriteLine($"Lecture interrompue Win32={error}");failures++;break;
                }
            }
            writer.WriteLine($"# END packets={packets} failures={failures}");
            Console.WriteLine($"Capture terminee : {packets} paquets, {failures} erreur(s). {output}");
            return failures==0?0:5;
        }
        finally { WinUsb_Free(usb); }
    }
}
