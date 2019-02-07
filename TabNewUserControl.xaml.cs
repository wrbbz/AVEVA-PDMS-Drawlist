using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aveva.Pdms.Geometry;
using Aveva.Pdms.Maths.Geometry;

namespace Polymetal.Pdms.Design.DrawListManager
{
    /// <summary>
    /// Логика взаимодействия для TabNewUserControl.xaml
    /// </summary>
    public partial class TabNewUserControl : UserControl
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



        public TabNewUserControl()
        {
            InitializeComponent();
        }

        public TabNewUserControl(List<List<D2Point>> coordList, List<D2FiniteLine> horizontal, List<D2FiniteLine> vertical, double scale,
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

                //DrawingLimits();



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

    }
}
