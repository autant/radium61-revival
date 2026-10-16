using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

// WinMM MIDI OUT : le port de destination doit etre un port virtuel explicitement choisi.
// Jamais d'ecriture USB. Fermeture avec extinction des notes actives.
internal static class V47MidiBridge
{
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    private struct MidiOutCaps
    {
        public ushort ManufacturerId, ProductId;
        public uint DriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string ProductName;
        public ushort Technology, Voices, Notes, ChannelMask;
        public uint Support;
    }
    [DllImport("winmm.dll",CharSet=CharSet.Unicode)] private static extern uint midiOutGetNumDevs();
    [DllImport("winmm.dll",EntryPoint="midiOutGetDevCapsW",CharSet=CharSet.Unicode)]
    private static extern uint midiOutGetDevCaps(UIntPtr deviceId,out MidiOutCaps caps,uint size);
    [DllImport("winmm.dll")] private static extern uint midiOutOpen(out IntPtr handle,uint deviceId,IntPtr callback,IntPtr instance,uint flags);
    [DllImport("winmm.dll")] private static extern uint midiOutShortMsg(IntPtr handle,uint msg);
    [DllImport("winmm.dll")] private static extern uint midiOutReset(IntPtr handle);
    [DllImport("winmm.dll")] private static extern uint midiOutClose(IntPtr handle);
    private static uint Packed(byte status,byte d1,byte d2) => (uint)(status | (d1<<8) | (d2<<16));
    public static int ListPorts()
    {
        if(!OperatingSystem.IsWindows()) {Console.Error.WriteLine("Windows uniquement.");return 2;}
        uint n=midiOutGetNumDevs();
        Console.WriteLine($"Sorties MIDI Windows detectees : {n}");
        for(uint i=0;i<n;i++)
        {
            uint e=midiOutGetDevCaps((UIntPtr)i,out var caps,(uint)Marshal.SizeOf<MidiOutCaps>());
            Console.WriteLine(e==0?$"  [{i}] {caps.ProductName}":$"  [{i}] erreur WinMM {e}");
        }
        Console.WriteLine("Choisir un port virtuel cree avec loopMIDI, pas un synthétiseur physique ou inconnu.");
        return 0;
    }

