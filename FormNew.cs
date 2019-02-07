using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Aveva.Pdms.Database;
using Aveva.Pdms.Geometry;
using Aveva.Pdms.Graphics;
using Aveva.Pdms.Shared;
using Aveva.PDMS.Database.Filters;
using cmd = Aveva.Pdms.Utilities.CommandLine.Command;
using MessageBox = System.Windows.Forms.MessageBox;



namespace Polymetal.Pdms.Design.DrawListManager
{
    public partial class FormNew : Form
    {
        internal LimitsBox LimitBoxR;
        internal double WidthOfLb;
        internal double HeightOfLb;
        internal Position2D P1 = Position2D.Create();
        internal Position2D P2 = Position2D.Create();
        internal Position2D P3 = Position2D.Create();
        internal Position2D P4 = Position2D.Create();
        private LimitsBox _lb;
        private Slider _rangesl;
        private DrawingLb _dlb;
        private readonly SettingsForm _sf = new SettingsForm();
        private readonly List<LimitsBox> _limits = new List<LimitsBox>();
        internal int Selit;
        private readonly DrawList _dList = Aveva.Pdms.Graphics.DrawListManager.Instance.CurrentDrawList;
        private bool _exec = true;
        private PickPoint.PointSelectedEventHandler _selectedpoint;
        
        public FormNew()
        {
            InitializeComponent();
        }

        #region Events

        private void FormNew_Load(object sender, EventArgs e)
        {
            LimitOfAxes();
            _rangesl = new Slider(this);
            _dlb = new DrawingLb(this);
            ProgressBar.Visible = false;
            toolStripComboBox1.SelectedIndex = 0;
            toolStripComboBox1.DropDownWidth = 300;
            toolStripButton4.Visible = false;
        }

        private void FormNew_FormClosed(object sender, FormClosedEventArgs e)
        {
            cmd.CreateCommand("AID CLEAR ALL BOX").Run();

            DrawingLb.LimitsInfo.Clear();
            DrawingLb.SmallRectsInfo.Clear();
            DrawingLb.ZCentr = 0.0;
            DrawingLb.ZLength = 0;
        }

        private void TextBoxAfterChange(object sender, EventArgs e)
        {
            var textBox = (TextBox) sender;
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

            SelectedItemTab1(Selit);

        }

        private void tabPage3_Enter(object sender, EventArgs e)
        {
            cmd.CreateCommand("AID CLEAR BOX 666").Run();

            elementHost1.Child = _dlb;
            elementHost2.Child = _rangesl;

            var selit = toolStripComboBox1.SelectedIndex;

            foreach (var elem in DrawingLb.LimitsInfo)
            {
                DrawingLb.LimitsBoxes(elem.Value, DrawingLb.ZCentr, DrawingLb.ZLength, elem.Key, selit);
            }
        }

        private void tabPage1_Enter(object sender, EventArgs e)
        {
            cmd.CreateCommand("AID CLEAR ALL BOX").Run();
            
            Selit = toolStripComboBox1.SelectedIndex;
            SelectedItemTab1(Selit);

        }
        
