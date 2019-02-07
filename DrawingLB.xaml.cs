using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Aveva.Pdms.Database;
using Aveva.Pdms.Geometry;
using Aveva.Pdms.Maths.Geometry;
using Aveva.Pdms.Utilities.CommandLine;
using Aveva.PDMS.Database.Filters;
using Brushes = System.Windows.Media.Brushes;
using Control = System.Windows.Forms.Control;
using FontFamily = System.Windows.Media.FontFamily;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace Polymetal.Pdms.Design.DrawListManager
{
    internal static class MyExtensions
    {
        internal static bool IntersectsWith(this Rectangle rect1, Rectangle rect2)
        {
            var lb1 = LimitsBox2D.Create(Position2D.Create(rect1.Margin.Left, rect1.Margin.Top),
                Position2D.Create(rect1.Margin.Right, rect1.Margin.Bottom));
            var lb2 = LimitsBox2D.Create(Position2D.Create(rect2.Margin.Left, rect2.Margin.Top),
                Position2D.Create(rect2.Margin.Right, rect2.Margin.Bottom));
            return lb1.Intersects(lb2);
        }
    }

    /// <summary>
    /// Логика взаимодействия для DrawingLB.xaml
    /// </summary>
    public partial class DrawingLb
    {
        internal static LimitsBox LimitBoxDr;
        internal static double Scale;
        private static readonly List<List<D2Point>> CoordList = new List<List<D2Point>>();
        private static readonly List<D2FiniteLine> Horizontal = new List<D2FiniteLine>();
        private static readonly List<D2FiniteLine> Vertical = new List<D2FiniteLine>();
        private bool _clickSelect;
        private Rectangle _selRect;
        private readonly Rectangle _limitrect = new Rectangle();
        internal static int ZLength;
        internal static double ZCentr;
        private Point _orig;
        private readonly FormNew _drawFormNew;

        internal static readonly Dictionary<string, Rectangle> LimitsInfo = new Dictionary<string, Rectangle>();
        internal static readonly Dictionary<string, Rectangle> SmallRectsInfo = new Dictionary<string, Rectangle>();

        public DrawingLb(FormNew frmn)
        {
            InitializeComponent();
            _drawFormNew = frmn;
            LimitBoxDr = _drawFormNew.LimitBoxR;
        }

        #region Events

        private void DrawingLb_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawingLimits();
        }

        private void DrawingLb_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            if (_orig.X.Equals(0.0) && _orig.Y.Equals(0.0)) return;

            GetSelRectangle(_orig, e.GetPosition(canvas), _selRect);

            foreach (var rec in canvas.Children)
            {
                if (!(rec.GetType().Name.Equals("Rectangle"))) continue;
                var rect = (Rectangle) rec;
                if (!rect.Name.StartsWith("NewRect")) continue;

                var brush = _clickSelect ? Brushes.Transparent : Brushes.Red;
                if (_selRect.IntersectsWith(rect))
                {
                    rect.Fill = brush;
                }
                else
                {
                    rect.Fill = (SmallRectsInfo.Any(x => x.Value.Equals(rect))) ? Brushes.Red : Brushes.Transparent;
                }
                Mouse.Capture(_limitrect);
            }
        }

        private void DrawingLb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (canvas.Children.Contains(_selRect)) canvas.Children.Remove(_selRect);

            if (_orig.X.Equals(0.0) && _orig.Y.Equals(0.0)) return;
            
            if (_orig != e.GetPosition(canvas))
            {
                foreach (var rect in canvas.Children)
                {
                    if (!(rect.GetType().Name.Equals("Rectangle"))) continue;
                    var wrect = (Rectangle) rect;
                    if (!wrect.Name.StartsWith("NewRect")) continue;

                    var key = wrect.Name.Replace("NewRect", "");

                    if (!_selRect.IntersectsWith((wrect))) continue;


                    if (_clickSelect)
                    {
                        LimitsInfo.Remove(key);
                        SmallRectsInfo.Remove(key);
                        wrect.Fill = Brushes.Transparent;
                        Command.CreateCommand("AID CLEAR BOX " + key).Run();

                    }
                    else
                    {
                        wrect.Fill = Brushes.Red;

                        foreach (var elem in canvas.Children)
                        {
                            if (!(elem.GetType().Name.Equals("Rectangle"))) continue;
                            var rectan = (Rectangle) elem;
                            if (!rectan.Name.StartsWith("Rect")) continue;
                            var rectname = rectan.Name.Replace("Rect", "");
                            if (!rectname.Equals(key)) continue;


                            DictionaryFilling(LimitsInfo, key, rectan);
                            DictionaryFilling(SmallRectsInfo, key, wrect);

                            LimitsBoxes(rectan, ZCentr, ZLength, key, _drawFormNew.Selit);
                        }
                    }
                }
            }
            else
            {
                var mouseClickPos = new PointF
                {
                    X = (float) e.GetPosition(canvas).X,
                    Y = (float) e.GetPosition(canvas).Y
                };

                var impIndex = 0;

                var xMin = 0.0;
                var xMax = 0.0;

                var yMin = 0.0;
                var yMax = 0.0;

                for (var i = 0; i < CoordList.Count; i++)
                {
                    xMin = CoordList[i][0].X;
                    xMax = CoordList[i][1].X;

                    yMin = CoordList[i][0].Y;
                    yMax = CoordList[i][2].Y;

                    if (!(xMin <= mouseClickPos.X) || !(mouseClickPos.X <= xMax) || !(yMin <= mouseClickPos.Y) ||
                        !(mouseClickPos.Y <= yMax)) continue;
                    impIndex = i + 1;
                    break;
                }

                if (yMin.Equals(0.0) || yMax.Equals(0.0) || xMin.Equals(0.0) || xMax.Equals(0.0)) return;

                var index = "NewRect" + "0" + impIndex + "0";
                var indexwn = "0" + impIndex + "0";

                foreach (var khm in canvas.Children)
                {
                    if (!(khm.GetType().Name.Equals("Rectangle"))) continue;
                    var nkhm = (Rectangle) khm;
                    if (nkhm.Name != index) continue;

                    if (!nkhm.Fill.Equals(Brushes.Transparent))
                    {
                        nkhm.Fill = Brushes.Transparent;
                        LimitsInfo.Remove(indexwn);
                        SmallRectsInfo.Remove(indexwn);
                        Command.CreateCommand("AID CLEAR BOX " + indexwn).Run();

                    }
                    else
                    {
                        nkhm.Fill = Brushes.Red;


                        foreach (var elem in canvas.Children)
                        {
                            if (!(elem.GetType().Name.Equals("Rectangle"))) continue;
                            var rectan = (Rectangle) elem;
                            if (!rectan.Name.StartsWith("Rect")) continue;
                            var rectname = rectan.Name.Replace("Rect", "");
                            if (!rectname.Equals(indexwn)) continue;

                            DictionaryFilling(LimitsInfo, indexwn, rectan);
                            DictionaryFilling(SmallRectsInfo, indexwn, nkhm);

                            LimitsBoxes(rectan, ZCentr, ZLength, indexwn, _drawFormNew.Selit);
                        }
                    }
                }
            }
            _orig.X = 0;
            _orig.Y = 0;
            Mouse.Capture(null);
        }

        private void DrawingLb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _selRect = new Rectangle();

            bool clickonrect = false;
            _orig.X = e.GetPosition(canvas).X;
            _orig.Y = e.GetPosition(canvas).Y;

            for (var i = 0; i < CoordList.Count; i++)
            {
                var xMin = CoordList[i][0].X;
                var xMax = CoordList[i][1].X;

                var yMin = CoordList[i][0].Y;
                var yMax = CoordList[i][2].Y;

                if (!(xMin <= _orig.X) || !(_orig.X <= xMax) || !(yMin <= _orig.Y) ||
                    !(_orig.Y <= yMax)) continue;
                clickonrect = true;
                break;
            }

            if (!clickonrect) return;

            canvas.Children.Add(_selRect);

            foreach (var pr in canvas.Children)
            {
                if (!(pr.GetType().Name.Equals("Rectangle"))) continue;
                var wpr = (Rectangle) pr;
                if (!wpr.Name.StartsWith("Rect")) continue;
                var wpri = wpr.Name.Replace("Rect", "");

                foreach (var ppr in canvas.Children)
                {
                    if (!(ppr.GetType().Name.Equals("Rectangle"))) continue;
                    var wppr = (Rectangle) ppr;
                    if (!wppr.Name.StartsWith("NewRect")) continue;
                    var wppri = wppr.Name.Replace("NewRect", "");

                    if (!wppri.Equals(wpri)) continue;

                    if (wpr.Margin.Left <= _orig.X && _orig.X <= wpr.Margin.Right &&
                        wpr.Margin.Top <= _orig.Y &&
                        _orig.Y <= wpr.Margin.Bottom)
                    {
                        _clickSelect = wppr.Fill.Equals(Brushes.Red);
                        break;
                    }
                }
            }
        }

        private void Canvas_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (_limitrect.Margin.Left <= e.GetPosition(canvas).X && e.GetPosition(canvas).X <= _limitrect.Margin.Right &&
                _limitrect.Margin.Top <= e.GetPosition(canvas).Y && e.GetPosition(canvas).Y <= _limitrect.Margin.Bottom)
            {
                canvas.ToolTip = "Выберите LimitBox для построения на DrawList";
            }
        }

        private void DrawingLb_Loaded(object sender, RoutedEventArgs e)
        {
            DrawingLimits();
        }

        #endregion

        #region Functions

        internal void RemAll()
        {
            foreach (var elem in canvas.Children)
            {
                if (!(elem.GetType().Name.Equals("Rectangle"))) continue;
                var rect = (Rectangle) elem;
                if (!rect.Name.StartsWith("NewRect")) continue;

                rect.Fill = Brushes.Transparent;

                LimitsInfo.Clear();
                SmallRectsInfo.Clear();
            }
        }

        private static void DictionaryFilling(Dictionary<string, Rectangle> dict, string index, Rectangle rect)
        {
            if (!dict.ContainsKey(index)) dict.Add(index, rect);
            else
            {
                dict.Remove(index);
                dict.Add(index, rect);
            }
        }

        internal static void LimitsBoxes(Rectangle rect, double cent, int length, string index, int selectedindex)
        {
            var widthOfRect =
                (int)
                    Math.Sqrt(Math.Pow((rect.Margin.Left - rect.Margin.Right), 2.0) +
                              Math.Pow((rect.Margin.Top - rect.Margin.Top), 2.0));
            var heightOfRect =
                (int)
                    Math.Sqrt(Math.Pow((rect.Margin.Left - rect.Margin.Left), 2.0) +
                              Math.Pow((rect.Margin.Top - rect.Margin.Bottom), 2.0));
            var centerOfLb = Position.Create();

            var koefA = (LimitBoxDr.Minimum.X - 10/Scale)*Scale;
            var koefB = (LimitBoxDr.Maximum.Y - 10/Scale)*Scale;

            centerOfLb.X = ((rect.Margin.Left + rect.Margin.Right)/2.0 + koefA)/Scale;
            centerOfLb.Y = (((rect.Margin.Top + rect.Margin.Bottom)/2.0 - koefB - 20)/
                            Scale)*
                           Math.Cos(Math.PI);
            centerOfLb.Z = cent;

            switch (selectedindex)
            {
                case 0:
                    Command.CreateCommand("AID CLEAR BOX " + index).Run();
                    Command.CreateCommand("AID BOX NUMBER " + index + " AT " + centerOfLb +
                                          " XLENGTH " + (widthOfRect/Scale).ToString().Replace(',', '.') +
                                          " YLENGTH " + (heightOfRect/Scale).ToString().Replace(',', '.') +
                                          " ZLENGTH " + length.ToString()).Run();
                    break;

                case 1:
                    Command.CreateCommand("AID CLEAR ALL BOX").Run();
                    break;

                case 2:
                    Command.CreateCommand("AID CLEAR BOX " + index).Run();
                    Command.CreateCommand("AID BOX NUMBER " + index + " AT " + centerOfLb +
                                          " XLENGTH " + (widthOfRect/Scale).ToString().Replace(',', '.') +
                                          " YLENGTH " + (heightOfRect/Scale).ToString().Replace(',', '.') +
                                          " ZLENGTH " + length.ToString() +
                                          " FILL ON").Run();
                    break;

                default:
                    Command.CreateCommand("AID CLEAR BOX " + index).Run();
                    Command.CreateCommand("AID BOX NUMBER " + index + " AT " + centerOfLb +
                                          " XLENGTH " + (widthOfRect/Scale).ToString().Replace(',', '.') +
                                          " YLENGTH " + (heightOfRect/Scale).ToString().Replace(',', '.') +
                                          " ZLENGTH " + length.ToString() +
                                          " FILL OFF").Run();
                    break;
            }
        }

        private static bool IsLinesIntersects(D2FiniteLine vertic, D2FiniteLine horizont, out D2Point pt)
        {
            pt = D2Point.Create();

            double begx1, begy1, endx1, endy1;
            double begx2, begy2, endx2, endy2;
            var origin = D2Point.Create(0.0, 0.0);

            var vertdistfre = vertic.End().Ddistance(origin);
            var vertdistfrs = vertic.Start().Ddistance(origin);

            var hordistfre = horizont.End().Ddistance(origin);
            var hordistfrs = horizont.Start().Ddistance(origin);

            if (vertdistfrs > vertdistfre)
            {
                begx1 = vertic.End().X;
                begy1 = vertic.End().Y;
                endx1 = vertic.Start().X;
                endy1 = vertic.Start().Y;
            }
            else
            {
                begx1 = vertic.Start().X;
                begy1 = vertic.Start().Y;
                endx1 = vertic.End().X;
                endy1 = vertic.End().Y;
            }

            if (hordistfrs > hordistfre)
            {
                begx2 = horizont.End().X;
                begy2 = horizont.End().Y;
                endx2 = horizont.Start().X;
                endy2 = horizont.Start().Y;
            }
            else
            {
                begx2 = horizont.Start().X;
                begy2 = horizont.Start().Y;
                endx2 = horizont.End().X;
                endy2 = horizont.End().Y;
            }

            var ua = ((endx2 - begx2)*(begy1 - begy2) - (endy2 - begy2)*(begx1 - begx2))/
                     ((endy2 - begy2)*(endx1 - begx1) - (endx2 - begx2)*(endy1 - begy1));
            var x = begx1 + ua*(endx1 - begx1);
            var y = begy1 + ua*(endy1 - begy1);

            if (begx1.Equals(begx2) && begy1.Equals(begy2) || endx1.Equals(endx2) && endy1.Equals(endy2))
            {
                pt.X = x;
                pt.Y = y;
                return true;
            }

            var lengthx1 = endx1 - begx1;
            var lengthx2 = endx2 - begx2; //Длина проекций на ось х
            var lengthy1 = endy1 - begy1;
            var lengthy2 = endy2 - begy2; //Длина проекций на ось у

            var lengthxx = begx1 - begx2;
            var lengthyy = begy1 - begy2;

            int div, mul;

            if ((div = (int) (lengthy2*lengthx1 - lengthx2*lengthy1)) == 0) //Линии параллельны
            {
                pt = null;
                return false;
            }

            if (div > 0)
            {
                //Проверка на пересечение отрезков за их границами
                if ((mul = (int) (lengthx1*lengthyy - lengthy1*lengthxx)) < 0 || mul > div)
                {
                    pt = null;
                    return false;
                }
                if ((mul = (int) (lengthx2*lengthyy - lengthy2*lengthxx)) < 0 || mul > div)
                {
                    pt = null;
                    return false;
                }
            }

            if ((mul = -(int) (lengthx1*lengthyy - lengthy1*lengthxx)) < 0 || mul > -div)
            {
                pt = null;
                return false;
            }

            if ((mul = -(int) (lengthx2*lengthyy - lengthy2*lengthxx)) < 0 || mul > -div)
            {
                pt = null;
                return false;
            }

            pt.X = x;
            pt.Y = y;
            return true;
        }

        private static void GetSelRectangle(Point orig, Point location,
            Rectangle rect)
        {
            var deltaX = location.X - orig.X;
            var deltaY = location.Y - orig.Y;
            if (deltaX.Equals(0.0) || deltaY.Equals(0.0)) return;

            rect.Width = Math.Abs(deltaX);
            rect.Height = Math.Abs(deltaY);
            rect.Margin =
                new Thickness(Math.Min(orig.X, location.X), Math.Min(orig.Y, location.Y),
                    Math.Max(orig.X, location.X), Math.Max(orig.Y, location.Y));
            rect.Fill = Brushes.Blue;
            rect.Opacity = 0.3;
        }

        private void DrawingLimits()
        {
            canvas.Children.Clear();
            Vertical.Clear();
            Horizontal.Clear();
            CoordList.Clear();

            var world = DbElement.GetElement("/*");
            foreach (var site in world.Members(DbElementTypeInstance.SITE))
            {
                var curMDB = MDB.CurrentMDB.Name.Replace("-", "");
                var splName = site.GetAsString(DbAttributeInstance.FLNN).Split('-')[0];
                if (!(site.GetAsString(DbAttributeInstance.PURP).ToUpper().Equals("AXES") && curMDB.Equals(splName)))
                    continue;

                var collection = new DBElementCollection(site, new TypeFilter(DbElementTypeInstance.SCTN));

                ElementHost host = null;
                var wpfHandle = PresentationSource.FromVisual(this) as HwndSource;

                if (wpfHandle != null)
                {
                    host = Control.FromChildHandle(wpfHandle.Handle) as ElementHost;
                }

                if (host == null) return;

                var widthOfHost = host.Width - 20;
                var heightOfHost = host.Height - 20;

                Scale = widthOfHost/_drawFormNew.WidthOfLb;

                var widthOfRect = _drawFormNew.WidthOfLb*Scale;
                var heightOfRect = _drawFormNew.HeightOfLb*Scale;

                if (heightOfRect > (heightOfHost))
                {
                    Scale = heightOfHost/_drawFormNew.HeightOfLb;
                    widthOfRect = _drawFormNew.WidthOfLb*Scale;
                    heightOfRect = _drawFormNew.HeightOfLb*Scale;
                }

                _limitrect.Width = widthOfRect;
                _limitrect.Height = heightOfRect;
                _limitrect.Stroke = Brushes.Transparent;
                _limitrect.Margin = new Thickness(10.0, 10.0, 10.0 + widthOfRect, 10.0 + heightOfRect);

                canvas.Children.Add(_limitrect);

                foreach (DbElement item in collection)
                {
                    var poss = item.GetPosition(DbAttributeInstance.POSS);
                    var pose = item.GetPosition(DbAttributeInstance.POSE);

                    var beg = new Point((float) (Math.Abs(poss.X - _drawFormNew.P2.X + 10/Scale)*Scale),
                        (float) (Math.Abs(poss.Y - _drawFormNew.P2.Y - 10/Scale)*Scale));
                    var end = new Point((float) (Math.Abs(pose.X - _drawFormNew.P2.X + 10/Scale)*Scale),
                        (float) (Math.Abs(pose.Y - _drawFormNew.P2.Y - 10/Scale)*Scale));

                    var ln = new System.Windows.Shapes.Line
                    {
                        X1 = beg.X,
                        Y1 = beg.Y,
                        X2 = end.X,
                        Y2 = end.Y,
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection() {2},
                        Stroke = Brushes.Blue
                    };

                    canvas.Children.Add(ln);

                    if (beg.X.Equals(end.X) && !beg.Y.Equals(end.Y))
                        Vertical.Add(D2FiniteLine.Create(D2Point.Create(beg.X, beg.Y), D2Point.Create(end.X, end.Y)));

                    if (!beg.X.Equals(end.X) && beg.Y.Equals(end.Y))
                        Horizontal.Add(D2FiniteLine.Create(D2Point.Create(beg.X, beg.Y), D2Point.Create(end.X, end.Y)));

                    if (!(beg.X.Equals(end.X)))
                    {
                        var begCont = beg.X > end.X
                            ? new Point((float) (beg.X + 3000*Scale), beg.Y)
                            : new Point((float) (beg.X - 3000*Scale), beg.Y);

                        var ln1 = new System.Windows.Shapes.Line
                        {
                            X1 = begCont.X,
                            Y1 = begCont.Y,
                            X2 = beg.X,
                            Y2 = beg.Y,
                            Stroke = Brushes.Blue
                        };

                        canvas.Children.Add(ln1);

                        double centEllipseX;
                        double centEllipseY;

                        if (beg.X > end.X)
                        {
                            centEllipseX = begCont.X;
                            centEllipseY = (float) (beg.Y - (1160/2.0*Scale));
                        }
                        else
                        {
                            centEllipseX = (float) (begCont.X - 1160*Scale);
                            centEllipseY = (float) (beg.Y - (1160/2.0*Scale));
                        }
                        var rad = (float) (1160*Scale);
                        var el = new Ellipse
                        {
                            Height = rad,
                            Width = rad,
                            Stroke = Brushes.Blue,
                            Margin =
                                new Thickness(centEllipseX, centEllipseY, centEllipseX + rad, centEllipseY + rad),
                        };

                        var tb = new TextBlock
                        {
                            Width = rad,
                            Height = rad,
                            Text = item.GetAsString(DbAttributeInstance.DESC),
                            TextAlignment = TextAlignment.Center,
                            FontFamily = new FontFamily("Times"),
                            Margin = new Thickness(centEllipseX, centEllipseY, 0, 0),
                            FontSize = rad,
                            Foreground = Brushes.Blue
                        };

                        canvas.Children.Add(el);
                        canvas.Children.Add(tb);
                    }
                    else
                    {
                        var begCont = beg.X > end.X
                            ? new Point(beg.X, (float) (beg.Y - 3000*Scale))
                            : new Point(beg.X, (float) (beg.Y + 3000*Scale));

                        var ln1 = new System.Windows.Shapes.Line
                        {
                            X1 = beg.X,
                            Y1 = beg.Y,
                            X2 = begCont.X,
                            Y2 = begCont.Y,
                            Stroke = Brushes.Blue
                        };

                        canvas.Children.Add(ln1);

                        double centEllipseX;
                        double centEllipseY;

                        if (beg.X > end.X)
                        {
                            centEllipseX = (float) (beg.X - (1160/2.0*Scale));
                            centEllipseY = (float) (begCont.Y - 1160*Scale);
                        }
                        else
                        {
                            centEllipseX = (float) (beg.X - (1160/2.0*Scale));
                            centEllipseY = begCont.Y;
                        }

                        var rad = (float) (1160*Scale);

                        var el = new Ellipse
                        {
                            Height = rad,
                            Width = rad,
                            Stroke = Brushes.Blue,
                            Margin =
                                new Thickness(centEllipseX, centEllipseY, centEllipseX + rad, centEllipseY + rad)
                        };

                        var tb = new TextBlock
                        {
                            Width = rad,
                            Height = rad,
                            Text = item.GetAsString(DbAttributeInstance.DESC),
                            TextAlignment = TextAlignment.Center,
                            FontFamily = new FontFamily("Times"),
                            Margin = new Thickness(centEllipseX, centEllipseY, 0, 0),
                            FontSize = rad,
                            Foreground = Brushes.Blue
                        };

                        canvas.Children.Add(el);
                        canvas.Children.Add(tb);
                    }
                }
            }
            
            Horizontal.Sort(ComparerHor);
            Vertical.Sort(ComparerVert);

            for (var i = 0; i < Horizontal.Count - 1; i++)
            {
                var secondIndexofVert = -1;

                for (var j = 0; j < Vertical.Count - 1; j++)
                {
                    D2Point unuse1, unuse2 = D2Point.Create();
                    if (!IsLinesIntersects(Vertical[j], Horizontal[i], out unuse1)) continue;
                    for (var k = j + 1; k < Vertical.Count; k++)
                    {
                        if (
                            !IsLinesIntersects(Vertical[k], Horizontal[i], out unuse2))
                            continue;

                        secondIndexofVert = k;
                        break;
                    }

                    if (secondIndexofVert < 0)
                    {
                        break;
                    }

                    for (var l = i + 1; l < Horizontal.Count; l++)
                    {
                        D2Point p1, p2;
                        if (!IsLinesIntersects(Vertical[j], Horizontal[l], out p1) ||
                            !IsLinesIntersects(Vertical[secondIndexofVert], Horizontal[l], out p2)) continue;
                        var unuse3 = p1;
                        var unuse4 = p2;

                        CoordList.Add(new List<D2Point>() {unuse1, unuse2, unuse3, unuse4});
                        break;
                    }
                    j = secondIndexofVert - 1;
                    secondIndexofVert = -1;
                }
            }

            for (var i = 0; i < CoordList.Count; i++)
            {
                var index = i + 1;
                var newWidthOfRect =
                    Math.Sqrt(Math.Pow(CoordList[i][0].X - CoordList[i][1].X, 2.0) +
                              Math.Pow(CoordList[i][0].Y - CoordList[i][1].Y, 2.0));

                var newHeightOfRect =
                    Math.Sqrt(Math.Pow(CoordList[i][0].X - CoordList[i][2].X, 2.0) +
                              Math.Pow(CoordList[i][0].Y - CoordList[i][2].Y, 2.0));

                var invRects = new Rectangle
                {
                    Height = newHeightOfRect,
                    Width = newWidthOfRect,
                    Margin =
                        new Thickness(CoordList[i][0].X, CoordList[i][0].Y, CoordList[i][0].X + newWidthOfRect,
                            CoordList[i][0].Y + newHeightOfRect),
                    Fill = Brushes.Transparent,
                    Name = "Rect" + "0" + index + "0"
                };


                var invRectsnew = new Rectangle
                {
                    Height = newHeightOfRect - 4,
                    Width = newWidthOfRect - 4,
                    Margin =
                        new Thickness(CoordList[i][0].X + 2, CoordList[i][0].Y + 2,
                            CoordList[i][0].X + newWidthOfRect - 2, CoordList[i][0].Y + newHeightOfRect - 2),
                    Fill = Brushes.Transparent,
                    Name = "NewRect" + "0" + index + "0",
                    RadiusX = 10,
                    RadiusY = 10

                };

                canvas.Children.Add(invRects);
                canvas.Children.Add(invRectsnew);

            }

            foreach (var pair in SmallRectsInfo)
            {
                if (!pair.Value.Fill.Equals(Brushes.Red)) continue;

                foreach (var elem in canvas.Children)
                {
                    if (!(elem.GetType().Name.Equals("Rectangle"))) continue;
                    var rect = (Rectangle) elem;
                    if (!rect.Name.StartsWith("NewRect")) continue;
                    var rectname = rect.Name.Replace("NewRect", "");
                    if (!rectname.Equals(pair.Key)) continue;

                    rect.Fill = Brushes.Red;
                }
            }

            foreach (var elem in canvas.Children)
            {
                if (!(elem.GetType().Name.Equals("Rectangle"))) continue;
                var rect = (Rectangle) elem;
                if (!rect.Name.StartsWith("NewRect")) continue;
                if (!rect.Fill.Equals(Brushes.Red)) continue;
                var rectname = rect.Name.Replace("NewRect", "");

                if (SmallRectsInfo.ContainsKey(rectname))
                {
                    SmallRectsInfo.Remove(rectname);
                    SmallRectsInfo.Add(rectname, rect);
                }
                else SmallRectsInfo.Add(rectname, rect);
            }
        }

        private static int ComparerVert(D2FiniteLine x, D2FiniteLine y)
        {
            var res = 0;
            if (x.Start().X < y.Start().X) res = -1;
            if (x.Start().X.Equals(y.Start().X)) res = 0;
            if (x.Start().X > y.Start().X) res = 1;

            return res;
        }

        private static int ComparerHor(D2FiniteLine x, D2FiniteLine y)
        {
            var res = 0;
            if (x.Start().Y < y.Start().Y) res = -1;
            if (x.Start().Y.Equals(y.Start().Y)) res = 0;
            if (x.Start().Y > y.Start().Y) res = 1;

            return res;
        }

        #endregion

    }
}

