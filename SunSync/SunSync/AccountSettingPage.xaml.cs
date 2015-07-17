﻿using System;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using SunSync.Models;
using System.IO;
using System.Threading;
using Qiniu.Util;
using Qiniu.Storage;
using Qiniu.Storage.Model;
namespace SunSync
{
    /// <summary>
    /// Interaction logic for AccountSettingPage.xaml
    /// </summary>
    public partial class AccountSettingPage : Page
    {
        private MainWindow mainWindow;
        public AccountSettingPage(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.loadAccountInfo();
        }

        private void BackToHome_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.mainWindow.GotoHomePage();
        }

        /// <summary>
        /// view my ak & sk button click handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewMyAKSK_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            string myAKSKLink = "https://portal.qiniu.com/setting/key";
            System.Diagnostics.Process.Start(myAKSKLink);
        }

        /// <summary>
        /// load account settings from local file if exists
        /// </summary>
        private void loadAccountInfo()
        {
            string myDocPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string accPath = System.IO.Path.Combine(myDocPath, "qsunbox", "account.json");
            if (File.Exists(accPath))
            {
                string accData = "";
                try
                {
                    using (StreamReader sr = new StreamReader(accPath, Encoding.UTF8))
                    {
                        accData = sr.ReadToEnd();
                    }
                    Account acct = JsonConvert.DeserializeObject<Account>(accData);
                    this.AccessKeyTextBox.Text = acct.AccessKey;
                    this.SecretKeyTextBox.Text = acct.SecretKey;
                }
                catch (Exception)
                {
                    //todo error log
                }
            }
        }

        /// <summary>
        /// save account settings to local file and check the validity of the settings
        /// </summary>
        private void SaveAccountSetting(object accountObj)
        {
            Account account = (Account)accountObj;
            //overwrite memory settings
            SystemConfig.ACCESS_KEY = account.AccessKey;
            SystemConfig.SECRET_KEY = account.SecretKey;

            //write settings to local file
            string accData = JsonConvert.SerializeObject(account);
            string myDocPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string appDir = System.IO.Path.Combine(myDocPath, "qsunbox");
            try
            {
                if (!Directory.Exists(appDir))
                {
                    Directory.CreateDirectory(appDir);
                }
                string accPath = System.IO.Path.Combine(appDir, "account.json");
                using (StreamWriter sw = new StreamWriter(accPath, false, Encoding.UTF8))
                {
                    sw.Write(accData);
                }
            }
            catch (Exception)
            {
                //todo error log

                Dispatcher.Invoke(new Action(delegate
                {
                    this.SettingsErrorTextBlock.Text = "帐号设置写入文件失败";
                }));
            }

            //check ak & sk validity
            Mac mac = new Mac(account.AccessKey, account.SecretKey);
            BucketManager bucketManager = new BucketManager(mac);
            StatResult statResult = bucketManager.stat("NONE_EXIST_BUCKET", "NONE_EXIST_KEY");
            if (statResult.ResponseInfo.isNetworkBroken())
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    this.SettingsErrorTextBlock.Text = "网络故障";
                }));
            }
            else
            {
                if (statResult.ResponseInfo.StatusCode == 401)
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        this.SettingsErrorTextBlock.Text = "AK 或 SK 设置不正确";
                    }));
                }
                else
                {
                    Dispatcher.Invoke(new Action(delegate
                    {

                        this.SettingsErrorTextBlock.Text = "";
                        this.mainWindow.GotoHomePage();
                    }));
                }
            }
        }


        /// <summary>
        /// save account settings button click handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAccountSettings_EventHandler(object sender, System.Windows.RoutedEventArgs e)
        {
            string accessKey = this.AccessKeyTextBox.Text.Trim();
            string secretKey = this.SecretKeyTextBox.Text.Trim();
            Account account = new Account();
            account.AccessKey = accessKey;
            account.SecretKey = secretKey;
            new Thread(new ParameterizedThreadStart(this.SaveAccountSetting)).Start(account);
        }
    }
}