    public static (uint Id, string Name)[] GetPorts()
    {
        if(!OperatingSystem.IsWindows()) return Array.Empty<(uint,string)>();
        var list=new System.Collections.Generic.List<(uint,string)>();
        for(uint i=0;i<midiOutGetNumDevs();i++)
            if(midiOutGetDevCaps((UIntPtr)i,out var caps,(uint)Marshal.SizeOf<MidiOutCaps>())==0)
                list.Add((i,caps.ProductName));
        return list.ToArray();
    }
    // v4.9 : port MIDI ouvert une seule fois; relance de la lecture WinUSB
    // lorsque le dispositif devient momentanement indisponible.
    // L'identite de la sortie est controlee par son nom pour eviter les changements d'index.
    public static int RunContinuous(string portName, Func<bool> keepRunning, Action<string> log)
    {
        if(!OperatingSystem.IsWindows() || !Environment.Is64BitProcess)
            throw new PlatformNotSupportedException("Windows x64 requis");
        var chosen=GetPorts().FirstOrDefault(p => string.Equals(p.Name,portName,StringComparison.Ordinal));
        if(!string.Equals(chosen.Name,portName,StringComparison.Ordinal))
            throw new InvalidOperationException("Sortie MIDI absente : "+portName);
        uint opened=midiOutOpen(out IntPtr midi,chosen.Id,IntPtr.Zero,IntPtr.Zero,0);
        if(opened!=0)throw new InvalidOperationException($"midiOutOpen : {opened}");
        var parser=new MidiParser(midi,log);
        int attempt=0;
        try
        {
            var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Radium61Inspector");
            Directory.CreateDirectory(dir);
            while(keepRunning())
            {
                // 20 000 paquets par fichier : limite la taille des journaux.
                // Chaque rotation ferme/reouvre WinUSB en conservant le port MIDI.
                var file=Path.Combine(dir,$"v49-usb-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.txt");
                log("Journal USB : "+file);
                int code=V44MidiCapture.Run("81",file,parser.Accept,0,keepRunning,true,20000);
                if(!keepRunning())break;
                if(code==0)
                {
                    attempt=0;
                    log("Rotation du journal USB; lecture poursuivie.");
                    continue;
                }
                parser.AllNotesOff(); // evite les notes bloquees a la deconnexion
                attempt++;
                log($"Lecture USB interrompue (code {code}). Nouvelle tentative #{attempt} dans 2 s.");
                log("Si le clavier est repasse en PID_1014, il faut restaurer son firmware manuellement.");
                for(int i=0;i<20 && keepRunning();i++)System.Threading.Thread.Sleep(100);
            }
            return 0;
        }
        finally
        {
            parser.AllNotesOff();
            midiOutReset(midi);
            uint result=midiOutClose(midi);
            if(result!=0)log($"Fermeture port MIDI : {result}");
            log($"Pont arrete. {parser.Total} messages MIDI transmis.");
        }
    }
    public static int Run(string endpoint,string destinationId)
    {
        if(!OperatingSystem.IsWindows() || !Environment.Is64BitProcess) {Console.Error.WriteLine("Windows x64 requis.");return 2;}
        if(endpoint!="81") {Console.Error.WriteLine("Seul endpoint 81 est valide pour le pont initial.");return 2;}
        if(!uint.TryParse(destinationId,NumberStyles.None,CultureInfo.InvariantCulture,out uint id)||id>=midiOutGetNumDevs())
        {Console.Error.WriteLine("Numero de port invalide. Voir --v47-list-ports.");return 2;}
        uint capErr=midiOutGetDevCaps((UIntPtr)id,out var caps,(uint)Marshal.SizeOf<MidiOutCaps>());
        if(capErr!=0){Console.Error.WriteLine($"Port MIDI inaccessible : {capErr}");return 3;}
        if(!caps.ProductName.Contains("Radium",StringComparison.OrdinalIgnoreCase))
            Console.WriteLine($"ATTENTION : port cible [{id}] '{caps.ProductName}'. Verifie qu'il s'agit bien du port virtuel voulu.");
        Console.WriteLine($"Sortie selectionnee : [{id}] {caps.ProductName}");
        uint err=midiOutOpen(out IntPtr midi,id,IntPtr.Zero,IntPtr.Zero,0);
        if(err!=0){Console.Error.WriteLine($"midiOutOpen a echoue : {err}");return 4;}
        var parser=new MidiParser(midi);
        try
        {
            string path=Path.Combine(Environment.CurrentDirectory,$"v47-usb-{DateTime.Now:yyyyMMdd-HHmmss-fff}.txt");
            Console.WriteLine("Le pont fonctionne pendant 60 secondes. Joue des notes et controle le logiciel MIDI recepteur.");
            Console.WriteLine("Journal USB brut : "+path);
            try {return V44MidiCapture.Run(endpoint,path,parser.Accept,60);}
            catch(Exception ex){Console.Error.WriteLine("Pont interrompu : "+ex.Message);return 5;}
        }
        finally
        {
            parser.AllNotesOff();
            midiOutReset(midi);
            uint close=midiOutClose(midi);
            if(close!=0) Console.Error.WriteLine($"midiOutClose : {close}");
            Console.WriteLine($"Fin du pont. {parser.Total} messages MIDI emis.");
        }
    }
    private sealed class MidiParser
    {
        private readonly IntPtr output;
        private readonly Action<string> log;
        private int status=-1,expected=0,count=0;
        private byte data1;
        private readonly bool[,] active=new bool[16,128];
        public int Total {get;private set;}
        public MidiParser(IntPtr output,Action<string>? log=null){this.output=output;this.log=log??Console.WriteLine;}
        public void Accept(byte[] packet,int length)
        {
            if(length%4!=0) {Console.Error.WriteLine("Paquet USB incomplet ignore.");return;}
            for(int i=0;i<length;i+=4)
            {
                byte a=packet[i],b=packet[i+1],c=packet[i+2],d=packet[i+3];
                if(a==0xFE && ((b==0 && c==0 && d==1)||(b==0xFE && c==0xFE && d==3))) continue;
                if(d==1 && b==0 && c==0) Feed(a);
                else if(d==2 && c==0){Feed(a);Feed(b);}
                else Console.Error.WriteLine($"Trame non reconnue : {a:X2}{b:X2}{c:X2}{d:X2}");
            }
        }
        private void Feed(byte b)
        {
            if(b>=0xF8)return; // active sensing et temps reel, sans perturber running status
            if(b>=0x80)
            {
                count=0;
                if(b>=0xF0){status=-1;expected=0;return;} // sysEx non pris en charge
                status=b;
                int commandNibble=b&0xF0;
                expected=(commandNibble==0xC0||commandNibble==0xD0)?1:2;
                return;
            }
            if(status<0||expected==0)return;
            if(count==0){data1=b;count=1;if(expected==2)return;}
            byte d2=expected==1?(byte)0:b;
            uint result=midiOutShortMsg(output,Packed((byte)status,data1,d2));
            if(result!=0)throw new Win32Exception((int)result,$"midiOutShortMsg : code MMRESULT={result}");
            int ch=status&15, cmd=status&0xF0;
            if((cmd==0x80||cmd==0x90)&&data1<128)
            {
                bool on=(cmd==0x90 && d2!=0);
                active[ch,data1]=on;
                log($"{(on?"NOTE_ON ":"NOTE_OFF")} canal={ch+1} note={data1} vel={d2}");
            }
            else log($"MIDI {(byte)status:X2} {data1:X2} {d2:X2}");
            Total++;count=0; // running status
        }
        public void AllNotesOff()
        {
            for(int ch=0;ch<16;ch++)for(int note=0;note<128;note++)if(active[ch,note])
            {
                midiOutShortMsg(output,Packed((byte)(0x80|ch),(byte)note,0));active[ch,note]=false;
            }
        }
    }
}
