using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RiftReference {
// Shared local asset ownership. Paint and selection never perform network I/O.
public sealed class LoadoutIcons:IDisposable {
 readonly string root;readonly Dictionary<string,Image> images=new Dictionary<string,Image>();
 public LoadoutIcons(DataStore data){root=Path.Combine(data.Root,"loadout-icons");}
 public Image Get(string group,int id){string key=group+"/"+id+".png";Image image;if(images.TryGetValue(key,out image))return image;try{using(var original=Image.FromFile(Path.Combine(root,key)))image=new Bitmap(original);}catch(ArgumentException){image=null;}catch(IOException){image=null;}images[key]=image;return image;}
 public void Dispose(){foreach(var image in images.Values)if(image!=null)image.Dispose();images.Clear();}
}

public sealed class LoadoutTile:Button {
 static readonly Font TileFont=new Font("Segoe UI",8);
 public Image Icon;public bool Chosen;public bool Round;public string Caption="";public string Badge="";
 public LoadoutTile(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Theme.Panel;Cursor=Cursors.Hand;Font=TileFont;}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Chosen?Color.FromArgb(30,54,56):Round?Theme.Background:BackColor);
  bool compact=Height<60;int size=compact?26:Math.Min(48,Height-38);var icon=new Rectangle(compact?6:(Width-size)/2,6,size,size);
  if(Icon!=null){if(Enabled)g.DrawImage(Icon,icon);else using(var muted=new Bitmap(Icon,icon.Size))ControlPaint.DrawImageDisabled(g,muted,icon.X,icon.Y,BackColor);}else TextRenderer.DrawText(g,Badge.Length>0?Badge:"?",Font,icon,Theme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
  using(var border=new Pen(Chosen?Theme.Accent:Theme.Border,Chosen?2:1)){if(Round)g.DrawEllipse(border,icon);else g.DrawRectangle(border,icon);}
  TextRenderer.DrawText(g,Caption,Font,compact?new Rectangle(39,3,Width-42,Height-6):new Rectangle(3,size+11,Width-6,Height-size-12),Chosen?Theme.Ink:Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);
  if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(2,2,Width-5,Height-5),Theme.Accent,BackColor);
 }
}