        private void toolStripButton1_Click_1(object sender, EventArgs e)
        {
            _sf.ShowDialog(this);
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            _exec = true;
            var typesofel = _sf.GetCheckedTypes();
            
            var checkedroots = _sf.GetCheckedRoots();
            
            if (tabControl1.SelectedTab.Equals(tabPage1))
            {
                var items = Spatial.Instance.ElementsInBox(_lb, typesofel.ToArray(), false);
                
                if (_lb == null)
                {
                    MessageBox.Show(@"Нет построенных LimitBox", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (checkedroots.Count == 0)
                {
                    var res = MessageBox.Show(
                        @"Не выбран ни один элемент из списка. Будут выведены все элементы. Продолжить?",
                        @"Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.Yes)
                    {
                        ProgressBar.Visible = true;
                        ProgressBar.Value = 0;
                        ProgressBar.Minimum = 0;
                        ProgressBar.Maximum = items.Length;
                        ProgressBar.Step = 1;
                        toolStripButton4.Visible = true;
                        toolStripButton1.Enabled = false;
                        toolStripButton3.Enabled = false;

                        foreach (var item in items)
                        {
                            ProgressBar.Value++;
                            System.Windows.Forms.Application.DoEvents();

                            if (!_exec)
                            {
                                ProgressBar.Value = 0;
                                ProgressBar.Visible = false;
                                toolStripButton4.Visible = false;
                                toolStripButton1.Enabled = true;
                                toolStripButton3.Enabled = true;
                                MessageBox.Show(@"Операция остановлена!", @"Предупреждение", MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                return;
                            }

                            cmd.CreateCommand("ADD " + item.GetAsString(DbAttributeInstance.REF)).Run();

                        }

                        ProgressBar.Visible = false;
                        ProgressBar.Value = 0;
                        toolStripButton4.Visible = false;
                        toolStripButton1.Enabled = true;
                        toolStripButton3.Enabled = true;

                    }
                    else
                    {
                        var mwb = MessageBox.Show(@"Выбрать элементы?", @"Предупреждение", MessageBoxButtons.YesNo);
                        if (mwb == DialogResult.Yes) _sf.ShowDialog(this);
                        else return;
                    }
                }

                ProgressBar.Visible = true;
                ProgressBar.Value = 0;
                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = items.Length;
                ProgressBar.Step = 1;

                toolStripButton4.Visible = true;
                toolStripButton1.Enabled = false;
                toolStripButton3.Enabled = false;
                
                foreach (var item in items)
                {
                    ProgressBar.Value++;
                    System.Windows.Forms.Application.DoEvents();

                    var owners = item.GetElementArray(DbAttributeInstance.OWNLST);

                    foreach (var root in checkedroots)
                    {

                        if (!_exec)
                        {
                            ProgressBar.Value = 0;
                            ProgressBar.Visible = false;

                            toolStripButton4.Visible = false;
                            toolStripButton1.Enabled = true;
                            toolStripButton3.Enabled = true;
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

                ProgressBar.Visible = false;
                ProgressBar.Value = 0;

                toolStripButton4.Visible = false;
                toolStripButton1.Enabled = true;
                toolStripButton3.Enabled = true;
            }
            else if (tabControl1.SelectedTab.Equals(tabPage3))
            {
                if (DrawingLb.SmallRectsInfo.Count == 0)
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
                    if (res == DialogResult.Yes)
                    {
                        ProgressBar.Visible = true;
                        ProgressBar.Value = 0;
                        ProgressBar.Minimum = 0;
                        ProgressBar.Maximum = items.Count;
                        ProgressBar.Step = 1;

                        toolStripButton4.Visible = true;
                        toolStripButton1.Enabled = false;
                        toolStripButton3.Enabled = false;
                        
                        foreach (var item in items)
                        {
                            ProgressBar.Value++;
                            System.Windows.Forms.Application.DoEvents();

                            if (!_exec)
                            {
                                ProgressBar.Value = 0;
                                ProgressBar.Visible = false;

                                toolStripButton4.Visible = false;
                                toolStripButton1.Enabled = true;
                                toolStripButton3.Enabled = true;

                                MessageBox.Show(@"Операция остановлена!", @"Предупреждение", MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                                return;
                            }

                            cmd.CreateCommand("ADD " + item.GetAsString(DbAttributeInstance.REF)).Run();
                            
                        }
                        
                        ProgressBar.Visible = false;
                        ProgressBar.Value = 0;

                        toolStripButton4.Visible = false;
                        toolStripButton1.Enabled = true;
                        toolStripButton3.Enabled = true;
                        
                    }
                    else
                    {
                        var mwb = MessageBox.Show(@"Выбрать элементы?", @"Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (mwb == DialogResult.Yes) _sf.ShowDialog(this);
                        else return;
                    }
                }

                ProgressBar.Visible = true;
                ProgressBar.Value = 0;
                ProgressBar.Minimum = 0;
                ProgressBar.Maximum = items.Count;
                ProgressBar.Step = 1;

                toolStripButton4.Visible = true;
                toolStripButton1.Enabled = false;
                toolStripButton3.Enabled = false;

                

                foreach (var item in items)
                {
                    ProgressBar.Value++;
                    System.Windows.Forms.Application.DoEvents();

                    var owners = item.GetElementArray(DbAttributeInstance.OWNLST);

                    foreach (var root in checkedroots)
                    {

                        if (!_exec)
                        {
                            ProgressBar.Value = 0;
                            ProgressBar.Visible = false;

                            toolStripButton4.Visible = false;
                            toolStripButton1.Enabled = true;
                            toolStripButton3.Enabled = true;

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

                ProgressBar.Visible = false;
                ProgressBar.Value = 0;

                toolStripButton4.Visible = false;
                toolStripButton1.Enabled = true;
                toolStripButton3.Enabled = true;
            }
            else
            {
                MessageBox.Show(@"Эта кнопка здесь неактивна", @"Уведомление", MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
        }

        private static void StopWatch(Stopwatch stopWatch)
        {
            TimeSpan ts;
            string elapsedTime;
            ts = stopWatch.Elapsed;
            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds/10);
            Debug.Print(elapsedTime);
            stopWatch.Reset();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            _exec = true;

            toolStripButton2.Enabled = false;
            toolStripButton1.Enabled = false;
            toolStripButton4.Visible = true;

            var drawListMembers = _dList.Members();

            ProgressBar.Visible = true;
            ProgressBar.Value = 0;
            ProgressBar.Minimum = 0;
            ProgressBar.Maximum = drawListMembers.Length;
            ProgressBar.Step = 1;

            if (tabControl1.SelectedTab.Equals(tabPage1))
            {
                if (_lb == null)
                {
                    MessageBox.Show(@"Нет построенных LimitBox", @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                foreach (var item in drawListMembers)
                {
                    ProgressBar.Value++;
                    System.Windows.Forms.Application.DoEvents();

                    if (!_exec)
                    {
                        ProgressBar.Value = 0;
                        ProgressBar.Visible = false;
                        toolStripButton2.Enabled = true;
                        toolStripButton1.Enabled = true;
                        toolStripButton4.Visible = false;
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
            else if (tabControl1.SelectedTab.Equals(tabPage3))
            {
                foreach (var item in drawListMembers)
                {
                    ProgressBar.Value++;
                    System.Windows.Forms.Application.DoEvents();
                    
                    foreach (var limit in _limits)
                    {
                        if (!_exec)
                        {
                            ProgressBar.Value = 0;
                            ProgressBar.Visible = false;
                            toolStripButton2.Enabled = true;
                            toolStripButton1.Enabled = true;
                            toolStripButton4.Visible = false;
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

            ProgressBar.Visible = false;
            ProgressBar.Value = 0;
            toolStripButton2.Enabled = true;
            toolStripButton1.Enabled = true;
            toolStripButton4.Visible = false;

        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Selit = toolStripComboBox1.SelectedIndex;

            if (tabControl1.SelectedTab.Equals(tabPage1))
            {
                SelectedItemTab1(Selit);
            }
            else if (tabControl1.SelectedTab.Equals(tabPage3))
            {
                if (DrawingLb.ZLength == 0 && Selit == 2)
                    System.Windows.MessageBox.Show("Так как фигуры не имеют объема, их не будет видно на DrawList",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                foreach (var elem in DrawingLb.LimitsInfo)
                {
                    DrawingLb.LimitsBoxes(elem.Value, DrawingLb.ZCentr, DrawingLb.ZLength, elem.Key, Selit);
                }
            }
        }
        
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            _exec = false;
        }

        private void FormNew_MouseEnter(object sender, EventArgs e)
        {
           Activate();
        }

        private void button1_Click(object sender, EventArgs e)
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

                    Selit = toolStripComboBox1.SelectedIndex;
                    SelectedItemTab1(Selit);
                }
                else
                {
                    MessageBox.Show(@"Неудалось построить LimitBox для текущего элемента", @"Предупреждение",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(@"Текущий эемент - WORLD. Выберите другой элемент для построения LimitBox", @"Предупреждение",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _selectedpoint = PickPointPointSelected;
            PickPoint.PointSelected += _selectedpoint;
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            var res = MessageBox.Show(@"Все элементы будут убраны с вида. Продолжить?", @"Подтвержденине",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (res != DialogResult.Yes) return;


            cmd.CreateCommand("REM ALL").Run();
            cmd.CreateCommand("AID CLEAR ALL BOX").Run();

            _dlb.RemAll();
        }

        #endregion

        #region Functions

        private void SelectedItemTab1(int selecteditem)
        {
            if (textBoxVX.Text.Equals("") || textBoxVY.Text.Equals("") || textBoxVZ.Text.Equals("") ||
                textBoxPX.Text.Equals("") || textBoxPY.Text.Equals("") || textBoxPZ.Text.Equals(""))
            {
                 MessageBox.Show(@"Введите значения в соответствующие поля", @"Предупреждение", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                button1.Enabled = false;
                button2.Enabled = false;

                return;
            }

            if (Convert.ToInt32(textBoxVX.Text) == 0 || Convert.ToInt32(textBoxVX.Text) < 0)
            {

                MessageBox.Show(@"Нулевой/отрицательный объем недопустим. Введите значения больше нуля", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                textBoxVX.Text = "";
                button1.Enabled = false;
                button2.Enabled = false;

                return;
            }
            else if (Convert.ToInt32(textBoxVY.Text) == 0 || Convert.ToInt32(textBoxVY.Text) < 0)
            {
                MessageBox.Show(@"Нулевой/отрицательный объем недопустим. Введите значения больше нуля", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                textBoxVY.Text = "";
                button1.Enabled = false;
                button2.Enabled = false;

                return;
            }
            else if (Convert.ToInt32(textBoxVZ.Text) == 0 || Convert.ToInt32(textBoxVZ.Text) < 0)
            {
                MessageBox.Show(@"Нулевой/отрицательный объем недопустим. Введите значения больше нуля", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                textBoxVZ.Text = "";
                button1.Enabled = false;
                button2.Enabled = false;

                return;
            }

            button1.Enabled = true;
            button2.Enabled = true;

            var p1 = Position.Create();
            p1.X = Convert.ToInt32(textBoxPX.Text) - Convert.ToInt32(textBoxVX.Text)/2;
            p1.Y = Convert.ToInt32(textBoxPY.Text) - Convert.ToInt32(textBoxVY.Text)/2;
            p1.Z = Convert.ToInt32(textBoxPZ.Text) - Convert.ToInt32(textBoxVZ.Text)/2;

            var p2 = Position.Create();
            p2.X = Convert.ToInt32(textBoxPX.Text) + Convert.ToInt32(textBoxVX.Text)/2;
            p2.Y = Convert.ToInt32(textBoxPY.Text) + Convert.ToInt32(textBoxVY.Text)/2;
            p2.Z = Convert.ToInt32(textBoxPZ.Text) + Convert.ToInt32(textBoxVZ.Text)/2;
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
            foreach (var smallrect in DrawingLb.SmallRectsInfo)
            {
                if (!smallrect.Value.Fill.Equals(Brushes.Red)) continue;

                foreach (var rect in DrawingLb.LimitsInfo)
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

                    var koefA = (DrawingLb.LimitBoxDr.Minimum.X - 10 / DrawingLb.Scale) * DrawingLb.Scale;
                    var koefB = (DrawingLb.LimitBoxDr.Maximum.Y - 10 / DrawingLb.Scale) * DrawingLb.Scale;

                    centerOfLb.X = ((rect.Value.Margin.Left + rect.Value.Margin.Right) / 2.0 + koefA) /
                                   DrawingLb.Scale;
                    centerOfLb.Y = (((rect.Value.Margin.Top + rect.Value.Margin.Bottom) / 2.0 - koefB - 20) /
                                    DrawingLb.Scale) *
                                   Math.Cos(Math.PI);
                    centerOfLb.Z = DrawingLb.ZCentr;

                    var p1 = Position.Create(centerOfLb.X - widthOfRect / 2.0 / DrawingLb.Scale,
                        centerOfLb.Y - heightOfRect / 2.0 / DrawingLb.Scale, centerOfLb.Z - DrawingLb.ZLength);
                    var p2 = Position.Create(centerOfLb.X + widthOfRect / 2.0 / DrawingLb.Scale,
                        centerOfLb.Y + heightOfRect / 2.0 / DrawingLb.Scale, centerOfLb.Z + DrawingLb.ZLength);

                    var lb = LimitsBox.Create(p1, p2);

                    _limits.Add(lb);
                }
            }
        }

        void PickPointPointSelected(Position pos)
        {
            textBoxPX.Text = Convert.ToInt32(pos.X).ToString();
            textBoxPY.Text = Convert.ToInt32(pos.Y).ToString();
            textBoxPZ.Text = Convert.ToInt32(pos.Z).ToString();

            Selit = toolStripComboBox1.SelectedIndex;
            SelectedItemTab1(Selit);

            PickPoint.PointSelected -= _selectedpoint;
        }
        
        internal void LimitOfAxes()
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
                    var koef = WidthOfLb/HeightOfLb;

                    Height = MinimumSize.Height;
                    Width = (Height * (int)koef);
                    
                }
                else if (WidthOfLb < HeightOfLb)
                {
                    var koef = HeightOfLb/WidthOfLb;

                    Width = MinimumSize.Width;
                    Height = (Width * (int)koef);
                }
                else
                {
                    Width = 750;
                    Height = 750;
                }
            }
        }

        #endregion

        private void toolStripComboBox1_MouseEnter(object sender, EventArgs e)
        {
            toolStripComboBox1.ToolTipText = toolStripComboBox1.SelectedText;
        }

    }
}

