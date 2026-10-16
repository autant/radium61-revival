using System;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Radium61Revival;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new PlayerForm());
    }
}

internal sealed class PlayerForm : Form
{
    private readonly ComboBox ports = new() { DropDownStyle=ComboBoxStyle.DropDownList, Width=365 };
    private readonly Button refresh = new() { Text="Actualiser les ports", AutoSize=true };
    private readonly Button start = new() { Text="▶ Démarrer", Width=125, Height=38 };
    private readonly Button stop = new() { Text="■ Arrêter", Width=125, Height=38, Enabled=false };
    private readonly Label state = new() { Text="Arrêté", AutoSize=true };
    private readonly TextBox events = new() { Multiline=true, ReadOnly=true, ScrollBars=ScrollBars.Vertical, Dock=DockStyle.Fill, Font=new Font("Consolas",9), BackColor=Color.FromArgb(18,28,42), ForeColor=Color.FromArgb(216,242,237), BorderStyle=BorderStyle.FixedSingle };
    private volatile bool running;
    private Task? worker;
    private bool closingAfterStop;
    private readonly string settingsPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Radium61Inspector","player-settings.json");
    private sealed record Settings(string? MidiPort);
    private int displayed;
    private sealed record Port(uint Id, string Name) { public override string ToString() => $"[{Id}] {Name}"; }

    public PlayerForm()
    {
        Text="Radium61 Revival 5.0 — Clavier MIDI USB";
        Icon=new Icon(Path.Combine(AppContext.BaseDirectory,"radium.ico"));
        BackColor=Color.FromArgb(245,248,251);
        MinimumSize=new Size(690,450);Size=new Size(820,540);
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(14), ColumnCount=1, RowCount=4 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var title=new Label { Text="Radium61 Revival 5.0  |  USB → MIDI Windows",AutoSize=true, Font=new Font(Font.FontFamily,13,FontStyle.Bold),Margin=new Padding(3,3,3,15) };
        var selection=new FlowLayoutPanel { Dock=DockStyle.Top, AutoSize=true, WrapContents=true };
        selection.Controls.Add(new Label { Text="Sortie MIDI :",AutoSize=true,Margin=new Padding(0,9,8,0) });
        selection.Controls.Add(ports); selection.Controls.Add(refresh);
        var controls=new FlowLayoutPanel { Dock=DockStyle.Top,AutoSize=true,Margin=new Padding(0,14,0,14) };
        controls.Controls.Add(start);controls.Controls.Add(stop);
        state.Margin=new Padding(12,11,0,0);controls.Controls.Add(state);
        layout.Controls.Add(title,0,0);layout.Controls.Add(selection,0,1);layout.Controls.Add(controls,0,2);layout.Controls.Add(events,0,3);
        Controls.Add(layout);
        start.BackColor=Color.FromArgb(33,125,109);start.ForeColor=Color.White;start.FlatStyle=FlatStyle.Flat;
        stop.BackColor=Color.FromArgb(212,226,231);stop.FlatStyle=FlatStyle.Flat;
        refresh.Click+=(_,_)=>RefreshPorts();
        start.Click+=(_,_)=>StartBridge();
        stop.Click+=(_,_)=>StopBridge();
        FormClosing+=OnClosing;
        RefreshPorts();
        Log("Radium61 Revival 5.0 — prêt. Choisissez une sortie MIDI et cliquez sur Démarrer.");
        Log("Prérequis : clavier initialisé en PID_1015, WinUSB associé au clavier.");
        Log("Pour un séquenceur : choisir le port loopMIDI comme sortie ici et comme entrée dans le séquenceur.");
        Log("Pour un test sonore direct : choisir Microsoft GS Wavetable Synth.");
        Log("Aucune commande USB OUT. Firmware non modifié.");
    }
    private void RefreshPorts()
    {
        var former=(ports.SelectedItem as Port)?.Name;
        if(former is null)
        {
            try
            {
                if(File.Exists(settingsPath))
                    former=JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath))?.MidiPort;
            }
            catch(Exception ex){Log("Parametres ignores : "+ex.Message);}
        }
        ports.Items.Clear();
        try
        {
            foreach(var item in V47MidiBridge.GetPorts()) ports.Items.Add(new Port(item.Id,item.Name));
            var ix=Enumerable.Range(0,ports.Items.Count).FirstOrDefault(i=>(ports.Items[i] as Port)?.Name==former,-1);
            if(ix<0)ix=Enumerable.Range(0,ports.Items.Count).FirstOrDefault(i=>(ports.Items[i] as Port)?.Name.Contains("Radium61 Virtual",StringComparison.OrdinalIgnoreCase)==true,-1);
            if(ix<0 && ports.Items.Count>0)ix=0;
            ports.SelectedIndex=ix;
        }
        catch(Exception ex){Log("Erreur de liste MIDI : "+ex.Message);}
    }
    private void StartBridge()
    {
        if(worker is { IsCompleted:false } || ports.SelectedItem is not Port port)return;
        running=true;displayed=0;
        start.Enabled=false;stop.Enabled=true;refresh.Enabled=false;ports.Enabled=false;
        state.Text="Lecture MIDI en cours...";
        Log("Demarrage sur "+port);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            File.WriteAllText(settingsPath,JsonSerializer.Serialize(new Settings(port.Name)));
        }
        catch(Exception ex){Log("Sauvegarde de la sortie impossible : "+ex.Message);}
        worker=Task.Run(()=>
        {
            try
            {
                var code=V47MidiBridge.RunContinuous(port.Name,()=>running,Log);
                Log("Fin de lecture, code="+code);
            }
            catch(Exception ex){Log("Erreur pont MIDI : "+ex.Message);}
            finally
            {
                running=false;
                try { BeginInvoke((Action)(()=>
                {
                    start.Enabled=true;stop.Enabled=false;refresh.Enabled=true;ports.Enabled=true;
                    state.Text="Arrêté";
                    if(closingAfterStop)Close();
                })); } catch(InvalidOperationException) { }
            }
        });
    }
    private void StopBridge(){running=false;stop.Enabled=false;state.Text="Arret demande...";}
    private void Log(string text)
    {
        if(IsDisposed || !IsHandleCreated)return;
        try
        {
            BeginInvoke((Action)(()=>
            {
                if(IsDisposed)return;
                displayed++;
                if(displayed%200==0 && events.TextLength>30000)events.Clear();
                events.AppendText($"{DateTime.Now:HH:mm:ss} {text}{Environment.NewLine}");
            }));
        }
        catch(InvalidOperationException) { }
    }
    private void OnClosing(object? sender,FormClosingEventArgs e)
    {
        if(worker is { IsCompleted:false })
        {
            e.Cancel=true;
            closingAfterStop=true;
            StopBridge();
        }
    }
}
