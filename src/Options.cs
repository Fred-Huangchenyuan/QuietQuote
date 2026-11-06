using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace QuietQuote {
public sealed class Options : Form {
 public Settings Result;
 readonly Settings draft;
 readonly List<Action> translations=new List<Action>();
 readonly Feed feed;
 CancellationTokenSource request;
 List<Market> markets=new List<Market>();
 string lang;
 bool binding, loaded, disposed;
 int current, generation, catalogState;
 ComboBox coin, unit, language, shape, theme, exchange, kind, window;
 NumericUpDown count;
 Label status;
 Button save;
 TabPage marketPage, lookPage;
 WindowSpec Spec { get { return draft.Windows[current]; } }
 string T(string zh,string en) { return lang=="en"?en:zh; }
 void Bind(Control c,string zh,string en) { translations.Add(delegate { c.Text=T(zh,en); }); c.Text=T(zh,en); }
 public Options(Settings s) : this(s,0) { }
 public Options(Settings s,int selectedWindow) : this(s,selectedWindow,new Feed(),true) { }
 internal Options(Settings s,int selectedWindow,Feed source,bool autoLoad) {
  feed=source;
  draft=s.Copy(); draft.EnsureWindows(); lang=draft.Language; current=Math.Min(selectedWindow,draft.Windows.Count-1);
  Text="QuietQuote"; ClientSize=new Size(540,635); StartPosition=FormStartPosition.CenterScreen;
  FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false;
  Font=new Font("Microsoft YaHei UI",9); BackColor=Color.FromArgb(248,249,251);
  Bind(this,"QuietQuote · 设置","QuietQuote · Settings");
  language=Choice(this,"界面语言","Language",16,"language"); language.Items.AddRange(new object[]{"中文","English"}); language.SelectedIndex=lang=="en"?1:0;
  var tabs=new TabControl {Left=12,Top=56,Width=516,Height=524}; Controls.Add(tabs);
  marketPage=new TabPage(); lookPage=new TabPage(); tabs.TabPages.AddRange(new[]{marketPage,lookPage});
  Bind(marketPage,"行情窗口","Market windows"); Bind(lookPage,"外观与刷新（所有窗口）","Appearance / refresh (all)");
  count=Number(marketPage,"并行窗口数量","Number of windows",18,1,8,draft.Windows.Count,"count");
  window=Choice(marketPage,"正在配置的窗口","Configure window",58,"window");
  exchange=Choice(marketPage,"行情平台","Exchange",112,"exchange"); exchange.Items.AddRange(new object[]{"OKX","Binance"});
  kind=Choice(marketPage,"市场类型","Market type",152,"kind");
  coin=Choice(marketPage,"币种（键入首字母查找）","Coin (type to find)",192,"coin");
  unit=Choice(marketPage,"计价单位","Quote currency",232,"unit");
  status=new Label {Left=20,Top=276,Width=463,Height=58,ForeColor=Color.DimGray,Name="status"};marketPage.Controls.Add(status);
  var retry=new Button {Left=304,Top=338,Width=179,Height=30,Name="retry"};Bind(retry,"更新币对列表","Refresh instruments");marketPage.Controls.Add(retry);
  var help=new Label {Left=20,Top=386,Width=463,Height=95,ForeColor=Color.DimGray};
  Bind(help,"各窗口独立选择平台、市场和币对，位置分别保存。\n合约仅含永续；Binance 使用 USDⓈ-M。\n公共行情无需 API Key。Ctrl+Alt+Q 隐藏 / 恢复全部窗口。","Each window has its own exchange, market, pair and position.\nContracts are perpetuals; Binance uses USDⓈ-M.\nPublic data: no API key. Ctrl+Alt+Q toggles all windows.");marketPage.Controls.Add(help);
  var interval=Number(lookPage,"刷新间隔（秒）","Refresh interval (seconds)",16,1,3600,s.Interval,"interval");
  var alpha=Number(lookPage,"背景浓度（%）","Background opacity (%)",54,30,100,s.Alpha,"alpha");
  var font=Number(lookPage,"数字大小","Digit size",92,10,28,s.FontSize,"font");
  shape=Choice(lookPage,"外形","Shape",130,"shape");
  var width=Number(lookPage,"胶囊宽度（像素）","Capsule width (px)",168,100,360,s.CapsuleWidth,"width");
  var height=Number(lookPage,"胶囊高度（像素）","Capsule height (px)",206,36,100,s.CapsuleHeight,"height");
  var diameter=Number(lookPage,"圆形直径（像素）","Circle diameter (px)",244,64,180,s.Diameter,"diameter");
  theme=Choice(lookPage,"非毛玻璃配色","Non-glass theme",282,"theme");
  var symbol=new CheckBox {Left=20,Top=326,Width=280,Checked=s.Symbol};Bind(symbol,"显示币名和单位","Show coin / currency");lookPage.Controls.Add(symbol);
  var top=new CheckBox {Left=323,Top=326,Width=160,Checked=s.Top};Bind(top,"窗口置顶","Always on top");lookPage.Controls.Add(top);
  var glass=new CheckBox {Left=20,Top=361,Width=463,Checked=s.Glass};Bind(glass,"毛玻璃（深色，依赖系统支持）","Glass blur (dark, system dependent)");lookPage.Controls.Add(glass);
  var appearanceHelp=new Label {Left=20,Top=409,Width=463,Height=65,ForeColor=Color.DimGray};Bind(appearanceHelp,"外观和刷新间隔应用于全部窗口。\n自动配色参考浮窗周围背景亮度，每秒更新。\n悬停浮窗可查看平台、市场类型和行情状态。","Appearance and refresh interval apply to all windows.\nAuto theme follows nearby background brightness.\nHover an overlay to see its exchange, market and data status.");lookPage.Controls.Add(appearanceHelp);
  save=new Button {Left=342,Top=594,Width=82,Height=30,Name="save"};Bind(save,"保存","Save");
  var cancel=new Button {Left=436,Top=594,Width=82,Height=30,DialogResult=DialogResult.Cancel};Bind(cancel,"取消","Cancel");
  Controls.Add(save);Controls.Add(cancel);AcceptButton=save;CancelButton=cancel;
  Action translateLists=delegate {
   bool was=binding;binding=true;int si=shape.SelectedIndex,ti=theme.SelectedIndex,ki=kind.SelectedIndex;
   shape.Items.Clear();shape.Items.AddRange(new object[]{T("扁胶囊","Capsule"),T("圆形","Circle")});shape.SelectedIndex=si<0?(s.Shape=="circle"?1:0):si;
   theme.Items.Clear();theme.Items.AddRange(new object[]{T("自动适应背景","Auto background"),T("浅色","Light"),T("深色","Dark")});theme.SelectedIndex=ti<0?(s.Theme=="light"?1:s.Theme=="dark"?2:0):ti;
   kind.Items.Clear();kind.Items.AddRange(new object[]{T("永续合约","Perpetual"),T("现货","Spot")});kind.SelectedIndex=ki<0?(Spec.Kind=="SPOT"?1:0):ki;
   FillWindows();binding=was;
  };
  translateLists();
  Action enable=delegate {width.Enabled=height.Enabled=shape.SelectedIndex==0;diameter.Enabled=shape.SelectedIndex==1;theme.Enabled=!glass.Checked;};
  shape.SelectedIndexChanged+=delegate {enable();};glass.CheckedChanged+=delegate {enable();};enable();
  language.SelectedIndexChanged+=delegate {lang=language.SelectedIndex==1?"en":"zh";foreach(var a in translations)a();translateLists();ShowStatus();};
  count.ValueChanged+=async delegate {
   if(binding)return;
   while(draft.Windows.Count<(int)count.Value)draft.Windows.Add(new WindowSpec {Exchange="Binance",Kind="SPOT",InstrumentId="BTC-USDT"});
   while(draft.Windows.Count>(int)count.Value)draft.Windows.RemoveAt(draft.Windows.Count-1);
   current=Math.Min(current,draft.Windows.Count-1);FillWindows();LoadWindow();if(loaded)await RefreshCatalog();
  };
  window.SelectedIndexChanged+=async delegate {if(binding||window.SelectedIndex<0)return;current=window.SelectedIndex;LoadWindow();if(loaded)await RefreshCatalog();};
  exchange.SelectedIndexChanged+=async delegate {if(!binding)await ChangeSource();};
  kind.SelectedIndexChanged+=async delegate {if(!binding)await ChangeSource();};
  coin.SelectedIndexChanged+=delegate {if(!binding){BindUnits("USDT");StoreSelection();}};
  unit.SelectedIndexChanged+=delegate {if(!binding)StoreSelection();};
  retry.Click+=async delegate {await RefreshCatalog();};
  LoadWindow();
  save.Click+=delegate {
   StoreSelection();draft.Language=lang;draft.Shape=shape.SelectedIndex==1?"circle":"capsule";draft.Theme=theme.SelectedIndex==1?"light":theme.SelectedIndex==2?"dark":"auto";
   draft.CapsuleWidth=(int)width.Value;draft.CapsuleHeight=(int)height.Value;draft.Diameter=(int)diameter.Value;draft.Interval=(int)interval.Value;draft.Alpha=(int)alpha.Value;draft.FontSize=(int)font.Value;
   draft.Symbol=symbol.Checked;draft.Top=top.Checked;draft.Glass=glass.Checked;
   var first=draft.Windows[0];draft.Exchange=first.Exchange;draft.Kind=first.Kind;draft.InstrumentId=first.InstrumentId;
   draft.EnsureWindows();Result=draft;DialogResult=DialogResult.OK;
  };
  Shown+=async delegate {loaded=autoLoad;if(autoLoad)await RefreshCatalog();};
 }
 void FillWindows(){bool was=binding;binding=true;window.Items.Clear();for(int i=0;i<draft.Windows.Count;i++)window.Items.Add(T("窗口 ","Window ")+(i+1));window.SelectedIndex=current;binding=was;}
 async Task ChangeSource(){Spec.Exchange=(string)exchange.SelectedItem;Spec.Kind=kind.SelectedIndex==1?"SPOT":"SWAP";Spec.InstrumentId=Spec.Kind=="SPOT"?"BTC-USDT":"BTC-USDT-SWAP";LoadWindow();if(loaded)await RefreshCatalog();}
 string CachePath {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"markets-"+Spec.Exchange+"-"+Spec.Kind+".json");}}
 void LoadWindow(){
  generation++;if(request!=null)request.Cancel();binding=true;exchange.SelectedItem=Spec.Exchange;kind.SelectedIndex=Spec.Kind=="SPOT"?1:0;binding=false;
  markets=new List<Market>{Market.FromId(Spec.InstrumentId,Spec.Kind)};catalogState=0;
  try{markets=Market.Parse(File.ReadAllText(CachePath),Spec.Exchange,Spec.Kind);catalogState=1;}catch{
   if(Spec.Exchange=="OKX"&&Spec.Kind=="SWAP")try{markets=Market.Parse(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"markets.json")));catalogState=1;}catch{}
  }
  // Preserve the saved instrument even if it was delisted; fetching it will show an error, never a different coin's price.
  if(!markets.Any(x=>x.Id==Spec.InstrumentId))markets.Insert(0,Market.FromId(Spec.InstrumentId,Spec.Kind));
  BindMarkets(Spec.InstrumentId);ShowStatus();
 }
 async Task RefreshCatalog(){
  if(disposed)return;int token=++generation;int index=current;string source=Spec.Exchange,type=Spec.Kind,path=CachePath;
  if(request!=null)request.Cancel();var own=new CancellationTokenSource();request=own;catalogState=4;ShowStatus();
  try{
   string json=await feed.Catalog(source,type,own.Token);
   if(disposed||token!=generation)return;
   var list=Market.Parse(json,source,type);string selected=draft.Windows[index].InstrumentId;
   // A saved unavailable pair remains selected so removal is not silently mistaken for another quote.
   if(!list.Any(x=>x.Id==selected))list.Insert(0,Market.FromId(selected,type));
   markets=list;BindMarkets(selected);catalogState=2;
   try{File.WriteAllText(path,json);}catch{}
  }catch{if(disposed||token!=generation)return;catalogState=3;}
  finally{if(request==own)request=null;own.Dispose();}
  ShowStatus();
 }
 void ShowStatus(){status.Text=Spec.Exchange+" · "+(Spec.Kind=="SPOT"?T("现货","Spot"):T("永续合约","Perpetual"))+"\n"+(catalogState==4?T("正在读取官方币对列表…","Loading official instruments…"):catalogState==2?T("官方列表已更新；已保存的下架币对会保留并显示断线。","Catalog updated; saved delisted pairs remain and show Offline."):catalogState==3?T("列表更新失败。保留缓存或已选币对，可点击重试。","Update failed. Cached/saved selection kept; click to retry."):catalogState==1?T("已载入缓存，等待更新。","Cached instruments loaded; waiting to refresh."):T("当前为已保存币对，等待加载官方列表。","Saved selection shown; waiting for official catalog."));}
 void StoreSelection(){var m=Selected();if(m!=null)Spec.InstrumentId=m.Id;save.Enabled=m!=null;}
 Market Selected(){return markets.FirstOrDefault(x=>x.Coin==(string)coin.SelectedItem&&x.Unit==(string)unit.SelectedItem);}
 void BindMarkets(string id){var m=Market.FromId(id,Spec.Kind);binding=true;coin.Items.Clear();coin.Items.AddRange(markets.Select(x=>x.Coin).Distinct().OrderBy(x=>x).Cast<object>().ToArray());coin.SelectedItem=m.Coin;binding=false;BindUnits(m.Unit);}
 void BindUnits(string preferred){binding=true;unit.Items.Clear();unit.Items.AddRange(markets.Where(x=>x.Coin==(string)coin.SelectedItem).Select(x=>x.Unit).Distinct().OrderBy(x=>x=="USDT"?0:1).ThenBy(x=>x).Cast<object>().ToArray());unit.SelectedItem=preferred;if(unit.SelectedIndex<0&&unit.Items.Count>0)unit.SelectedIndex=0;binding=false;save.Enabled=Selected()!=null;}
 ComboBox Choice(Control parent,string zh,string en,int y,string name){Label(parent,zh,en,y);var c=new ComboBox{Left=304,Top=y,Width=179,Name=name,DropDownStyle=ComboBoxStyle.DropDownList,AutoCompleteSource=AutoCompleteSource.ListItems,AutoCompleteMode=AutoCompleteMode.SuggestAppend,MaxDropDownItems=16};parent.Controls.Add(c);return c;}
 void Label(Control parent,string zh,string en,int y){var l=new Label{Left=20,Top=y+4,Width=280,Height=28};Bind(l,zh,en);parent.Controls.Add(l);}
 NumericUpDown Number(Control parent,string zh,string en,int y,int min,int max,int val,string name){Label(parent,zh,en,y);var n=new NumericUpDown{Left=304,Top=y,Width=179,Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,val)),Name=name};parent.Controls.Add(n);return n;}
 protected override void OnFormClosing(FormClosingEventArgs e){generation++;if(request!=null)request.Cancel();base.OnFormClosing(e);}
 protected override void Dispose(bool disposing){if(disposing){disposed=true;generation++;if(request!=null)request.Cancel();feed.Dispose();}base.Dispose(disposing);}
}
}
