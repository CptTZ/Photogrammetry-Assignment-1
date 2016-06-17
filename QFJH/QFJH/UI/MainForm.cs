using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using QFJH.Properties;

namespace QFJH.UI
{
    /// <summary>
    /// 主界面-基础代码放这里
    /// </summary>
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            this.Text = Resources.Prog_Name;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show(Resources.MainForm_ClosingTip, Resources.Prog_Name,
                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }
        
        #region 图像上画十字
        private void DrawPicMarkSearch()
        {
            foreach (var t in _targetData)
            {
                _serImg?.DrawMark((int)t.RightColNumber, (int)t.RightRowNumber, t.PointNumber);
            }
            foreach (var t in _existData)
            {
                _serImg?.DrawMark((int)t.RightColNumber, (int)t.RightRowNumber, t.PointNumber);
            }
            pictureMatch.Image = _serImg?.ImgData;
        }

        private void DrawPicMarkBase()
        {
            foreach (var t in _existData)
            {
                _baseImg?.DrawMark((int)t.LeftColNumber, (int)t.LeftRowNumber, t.PointNumber);
            }
            pictureRef.Image = _baseImg?.ImgData;
        }
        #endregion

        private bool CheckOpen(bool a = false)
        {
            bool isOk = true;

            if (_existData.Count == 0)
                isOk = false;
            else if (_camPara == null)
                isOk = false;
            else if (a)
            {
                if (_targetData.Count == 0)
                    isOk = false;
            }

            if (isOk == false)
            {
                MessageBox.Show("请打开相应数据", Resources.Prog_Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return isOk;
        }

        private OpenFileDialog MyOpenFileDialog(string def, string filter)
        {
            var ofd = new OpenFileDialog
            {
                ValidateNames = true,
                CheckFileExists = true,
                CheckPathExists = true,
                InitialDirectory = @"E:\大学\大三下\摄影测量学\摄影测量学作业\Data",
                Multiselect = false,
                Title = def + " - " + Resources.Prog_Name,
                Filter = filter
            };

            return ofd.ShowDialog() == DialogResult.OK ? ofd : null;
        }

        private void 退出XToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void 关于AToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog();
        }

        /// <summary>
        /// 切换参数信息显示
        /// </summary>
        private void treeViewImg_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var parent = e.Node.Parent;
            if (parent == null)
            {
                textBox8.Text = "";
                return;
            }

            // 0为基准，1为搜索
            switch (parent.Index)
            {
                case 0:
                    textBox8.Text = "基准影像：" + e.Node.Text;
                    break;

                case 1:
                    textBox8.Text = "搜索影像：" + e.Node.Text;
                    break;
            }
        }

        #region 滚动条跳转
        private void dataExistPoint_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int row = e.RowIndex,
                col = e.ColumnIndex;
            if (row < 0) return;
            if (pictureRef.Image == null | _existData.Count == 0) return;

            try
            {
                panelLU.VerticalScroll.Value = (int)_existData[row].LeftRowNumber;
                panelLU.HorizontalScroll.Value = (int)_existData[row].LeftColNumber;
            }
            catch (Exception)
            {
                return;
            }
        }

        private void dataTargetPoint_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int row = e.RowIndex,
                col = e.ColumnIndex;
            if (row < 0) return;
            if (pictureMatch.Image == null | _targetData.Count == 0) return;
            try
            {
                panelRU.VerticalScroll.Value = (int)_targetData[row].RightRowNumber;
                panelRU.HorizontalScroll.Value = (int)_targetData[row].RightColNumber;
            }
            catch (Exception)
            {
                return;
            }
        }
        #endregion

    }
}
