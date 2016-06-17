using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using QFJH.Algorithm;
using QFJH.DataStruct;
using QFJH.Properties;

namespace QFJH.UI
{
    /// <summary>
    /// 主界面-设计算法的代码放这里
    /// </summary>
    public partial class MainForm
    {
        /// <summary>
        /// 打开的图像
        /// </summary>
        private DigitalImage _baseImg, _serImg;

        /// <summary>
        /// 后方交会计算出的外方位元素
        /// </summary>
        private BackMatch _left, _right;

        /// <summary>
        /// 前方交会计算
        /// </summary>
        private FrontMatch _fr;

        /// <summary>
        /// 已有同名点数据
        /// </summary>
        private readonly List<DataList> _existData = new List<DataList>();

        /// <summary>
        /// 目标匹配数据
        /// </summary>
        private readonly List<DataList> _targetData = new List<DataList>();

        private CameraPara _camPara;

        private void 保存SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_fr == null || _fr?.HasProcessed == false)
            {
                MessageBox.Show(Resources.NotProcessed, Resources.Prog_Name, MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "保存结果",
                CheckPathExists = true,
                InitialDirectory = @"E:\大学\大三下\摄影测量学\摄影测量学作业\Data",
                Filter = "CSV文件(*.csv)|*.csv"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                using (StreamWriter sw = new StreamWriter(sfd.OpenFile(), Encoding.Default)) 
                {
                    sw.WriteLine("点号,左影像行号,左影像列号,右影像行号,右影像列号,X坐标,Y坐标,Z坐标");
                    foreach (var t in _targetData)
                    {
                        sw.WriteLine(t.PointNumber + "," + t.LeftRowNumber + "," + t.LeftColNumber + "," +
                                     t.RightRowNumber + "," + t.RightColNumber
                                     + "," + t.X + "," + t.Y + "," + t.Z);
                    }
                }
                MessageBox.Show("成功！", Resources.Prog_Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.Prog_Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void 左图像LToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_left == null || _left?.HasProcessed == false) 
            {
                MessageBox.Show(Resources.NotProcessed, Resources.Prog_Name, MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }

            new ViewOuter(_left, "左图像").Show();
        }

        private void 右图像LToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_right == null || _right?.HasProcessed == false)
            {
                MessageBox.Show(Resources.NotProcessed, Resources.Prog_Name, MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }

            new ViewOuter(_right, "右图像").Show();
        }

        private void 左影像LToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckOpen()) return;

            try
            {
                _left = new BackMatch(_existData, _camPara, "LEFT");
                _left.SetLimit(1E-5);
                _left.Process();
                new ViewOuter(_left, "左图像").Show();

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.Prog_Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void 右影像RToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckOpen()) return;

            try
            {

                _right = new BackMatch(_existData, _camPara, "RIGHT");
                _right.SetLimit(1E-5);
                _right.Process();
                new ViewOuter(_right, "右图像").Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.Prog_Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void 前方交汇计算QToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckOpen(true)) return;
            if (_left == null | _right == null)
            {
                MessageBox.Show("请先计算后方交会结果！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            try
            {
                _fr = new FrontMatch(_left, _right, _targetData, _camPara);
                _fr.Process();

                dataTargetPoint.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.Prog_Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            var ofd = MyOpenFileDialog("选择相机参数文件", "文本文件(*.txt)|*.txt");
            if (ofd == null) return;

            try
            {
                _camPara = new CameraPara(ofd.FileName);
                textBox1.Text = _camPara.Type;
                textBox2.Text = _camPara.WidthPix.ToString("##.0");
                textBox3.Text = _camPara.HeightPix.ToString("##.0");
                textBox4.Text = _camPara.f.ToString("##.00");
                textBox5.Text= _camPara.PixSize.ToString("##.00");
                textBox6.Text = _camPara.MainPosX.ToString("0.00");
                textBox7.Text = _camPara.MainPosY.ToString("0.00");
                MessageBox.Show(Resources.MainForm_OpenSucc, Resources.Prog_Name, MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.Prog_Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void 打开同名点toolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = MyOpenFileDialog("选择同名点文件", "逗号分割文件(*.csv)|*.csv");
            if (ofd == null) return;

            try
            {
                using (StreamReader sr = new StreamReader(ofd.OpenFile()))
                {
                    string lineData = null;
                    int colCount = sr.ReadLine().Split(',').Length;
                    if (colCount != 8) throw new FormatException("文件格式错误！");

                    _existData.Clear();
                    while ((lineData = sr.ReadLine()) != null)
                    {
                        var eachData = lineData.Split(',');
                        if (eachData.Length != colCount) throw new FormatException("文件格式错误！");

                        var aData = new DataList(int.Parse(eachData[0]),
                            double.Parse(eachData[2]),
                            double.Parse(eachData[1]),
                            double.Parse(eachData[4]),
                            double.Parse(eachData[3]));
                        aData.SetX(double.Parse(eachData[5]));
                        aData.SetY(double.Parse(eachData[6]));
                        aData.SetZ(double.Parse(eachData[7]));

                        _existData.Add(aData);
                    }
                    DrawPicMarkBase();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.Prog_Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                dataExistPoint.DataSource = _existData;
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            var ofd = MyOpenFileDialog("选择目标点文件", "逗号分割文件(*.csv)|*.csv");
            if (ofd == null) return;

            try
            {
                using (StreamReader sr = new StreamReader(ofd.OpenFile()))
                {
                    string lineData = null;
                    int colCount = sr.ReadLine().Split(',').Length;
                    if (colCount != 5) throw new FormatException("文件格式错误！");

                    _targetData.Clear();
                    while ((lineData = sr.ReadLine()) != null)
                    {
                        var eachData = lineData.Split(',');
                        if (eachData.Length != colCount) throw new FormatException("文件格式错误！");

                        var aData = new DataList(int.Parse(eachData[0]),
                            double.Parse(eachData[2]),
                            double.Parse(eachData[1]),
                            double.Parse(eachData[4]),
                            double.Parse(eachData[3]));

                        _targetData.Add(aData);
                    }
                    DrawPicMarkSearch();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.Prog_Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                dataTargetPoint.DataSource = _targetData;
            }
        }

        private void 基准图像BToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = MyOpenFileDialog("选择基准图像", Resources.OpenFileFilter);
            if (ofd == null) return;

            try
            {
                _baseImg = new DigitalImage(ofd.FileName);
                pictureRef.Image = _baseImg.ImgData;
                DrawPicMarkBase();
                pictureRef.Refresh();
                treeViewImg.Nodes[0].Nodes.Clear();
                treeViewImg.Nodes[0].Nodes.Add(ofd.FileName);
                treeViewImg.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.Prog_Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
        }

        private void 搜索图像SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd= MyOpenFileDialog("选择搜索图像", Resources.OpenFileFilter);
            if (ofd == null) return;

            try
            {
                _serImg = new DigitalImage(ofd.FileName);
                pictureMatch.Image = _serImg.ImgData;
                DrawPicMarkSearch();
                pictureMatch.Refresh();
                treeViewImg.Nodes[1].Nodes.Clear();
                treeViewImg.Nodes[1].Nodes.Add(ofd.FileName);
                treeViewImg.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.Prog_Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
        }

    }
}
