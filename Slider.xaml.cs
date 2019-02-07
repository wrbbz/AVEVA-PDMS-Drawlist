using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Aveva.Pdms.Utilities.CommandLine;

namespace Polymetal.Pdms.Design.DrawListManager
{
    /// <summary>
    /// Логика взаимодействия для Slider.xaml
    /// </summary>
    public partial class Slider
    {
        private const double Minz = -5000.0;
        private const double Maxz = 25000.0;
        private readonly FormNew _slFormNew;

        public Slider(FormNew frmn)
        {
            InitializeComponent();
           
            Loaded += Slider_Loaded;
            PreviewMouseLeftButtonUp += Slider_MouseUp;
            _slFormNew = frmn;
        }

        private void ScrollingOfValues(RangeBase min, RangeBase max)
        {
            if (max.Value - min.Value < 100) return;

            DrawingLb.ZLength = (int) Math.Abs(max.Value - min.Value);
            DrawingLb.ZCentr = min.Value + DrawingLb.ZLength/2.0;

            foreach (var pair in DrawingLb.LimitsInfo)
            {

                Command.CreateCommand("AID CLEAR BOX " + pair.Key).Run();

                DrawingLb.LimitsBoxes(pair.Value, DrawingLb.ZCentr, DrawingLb.ZLength, pair.Key, _slFormNew.Selit);
            }
        }

        #region Events

        private void Slider_Loaded(object sender, RoutedEventArgs e)
        {
            LowerSlider.ValueChanged += LowerSlider_ValueChanged;
            UpperSlider.ValueChanged += UpperSlider_ValueChanged;


            LowerSlider.Minimum = Minz;
            LowerSlider.Maximum = Maxz - 100;

            UpperSlider.Minimum = Minz + 100;
            UpperSlider.Maximum = Maxz;

            LowerSlider.Value = 0;
            UpperSlider.Value = 3000;

            DrawingLb.ZLength = (int)Math.Abs(UpperSlider.Value - LowerSlider.Value);
            DrawingLb.ZCentr = LowerSlider.Value + DrawingLb.ZLength / 2.0;

            LowerSliderValue.Text = LowerSlider.Value.ToString();
            UpperSliderValue.Text = UpperSlider.Value.ToString();
        }

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
                if (ch < 58 && ch > 47 || ch.Equals('-')&& i==0)
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
                MessageBox.Show("Введены недопустимые значения. Значения должны быть от -5000 до 25000", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

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
                MessageBox.Show("Введены недопустимые значения. Значения должны быть от -5000 до 25000", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

        #region Dependency Properties

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(Slider), new UIPropertyMetadata(0d));

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

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(Slider), new UIPropertyMetadata(1d));

        #endregion
    }
}
