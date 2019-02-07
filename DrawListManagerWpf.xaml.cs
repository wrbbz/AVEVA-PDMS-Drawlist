using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Aveva.Pdms.Database;
using Aveva.Pdms.Geometry;
using Aveva.Pdms.Graphics;
using Aveva.Pdms.Maths.Geometry;
using Aveva.Pdms.Shared;
using Aveva.Pdms.Utilities.CommandLine;
using Aveva.PDMS.Database.Filters;
using Brushes = System.Windows.Media.Brushes;
using cmd = Aveva.Pdms.Utilities.CommandLine.Command;
using FontFamily = System.Windows.Media.FontFamily;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.Forms.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace Polymetal.Pdms.Design.DrawListManager
{
    /// <summary>
    /// Логика взаимодействия для DrawListManagerWpf.xaml
    /// </summary>
    public partial class DrawListManagerWpf : Window
    {
        private const double Minz = -5000.0;
        private const double Maxz = 25000.0;
        
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

        private bool _exec = true;
        private readonly SettingsForm _sf = new SettingsForm();
        private readonly List<LimitsBox> _limits = new List<LimitsBox>();
        private readonly DrawList _dList = Aveva.Pdms.Graphics.DrawListManager.Instance.CurrentDrawList;
        private PickPoint.PointSelectedEventHandler _selectedpoint;
        
        private int Selit;
        private LimitsBox LimitBoxR;
        private LimitsBox _lb;
        private double HeightOfLb;
        private double WidthOfLb;
        private Position2D P1 = Position2D.Create();
        private Position2D P2 = Position2D.Create();
        private Position2D P3 = Position2D.Create();
        private Position2D P4 = Position2D.Create();

        internal static readonly Dictionary<string, Rectangle> LimitsInfo = new Dictionary<string, Rectangle>();
        internal static readonly Dictionary<string, Rectangle> SmallRectsInfo = new Dictionary<string, Rectangle>();

        private List<ExistLimits> existLimitsLst=new List<ExistLimits>();
        private List<TabItem>  lstaddTabItem=new List<TabItem>();

        public DrawListManagerWpf()
        {
            InitializeComponent();

            SetSliderInitializeValue();

            LimitOfAxes();

            Height = 680;
            Width = 680;

        }


        #region  Slider

        #region Events


        void Slider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ScrollingOfValues(LowerSlider, UpperSlider);
        }

        private void LowerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Math.Abs(UpperSlider.Value - LowerSlider.Value) < 100)
                UpperSlider.Value = LowerSlider.Value + 100;

            UpperSlider.Value = Math.Max(UpperSlider.Value, LowerSlider.Value);
            LowerSliderValue.Text = ((int)Math.Floor(LowerSlider.Value)).ToString();
        }

        private void UpperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Math.Abs(UpperSlider.Value - LowerSlider.Value) < 100)
                LowerSlider.Value = UpperSlider.Value - 100;

            LowerSlider.Value = Math.Min(UpperSlider.Value, LowerSlider.Value);
            UpperSliderValue.Text = ((int)Math.Ceiling(UpperSlider.Value)).ToString();
        }

        private void TextChangedev(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var text = textBox.Text;

            var tempText = "";
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch < 58 && ch > 47 || ch.Equals('-') && i == 0)
                {
                    tempText += ch;
                }
            }
            var selStart = textBox.SelectionStart;
            textBox.Text = tempText;
            textBox.Select(selStart > textBox.Text.Length ? textBox.Text.Length : selStart, 0);
        }

        private void UpperSliderValue_OnKeyUp(object sender, KeyEventArgs e)
        {
            int newval;

            if (!int.TryParse(UpperSliderValue.Text, out newval)) return;

            if (newval >= Minz & newval <= Maxz)
            {
                UpperSlider.Value = newval;
            }
            else
            {
                MessageBox.Show(@"Введены недопустимые значения. Значения должны быть от -5000 до 25000", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpperSlider.Value = UpperSlider.Maximum;
            }
        }

        private void LowerSliderValue_OnKeyUp(object sender, KeyEventArgs e)
        {
            int newval;

            if (!int.TryParse(LowerSliderValue.Text, out newval)) return;

            if (newval >= Minz & newval <= Maxz)
            {
                LowerSlider.Value = newval;
            }
            else
            {
                MessageBox.Show(@"Введены недопустимые значения. Значения должны быть от -5000 до 25000", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LowerSlider.Value = LowerSlider.Minimum;
            }
        }

        private void UpperSliderValue_OnMouseEnter(object sender, MouseEventArgs e)
        {
            UpperSliderValue.ToolTip = "Введите верхнюю границу LimitBox";
        }

        private void LowerSliderValue_OnMouseEnter(object sender, MouseEventArgs e)
        {
            LowerSliderValue.ToolTip = "Введите нижнюю границу LimitBox";
        }

        #endregion

        #region Helpers

        private void ScrollingOfValues(RangeBase min, RangeBase max)
        {
            if (max.Value - min.Value < 100) return;

            ZLength = (int)Math.Abs(max.Value - min.Value);
            ZCentr = min.Value + ZLength / 2.0;

            foreach (var pair in LimitsInfo)
            {
                Command.CreateCommand("AID CLEAR BOX " + pair.Key).Run();
                LimitsBoxes(pair.Value, ZCentr, ZLength, pair.Key, Selit);
            }
        }

        private void SetSliderInitializeValue()
        {
            LowerSlider.ValueChanged += LowerSlider_ValueChanged;
            UpperSlider.ValueChanged += UpperSlider_ValueChanged;
            
            LowerSlider.Minimum = Minz;
            LowerSlider.Maximum = Maxz - 100;

            UpperSlider.Minimum = Minz + 100;
            UpperSlider.Maximum = Maxz;

            LowerSlider.Value = 0;
            UpperSlider.Value = 3000;

            ZLength = (int)Math.Abs(UpperSlider.Value - LowerSlider.Value);
            ZCentr = LowerSlider.Value + ZLength / 2.0;

            LowerSliderValue.Text = LowerSlider.Value.ToString(CultureInfo.InvariantCulture);
            UpperSliderValue.Text = UpperSlider.Value.ToString(CultureInfo.InvariantCulture);
        }

        private void GetExistLimit()
        {
            try
            {
                var world = DbElement.GetElement("/*");
                foreach (var site in world.Members(DbElementTypeInstance.SITE))
                {
                    var curMDB = MDB.CurrentMDB.Name.Replace("-", "");
                    var splName = site.GetAsString(DbAttributeInstance.FLNN).Split('-')[0];
                    if (!(site.GetAsString(DbAttributeInstance.PURP).ToUpper().Equals("AXES") && curMDB.Equals(splName)))
                        continue;

                    foreach (DbElement member in site.Members().Where(member => member.GetAsString(DbAttributeInstance.NAME) == "/TMPL_DRWL"))
                    {

                        existLimitsLst = new List<ExistLimits>();
                        existLimitsLst = GetAllExistLimits(member);
                    }

                }

                CreateTabItem(existLimitsLst);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }


        private void CreateTabItem(List<ExistLimits> lstLimits)
        {

            foreach (TabItem tabItem in lstaddTabItem)
            {
                TabControlDrwLst.Items.Remove(tabItem);
            }
            lstaddTabItem.Clear();


            List<DbElement> distList = lstLimits.Select(existLimitse => existLimitse.MarkHeight).Distinct().ToList();
            
            foreach (DbElement element in distList)
            {

                TabItem addtab = new TabItem
                {
                    Header = element.GetAsString(DbAttributeInstance.DESC),
                    Content = new TabNewUserControl(CoordList, Horizontal, Vertical, Scale, HeightOfLb, WidthOfLb,
                    P2, SmallRectsInfo, lstLimits.Where(x => x.MarkHeight == element).ToList())
                };

                TabControlDrwLst.Items.Add(addtab);
                lstaddTabItem.Add(addtab);
            }
            
        }
        

        private List<ExistLimits> GetAllExistLimits(DbElement zoneExist)
        {
            try
            {
                return (from equiElement in zoneExist.Members()
                    where equiElement.GetAsString(DbAttributeInstance.TYPE) == "EQUI"
                    from subEquiElement in equiElement.Members()
                    where subEquiElement.GetAsString(DbAttributeInstance.TYPE) == "SUBE"
                    from boxElement in subEquiElement.Members()
                    where boxElement.GetAsString(DbAttributeInstance.TYPE) == "BOX"
                    select new ExistLimits
                    {
                        BoxElementRegion = boxElement, RegionMark = subEquiElement, MarkHeight = equiElement
                    }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }

        private void LimitOfAxes()
        {
            var world = DbElement.GetElement("/*");
            foreach (var site in world.Members(DbElementTypeInstance.SITE))
            {
                var curMDB = MDB.CurrentMDB.Name.Replace("-", "");
                var splName = site.GetAsString(DbAttributeInstance.FLNN).Split('-')[0];
                if (!(site.GetAsString(DbAttributeInstance.PURP).ToUpper().Equals("AXES") && curMDB.Equals(splName)))
                    continue;

                Spatial.Instance.LimitsBox(site, out LimitBoxR);

                P1.X = LimitBoxR.Minimum.X;
                P1.Y = LimitBoxR.Minimum.Y;

                P2.X = LimitBoxR.Minimum.X;
                P2.Y = LimitBoxR.Maximum.Y;

                P3.X = LimitBoxR.Maximum.X;
                P3.Y = LimitBoxR.Maximum.Y;

                P4.X = LimitBoxR.Maximum.X;
                P4.Y = LimitBoxR.Minimum.Y;

                WidthOfLb = P1.Distance(P4);
                HeightOfLb = P1.Distance(P2);

                if (WidthOfLb > HeightOfLb)
                {
                    var koef = WidthOfLb / HeightOfLb;

                    Height = this.MinHeight;
                    Width = (Height * (int)koef);

                }
                else if (WidthOfLb < HeightOfLb)
                {
                    var koef = HeightOfLb / WidthOfLb;

                    Width = this.MinWidth;
                    Height = (Width * (int)koef);
                }
                else
                {
                    Width = 750;
                    Height = 750;
                }
            }
        }

        private void SelectedItemTab1(int selecteditem)
        {
            if (textBoxVX.Text.Equals("") || textBoxVY.Text.Equals("") || textBoxVZ.Text.Equals("") ||
                textBoxPX.Text.Equals("") || textBoxPY.Text.Equals("") || textBoxPZ.Text.Equals(""))
            {
                MessageBox.Show(@"Введите значения в соответствующие поля", @"Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                button1.IsEnabled = false;
                button2.IsEnabled = false;
                return;
            }

            if (Convert.ToInt32(textBoxVX.Text) == 0 || Convert.ToInt32(textBoxVX.Text) < 0)
            {
                MessageBox.Show(@"Нулевой/отрицательный объем недопустим. Введите значения больше нуля", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxVX.Text = "";
                button1.IsEnabled = false;
                button2.IsEnabled = false;
                return;
            }
            else if (Convert.ToInt32(textBoxVY.Text) == 0 || Convert.ToInt32(textBoxVY.Text) < 0)
            {
                MessageBox.Show(@"Нулевой/отрицательный объем недопустим. Введите значения больше нуля", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxVY.Text = "";
                button1.IsEnabled = false;
                button2.IsEnabled = false;
                return;
            }
            else if (Convert.ToInt32(textBoxVZ.Text) == 0 || Convert.ToInt32(textBoxVZ.Text) < 0)
            {
                MessageBox.Show(@"Нулевой/отрицательный объем недопустим. Введите значения больше нуля", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxVZ.Text = "";
                button1.IsEnabled = false;
                button2.IsEnabled = false;
                return;
            }

            button1.IsEnabled = true;
            button2.IsEnabled = true;

            var p1 = Position.Create();
            p1.X = Convert.ToInt32(textBoxPX.Text) - Convert.ToInt32(textBoxVX.Text) / 2;
            p1.Y = Convert.ToInt32(textBoxPY.Text) - Convert.ToInt32(textBoxVY.Text) / 2;
            p1.Z = Convert.ToInt32(textBoxPZ.Text) - Convert.ToInt32(textBoxVZ.Text) / 2;

            var p2 = Position.Create();
            p2.X = Convert.ToInt32(textBoxPX.Text) + Convert.ToInt32(textBoxVX.Text) / 2;
            p2.Y = Convert.ToInt32(textBoxPY.Text) + Convert.ToInt32(textBoxVY.Text) / 2;
            p2.Z = Convert.ToInt32(textBoxPZ.Text) + Convert.ToInt32(textBoxVZ.Text) / 2;
            _lb = LimitsBox.Create(p1, p2);

            if (_lb != null)
            {
                switch (selecteditem)
                {
                    case 0:
                        cmd.CreateCommand("AID CLEAR BOX 666").Run();
                        cmd.CreateCommand("AID BOX NUMBER 666 AT " + _lb.Centre() +
                                          " XLENGTH " + Math.Round(_lb.Maximum.X - _lb.Minimum.X).ToString() +
                                          " YLENGTH " + Math.Round(_lb.Maximum.Y - _lb.Minimum.Y).ToString() +
                                          " ZLENGTH " + Math.Round(_lb.Maximum.Z - _lb.Minimum.Z).ToString() +
                                          " FILL OFF").Run();
                        break;

                    case 1:
                        cmd.CreateCommand("AID CLEAR BOX 666").Run();
                        break;

                    case 2:
                        cmd.CreateCommand("AID CLEAR BOX 666").Run();
                        cmd.CreateCommand("AID BOX NUMBER 666 AT " + _lb.Centre() +
                                          " XLENGTH " + Math.Round(_lb.Maximum.X - _lb.Minimum.X).ToString() +
                                          " YLENGTH " + Math.Round(_lb.Maximum.Y - _lb.Minimum.Y).ToString() +
                                          " ZLENGTH " + Math.Round(_lb.Maximum.Z - _lb.Minimum.Z).ToString() +
                                          " FILL ON").Run();
                        break;

                    default:
                        cmd.CreateCommand("AID CLEAR BOX 666").Run();
                        cmd.CreateCommand("AID BOX NUMBER 666 AT " + _lb.Centre() +
                                          " XLENGTH " + Math.Round(_lb.Maximum.X - _lb.Minimum.X).ToString() +
                                          " YLENGTH " + Math.Round(_lb.Maximum.Y - _lb.Minimum.Y).ToString() +
                                          " ZLENGTH " + Math.Round(_lb.Maximum.Z - _lb.Minimum.Z).ToString() +
                                          " FILL OFF").Run();
                        break;
                }
            }
            else
            {
                MessageBox.Show(@"Нет LimitBox для построения", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimitsList()
        {
            foreach (var smallrect in SmallRectsInfo)
            {
                if (!smallrect.Value.Fill.Equals(Brushes.Red)) continue;

                foreach (var rect in LimitsInfo)
                {
                    if (!smallrect.Key.Equals(rect.Key)) continue;

                    var widthOfRect =
                        (int)
                            Math.Sqrt(Math.Pow((rect.Value.Margin.Left - rect.Value.Margin.Right), 2.0) +
                                      Math.Pow((rect.Value.Margin.Top - rect.Value.Margin.Top), 2.0));
                    var heightOfRect =
                        (int)
                            Math.Sqrt(Math.Pow((rect.Value.Margin.Left - rect.Value.Margin.Left), 2.0) +
                                      Math.Pow((rect.Value.Margin.Top - rect.Value.Margin.Bottom), 2.0));
                    var centerOfLb = Position.Create();

                    var koefA = (LimitBoxR.Minimum.X - 10 / Scale) * Scale;
                    var koefB = (LimitBoxR.Maximum.Y - 10 / Scale) * Scale;

                    centerOfLb.X = ((rect.Value.Margin.Left + rect.Value.Margin.Right) / 2.0 + koefA) /
                                   Scale;
                    centerOfLb.Y = (((rect.Value.Margin.Top + rect.Value.Margin.Bottom) / 2.0 - koefB - 20) /
                                    Scale) *
                                   Math.Cos(Math.PI);
                    centerOfLb.Z = ZCentr;

                    var p1 = Position.Create(centerOfLb.X - widthOfRect / 2.0 / Scale,
                        centerOfLb.Y - heightOfRect / 2.0 / Scale, centerOfLb.Z - ZLength);
                    var p2 = Position.Create(centerOfLb.X + widthOfRect / 2.0 / Scale,
                        centerOfLb.Y + heightOfRect / 2.0 / Scale, centerOfLb.Z + ZLength);

                    var lb = LimitsBox.Create(p1, p2);

                    _limits.Add(lb);
                }
            }
        }

        #endregion
        
        #region Dependency Properties

        public double MinimumValue
        {
            get { return (double)GetValue(MinimumValueProperty); }
            set { SetValue(MinimumValueProperty, value); }
        }

        public static readonly DependencyProperty MinimumValueProperty =
            DependencyProperty.Register("MinimumValue", typeof(double), typeof(Slider), new UIPropertyMetadata(0d));

        public double LowerValue
        {
            get { return (double)GetValue(LowerValueProperty); }
            set { SetValue(LowerValueProperty, value); }
        }

        public static readonly DependencyProperty LowerValueProperty =
            DependencyProperty.Register("LowerValue", typeof(double), typeof(Slider), new UIPropertyMetadata(0d));

        public double UpperValue
        {
            get { return (double)GetValue(UpperValueProperty); }
            set { SetValue(UpperValueProperty, value); }
        }

        public static readonly DependencyProperty UpperValueProperty =
            DependencyProperty.Register("UpperValue", typeof(double), typeof(Slider), new UIPropertyMetadata(0d));

        public double MaximumValue
        {
            get { return (double)GetValue(MaximumValueProperty); }
            set { SetValue(MaximumValueProperty, value); }
        }

        public static readonly DependencyProperty MaximumValueProperty =
            DependencyProperty.Register("MaximumValue", typeof(double), typeof(Slider), new UIPropertyMetadata(1d));

        #endregion

        #endregion


        #region canvas
        
        #region Events

        private void DrwstEditWind_Loaded(object sender, RoutedEventArgs e)
        {
            DrawingLimits();
            ScrollingOfValues(LowerSlider, UpperSlider);
        }

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
                var rect = (Rectangle)rec;
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

            try
            {
                if (canvas.Children.Contains(_selRect)) canvas.Children.Remove(_selRect);

                if (_orig.X.Equals(0.0) && _orig.Y.Equals(0.0)) return;

                if (_orig != e.GetPosition(canvas))
                {
                    foreach (var rect in canvas.Children)
                    {
                        if (!(rect.GetType().Name.Equals("Rectangle"))) continue;
                        var wrect = (Rectangle)rect;
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
                                var rectan = (Rectangle)elem;
                                if (!rectan.Name.StartsWith("Rect")) continue;
                                var rectname = rectan.Name.Replace("Rect", "");
                                if (!rectname.Equals(key)) continue;

                                DictionaryFilling(LimitsInfo, key, rectan);
                                DictionaryFilling(SmallRectsInfo, key, wrect);

                                LimitsBoxes(rectan, ZCentr, ZLength, key, Selit);
                            }
                        }
                    }
                }
                else
                {
                    var mouseClickPos = new PointF
                    {
                        X = (float)e.GetPosition(canvas).X,
                        Y = (float)e.GetPosition(canvas).Y
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
                        var nkhm = (Rectangle)khm;
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
                                var rectan = (Rectangle)elem;
                                if (!rectan.Name.StartsWith("Rect")) continue;
                                var rectname = rectan.Name.Replace("Rect", "");
                                if (!rectname.Equals(indexwn)) continue;

                                DictionaryFilling(LimitsInfo, indexwn, rectan);
                                DictionaryFilling(SmallRectsInfo, indexwn, nkhm);

                                LimitsBoxes(rectan, ZCentr, ZLength, indexwn, Selit);
                            }
                        }
                    }
                }
                _orig.X = 0;
                _orig.Y = 0;
                Mouse.Capture(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DrawingLb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
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
                    var wpr = (Rectangle)pr;
                    if (!wpr.Name.StartsWith("Rect")) continue;
                    var wpri = wpr.Name.Replace("Rect", "");

                    foreach (var ppr in canvas.Children)
                    {
                        if (!(ppr.GetType().Name.Equals("Rectangle"))) continue;
                        var wppr = (Rectangle)ppr;
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                var rect = (Rectangle)elem;
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

        internal  void LimitsBoxes(Rectangle rect, double cent, int length, string index, int selectedindex)
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

            var koefA = (LimitBoxR.Minimum.X - 10 / Scale) * Scale;
            var koefB = (LimitBoxR.Maximum.Y - 10 / Scale) * Scale;

            centerOfLb.X = ((rect.Margin.Left + rect.Margin.Right) / 2.0 + koefA) / Scale;
            centerOfLb.Y = (((rect.Margin.Top + rect.Margin.Bottom) / 2.0 - koefB - 20) /
                            Scale) *
                           Math.Cos(Math.PI);
            centerOfLb.Z = cent;

            switch (selectedindex)
            {
                case 0:
                    Command.CreateCommand("AID CLEAR BOX " + index).Run();
                    Command.CreateCommand("AID BOX NUMBER " + index + " AT " + centerOfLb +
                                          " XLENGTH " + (widthOfRect / Scale).ToString().Replace(',', '.') +
                                          " YLENGTH " + (heightOfRect / Scale).ToString().Replace(',', '.') +
                                          " ZLENGTH " + length.ToString()).Run();
                    break;

                case 1:
                    Command.CreateCommand("AID CLEAR ALL BOX").Run();
                    break;

                default:
                    Command.CreateCommand("AID CLEAR BOX " + index).Run();
                    Command.CreateCommand("AID BOX NUMBER " + index + " AT " + centerOfLb +
                                          " XLENGTH " + (widthOfRect / Scale).ToString().Replace(',', '.') +
                                          " YLENGTH " + (heightOfRect / Scale).ToString().Replace(',', '.') +
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

        private static void GetSelRectangle(Point orig, Point location, Rectangle rect)
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

                var widthOfHost = canvas.ActualWidth - 20;
                var heightOfHost = canvas.ActualHeight - 20;

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

                canvas.Children.Add(_limitrect);

                foreach (DbElement item in collection)
                {
                    var poss = item.GetPosition(DbAttributeInstance.POSS);
                    var pose = item.GetPosition(DbAttributeInstance.POSE);

                    var beg = new Point((float)(Math.Abs(poss.X - P2.X + 10 / Scale) * Scale),
                        (float)(Math.Abs(poss.Y - P2.Y - 10 / Scale) * Scale));
                    var end = new Point((float)(Math.Abs(pose.X - P2.X + 10 / Scale) * Scale),
                        (float)(Math.Abs(pose.Y - P2.Y - 10 / Scale) * Scale));

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

                    canvas.Children.Add(ln);

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

                        canvas.Children.Add(ln1);

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

                        canvas.Children.Add(el);
                        canvas.Children.Add(tb);
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

                        canvas.Children.Add(ln1);

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

                canvas.Children.Add(invRects);
                canvas.Children.Add(invRectsnew);

            }

            foreach (var pair in SmallRectsInfo)
            {
                if (!pair.Value.Fill.Equals(Brushes.Red)) continue;

                foreach (var elem in canvas.Children)
                {
                    if (!(elem.GetType().Name.Equals("Rectangle"))) continue;
                    var rect = (Rectangle)elem;
                    if (!rect.Name.StartsWith("NewRect")) continue;
                    var rectname = rect.Name.Replace("NewRect", "");
                    if (!rectname.Equals(pair.Key)) continue;

                    rect.Fill = Brushes.Red;
                }
            }

            foreach (var elem in canvas.Children)
            {
                if (!(elem.GetType().Name.Equals("Rectangle"))) continue;
                var rect = (Rectangle)elem;
                if (!rect.Name.StartsWith("NewRect")) continue;
                if (!rect.Fill.Equals(Brushes.Red)) continue;
                var rectname = rect.Name.Replace("NewRect", "");

                if (SmallRectsInfo.ContainsKey(rectname))
                {
                    SmallRectsInfo.Remove(rectname);
                    SmallRectsInfo.Add(rectname, rect);
                }
                else 
                    SmallRectsInfo.Add(rectname, rect);
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
        
        #endregion


        private void SettingsViewBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Selit = m_settingsViewBox.SelectedIndex;

            if (TabControlDrwLst != null)
            {
                if (TabControlDrwLst.SelectedItem != null)
                {
                    TabItem selti = TabControlDrwLst.SelectedItem as TabItem;

                    if (selti != null && selti.Equals(PaintByXYZ))
                    {
                        SelectedItemTab1(Selit);
                    }
                    else if (selti != null && selti.Equals(PaintLimitBoxItem))
                    {
                        if (ZLength == 0 && Selit == 2)
                            System.Windows.MessageBox.Show("Так как фигуры не имеют объема, их не будет видно на DrawList",
                                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        foreach (var elem in LimitsInfo)
                        {
                            LimitsBoxes(elem.Value, ZCentr, ZLength, elem.Key, Selit);
                        }
                    }
                }
            }
        }
        
        private void DrwstEditWind_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawingLimits();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var curel = CurrentElement.Element;
            if (!curel.Equals(DbElement.GetElement("/*")))
            {
                LimitsBox lbforCurEl;
                Spatial.Instance.LimitsBox(curel, out lbforCurEl);

                if (lbforCurEl != null)
                {
                    textBoxPX.Text = Convert.ToInt32(lbforCurEl.Centre().X).ToString();
                    textBoxPY.Text = Convert.ToInt32(lbforCurEl.Centre().Y).ToString();
                    textBoxPZ.Text = Convert.ToInt32(lbforCurEl.Centre().Z).ToString();

                    Selit = m_settingsViewBox.SelectedIndex;
                    SelectedItemTab1(Selit);
                }
                else
                {
                    MessageBox.Show(@"Неудалось построить LimitBox для текущего элемента", @"Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(@"Текущий эемент - WORLD. Выберите другой элемент для построения LimitBox", @"Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AddToDrwLstButton_Click(object sender, RoutedEventArgs e)
        {
            _exec = true;
            var typesofel = _sf.GetCheckedTypes();

            var checkedroots = _sf.GetCheckedRoots();

            TabItem selti = TabControlDrwLst.SelectedItem as TabItem;

            if (selti != null && selti.Equals(PaintByXYZ))
            {
                var items = Spatial.Instance.ElementsInBox(_lb, typesofel.ToArray(), false);

                if (_lb == null)
                {
                    MessageBox.Show(@"Нет построенных LimitBox", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (checkedroots.Count == 0)
                {
                    var res = System.Windows.Forms.MessageBox.Show(
                        @"Не выбран ни один элемент из списка. Будут выведены все элементы. Продолжить?",
                        @"Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (res == System.Windows.Forms.DialogResult.Yes)
                    {
                        foreach (var item in items)
                        {
                            System.Windows.Forms.Application.DoEvents();

                            if (!_exec)
                            {
                                MessageBox.Show(@"Операция остановлена!", @"Предупреждение", MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                return;
                            }
                            cmd.CreateCommand("ADD " + item.GetAsString(DbAttributeInstance.REF)).Run();
                        }
                    }
                    else
                    {
                        var mwb = MessageBox.Show(@"Выбрать элементы?", @"Предупреждение", MessageBoxButtons.YesNo);
                        if (mwb == System.Windows.Forms.DialogResult.Yes) _sf.ShowDialog();
                        else return;
                    }
                }
                
                foreach (var item in items)
                {
                    System.Windows.Forms.Application.DoEvents();

                    var owners = item.GetElementArray(DbAttributeInstance.OWNLST);
                    foreach (var root in checkedroots)
                    {
                        if (!_exec)
                        {
                            MessageBox.Show(@"Операция остановлена!", @"Предупреждение", MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        if (!owners.Contains(root)) continue;
                        cmd.CreateCommand("ADD " + item.GetAsString(DbAttributeInstance.REF)).Run();
                        break;
                    }
                }

                MessageBox.Show(@"Вывод элементов на экран завершен", @"Уведомление", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

            }
            else if (selti != null && selti.Equals(PaintLimitBoxItem))
            {
                if (SmallRectsInfo.Count == 0)
                {
                    MessageBox.Show(@"Не построен ни один LimitBox", @"Предупреждение", MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                LimitsList();

                var items = new List<DbElement>();
                foreach (var limit in _limits)
                {
                    items.AddRange(Spatial.Instance.ElementsInBox(limit, typesofel.ToArray(), false).ToList());
                }

                if (checkedroots.Count == 0)
                {
                    var res = MessageBox.Show(
                        @"Не выбран ни один элемент из списка. Будут выведены все элементы. Продолжить?",
                        @"Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == System.Windows.Forms.DialogResult.Yes)
                    {
                        foreach (var item in items)
                        {
                            System.Windows.Forms.Application.DoEvents();

                            if (!_exec)
                            {
                                MessageBox.Show(@"Операция остановлена!", @"Предупреждение", MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                return;
                            }
                            cmd.CreateCommand("ADD " + item.GetAsString(DbAttributeInstance.REF)).Run();
                        }
                    }
                    else
                    {
                        var mwb = MessageBox.Show(@"Выбрать элементы?", @"Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (mwb == System.Windows.Forms.DialogResult.Yes) _sf.ShowDialog();
                        else return;
                    }
                }

                foreach (var item in items)
                {
                    System.Windows.Forms.Application.DoEvents();
                    var owners = item.GetElementArray(DbAttributeInstance.OWNLST);

                    foreach (var root in checkedroots)
                    {
                        if (!_exec)
                        {
                            MessageBox.Show(@"Операция остановлена!", @"Предупреждение", MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        if (!owners.Contains(root)) continue;

                        cmd.CreateCommand("ADD " + item.GetAsString(DbAttributeInstance.REF)).Run();
                        break;
                    }
                }

                MessageBox.Show(@"Вывод элементов на экран завершен", @"Уведомление", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(@"Эта кнопка здесь неактивна", @"Уведомление", MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
        }
        
        private void RemFromDrwLstButton_Click(object sender, RoutedEventArgs e)
        {
            _exec = true;

            var drawListMembers = _dList.Members();

            TabItem selti = TabControlDrwLst.SelectedItem as TabItem;

            if (selti != null && selti.Equals(PaintByXYZ))
            {
                if (_lb == null)
                {
                    MessageBox.Show(@"Нет построенных LimitBox", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                foreach (var item in drawListMembers)
                {
                    System.Windows.Forms.Application.DoEvents();

                    if (!_exec)
                    {
                        MessageBox.Show(@"Операция остановлена!", @"Предупреждение", MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    if (!Spatial.Instance.ElementInBox(item.DbElement, _lb)) continue;

                    cmd.CreateCommand("REM " + item.DbElement.GetAsString(DbAttributeInstance.REF)).Run();
                }

                MessageBox.Show(@"Удаление элементов завершено", @"Уведомление", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (selti != null && selti.Equals(PaintLimitBoxItem))
            {
                foreach (var item in drawListMembers)
                {
                    System.Windows.Forms.Application.DoEvents();

                    foreach (var limit in _limits)
                    {
                        if (!_exec)
                        {
                            MessageBox.Show(@"Операция остановлена!", @"Предупреждение", MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }

                        if (!Spatial.Instance.ElementInBox(item.DbElement, limit)) continue;

                        cmd.CreateCommand("REM " + item.DbElement.GetAsString(DbAttributeInstance.REF)).Run();
                        break;
                    }
                }
                MessageBox.Show(@"Удаление элементов завершено", @"Уведомление", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
          
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _sf.ShowDialog();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            _selectedpoint = PickPointPointSelected;
            PickPoint.PointSelected += _selectedpoint;
        }

        void PickPointPointSelected(Position pos)
        {
            textBoxPX.Text = Convert.ToInt32(pos.X).ToString();
            textBoxPY.Text = Convert.ToInt32(pos.Y).ToString();
            textBoxPZ.Text = Convert.ToInt32(pos.Z).ToString();

            Selit = m_settingsViewBox.SelectedIndex;
            SelectedItemTab1(Selit);

            PickPoint.PointSelected -= _selectedpoint;
        }

        private void CleanDrwLstButton_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show(@"Все элементы будут убраны с вида. Продолжить?", @"Подтвержденине",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (res != System.Windows.Forms.DialogResult.Yes) return;
            
            cmd.CreateCommand("REM ALL").Run();
            cmd.CreateCommand("AID CLEAR ALL BOX").Run();

            RemAllFromView();
        }

        private void RemAllFromView()
        {
            foreach (var elem in canvas.Children)
            {
                if (!(elem.GetType().Name.Equals("Rectangle"))) continue;
                var rect = (Rectangle)elem;
                if (!rect.Name.StartsWith("NewRect")) continue;

                rect.Fill = Brushes.Transparent;

                LimitsInfo.Clear();
                SmallRectsInfo.Clear();
            }
        }

        private void DrwstEditWind_Activated(object sender, EventArgs e)
        {
            if (lstaddTabItem.Count==0)
            {
                GetExistLimit();
            }
            
        }

       

    }


    public struct ExistLimits
    {
        public DbElement MarkHeight;
        public DbElement RegionMark;
        public DbElement BoxElementRegion;
    }
}
