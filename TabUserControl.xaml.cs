using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shapes;
using Aveva.Pdms.Database;
using Aveva.Pdms.Geometry;
using Aveva.Pdms.Maths.Geometry;
using Aveva.PDMS.Database.Filters;
using Aveva.Pdms.Utilities.CommandLine;
using TypeFilter = Aveva.PDMS.Database.Filters.TypeFilter;
using UserControl = System.Windows.Controls.UserControl;

namespace Polymetal.Pdms.Design.DrawListManager
{
    /// <summary>
    /// Логика взаимодействия для TestUserControl.xaml
    /// </summary>
    public partial class TabUserControl : UserControl
    {

        public static  List<List<D2Point>> CoordList = new List<List<D2Point>>();
        public static  List<D2FiniteLine> Horizontal = new List<D2FiniteLine>();
        public static  List<D2FiniteLine> Vertical = new List<D2FiniteLine>();

        internal static double Scale;

        private double HeightOfLb;
        private double WidthOfLb;

        private Rectangle _limitrect = new Rectangle();

        private Position2D P2 = Position2D.Create();

        internal static Dictionary<string, Rectangle> SmallRectsInfo = new Dictionary<string, Rectangle>();

        private List<ExistLimits> _lstLimits=new List<ExistLimits>();

        public TabUserControl()
        {
            InitializeComponent();
        }

