using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;

namespace GrabTradeInfo
{
    public partial class ChangeVer : Form
    {
        public ChangeVer()
        {
            InitializeComponent();
            blstate = false;
            this.Text = "更新...";
            UpdateDownLoad();
        }
        public string XdPath = Application.StartupPath.ToString();
        WebClient wc = null;
        public delegate void ChangeBarDel(System.Net.DownloadProgressChangedEventArgs e);

        private void UpdateDownLoad()
        {
            wc = new WebClient();
            wc.DownloadProgressChanged += wc_DownloadProgressChanged;
            wc.DownloadFileAsync(new Uri("http://erp.scminghua.cn/File/ZCostZqTander/Ver/MHGrabTradeInfoSetup.msi"), XdPath.Replace("\\bin\\Debug", "") + @"\file\MHGrabTradeInfoSetup.msi");//要下载文件的路径,下载之后的命名
        }
        bool blstate = false;
        //  int index = 0;
        void wc_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            Action act = () =>
            {
                this.progressBar1.Value = e.ProgressPercentage;
                this.label1.Text = e.ProgressPercentage + "%";

            };
            this.Invoke(act);

            if (e.ProgressPercentage == 100 && blstate==false)
            {
                blstate = true;
                //下载完成之后开始覆盖
                string path = XdPath.Replace("\\bin\\Debug", "") + @"\file";
                path +=  @"\MHGrabTradeInfoSetup.msi";
                string s7z = path;
                System.Diagnostics.Process pNew = new System.Diagnostics.Process();
                pNew.StartInfo.FileName = s7z;
                pNew.Start();
                //等待完成
                pNew.WaitForExit();
                FileInfo file = new FileInfo(path);
                if (file.Exists)
                    file.Delete();
                //MessageBox.Show("升级成功");
                this.Hide();
            }
        }

        private void ChangeVer_Load(object sender, EventArgs e)
        {

        }
    }
}