public sealed class RuneEditor:UserControl {
 readonly DataStore data;readonly LoadoutIcons icons;readonly object[] trees;
 readonly List<LoadoutTile> tiles=new List<LoadoutTile>();readonly List<int> secondaryOrder=new List<int>();
 Label detailTitle,detailBody,summary;Panel choices;public int Primary{get;private set;}public int Secondary{get;private set;}public int[] Perks{get;private set;}
 public event EventHandler Changed;
 static readonly int[][] Shards={new[]{5008,5005,5007},new[]{5008,5010,5001},new[]{5011,5013,5001}};
 static readonly string[][] ShardNames={new[]{"Adaptive force","Attack speed","Ability haste"},new[]{"Adaptive force","Move speed","Scaling health"},new[]{"Health","Tenacity","Scaling health"}};
 public RuneEditor(DataStore d,LoadoutIcons assets){data=d;icons=assets;trees=Loadouts.Trees(d).OrderBy(t=>Array.IndexOf(new[]{8000,8100,8200,8400,8300},(int)J.N(t,"id"))).ToArray();Perks=new int[9];Primary=8000;Secondary=8400;BackColor=Theme.Background;Size=new Size(1218,538);
  choices=new Panel{Location=new Point(0,0),Size=new Size(850,538),BackColor=Theme.Background};Controls.Add(choices);
  detailTitle=Label("YOUR RUNE PAGE",876,32,310,60,15);detailBody=Label("Select one rune in each primary row, two secondary runes from different rows, and three stat shards.\n\nSelect or focus an icon to read its description.",876,105,310,260,10);
  summary=Label("",876,400,310,100,10);Build();
 }
 Label Label(string text,int x,int y,int w,int h,int font,Control parent=null){var label=new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),ForeColor=Theme.Ink,BackColor=Theme.Background,Font=new Font("Segoe UI",font)};(parent??this).Controls.Add(label);return label;}
 object Tree(int id){return trees.FirstOrDefault(t=>J.N(t,"id")==id);}
 public void ChooseTree(bool first,int id){if(Tree(id)==null)return;if(first){if(Primary==id)return;Primary=id;Array.Clear(Perks,0,4);if(Secondary==id){Secondary=0;Perks[4]=Perks[5]=0;secondaryOrder.Clear();}}else{if(id==Primary||Secondary==id)return;Secondary=id;Perks[4]=Perks[5]=0;secondaryOrder.Clear();}Build();Notify();}
 public void ChooseRune(bool first,int row,int id){var tree=Tree(first?Primary:Secondary);if(tree==null)return;var rows=J.A(J.Get(tree,"slots"));if(row<0||row>=rows.Length||!J.A(J.Get(rows[row],"runes")).Any(r=>J.N(r,"id")==id))return;
  if(first)Perks[row]=id;else{if(row==0)return;int previous=secondaryOrder.FirstOrDefault(r=>J.A(J.Get(rows[row],"runes")).Any(v=>J.N(v,"id")==r));if(previous!=0)secondaryOrder.Remove(previous);if(previous!=id){if(secondaryOrder.Count==2)secondaryOrder.RemoveAt(0);secondaryOrder.Add(id);}Perks[4]=secondaryOrder.Count>0?secondaryOrder[0]:0;Perks[5]=secondaryOrder.Count>1?secondaryOrder[1]:0;}UpdateSelection();Notify();
 }
 public void ChooseShard(int row,int id){if(row<0||row>2||!Shards[row].Contains(id))return;Perks[row+6]=id;UpdateSelection();Notify();}
 public void Restore(int primary,int secondary,int[] perks){Primary=Tree(primary)!=null?primary:0;Secondary=Tree(secondary)!=null&&secondary!=Primary?secondary:0;Perks=new int[9];secondaryOrder.Clear();if(perks!=null){for(int i=0;i<Math.Min(4,perks.Length);i++)ChooseRune(true,i,perks[i]);for(int i=4;i<Math.Min(6,perks.Length);i++){var tree=Tree(Secondary);if(tree!=null){var rows=J.A(J.Get(tree,"slots"));for(int r=1;r<rows.Length;r++)ChooseRune(false,r,perks[i]);}}for(int i=6;i<Math.Min(9,perks.Length);i++)ChooseShard(i-6,perks[i]);}Build();}
 public string[] Names(){return Perks.Select((id,index)=>{if(index>=6){int offset=Array.IndexOf(Shards[index-6],id);return offset<0?"Not selected":ShardNames[index-6][offset];}var rune=trees.SelectMany(t=>J.A(J.Get(t,"slots"))).SelectMany(s=>J.A(J.Get(s,"runes"))).FirstOrDefault(r=>J.N(r,"id")==id);return rune==null?"Not selected":J.S(rune,"name");}).ToArray();}
 void Notify(){if(Changed!=null)Changed(this,EventArgs.Empty);}
 LoadoutTile Tile(int id,string name,string description,int x,int y,int w,int h,Action action,bool round,int slot){var tile=new LoadoutTile{Icon=icons.Get("runes",id),Caption=name,AccessibleName=name,Round=round,Location=new Point(x,y),Size=new Size(w,h),Tag=new[]{id,slot}};tile.Click+=(s,e)=>action();EventHandler show=(s,e)=>{detailTitle.Text=name;detailBody.Text=J.Clean(description);};tile.MouseEnter+=show;tile.Enter+=show;choices.Controls.Add(tile);tiles.Add(tile);return tile;}
 void Build(){choices.SuspendLayout();foreach(Control control in choices.Controls.Cast<Control>().ToArray())control.Dispose();tiles.Clear();
  Label("PRIMARY  /  ONE PER ROW",16,12,390,22,10,choices);Label("SECONDARY  /  TWO DIFFERENT ROWS",445,12,390,22,10,choices);
  for(int i=0;i<trees.Length;i++){var tree=trees[i];int id=(int)J.N(tree,"id");string name=J.S(tree,"name");Tile(id,name,"Choose "+name+" as your primary tree.",16+i*80,42,76,80,()=>ChooseTree(true,id),true,-1);var secondary=Tile(id,name,id==Primary?"Your secondary tree must be different from your primary tree.":"Choose "+name+" as your secondary tree.",445+i*80,42,76,80,()=>ChooseTree(false,id),true,-2);secondary.Enabled=id!=Primary;}
  BuildRows(true,16,138,90);BuildRows(false,445,138,73);
  Label("STAT SHARDS",445,360,390,20,9,choices);
  for(int row=0;row<3;row++)for(int option=0;option<3;option++){int r=row,id=Shards[row][option];var tile=Tile(id,ShardNames[row][option],ShardNames[row][option]+" · stat shard",445+option*132,384+row*48,126,44,()=>ChooseShard(r,id),true,6+row);tile.Badge=id==5008?"◆":id==5005?"AS":id==5007?"AH":id==5010?"MS":id==5013?"TEN":"HP";}
  if(Primary==0)Label("Choose a primary tree above.",20,200,380,40,11,choices);if(Secondary==0)Label("Choose a different secondary tree above.",445,200,390,40,11,choices);
  UpdateSelection();choices.ResumeLayout();
 }
 void BuildRows(bool first,int x,int y,int spacing){var tree=Tree(first?Primary:Secondary);if(tree==null)return;var rows=J.A(J.Get(tree,"slots"));for(int row=first?0:1;row<rows.Length;row++){var options=J.A(J.Get(rows[row],"runes"));for(int j=0;j<options.Length;j++){int r=row,id=(int)J.N(options[j],"id");Tile(id,J.S(options[j],"name"),J.S(options[j],"longDesc"),x+j*(400/options.Length),y+(first?row:row-1)*spacing,400/options.Length-6,first?84:70,()=>ChooseRune(first,r,id),true,first?row:4);}}}
 void UpdateSelection(){foreach(var tile in tiles){var tag=(int[])tile.Tag;tile.Chosen=tag[1]==-1?tag[0]==Primary:tag[1]==-2?tag[0]==Secondary:tag[1]==4?Perks.Skip(4).Take(2).Contains(tag[0]):Perks[tag[1]]==tag[0];tile.Invalidate();}summary.Text=Perks.Count(id=>id!=0)+" / 9 RUNES SELECTED\n\nCreates a new page in League. Existing pages are preserved.";}
}