        public TabUserControl(List<List<D2Point>> coordList, List<D2FiniteLine> horizontal, List<D2FiniteLine> vertical, double scale,
            double heightOfLb, double widthOfLb, Position2D p2, Dictionary<string, Rectangle> smallRectsInfo, List<ExistLimits> lstLimits)
        {
            try
            {
                InitializeComponent();

                CoordList = coordList;
                Horizontal = horizontal;
                Vertical = vertical;
                Scale = scale;
                HeightOfLb = heightOfLb;
                WidthOfLb = widthOfLb;
                P2 = p2;
                SmallRectsInfo = smallRectsInfo;
                _lstLimits = lstLimits;

                DrawingLimits();


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }


        private void PaintExistLimits()
        {
            try
            {
                Command.CreateCommand("AID CLEAR BOX ALL" ).Run();

                List<DbElement> distList = _lstLimits.Select(existLimitse => existLimitse.RegionMark).Distinct().ToList();

                foreach (DbElement regionLimit in distList)
                {
                    foreach (ExistLimits source in _lstLimits.Where(x => x.RegionMark == regionLimit).ToList())
                    {
                        var box = source.BoxElementRegion;


                        var boxPos = box.GetAsString(DbAttributeInstance.POS);
                        var boxOri = box.GetAsString(DbAttributeInstance.ORI);
                        
                        var boxX = box.GetAsString(DbAttributeInstance.XLEN);
                        var boxY = box.GetAsString(DbAttributeInstance.YLEN);
                        var boxZ = box.GetAsString(DbAttributeInstance.ZLEN);

                        Position posbox = box.GetPosition(DbAttributeInstance.POS);

                        var polygon = new System.Windows.Shapes.Polygon();
                        
                        

                        PaintAidBox(boxPos, boxOri, boxX, boxY, boxZ);
                        
                        var rectancle = new System.Windows.Shapes.Rectangle
                        {
                            Height = Convert.ToDouble(boxX.Replace("mm", ""))*Scale,
                            Width = Convert.ToDouble(boxY.Replace("mm", ""))*Scale,
                            

                            Fill = Brushes.DarkRed,
                            Stroke = System.Windows.Media.Brushes.LightSteelBlue,
                            RadiusX = 8,
                            RadiusY = 8,
                            Name =
                                "Reg" +
                                source.RegionMark.GetAsString(DbAttributeInstance.REF).Replace("=", "").Replace("/", ""),
                            Tag = source.RegionMark.GetAsString(DbAttributeInstance.DESC)
                        };

                        var tmpKoef = Scale;
                        rectancle.Margin =
                                new Thickness(Math.Abs((posbox.X - P2.X + 10 / tmpKoef) * tmpKoef) - 20,
                                    Math.Abs((posbox.Y - P2.Y + 10 / tmpKoef) * tmpKoef), 0, 0);

                        canvasUC.Children.Add(rectancle);

                        rectancle.MouseEnter += rectancle_MouseEnter;
                        rectancle.MouseLeave += rectancle_MouseLeave;

                        rectancle.MouseLeftButtonDown += rectancle_MouseLeftButtonDown;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        void rectancle_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Rectangle rect = (Rectangle)sender;

            if (rect.Fill == Brushes.DarkOrange)
            {
                //Brush rBrush = GetRandomBrush();

                foreach (UIElement child in canvasUC.Children)
                {
                    if (child is Rectangle)
                    {
                        Rectangle tmprect = child as Rectangle;
                        if (tmprect.Name == rect.Name)
                        {
                            tmprect.Fill = Brushes.Transparent;
                        }
                    }
                }

                rect.Fill = Brushes.Transparent; 
                //rect.Stroke = System.Windows.Media.Brushes.Sienna;
                //rect.StrokeThickness = 3;
                if (!NameRegionBlock.Text.Contains(rect.Tag.ToString()))
                {
                    NameRegionBlock.Text = NameRegionBlock.Text + rect.Tag.ToString()+" ";
                }
            }
            else
            {
                foreach (UIElement child in canvasUC.Children)
                {
                    if (child is Rectangle)
                    {
                        Rectangle tmprect = child as Rectangle;
                        if (tmprect.Name == rect.Name)
                        {
                            tmprect.Fill = Brushes.DarkOrange;
                        }
                    }
                }

                rect.Fill = Brushes.DarkOrange;
                NameRegionBlock.Text = NameRegionBlock.Text.Replace(rect.Tag.ToString(),"").TrimStart();
            }
        }



        void rectancle_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Rectangle rect = (Rectangle) sender;
            if (rect.Fill == Brushes.DarkRed)
            {
                foreach (UIElement child in canvasUC.Children)
                {
                    if (child is Rectangle)
                    {
                        Rectangle tmprect = child as Rectangle;
                        if (tmprect.Name == rect.Name)
                        {
                            tmprect.Fill = Brushes.DarkOrange;
                        }
                    }
                }

                rect.Fill = Brushes.DarkOrange;
                if (!NameRegionBlock.Text.Contains(rect.Tag.ToString()))
                {
                    NameRegionBlock.Text = NameRegionBlock.Text + rect.Tag.ToString() + " ";
                }
            }
        }
        

        void rectancle_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Rectangle rect = (Rectangle)sender;
            if (rect.Fill == Brushes.DarkOrange)
            {
                foreach (UIElement child in canvasUC.Children)
                {
                    if (child is Rectangle)
                    {
                        Rectangle tmprect = child as Rectangle;
                        if (tmprect.Name == rect.Name)
                        {
                            tmprect.Fill = Brushes.DarkRed;
                        }
                    }
                }

                rect.Fill = Brushes.DarkRed;
                NameRegionBlock.Text = NameRegionBlock.Text.Replace(rect.Tag.ToString(), "").TrimStart();
            }
        }
        

        private void PaintAidBox(string pos, string ori, string x, string y, string z)
        {
            Command.CreateCommand("AID BOX Position "  + pos +
                                        " Orientation " + ori +
                                         " XLENGTH " + x +
                                         " YLENGTH " + y +
                                         " ZLENGTH " + z).Run();
        }


        private Brush GetRandomBrush()
        {
            Brush result = Brushes.Transparent;

            Random rnd = new Random();

            Type brushesType = typeof(Brushes);

            PropertyInfo[] properties = brushesType.GetProperties();

            int random = rnd.Next(properties.Length);
            result = (Brush)properties[random].GetValue(null, null);

            return result;
        }

        private void DrawingLimits()
        {
            if (canvasUC.Children != null)
            {
                canvasUC.Children.Clear();
            }
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
                
              
                var widthOfHost = ActualWidth - 30;
                var heightOfHost = ActualHeight - 30;

                Scale = widthOfHost / WidthOfLb;

                var widthOfRect = WidthOfLb * Scale;
                var heightOfRect = HeightOfLb * Scale;

                if (heightOfRect > (heightOfHost))
                {
                    Scale = heightOfHost / HeightOfLb;
                    widthOfRect = WidthOfLb * Scale;
                    heightOfRect = HeightOfLb * Scale;
                }

                _limitrect.Width = widthOfRect;
                _limitrect.Height = heightOfRect;
                _limitrect.Stroke = Brushes.Transparent;
                _limitrect.Margin = new Thickness(10.0, 10.0, 10.0 + widthOfRect, 10.0 + heightOfRect);

                canvasUC.Children.Add(_limitrect);

                foreach (DbElement item in collection)
                {
                    var poss = item.GetPosition(DbAttributeInstance.POSS);
                    var pose = item.GetPosition(DbAttributeInstance.POSE);

                    var beg = new Point((float)(Math.Abs(poss.X - P2.X + 10 / Scale) * Scale), (float)(Math.Abs(poss.Y - P2.Y - 10 / Scale) * Scale));
                    var end = new Point((float)(Math.Abs(pose.X - P2.X + 10 / Scale) * Scale), (float)(Math.Abs(pose.Y - P2.Y - 10 / Scale) * Scale));

                    var ln = new System.Windows.Shapes.Line
                    {
                        X1 = beg.X,
                        Y1 = beg.Y,
                        X2 = end.X,
                        Y2 = end.Y,
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection() { 2 },
                        Stroke = Brushes.Blue
                    };

                    canvasUC.Children.Add(ln);

                    if (beg.X.Equals(end.X) && !beg.Y.Equals(end.Y))
                        Vertical.Add(D2FiniteLine.Create(D2Point.Create(beg.X, beg.Y), D2Point.Create(end.X, end.Y)));

                    if (!beg.X.Equals(end.X) && beg.Y.Equals(end.Y))
                        Horizontal.Add(D2FiniteLine.Create(D2Point.Create(beg.X, beg.Y), D2Point.Create(end.X, end.Y)));

                    double centEllipseX;
                    double centEllipseY;
                    var rad = (float)(1160 * Scale);

                    if (!(beg.X.Equals(end.X)))
                    {
                        var begCont = beg.X > end.X
                            ? new Point((float)(beg.X + 3000 * Scale), beg.Y)
                            : new Point((float)(beg.X - 3000 * Scale), beg.Y);

                        var ln1 = new System.Windows.Shapes.Line
                        {
                            X1 = begCont.X,
                            Y1 = begCont.Y,
                            X2 = beg.X,
                            Y2 = beg.Y,
                            Stroke = Brushes.Blue
                        };

                        canvasUC.Children.Add(ln1);

                        if (beg.X > end.X)
                        {
                            centEllipseX = begCont.X;
                            centEllipseY = (float)(beg.Y - (1160 / 2.0 * Scale));
                        }
                        else
                        {
                            centEllipseX = (float)(begCont.X - 1160 * Scale);
                            centEllipseY = (float)(beg.Y - (1160 / 2.0 * Scale));
                        }
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

                        canvasUC.Children.Add(el);
                        canvasUC.Children.Add(tb);
                    }
                    else
                    {
                        var begCont = beg.X > end.X
                            ? new Point(beg.X, (float)(beg.Y - 3000 * Scale))
                            : new Point(beg.X, (float)(beg.Y + 3000 * Scale));

                        var ln1 = new System.Windows.Shapes.Line
                        {
                            X1 = beg.X,
                            Y1 = beg.Y,
                            X2 = begCont.X,
                            Y2 = begCont.Y,
                            Stroke = Brushes.Blue
                        };

                        canvasUC.Children.Add(ln1);

                        if (beg.X > end.X)
                        {
                            centEllipseX = (float)(beg.X - (1160 / 2.0 * Scale));
                            centEllipseY = (float)(begCont.Y - 1160 * Scale);
                        }
                        else
                        {
                            centEllipseX = (float)(beg.X - (1160 / 2.0 * Scale));
                            centEllipseY = begCont.Y;
                        }

                        rad = (float)(1160 * Scale);

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

                        canvasUC.Children.Add(el);
                        canvasUC.Children.Add(tb);
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
                        break;


                    for (var l = i + 1; l < Horizontal.Count; l++)
                    {
                        D2Point p1, p2;
                        if (!IsLinesIntersects(Vertical[j], Horizontal[l], out p1) ||
                            !IsLinesIntersects(Vertical[secondIndexofVert], Horizontal[l], out p2)) continue;
                        var unuse3 = p1;
                        var unuse4 = p2;

                        CoordList.Add(new List<D2Point>() { unuse1, unuse2, unuse3, unuse4 });
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

                canvasUC.Children.Add(invRects);
                canvasUC.Children.Add(invRectsnew);

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

            var ua = ((endx2 - begx2) * (begy1 - begy2) - (endy2 - begy2) * (begx1 - begx2)) /
                     ((endy2 - begy2) * (endx1 - begx1) - (endx2 - begx2) * (endy1 - begy1));
            var x = begx1 + ua * (endx1 - begx1);
            var y = begy1 + ua * (endy1 - begy1);

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

            if ((div = (int)(lengthy2 * lengthx1 - lengthx2 * lengthy1)) == 0) //Линии параллельны
            {
                pt = null;
                return false;
            }

            if (div > 0)
            {
                //Проверка на пересечение отрезков за их границами
                if ((mul = (int)(lengthx1 * lengthyy - lengthy1 * lengthxx)) < 0 || mul > div)
                {
                    pt = null;
                    return false;
                }
                if ((mul = (int)(lengthx2 * lengthyy - lengthy2 * lengthxx)) < 0 || mul > div)
                {
                    pt = null;
                    return false;
                }
            }

            if ((mul = -(int)(lengthx1 * lengthyy - lengthy1 * lengthxx)) < 0 || mul > -div)
            {
                pt = null;
                return false;
            }

            if ((mul = -(int)(lengthx2 * lengthyy - lengthy2 * lengthxx)) < 0 || mul > -div)
            {
                pt = null;
                return false;
            }

            pt.X = x;
            pt.Y = y;
            return true;
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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawingLimits();
            PaintExistLimits();
        }


    }
}
