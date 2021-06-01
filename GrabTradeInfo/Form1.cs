using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;

using System.Collections;
using System.Data.OleDb;
using System.IO;
using System.Reflection;
using System.Configuration;
using Microsoft.Win32;

using System.Diagnostics;
using System.Net;

namespace GrabTradeInfo
{
    public partial class form1 : Form
    {
        public form1()
        {
            InitializeComponent();

            ///checkUpdate();//检查更新
        }
        public string XdPath = Application.StartupPath.ToString();
        /// <summary>
        /// 接口地址
        /// </summary>
        public string ApiUrl = "http://ggzyjy.sc.gov.cn/inteligentsearch/rest/inteligentSearch/getFullTextData";

        public void checkUpdate()
        {
            cofig.SoftUpdate app = new cofig.SoftUpdate(Application.ExecutablePath, "ZqCost");
            try
            {
                if (app.IsUpdate && MessageBox.Show("检查到新版本，是否更新？", "Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    ChangeVer cver = new ChangeVer();
                    cver.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
        private void form1_Load(object sender, EventArgs e)
        {
            label2.Text = "";
            //设置开机启动
            ///Fun_AutoStart(true);

            pager1.PageIndex = 1;
            QueryAllData();

            mytimer.Enabled = true;
            mytimer.Interval = 60000;//执行间隔时间,单位为毫秒;此时时间间隔为60秒
            mytimer.Start();   //定时器开始工作
        }
        /// <summary>
        /// 手动抓取【全部】
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnZQ_Click(object sender, EventArgs e)
        {
            btnZQ.Text = "抓取中...";

            btnZQ.Enabled = false;
            btxExe.Enabled = false;
            btnSearch.Enabled = false;
            ck_Auto.Enabled = false;
            textBox1.Enabled = false;

            //清空数据表
            GrabTradeInfo.Common.SqlData.SelectDataTable("", " truncate table ggzyjy_GrabData ");
            //条件-起始时间
            var beginTime = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd 00:00:00");//抓取1月内的
            var endtime = DateTime.Now.ToString("yyyy-MM-dd 23:59:59");
            //Post参数
            string postString = "{\"token\":\"\",\"pn\":0,\"rn\":999,\"sdt\":\"\",\"edt\":\"\",\"wd\":\"\",\"inc_wd\":\"\",\"exc_wd\":\"\",\"fields\":\"title\",\"cnum\":\"\",\"sort\":\"{'webdate':'0'}\",\"ssort\":\"title\",\"cl\":500,\"terminal\":\"\",\"condition\":[{\"fieldName\":\"categorynum\",\"equal\":\"002008001\",\"notEqual\":null,\"equalList\":null,\"notEqualList\":null,\"isLike\":true,\"likeType\":2}],\"time\":[{\"fieldName\":\"webdate\",\"startTime\":\"" + beginTime + "\",\"endTime\":\"" + endtime + "\"}],\"highlights\":\"\",\"statistics\":null,\"unionCondition\":null,\"accuracy\":\"\",\"noParticiple\":\"0\",\"searchRange\":null,\"isBusiness\":\"1\"}";
            //返回的json字符串
            var jsonStr = sendHttpRequest(ApiUrl, postString);

            Newtonsoft.Json.Linq.JObject jobject =
                (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(jsonStr);
            //var totalcount = jobject["result"]["totalcount"].ToString();//总条数
            //var count = jobject["result"]["records"].Count();//当前返回记录条数
            var records = jobject["result"]["records"];//当前返回记录
            for (int j = records.Count() - 1; j >= 0; j--)
            {
                var flag = Post("http://ggzyjy.sc.gov.cn/" + records[j]["linkurl"].ToString());
                if (flag.Item1)
                {
                    var strId = records[j]["id"].ToString();
                    var strTitle = records[j]["title"].ToString();
                    var strUrl = records[j]["linkurl"].ToString();
                    var strWebDate= records[j]["webdate"].ToString();

                    var strUrlLink = "http://ggzyjy.sc.gov.cn/" + strUrl;

                    string strsql = "insert into ggzyjy_GrabData(id,title,url,content,webdate) "
                        + " values('" + strId + "','" + strTitle + "','" + strUrlLink + "','" + flag.Item2.Replace("'", "''").Trim() + "','" + strWebDate + "')";
                    //判断是否存在重复记录
                    string strsql2 = "select * from ggzyjy_GrabData where id='" + strId + "'";
                    DataTable dt = GrabTradeInfo.Common.SqlData.SelectDataTable("", strsql2);
                    if (dt.Rows.Count == 0)
                    {
                        GrabTradeInfo.Common.SqlData.InsDelUpdData("", strsql);
                    }
                }
            }

            btnZQ.Enabled = true;
            btxExe.Enabled = true;
            btnSearch.Enabled = true;
            ck_Auto.Enabled = true;
            textBox1.Enabled = true;
            btnZQ.Text = "全部抓取...";

            pager1.PageIndex = 1;
            //调用查询全部的方法
            QueryAllData();

            MessageBox.Show("成功导入");
        }
        /// <summary>
        /// 导出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btxExe_Click(object sender, EventArgs e)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory.Replace("\\bin\\Debug", "\\") + "file\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
            string sql = " select grabID,title,url,webdate,createTime from ggzyjy_GrabData where 1=1 ";
            string txttitle = textBox1.Text.Trim();
            if (txttitle != "") {
                sql += " and title like '%" + txttitle + "%' ";
            }
            DataTable myTable = GrabTradeInfo.Common.SqlData.SelectDataTable("", sql);
            if (dt2csv(myTable, path, "四川省公共资源交易信息", "编号,标题,链接,发布时间,抓取时间"))
            {
                MessageBox.Show("导出成功,文件位置:" + path);
            }
            else
            {
                MessageBox.Show("导出失败");
            }
        }
        /// <summary>
        /// 导出报表为Csv
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="strFilePath">物理路径</param>
        /// <param name="tableheader">表头</param>
        /// <param name="columname">字段标题,逗号分隔</param>
        public bool dt2csv(DataTable dt, string strFilePath, string tableheader, string columname)
        {
            try
            {
                string strBufferLine = "";
                StreamWriter strmWriterObj = new StreamWriter(strFilePath, false, System.Text.Encoding.UTF8);
                strmWriterObj.WriteLine(tableheader);
                strmWriterObj.WriteLine(columname);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    strBufferLine = "";
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        if (j > 0)
                            strBufferLine += ",";
                        strBufferLine += dt.Rows[i][j].ToString();
                    }
                    strmWriterObj.WriteLine(strBufferLine);
                }
                strmWriterObj.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// List转DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public DataTable ToDataTable<T>(IEnumerable<T> collection)
        {
            var props = typeof(T).GetProperties();
            var dt = new DataTable();
            dt.Columns.AddRange(props.Select(p => new DataColumn(p.Name, p.PropertyType)).ToArray());
            if (collection.Count() > 0)
            {
                for (int i = 0; i < collection.Count(); i++)
                {
                    ArrayList tempList = new ArrayList();
                    foreach (PropertyInfo pi in props)
                    {
                        object obj = pi.GetValue(collection.ElementAt(i), null);
                        tempList.Add(obj);
                    }
                    object[] array = tempList.ToArray();
                    dt.LoadDataRow(array, true);
                }
            }
            return dt;
        }
        private void QueryAllData()
        {
            try
            {
                string txttitle = textBox1.Text.Trim();
                string sql = " select count(1) from ggzyjy_GrabData where 1=1 ";
                if (txttitle != "")
                {
                    sql += " and title like '%" + txttitle + "%' ";
                }
                pager1.PageSize = 20;
                int[] arr=cofig.config.CountStartEnd(pager1.PageIndex, pager1.PageSize);
                object o = GrabTradeInfo.Common.SqlData.ExecuteDataSql("", sql);
                int total = o==null?0:(int)o;
                pager1.RecordCount = total;
                pager1.Page();

                string sql1 = "SELECT  [grabID],[title],[url],[webdate] ,[createTime] FROM(select *,ROW_NUMBER() over(order by webdate desc) rows from ggzyjy_GrabData where title like '%{0}%') t where rows between " + arr[0] + " and " + arr[1] + " ";
                sql1 = string.Format(sql1, textBox1.Text);
                DataTable myTable = GrabTradeInfo.Common.SqlData.SelectDataTable("", sql1);
                dataGridView1.DataSource = myTable;
                
                //不允许添加行
                dataGridView1.AllowUserToAddRows = false;
                //背景为白色
                dataGridView1.BackgroundColor = Color.White;
                //只允许选中单行
                dataGridView1.MultiSelect = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("查询错误！" + ex.Message);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            pager1.PageIndex = 1;
            QueryAllData();
        }
        /// <summary>
        /// 执行的定时方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mytimer_Tick(object sender, EventArgs e)
        {
            //如果当前时间是1点00分
            DateTime dt = DateTime.Now;
            string[] arrTime= GrabTradeInfo.cofig.config.GetTime(XdPath).Split(':');
            if (dt.Hour == Convert.ToInt32(arrTime[0]) && dt.Minute == Convert.ToInt32(arrTime[1]))
            {
                //执行定时抓取方法
                //条件-起始时间
                var beginTime = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd 00:00:00");//抓取7天内的
                var endtime = DateTime.Now.ToString("yyyy-MM-dd 23:59:59");
                //Post参数
                string postString = "{\"token\":\"\",\"pn\":0,\"rn\":999,\"sdt\":\"\",\"edt\":\"\",\"wd\":\"\",\"inc_wd\":\"\",\"exc_wd\":\"\",\"fields\":\"title\",\"cnum\":\"\",\"sort\":\"{'webdate':'0'}\",\"ssort\":\"title\",\"cl\":500,\"terminal\":\"\",\"condition\":[{\"fieldName\":\"categorynum\",\"equal\":\"002008001\",\"notEqual\":null,\"equalList\":null,\"notEqualList\":null,\"isLike\":true,\"likeType\":2}],\"time\":[{\"fieldName\":\"webdate\",\"startTime\":\"" + beginTime + "\",\"endTime\":\"" + endtime + "\"}],\"highlights\":\"\",\"statistics\":null,\"unionCondition\":null,\"accuracy\":\"\",\"noParticiple\":\"0\",\"searchRange\":null,\"isBusiness\":\"1\"}";
                //返回的json字符串
                var jsonStr = sendHttpRequest(ApiUrl, postString);

                Newtonsoft.Json.Linq.JObject jobject =
                    (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(jsonStr);
                var records = jobject["result"]["records"];//当前返回记录
                for (int j = records.Count() - 1; j >= 0; j--)
                {
                    var flag = Post("http://ggzyjy.sc.gov.cn/" + records[j]["linkurl"].ToString());
                    if (flag.Item1)
                    {
                        var strId = records[j]["id"].ToString();
                        var strTitle = records[j]["title"].ToString();
                        var strUrl = records[j]["linkurl"].ToString();
                        var strWebDate = records[j]["webdate"].ToString();

                        var strUrlLink = "http://ggzyjy.sc.gov.cn/" + strUrl;

                        string strsql = "insert into ggzyjy_GrabData(id,title,url,content,webdate) "
                            + " values('" + strId + "','" + strTitle + "','" + strUrlLink + "','" + flag.Item2.Replace("'", "''").Trim() + "','" + strWebDate + "')";
                        //判断是否存在重复记录
                        string strsql2 = "select * from ggzyjy_GrabData where id='" + strId + "'";
                        DataTable dt2 = GrabTradeInfo.Common.SqlData.SelectDataTable("", strsql2);
                        if (dt2.Rows.Count == 0)
                        {
                            GrabTradeInfo.Common.SqlData.InsDelUpdData("", strsql);
                        }
                    }
                }
                QueryAllData();//查询看看是否新增了数据
            }
        }
        /// <summary>
        /// 窗口关闭 停止定时器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            mytimer.Enabled = false;
            mytimer.Stop();
        }

        /// <summary>
        /// 定时服务开启/关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ck_Auto_CheckedChanged(object sender, EventArgs e)
        {
            if (ck_Auto.Checked == true)
            {
                mytimer.Enabled = true;
                mytimer.Start();
            }
            else
            {
                mytimer.Enabled = false;
                mytimer.Stop();
            }
        }

        /// <summary>
        /// 开机自启
        /// </summary>
        public static void Fun_AutoStart(bool isAutoRun = true)
        {
            try
            {
                string path = Application.ExecutablePath;
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                if (isAutoRun)
                    rk2.SetValue("System Security", path); //rk2.DeleteValue("OIMSServer", false);
                else
                    rk2.DeleteValue("System Security", false);
                rk2.Close();
                rk.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show("开机自动启动服务注册被拒绝!请确认有系统管理员权限!");
            }
        }

        private void pager1_OnPageChanged(object sender, EventArgs e)
        {
            QueryAllData();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "链接" && e.RowIndex >= 0)
            {
                DataGridViewColumn column = dataGridView1.Columns[e.ColumnIndex];
                int row = this.dataGridView1.CurrentRow.Index;
                string projectPath = dataGridView1.Rows[row].Cells["链接"].Value.ToString();
                System.Diagnostics.Process.Start(projectPath);
            }

        }

        private void form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            //关闭退到右小角栏目
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
            this.notifyIcon1.Visible = true;

        }

        private void form1_SizeChanged(object sender, EventArgs e)
        {
          
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //双击打开
            this.notifyIcon1.Visible = true;
            this.Show();
            this.Activate();
            this.WindowState = FormWindowState.Normal;
        }
        #region my-code-Center
        /// <summary>
        /// 跳转到详情页面判断是否符合条件
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        static Tuple<bool, string> Post(string url)
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            htmlWeb.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36";
            htmlWeb.OverrideEncoding = Encoding.GetEncoding("utf-8");//解决中文乱码
            bool flag = false;
            var content = "";
            HtmlAgilityPack.HtmlDocument doc = htmlWeb.Load(url);
            #region 条件取消2021-1-19
            //var htmlNode1 = doc.DocumentNode.SelectSingleNode("//p[@class='MsoNormal'][position()=23]/u[position()=1]");
            //var htmlNode2 = doc.DocumentNode.SelectSingleNode("//p[@class='MsoNormal'][position()=23]/span[position()=9]/font/u");
            //if (htmlNode1 == null)//12-21 龙溪河 位置变了wr
            //{
            //    htmlNode1 = doc.DocumentNode.SelectSingleNode("//p[@class='MsoNormal'][position()=24]/u[position()=1]");
            //    htmlNode2 = doc.DocumentNode.SelectSingleNode("//p[@class='MsoNormal'][position()=24]/span[position()=9]/font/u");
            //}
            //var personNum = Convert.ToInt32(ReplaceHtmlTag(htmlNode1.InnerText.Trim()));
            //var personType = ReplaceHtmlTag(htmlNode2.InnerText.Trim());

            //if (personType == "房屋建筑类" || personType == "市政公用工程类")
            //{
            //    if (personNum <= 3)
            //    {
            //        flag = true;
            //        content = doc.DocumentNode.SelectSingleNode("//div[@id='newsText']").InnerHtml;
            //    }
            //}
            #endregion
            flag = true;
            content = doc.DocumentNode.SelectSingleNode("//div[@id='newsText']").InnerHtml;
            return new Tuple<bool, string>(flag, content);
        }
        /// <summary>
        /// 请求接口
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="auth"></param>
        /// <param name="reqParams"></param>
        /// <returns></returns>
        public static string SendRequest(string url, string method, string auth, string reqParams)
        {
            //这是发送Http请求的函数，可根据自己内部的写法改造
            HttpWebRequest myReq = null;
            HttpWebResponse response = null;
            string result = string.Empty;
            try
            {
                myReq = (HttpWebRequest)WebRequest.Create(url);
                myReq.Method = method;
                myReq.ContentType = "application/json;";
                myReq.KeepAlive = false;
                myReq.ProtocolVersion = HttpVersion.Version10;
                //增加获取身份验证信息
                myReq.UserAgent = "Code Sample Web Client";



                //basic 验证下面这句话不能少
                if (!String.IsNullOrEmpty(auth))
                {
                    myReq.Headers.Add("Authorization", "Basic " + auth);
                }

                if (method == "POST" || method == "PUT")
                {
                    byte[] bs = Encoding.UTF8.GetBytes(reqParams);
                    myReq.ContentLength = bs.Length;
                    using (Stream reqStream = myReq.GetRequestStream())
                    {
                        reqStream.Write(bs, 0, bs.Length);
                        reqStream.Close();
                    }
                }

                response = (HttpWebResponse)myReq.GetResponse();
                HttpStatusCode statusCode = response.StatusCode;
                if (Equals(response.StatusCode, HttpStatusCode.OK))
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpStatusCode errorCode = ((HttpWebResponse)e.Response).StatusCode;
                    string statusDescription = ((HttpWebResponse)e.Response).StatusDescription;
                    using (StreamReader sr = new StreamReader(((HttpWebResponse)e.Response).GetResponseStream(), Encoding.UTF8))
                    {
                        result = sr.ReadToEnd();
                    }
                }
                else
                {
                    result = e.Message;
                }
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
                if (myReq != null)
                {
                    myReq.Abort();
                }
            }
            return result;
        }
        /// <summary>
        /// 使用请求接口
        /// </summary>
        /// <param name="url"></param>
        /// <param name="reqparam"></param>
        /// <returns></returns>
        public static string sendHttpRequest(string url, string reqparam)
        {
            string auth = Base64Encode("key:secret");
            return SendRequest(url, "POST", auth, reqparam);
        }

        private static string Base64Encode(string value)
        {
            byte[] bytes = Encoding.Default.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }
        /// <summary>
        /// 替换html标签
        /// </summary>
        /// <param name="html"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string ReplaceHtmlTag(string html, int length = 0)
        {
            string strText = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
            strText = System.Text.RegularExpressions.Regex.Replace(strText, "&[^;]+;", "");

            if (length > 0 && strText.Length > length)
                return strText.Substring(0, length);

            return strText;
        }
        #endregion
    }
}
