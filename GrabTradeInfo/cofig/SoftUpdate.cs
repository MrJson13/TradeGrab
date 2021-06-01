using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.IO;
using System.Net;
using System.Xml;

namespace GrabTradeInfo.cofig
{
    /// <summary>
    /// 程序更新 
    /// </summary>
    public class SoftUpdate
    {
        private string download;
        private const string updateUrl = "http://erp.scminghua.cn/File/ZCostZqTander/update.xml";//升级配置的XML文件地址  

        #region 构造函数
        public SoftUpdate() { }

        /// <summary>  
        /// 程序更新  
        /// </summary>  
        /// <param name="file">要更新的文件</param>  
        public SoftUpdate(string file, string softName)
        {
            this.LoadFile = file;
            this.SoftName = softName;
        }
        #endregion

        #region 属性
        private string loadFile;
        private string newVerson;
        private string softName;
        private bool isUpdate;

        /// <summary>  
        /// 或取是否需要更新  
        /// </summary>  
        public bool IsUpdate
        {
            get
            {
                checkUpdate();
                return isUpdate;
            }
        }

        /// <summary>  
        /// 要检查更新的文件  
        /// </summary>  
        public string LoadFile
        {
            get { return loadFile; }
            set { loadFile = value; }
        }

        /// <summary>  
        /// 程序集新版本  
        /// </summary>  
        public string NewVerson
        {
            get { return newVerson; }
        }

        /// <summary>  
        /// 升级的名称  
        /// </summary>  
        public string SoftName
        {
            get { return softName; }
            set { softName = value; }
        }

        #endregion

        /// <summary>  
        /// 检查是否需要更新  
        /// </summary>  
        public void checkUpdate()
        {
            try
            {
                WebClient wc = new WebClient();
                Stream stream = wc.OpenRead(updateUrl);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(stream);
                XmlNode list = xmlDoc.SelectSingleNode("Update");
                foreach (XmlNode node in list)
                {
                    if (node.Name == "Soft" && node.Attributes["Name"].Value.ToLower() == SoftName.ToLower())
                    {
                        foreach (XmlNode xml in node)
                        {
                            if (xml.Name == "Verson")
                                newVerson = xml.InnerText;//拿到最新版本号
                            else
                                download = xml.InnerText;//拿到要更新的文件(这块不用拿的,懒得改了)
                        }
                    }
                }

                Version ver = new Version(newVerson);
                Version verson = Assembly.LoadFrom(loadFile).GetName().Version;
                int tm = verson.CompareTo(ver); //版本号比较
                if (tm >= 0)
                    isUpdate = false;
                else
                    isUpdate = true;
            }
            catch (Exception ex)
            {
                throw new Exception("更新出现错误，请确认网络连接无误后重试！");
            }
        }

        /// <summary>  
        /// 获取要更新的文件  
        /// </summary>  
        /// <returns></returns>  
        public override string ToString()
        {
            return this.loadFile;
        }
    }
}