public sealed class ItemBlock {
 public string Name;public readonly List<int> Items=new List<int>();public ItemBlock(string name){Name=name;}
}
public sealed class ItemEditor:UserControl {
 readonly DataStore data;readonly LoadoutIcons icons;readonly ToolTip tips=new ToolTip();readonly List<ItemBlock> blocks=new List<ItemBlock>();
 readonly FlowLayoutPanel catalog,sectionList;readonly TextBox search;readonly RiftComboBox filter;readonly Label feedback;int active;public IList<ItemBlock> Blocks{get{return blocks.AsReadOnly();}}
 public ItemEditor(DataStore d,LoadoutIcons assets){data=d;icons=assets;BackColor=Theme.Background;Size=new Size(1218,538);
  AddLabel("ITEM CATALOG",16,12,440,24);search=new TextBox{Location=new Point(16,45),Size=new Size(287,28),AccessibleName="Search items"};Controls.Add(search);filter=new RiftComboBox{Location=new Point(315,45),Size=new Size(151,28),AccessibleName="Item category"};filter.Items.AddRange(new object[]{"All items","Attack damage","Attack speed","Critical strike","Ability power","Armor","Magic resist","Health","Boots"});filter.SelectedIndex=0;Controls.Add(filter);
  catalog=new FlowLayoutPanel{Location=new Point(16,87),Size=new Size(458,408),AutoScroll=true,BackColor=Theme.Background};Controls.Add(catalog);
  AddLabel("CUSTOM SHOP SECTIONS",498,12,510,24);Button("+ New section",1060,8,140,()=>AddSection("Custom"));
  sectionList=new FlowLayoutPanel{Location=new Point(498,45),Size=new Size(704,450),AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Theme.Background};Controls.Add(sectionList);
  feedback=AddLabel("Select a section, then click an item to add it. Select a placed item for reorder / remove controls.",16,507,1184,24);
  search.TextChanged+=(s,e)=>BuildCatalog();filter.SelectedIndexChanged+=(s,e)=>BuildCatalog();foreach(var name in new[]{"Starting items","First recall","Core path","Situational"})blocks.Add(new ItemBlock(name));BuildCatalog();BuildSections();
 }
 Label AddLabel(string text,int x,int y,int w,int h,Control parent=null){var l=new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),ForeColor=Theme.Ink,Font=new Font("Segoe UI",9)};(parent??this).Controls.Add(l);return l;}
 Button Button(string text,int x,int y,int w,Action action,Control parent=null){var b=new Button{Text=text,Location=new Point(x,y),Size=new Size(w,28),FlatStyle=FlatStyle.Flat,BackColor=Theme.Panel,ForeColor=Theme.Ink};b.FlatAppearance.BorderColor=Theme.Border;b.Click+=(s,e)=>action();(parent??this).Controls.Add(b);return b;}
 public void AddSection(string name){if(blocks.Count>=12){feedback.Text="Use at most 12 sections.";return;}blocks.Add(new ItemBlock(name));active=blocks.Count-1;BuildSections();sectionList.ScrollControlIntoView(sectionList.Controls[active]);}
 public void SelectSection(int index){if(index<0||index>=blocks.Count)return;active=index;BuildSections();}
 public void AddItem(int id){if(blocks.Count==0)AddSection("Custom");object item;if(!data.Items.TryGetValue(id.ToString(),out item)||!Purchasable(item))return;if(blocks[active].Items.Count>=20){feedback.Text="Use at most 20 items in a section.";return;}blocks[active].Items.Add(id);BuildSections();feedback.Text="Added "+J.S(item,"name")+" to "+blocks[active].Name+".";}
 public void MoveSection(int index,int delta){int next=index+delta;if(index<0||index>=blocks.Count||next<0||next>=blocks.Count)return;var block=blocks[index];blocks.RemoveAt(index);blocks.Insert(next,block);active=next;BuildSections();}
 public void MoveItem(int section,int index,int delta){if(section<0||section>=blocks.Count)return;var items=blocks[section].Items;int next=index+delta;if(index<0||index>=items.Count||next<0||next>=items.Count)return;int item=items[index];items.RemoveAt(index);items.Insert(next,item);BuildSections();}
 public void RemoveItem(int section,int index){if(section<0||section>=blocks.Count||index<0||index>=blocks[section].Items.Count)return;blocks[section].Items.RemoveAt(index);BuildSections();}
 public List<KeyValuePair<string,string>> Sections(){return blocks.Select(b=>new KeyValuePair<string,string>(b.Name,String.Join(", ",b.Items))).ToList();}
 public void Restore(IEnumerable<KeyValuePair<string,string>> saved){var restored=new List<ItemBlock>();foreach(var section in saved){if(restored.Count>=12)throw new ArgumentException("Plan has too many sections.");var block=new ItemBlock(section.Key);foreach(var value in (section.Value??"").Split(new[]{','},StringSplitOptions.RemoveEmptyEntries)){var matches=data.Items.Where(kv=>(kv.Key==value.Trim()||J.S(kv.Value,"name").Equals(value.Trim(),StringComparison.OrdinalIgnoreCase))&&Purchasable(kv.Value)).ToArray();if(matches.Length!=1)throw new ArgumentException("Unknown or ambiguous saved item: "+value.Trim());block.Items.Add(Int32.Parse(matches[0].Key));}if(block.Items.Count>20)throw new ArgumentException("Use at most 20 items per section.");restored.Add(block);}blocks.Clear();blocks.AddRange(restored);active=0;BuildSections();}
 static bool Purchasable(object item){return Convert.ToBoolean(J.Get(J.Get(item,"maps"),"11")??false)&&Convert.ToBoolean(J.Get(J.Get(item,"gold"),"purchasable")??false);}
 void Clear(Control parent){foreach(Control child in parent.Controls.Cast<Control>().ToArray())child.Dispose();}
 LoadoutTile ItemTile(int id){var item=data.Items[id.ToString()];string name=J.S(item,"name");var tile=new LoadoutTile{Icon=icons.Get("items",id),Caption=name+"\n"+J.N(J.Get(item,"gold"),"total")+"g",AccessibleName=name,Size=new Size(100,105),Margin=new Padding(3)};tips.SetToolTip(tile,name+"\n"+J.Clean(J.S(item,"description")));return tile;}
 void BuildCatalog(){catalog.SuspendLayout();Clear(catalog);string[] tags={"","Damage","AttackSpeed","CriticalStrike","SpellDamage","Armor","SpellBlock","Health","Boots"};string tag=tags[Math.Max(0,filter.SelectedIndex)];foreach(var kv in data.Items.Where(kv=>Purchasable(kv.Value)&&(J.S(kv.Value,"name").IndexOf(search.Text.Trim(),StringComparison.OrdinalIgnoreCase)>=0||kv.Key==search.Text.Trim())&&(tag==""||J.A(J.Get(kv.Value,"tags")).Any(t=>Convert.ToString(t)==tag))).OrderBy(kv=>J.N(J.Get(kv.Value,"gold"),"total")).ThenBy(kv=>J.S(kv.Value,"name"))){int id=Int32.Parse(kv.Key);var tile=ItemTile(id);tile.Click+=(s,e)=>AddItem(id);catalog.Controls.Add(tile);}if(catalog.Controls.Count==0)catalog.Controls.Add(new Label{Text="No matching items.",ForeColor=Theme.Muted,Size=new Size(400,40)});catalog.ResumeLayout();}
 void BuildSections(){int scroll=-sectionList.AutoScrollPosition.Y;sectionList.SuspendLayout();Clear(sectionList);for(int i=0;i<blocks.Count;i++){int index=i;var block=blocks[i];int rows=Math.Max(1,(block.Items.Count+5)/6);var panel=new Panel{Size=new Size(676,40+rows*111),Margin=new Padding(0,0,0,12),BackColor=Theme.Panel};sectionList.Controls.Add(panel);
   var choose=Button(index==active?"●":"○",8,6,32,()=>SelectSection(index),panel);choose.AccessibleName="Select section "+block.Name;
   var name=new TextBox{Text=block.Name,Location=new Point(48,7),Size=new Size(414,26),MaxLength=60,BackColor=Theme.Panel,ForeColor=Theme.Ink,BorderStyle=BorderStyle.FixedSingle,AccessibleName="Section name"};name.TextChanged+=(s,e)=>block.Name=name.Text;panel.Controls.Add(name);
   Button("↑",478,5,40,()=>MoveSection(index,-1),panel).AccessibleName="Move section up";Button("↓",524,5,40,()=>MoveSection(index,1),panel).AccessibleName="Move section down";Button("Remove",574,5,92,()=>{blocks.RemoveAt(index);active=Math.Max(0,Math.Min(active,blocks.Count-1));BuildSections();},panel);
   if(block.Items.Count==0)AddLabel(index==active?"Selected section · click an item in the catalog to add it.":"Select this section to add items.",18,70,630,40,panel);
   for(int j=0;j<block.Items.Count;j++){int itemIndex=j;var tile=ItemTile(block.Items[j]);tile.Location=new Point(8+(j%6)*110,39+(j/6)*111);tile.Chosen=index==active;panel.Controls.Add(tile);tile.Click+=(s,e)=>{var menu=new ContextMenuStrip();menu.Items.Add("Move earlier",null,(sender,args)=>MoveItem(index,itemIndex,-1)).Enabled=itemIndex>0;menu.Items.Add("Move later",null,(sender,args)=>MoveItem(index,itemIndex,1)).Enabled=itemIndex<block.Items.Count-1;menu.Items.Add("Remove item",null,(sender,args)=>RemoveItem(index,itemIndex));menu.Closed+=(sender,args)=>menu.Dispose();menu.Show(tile,new Point(0,tile.Height));};}
  }sectionList.ResumeLayout();sectionList.AutoScrollPosition=new Point(0,scroll);}
 protected override void Dispose(bool disposing){if(disposing)tips.Dispose();base.Dispose(disposing);}
}
}
