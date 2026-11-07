using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using VNotch.Models;

namespace VNotch.Services;
internal static class ZuzongIntegration {
 static string Argument(string key,string fallback){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?Path.GetFullPath(args[i+1]):fallback;}
 internal static bool IsIsolated=>Environment.GetCommandLineArgs().Contains("--isolated-data")||string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath),"VNotch.Tests",StringComparison.OrdinalIgnoreCase);
 internal static string ProfileRoot=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ZuzongDynamic");
 internal static string ConfigPath=>Argument("--settings",Path.Combine(ProfileRoot,"notch.json"));
 internal static void EnsureConfiguration(){
  Directory.CreateDirectory(ParentData);
  if(File.Exists(ConfigPath)){
   var saved=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(ConfigPath))!.AsObject();
   bool changed=false;
   if(!saved.ContainsKey("neutralGlassVersion")){saved["tintColor"]="#18000000";saved["neutralGlassVersion"]=1;changed=true;}
   // An explicit launch must recover the disabled flag left by an update or test.
   if(!IsIsolated&&saved["enabled"]?.GetValue<bool>()==false){saved["enabled"]=true;changed=true;}
   if(changed){string temp=ConfigPath+".startup-new";File.WriteAllText(temp,saved.ToJsonString(new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,ConfigPath,true);}
   return;
  }
  File.WriteAllText(ConfigPath,JsonSerializer.Serialize(new {version=1,enabled=true,media=true,files=true,battery=true,width=230,expandedWidth=480,style="attached",top=0,showLogo=false,hover=true,glass=false,followTaskbar=false,tintColor="#18000000",neutralGlassVersion=1,refraction=32,blur=3,bezelWidth=16,rimHighlight=35,fps=30,hideFullscreen=false,clipboardNotifications=true,codexNotifications=true}));
 }
 internal static string ParentData=>Path.GetDirectoryName(ConfigPath)!;
 internal static string DataDirectory=>IsIsolated?Path.Combine(ParentData,"NotchFull"):Path.Combine(ProfileRoot,"NotchFull");
 internal static string EditorPath=>Environment.ProcessPath!;
 internal static string[] RestartArguments()=>new[]{"--settings",ConfigPath,"--background","--restart"};
 internal static bool MusicEnabled=true,ClipboardNotice=true,CodexNotice=true,ShowLogo,HistoryEnabled;
 internal static int ExpandedWidth=360,TopOffset=14;
 internal static string Tint="#18000000";
 internal static bool SettingsEnabled {get {try{using var d=ReadConfig();return Bool(d.RootElement,"enabled",false);}catch{return true;}}}
 internal static JsonDocument ReadConfig(){if(new FileInfo(ConfigPath).Length>65536)throw new InvalidDataException("Cấu hình quá lớn");return JsonDocument.Parse(File.ReadAllText(ConfigPath));}
 static bool Bool(JsonElement e,string key,bool fallback)=>e.TryGetProperty(key,out var p)?p.GetBoolean():fallback;
 static int Number(JsonElement e,string key,int fallback)=>e.TryGetProperty(key,out var p)?p.GetInt32():fallback;
 internal static void MapCustomSettings(NotchSettings s){
  HistoryEnabled=s.ZuzongClipboardHistory;
  if(!File.Exists(ConfigPath))return;
  using var doc=ReadConfig();var c=doc.RootElement;MusicEnabled=s.ZuzongMusic=Bool(c,"media",true);ShowLogo=Bool(c,"showLogo",false);ClipboardNotice=Bool(c,"clipboardNotifications",true);CodexNotice=Bool(c,"codexNotifications",true);
  ExpandedWidth=Math.Clamp(Number(c,"expandedWidth",360),320,760);TopOffset=Math.Clamp(Number(c,"top",14),0,140);
  s.Width=Math.Clamp(Number(c,"width",260),180,420);s.DynamicIslandWidth=s.Width;
  s.EnableDynamicIslandMode=c.TryGetProperty("style",out var style)&&style.GetString()=="floating";
  s.EnableHoverExpand=Bool(c,"hover",false);s.ShowBatteryIndicator=Bool(c,"battery",true);
  bool hide=Bool(c,"hideFullscreen",false);if(hide!=(s.HideOnExclusiveFullscreen||s.HideOnWindowedFullscreen))s.HideOnExclusiveFullscreen=s.HideOnWindowedFullscreen=hide;
  bool glass=Bool(c,"glass",false);s.NotchStyle=glass?"liquidglass":"default";
  int blur=Number(c,"blur",3),refraction=Number(c,"refraction",32),bezel=Number(c,"bezelWidth",16),rim=Number(c,"rimHighlight",35);
  Tint=c.TryGetProperty("tintColor",out var color)?color.GetString()??"#18000000":"#18000000";
  if(Bool(c,"followTaskbar",false))try{using var master=JsonDocument.Parse(File.ReadAllText(Path.Combine(ParentData,"settings.json")));var m=master.RootElement;Tint=m.GetProperty("tintColor").GetString()!;blur=Number(m,"blur",blur);refraction=Number(m,"refraction",refraction);bezel=Number(m,"bezelWidth",bezel);rim=Number(m,"rimHighlight",rim);}catch{}
  _=System.Windows.Media.ColorConverter.ConvertFromString(Tint);
  s.LiquidGlass.BlurAmount=Math.Clamp(blur/28.0,0,1);s.LiquidGlass.Refraction=Math.Clamp(Math.Abs(refraction)/32.0,0,4);s.LiquidGlass.ZRadius=Math.Clamp(bezel/100.0,.01,.8);s.LiquidGlass.EdgeHighlight=rim/100.0;
  
  
  s.LiquidGlass.TargetFps=Math.Clamp(Number(c,"fps",30),30,60);
  s.AutoStart=StartupManager.IsAutoStartEnabled();
  var tabs=s.VisibleNavTabs.Split(',',StringSplitOptions.RemoveEmptyEntries).ToHashSet();if(Bool(c,"files",true))tabs.Add("Secondary");else tabs.Remove("Secondary");s.VisibleNavTabs=string.Join(",",tabs);
  
  s.LastRunVersion=AppVersion.Current;s.HasSeenSpotlightIntro=true;s.HasSeenCpp26Notice=true;
  ZuzongLite.Apply(s);
 }
 internal static void WriteBasicFromFull(NotchSettings s){
  if(!File.Exists(ConfigPath))return;
  var values=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(ConfigPath))!.AsObject();values["media"]=s.ZuzongMusic;values["width"]=s.EnableDynamicIslandMode?s.DynamicIslandWidth:s.Width;values["glass"]=s.NotchStyle=="liquidglass";values["files"]=s.VisibleNavTabs.Split(',').Contains("Secondary");values["hideFullscreen"]=s.HideOnExclusiveFullscreen||s.HideOnWindowedFullscreen;values["refraction"]=(int)Math.Clamp(Math.Round(s.LiquidGlass.Refraction*32),-120,120);values["blur"]=(int)Math.Round(s.LiquidGlass.BlurAmount*28);values["bezelWidth"]=(int)Math.Clamp(Math.Round(s.LiquidGlass.ZRadius*100),1,80);values["rimHighlight"]=(int)Math.Clamp(Math.Round(s.LiquidGlass.EdgeHighlight*100),0,100);values["hover"]=s.EnableHoverExpand;values["battery"]=s.ShowBatteryIndicator;values["style"]=s.EnableDynamicIslandMode?"floating":"attached";
  string temp=ConfigPath+".full-new";File.WriteAllText(temp,values.ToJsonString(new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,ConfigPath,true);MusicEnabled=s.ZuzongMusic;
 }
}
