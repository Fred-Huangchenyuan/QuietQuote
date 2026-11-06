using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace QuietQuote {
internal static class Appearance {
 public static GraphicsPath Outline(RectangleF r,bool circle) {
  var p=new GraphicsPath(); if(circle)p.AddEllipse(r);else{float d=r.Height;p.AddArc(r.Left,r.Top,d,d,90,180);p.AddArc(r.Right-d,r.Top,d,d,270,180);p.CloseFigure();}return p;
 }
 public static bool ChooseLight(double luma,bool previous){return previous?luma>=.43:luma>.57;}
 public static bool NearbyLight(Rectangle r,bool previous) {
  IntPtr dc=GetDC(IntPtr.Zero);if(dc==IntPtr.Zero)return previous;
  try {
   var values=new List<double>();
   for(int i=1;i<=4;i++) {
    foreach(var p in new[]{new Point(r.Left+r.Width*i/5,r.Top-6),new Point(r.Left+r.Width*i/5,r.Bottom+6),new Point(r.Left-6,r.Top+r.Height*i/5),new Point(r.Right+6,r.Top+r.Height*i/5)}){
     if(!Screen.AllScreens.Any(s=>s.Bounds.Contains(p)))continue;
     uint c=GetPixel(dc,p.X,p.Y);if(c==0xffffffff)continue;
     values.Add((.2126*(c&255)+.7152*((c>>8)&255)+.0722*((c>>16)&255))/255.0);
    }
   }
   if(values.Count==0)return previous;values.Sort();return ChooseLight(values[values.Count/2],previous);
  }finally{ReleaseDC(IntPtr.Zero,dc);}
 }
 [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr window);
 [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr window,IntPtr dc);
 [DllImport("gdi32.dll")]static extern uint GetPixel(IntPtr dc,int x,int y);
}
internal static class SegmentDigits {
 internal static readonly int[] Masks={63,6,91,79,102,109,125,7,127,111};
 static readonly PointF[] A={new PointF(2,1),new PointF(9,2),new PointF(9,10),new PointF(2,18),new PointF(1,10),new PointF(1,2),new PointF(2,9.5f)};
 static readonly PointF[] B={new PointF(8,1),new PointF(9,9),new PointF(9,17),new PointF(8,18),new PointF(1,17),new PointF(1,9),new PointF(8,9.5f)};
 public static void Draw(Graphics g,string text,RectangleF bounds,float desired,Color color) {
  float units=0;foreach(char c in text)units+=c=='.'?4:12;
  if(units==0)return;float scale=Math.Min(desired/19f,Math.Min(bounds.Width/units,bounds.Height/20f));
  var state=g.Save();g.TranslateTransform(bounds.X+(bounds.Width-units*scale)/2,bounds.Y+(bounds.Height-20*scale)/2);g.ScaleTransform(scale,scale);
  foreach(char c in text) {
   if(c=='.'){using(var b=new SolidBrush(color))g.FillEllipse(b,0,16.3f,2.1f,2.1f);g.TranslateTransform(4,0);continue;}
   int mask=c>='0'&&c<='9'?Masks[c-'0']:64;
   for(int i=0;i<7;i++)if((mask&(1<<i))!=0) {
    using(var p=Segment(A[i],B[i])) {
     using(var glow=new Pen(Color.FromArgb(30,color),1.6f))g.DrawPath(glow,p);
     using(var b=new SolidBrush(color))g.FillPath(b,p);
    }
   }
   g.TranslateTransform(12,0);
  }
  g.Restore(state);
 }
 static GraphicsPath Segment(PointF a,PointF b) {
  float dx=b.X-a.X,dy=b.Y-a.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);dx/=length;dy/=length;float t=.65f;
  var p=new GraphicsPath();p.AddPolygon(new[]{a,new PointF(a.X+dx*t-dy*t,a.Y+dy*t+dx*t),new PointF(b.X-dx*t-dy*t,b.Y-dy*t+dx*t),b,new PointF(b.X-dx*t+dy*t,b.Y-dy*t-dx*t),new PointF(a.X+dx*t+dy*t,a.Y+dy*t-dx*t)});return p;
 }
}
}
