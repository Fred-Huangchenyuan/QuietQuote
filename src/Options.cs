using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
namespace QuietQuote {
public sealed class Options : Form {
 public Settings Result;
 string lang;
 readonly List<Action> translations = new List<Action>();
 List<Market> markets;
 ComboBox coin, unit, language, shape, theme;
 Label status;
 Button save;
 bool binding;
 int catalogState, count;
 readonly CancellationTokenSource stop = new CancellationTokenSource();
 readonly Feed feed = new Feed();
 string T(string zh,string en) { return lang == "en" ? en : zh; }
 void Bind(Control c,string zh,string en) { translations.Add(delegate { c.Text=T(zh,en); }); c.Text=T(zh,en); }
 public Options(Settings s) {
  lang=s.Language; Text="QuietQuote"; ClientSize=new Size(470,685); StartPosition=FormStartPosition.CenterScreen; FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false;
  Font=new Font("Microsoft YaHei UI",9); BackColor=Color.FromArgb(248,249,251);
  Bind(this,"QuietQuote · 设置","QuietQuote · Settings");
  language=Choice("界面语言","Language",20); language.Items.AddRange(new object[]{"中文","English"}); language.SelectedIndex=lang=="en"?1:0;
  coin=Choice("币种（键入首字母查找）","Coin (type to find)",60);
  unit=Choice("计价单位","Quote currency",100);
  status=new Label { Left=22,Top=137,Width=420,Height=34,ForeColor=Color.DimGray }; Controls.Add(status);
  var interval=Number("刷新间隔（秒）","Refresh interval (seconds)",180,1,3600,s.Interval);
  var alpha=Number("背景浓度（%）","Background opacity (%)",218,30,100,s.Alpha);
  var font=Number("数字大小","Digit size",256,10,28,s.FontSize);
  shape=Choice("外形","Shape",294);
  var width=Number("胶囊宽度（像素）","Capsule width (px)",332,100,360,s.CapsuleWidth);
  var height=Number("胶囊高度（像素）","Capsule height (px)",370,36,100,s.CapsuleHeight);
  var diameter=Number("圆形直径（像素）","Circle diameter (px)",408,64,180,s.Diameter);
  theme=Choice("非毛玻璃配色","Non-glass theme",446);
  var symbol=new CheckBox { Left=22,Top=490,Width=235,Checked=s.Symbol }; Bind(symbol,"显示币名和单位","Show coin / currency"); Controls.Add(symbol);
  var top=new CheckBox { Left=290,Top=490,Width=155,Checked=s.Top }; Bind(top,"窗口置顶","Always on top"); Controls.Add(top);
  var glass=new CheckBox { Left=22,Top=521,Width=420,Checked=s.Glass }; Bind(glass,"毛玻璃（深色，依赖系统支持）","Glass blur (dark, system dependent)"); Controls.Add(glass);
  var help=new Label { Left=22,Top=558,Width=430,Height=65,ForeColor=Color.DimGray }; Bind(help,"自动配色参考浮窗周围背景亮度，每秒更新。\n数字采用七段数码管样式。\nCtrl+Alt+Q 隐藏 / 恢复","Auto theme follows nearby background brightness each second.\nSeven-segment digital numerals.\nCtrl+Alt+Q to hide / show"); Controls.Add(help);
  save=new Button { Left=272,Top=637,Width=82,Height=30 }; Bind(save,"保存","Save");
  var cancel=new Button { Left=365,Top=637,Width=82,Height=30,DialogResult=DialogResult.Cancel }; Bind(cancel,"取消","Cancel");
  Controls.Add(save); Controls.Add(cancel); AcceptButton=save; CancelButton=cancel;
  Action translateLists=delegate {
   int si=shape.SelectedIndex, ti=theme.SelectedIndex;
   shape.Items.Clear(); shape.Items.AddRange(new object[]{T("扁胶囊","Capsule"),T("圆形","Circle")}); shape.SelectedIndex=si<0?(s.Shape=="circle"?1:0):si;
   theme.Items.Clear(); theme.Items.AddRange(new object[]{T("自动适应背景","Auto background"),T("浅色","Light"),T("深色","Dark")}); theme.SelectedIndex=ti<0?(s.Theme=="light"?1:s.Theme=="dark"?2:0):ti;
  };
  translateLists();
  Action enable=delegate { width.Enabled=height.Enabled=shape.SelectedIndex==0; diameter.Enabled=shape.SelectedIndex==1; theme.Enabled=!glass.Checked; };
  shape.SelectedIndexChanged+=delegate { enable(); }; glass.CheckedChanged+=delegate { enable(); }; enable();
  language.SelectedIndexChanged+=delegate { lang=language.SelectedIndex==1?"en":"zh"; foreach(var a in translations) a(); translateLists(); ShowStatus(); };
  coin.SelectedIndexChanged+=delegate { if(!binding) BindUnits("USDT"); }; unit.SelectedIndexChanged+=delegate { if(!binding) save.Enabled=Selected()!=null; };
  markets=new List<Market>{Market.FromId(s.InstrumentId)};
  try { markets=Market.Parse(File.ReadAllText(CachePath)); catalogState=1; } catch { }
  BindMarkets(s.InstrumentId); ShowStatus();
  save.Click+=delegate {
   var selected=Selected(); if(selected==null)return;
   Result=new Settings { Language=lang,Shape=shape.SelectedIndex==1?"circle":"capsule",Theme=theme.SelectedIndex==1?"light":theme.SelectedIndex==2?"dark":"auto",CapsuleWidth=(int)width.Value,CapsuleHeight=(int)height.Value,Diameter=(int)diameter.Value,Interval=(int)interval.Value,Alpha=(int)alpha.Value,FontSize=(int)font.Value,Symbol=symbol.Checked,Top=top.Checked,Glass=glass.Checked,InstrumentId=selected.Id,X=s.X,Y=s.Y };
   Result.Normalize(); DialogResult=DialogResult.OK;
  };
  Shown+=async delegate {
   try { string json=await feed.Catalog(stop.Token); if(IsDisposed||stop.IsCancellationRequested)return; var selected=Selected(); markets=Market.Parse(json); BindMarkets(selected==null?s.InstrumentId:selected.Id); count=markets.Select(x=>x.Coin).Distinct().Count(); catalogState=2; try{File.WriteAllText(CachePath,json);}catch{} }
   catch { if(IsDisposed||stop.IsCancellationRequested)return; catalogState=3; }
   ShowStatus();
  };
 }
 static string CachePath { get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"markets.json");} }
 void ShowStatus() { status.Text=catalogState==0?T("正在读取官方合约列表…","Loading OKX instruments…"):catalogState==1?T("已载入缓存，正在更新…","Cached instruments loaded; refreshing…"):catalogState==2?T("已更新：","Updated: ")+count+T(" 个币种；仅显示可用单位。"," coins; available quote currencies only."):T("更新失败，使用已保存列表；重新打开设置可重试。","Update failed; using saved list. Reopen settings to retry."); }
 ComboBox Choice(string zh,string en,int y) { Label(zh,en,y); var c=new ComboBox {Left=274,Top=y,Width=172,DropDownStyle=ComboBoxStyle.DropDownList,AutoCompleteSource=AutoCompleteSource.ListItems,AutoCompleteMode=AutoCompleteMode.SuggestAppend,MaxDropDownItems=16}; Controls.Add(c);return c; }
 void Label(string zh,string en,int y) { var l=new Label{Left=22,Top=y+4,Width=249,Height=26}; Bind(l,zh,en);Controls.Add(l); }
 NumericUpDown Number(string zh,string en,int y,int min,int max,int val) { Label(zh,en,y);var n=new NumericUpDown {Left=274,Top=y,Width=172,Minimum=min,Maximum=max,Value=val};Controls.Add(n);return n; }
 Market Selected() {return markets.FirstOrDefault(x=>x.Coin==(string)coin.SelectedItem&&x.Unit==(string)unit.SelectedItem);}
 void BindMarkets(string id) {var m=Market.FromId(id);binding=true;coin.Items.Clear();coin.Items.AddRange(markets.Select(x=>x.Coin).Distinct().Cast<object>().ToArray());coin.SelectedItem=m==null?null:m.Coin;if(coin.SelectedIndex<0&&coin.Items.Count>0)coin.SelectedIndex=0;binding=false;BindUnits(m==null?"USDT":m.Unit);}
 void BindUnits(string preferred) {binding=true;unit.Items.Clear();unit.Items.AddRange(markets.Where(x=>x.Coin==(string)coin.SelectedItem).Select(x=>x.Unit).Distinct().OrderBy(x=>x=="USDT"?0:1).ThenBy(x=>x).Cast<object>().ToArray());unit.SelectedItem=preferred;if(unit.SelectedIndex<0&&unit.Items.Count>0)unit.SelectedIndex=0;binding=false;save.Enabled=Selected()!=null;}
 protected override void OnFormClosing(FormClosingEventArgs e){stop.Cancel();base.OnFormClosing(e);}
 protected override void Dispose(bool disposing){if(disposing){stop.Cancel();feed.Dispose();}base.Dispose(disposing);}
}
}
