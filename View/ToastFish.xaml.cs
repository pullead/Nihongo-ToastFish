using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using ToastFish.ViewModel;
using ToastFish.Resources;
using System.Windows.Forms;
using ToastFish.Model.SqliteControl;
using System.Threading;
using ToastFish.Model.Mp3;
using System.Diagnostics;
using ToastFish.Model.PushControl;
using ToastFish.Model.Log;
using System.Speech.Synthesis;
using ToastFish.Model.StartWithWindows;
using System.IO;
using System.Windows.Xps.Packaging;
using System.Windows.Input;
using System.Threading.Tasks;
using ToastFish.Services.Study;

namespace ToastFish
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        
        ToastFishModel Vm = new ToastFishModel();
        Select Se = new Select();
        PushWords pushWords = new PushWords();
        StudySessionController importedContentSession = new StudySessionController();
        StudySessionController legacyStudySession = new StudySessionController();
        StudySessionStateService studyStateService = new StudySessionStateService();
        KeyboardHookHotKey globalHotKeys;
        bool isRestoringMenuSelection = false;
        const string ProjectWebsiteUrl = "https://github.com/pullead/Nihongo-ToastFish";
        Thread thread = new Thread(new ParameterizedThreadStart(PushWords.Recitation));
        Dictionary<string, string> TablelDictionary = new Dictionary<string, string>(){
        {"CET4_1", "四级核心词汇"},{"CET4_3", "四级完整词汇"},{"CET6_1", "六级核心词汇"},
        {"CET6_3", "六级完整词汇"},{"GMAT_3", "GMAT词汇"},{"GRE_2", "GRE词汇"},
        {"IELTS_3", "IELTS词汇"},{"TOEFL_2", "TOEFL词汇"},{"SAT_2", "SAT词汇"},
        {"KaoYan_1", "考研必考词汇"},{"KaoYan_2", "考研完整词汇"},{"Level4_1", "专四真题高频词"},
        {"Level4luan_2", "专四核心词汇"},{"Level8_1", "专八真题高频词"},{"Level8luan_2", "专八核心词汇"},
        {"Goin", "顺序五十音"},{"StdJp_Mid", "标准日本语中级词汇"} };
        // private NotifyIcon _notifyIcon = null;
       //HotKey _hotKey0, _hotKey1, _hotKey2, _hotKey3, _hotKey4;
        public MainWindow()
        {
            if (IsAnotherInstanceRunning())
            {
                System.Windows.Forms.MessageBox.Show(
                    "程序已经在运行了，不能运行两次。\n如果右下角软件已经退出，请在任务管理器中结束 Nihongo ToastFish 任务。",
                    "提示",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                System.Windows.Application.Current.Shutdown();
                return;
            }

            InitializeComponent();
            DataContext = Vm;
            SetNotifyIcon();
            this.Visibility = Visibility.Hidden;
            Se.LoadGlobalConfig();
            ContextMenu();
            globalHotKeys = new KeyboardHookHotKey(OnHotKeyHandler, new[] { "msedge", "MicrosoftEdge" });
            this.Closed += (sender, args) => globalHotKeys?.Dispose();

            // 谜之bug，如果不先播放一段音频，那么什么声音都播不出来。
            // 所以播个没声音的音频先。
            PlayMute();
            ShowStartupStudyStatus();
            //this.WindowState = (WindowState)FormWindowState.Minimized;
        }

        private void OnHotKeyHandler(HotKey hotKey)
        {
            string key = hotKey.Key.ToString();
            Debug.WriteLine("key pressed:" + key);
            bool isCtrlAlt = hotKey.KeyModifiers == (KeyModifier.Ctrl | KeyModifier.Alt);
            if (isCtrlAlt)
            {
                switch (key)
                {
                    case "J":
                        Begin_Click(null, null);
                        return;
                    case "V":
                        ShowImportedContentPreview(ImportedContentStudyMode.Vocabulary, "N5");
                        return;
                    case "G":
                        ShowImportedContentPreview(ImportedContentStudyMode.Grammar, "N5");
                        return;
                    case "E":
                        ShowImportedContentPreview(ImportedContentStudyMode.Example, "N5");
                        return;
                    case "P":
                        PauseStudySessions();
                        return;
                    case "O":
                        ToggleMainWindow();
                        return;
                }
            }

            switch (key)
            {
                case "A":
                    PushWords.HotKeytObservable.raiseEvent("previous");
                    break;
                case "D":
                    PushWords.HotKeytObservable.raiseEvent("next");
                    break;
                case "W":
                    ToastNotificationManagerCompat.History.Clear();
                    PushWords.HotKeytObservable.raiseEvent("cancel");
                    break;
                case "S":
                    PushWords.HotKeytObservable.raiseEvent("add-note");
                    break;
                case "E":
                    if (StudyCardDetailWindow.CloseActiveWindow())
                        break;
                    PushWords.HotKeytObservable.raiseEvent("details");
                    break;
                case "Q":
                    Begin_Click(null, null);
                    break;
                case "D1":
                    PushWords.HotKeytObservable.raiseEvent("1");
                    break;
                case "D2":
                    PushWords.HotKeytObservable.raiseEvent("2");
                    break;
                case "D3":
                    PushWords.HotKeytObservable.raiseEvent("3");
                    break;
                case "D4":
                    PushWords.HotKeytObservable.raiseEvent("4");
                    break;
                case "Oem3":
                    PushWords.HotKeytObservable.raiseEvent("S");
                    break;
                default:
                    break;
            }           
        }

        private bool IsAnotherInstanceRunning()
        {
            //获取当前活动进程的模块名称
            string moduleName = Process.GetCurrentProcess().MainModule.ModuleName;
            //返回指定路径字符串的文件名
            string processName = System.IO.Path.GetFileNameWithoutExtension(moduleName);
            //根据文件名创建进程资源数组
            Process[] processes = Process.GetProcessesByName(processName);
            //如果该数组长度大于1，说明多次运行
            return processes.Length > 1;
        }

        private void SetNotifyIcon()
        {
            Vm.notifyIcon = new NotifyIcon();
            Vm.notifyIcon.Text = "Nihongo ToastFish";
            System.Drawing.Icon icon = IconChika.chika16;

            Vm.notifyIcon.Icon = icon;
            Vm.notifyIcon.Visible = true;
            Vm.notifyIcon.MouseClick += NotifyIconMouseClick;
            Vm.notifyIcon.DoubleClick += Begin_Click;
            //Vm.notifyIcon.DoubleClick += NotifyIconDoubleClick;
        }

        public void PlayMute()
        {
            MUSIC Temp = new MUSIC();
            Temp.FileName = ".\\Resources\\mute.mp3";
            Temp.play();
        }

        private void NotifyIconDoubleClick(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void NotifyIconMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ShowMainWindow();
        }

        private void ToggleMainWindow()
        {
            if (this.IsVisible && this.WindowState != WindowState.Minimized)
            {
                this.Hide();
                this.ShowInTaskbar = false;
                return;
            }

            ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = true;
            this.Topmost = true;
            this.Show();
            this.Activate();
            this.Focus();
            Keyboard.Focus(this);
            this.Topmost = false;
        }

        private void PauseStudySessions()
        {
            importedContentSession.CancelActiveSession();
            legacyStudySession.CancelActiveSession();
            pushWords.PushMessage("已暂停当前学习。");
        }

        #region 托盘右键菜单

        System.Windows.Forms.ToolStripMenuItem Begin = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem ContinueLastStudy = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem Settings = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem SetNumber = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem SetEngType = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem ImportWords = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem SelectBook = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem SelectJpBook = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem RandomTest = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem Notebook = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem BuiltinN5Preview = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem BuiltinN5GrammarPreview = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem BuiltinN5ExamplePreview = new System.Windows.Forms.ToolStripMenuItem();

        System.Windows.Forms.ToolStripMenuItem GotoHtml = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem Start = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();

        System.Windows.Forms.ToolStripMenuItem SetAutoPlay = new System.Windows.Forms.ToolStripMenuItem();
        System.Windows.Forms.ToolStripMenuItem SetAutoLog = new System.Windows.Forms.ToolStripMenuItem();

        private new void ContextMenu()
        {
            ContextMenuStrip Cms = new ContextMenuStrip();

            Vm.notifyIcon.ContextMenuStrip = Cms;


            Begin.Text = "开始学习";
            Begin.Click += new EventHandler(Begin_Click);
            ContinueLastStudy.Text = "继续上次学习";
            ContinueLastStudy.Click += new EventHandler(Begin_Click);
            Settings.Text = "设置";


            SetNumber.Text = "每轮数量";
            SetNumber.Click += new EventHandler(SetNumber_Click);

            SetEngType.Text = "英语发音类型";
            SetEngType.Click += new EventHandler(SetEngType_Click);

            SetAutoPlay.Text="自动播放";
            SetAutoPlay.Click += new EventHandler(AutoPlay_Click);
            if (Select.AUTO_PLAY != 0)
                SetAutoPlay.Checked = true;
            else
                SetAutoPlay.Checked = false;

            SetAutoLog.Text = "自动日志";
            SetAutoLog.Click += new EventHandler(AutoLog_Click);
            if (Select.AUTO_LOG != 0)
                SetAutoLog.Checked = true;
            else
                SetAutoLog.Checked = false;


            ImportWords.Text = "导入自定义内容";
            ImportWords.Click += new EventHandler(ImportWords_Click);

            SelectBook.Text = "旧版英语词库";

            SelectJpBook.Text = "日语学习";

            RandomTest.Text = "练习";
            Notebook.Text = "笔记本";
            Notebook.Click += new EventHandler(Notebook_Click);

            GotoHtml.Text = "帮助";
            GotoHtml.Click += new EventHandler(HowToUse_Click);

            Start.Text = "开机启动";
            Start.Click += new EventHandler(Start_Click);
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Nihongo ToastFish.lnk")))
                Start.Checked = true;
            else
                Start.Checked = false;

            ExitMenuItem.Text = "退出";
            ExitMenuItem.Click += new EventHandler(ExitApp_Click);

            ToolStripItem CET4_1 = new ToolStripMenuItem("四级核心词汇");
            CET4_1.Click += new EventHandler(SelectBook_Click);
            ToolStripItem CET4_3 = new ToolStripMenuItem("四级完整词汇");
            CET4_3.Click += new EventHandler(SelectBook_Click);
            ToolStripItem CET6_1 = new ToolStripMenuItem("六级核心词汇");
            CET6_1.Click += new EventHandler(SelectBook_Click);
            ToolStripItem CET6_3 = new ToolStripMenuItem("六级完整词汇");
            CET6_3.Click += new EventHandler(SelectBook_Click);
            ToolStripItem GMAT_3 = new ToolStripMenuItem("GMAT词汇");
            GMAT_3.Click += new EventHandler(SelectBook_Click);
            ToolStripItem GRE_2 = new ToolStripMenuItem("GRE词汇");
            GRE_2.Click += new EventHandler(SelectBook_Click);
            ToolStripItem IELTS_3 = new ToolStripMenuItem("IELTS词汇");
            IELTS_3.Click += new EventHandler(SelectBook_Click);
            ToolStripItem TOEFL_2 = new ToolStripMenuItem("TOEFL词汇");
            TOEFL_2.Click += new EventHandler(SelectBook_Click);
            ToolStripItem SAT_2 = new ToolStripMenuItem("SAT词汇");
            SAT_2.Click += new EventHandler(SelectBook_Click);
            ToolStripItem KaoYan_1 = new ToolStripMenuItem("考研必考词汇");
            KaoYan_1.Click += new EventHandler(SelectBook_Click);
            ToolStripItem KaoYan_2 = new ToolStripMenuItem("考研完整词汇");
            KaoYan_2.Click += new EventHandler(SelectBook_Click);
            ToolStripItem Level4_1 = new ToolStripMenuItem("专四真题高频词");
            Level4_1.Click += new EventHandler(SelectBook_Click);
            ToolStripItem Level4luan_2 = new ToolStripMenuItem("专四核心词汇");
            Level4luan_2.Click += new EventHandler(SelectBook_Click);
            ToolStripItem Level8_1 = new ToolStripMenuItem("专八真题高频词");
            Level8_1.Click += new EventHandler(SelectBook_Click);
            ToolStripItem Level8luan_2 = new ToolStripMenuItem("专八核心词汇");
            Level8luan_2.Click += new EventHandler(SelectBook_Click);
            ToolStripItem Goin = new ToolStripMenuItem("顺序五十音");
            Goin.Click += new EventHandler(SelectBook_Click);
            ToolStripItem StdJp_Mid = new ToolStripMenuItem("标准日本语中级词汇");
            StdJp_Mid.Click += new EventHandler(SelectBook_Click);
            ToolStripItem RandomWord = new ToolStripMenuItem("英语单词练习");
            RandomWord.Click += new EventHandler(RandomWordTest_Click);
            ToolStripItem RandomGoin = new ToolStripMenuItem("五十音练习");
            RandomGoin.Click += new EventHandler(RandomGoinTest_Click);
            ToolStripItem RandomJpWord = new ToolStripMenuItem("日语单词练习");
            RandomJpWord.Click += new EventHandler(RandomJpWordTest_Click);
            ToolStripMenuItem RandomJpGrammar = new ToolStripMenuItem("日语语法练习");
            AddGrammarPracticeMenus(RandomJpGrammar);
            ToolStripItem Pdf = new ToolStripMenuItem("Star!!");
            Pdf.Click += new EventHandler(OpenPdf_Click);
            ToolStripItem Use = new ToolStripMenuItem("使用说明(必读)");
            Use.Click += new EventHandler(HowToUse_Click);
            ToolStripItem Site = new ToolStripMenuItem("Nihongo ToastFish 项目网站");
            Site.Click += new EventHandler(Site_Click);
            ToolStripItem Shortcuts = new ToolStripMenuItem("快捷方式");
            Shortcuts.Click += new EventHandler(ShortCuts_Click);
            ToolStripItem ResetLearingStatus = new ToolStripMenuItem("重置进度");
            ResetLearingStatus.Click += new EventHandler(ResetLearingStatus_Click);


 



            isRestoringMenuSelection = true;
            try
            {
                if (Select.TABLE_NAME == "CET4_1")
                    CET4_1.PerformClick();
                else if (Select.TABLE_NAME == "CET4_3")
                    CET4_3.PerformClick();
                else if (Select.TABLE_NAME == "CET6_1")
                    CET6_1.PerformClick();
                else if (Select.TABLE_NAME == "CET6_3")
                    CET6_3.PerformClick();
                else if (Select.TABLE_NAME == "GMAT_3")
                    GMAT_3.PerformClick();
                else if (Select.TABLE_NAME == "GRE_2")
                    GRE_2.PerformClick();
                else if (Select.TABLE_NAME == "IELTS_3")
                    IELTS_3.PerformClick();
                else if (Select.TABLE_NAME == "TOEFL_2")
                    TOEFL_2.PerformClick();
                else if (Select.TABLE_NAME == "SAT_2")
                    SAT_2.PerformClick();
                else if (Select.TABLE_NAME == "KaoYan_1")
                    KaoYan_1.PerformClick();
                else if (Select.TABLE_NAME == "KaoYan_2")
                    KaoYan_2.PerformClick();
                else if (Select.TABLE_NAME == "Level4_1")
                    Level4_1.PerformClick();
                else if (Select.TABLE_NAME == "Level4luan_2")
                    Level4luan_2.PerformClick();
                else if (Select.TABLE_NAME == "Level8_1")
                    Level8_1.PerformClick();
                else if (Select.TABLE_NAME == "Level8luan_2")
                    Level8luan_2.PerformClick();
                else if (Select.TABLE_NAME == "Goin")
                    Goin.PerformClick();
                else if (Select.TABLE_NAME == "StdJp_Mid")
                    StdJp_Mid.PerformClick();
            }
            finally
            {
                isRestoringMenuSelection = false;
            }

            Cms.Items.Add(ContinueLastStudy);
            Cms.Items.Add(Begin);
            Cms.Items.Add(SelectJpBook);
            Cms.Items.Add(RandomTest);
            Cms.Items.Add(Notebook);
            Cms.Items.Add(ImportWords);
            Cms.Items.Add(Settings);
            Cms.Items.Add(GotoHtml);
            Cms.Items.Add(SelectBook);
            Cms.Items.Add(Start);
            Cms.Items.Add(ExitMenuItem);

            SelectJpBook.DropDownItems.Add(Goin);
            SelectJpBook.DropDownItems.Add(StdJp_Mid);
            AddBuiltinContentMenus(SelectJpBook);
            RandomTest.DropDownItems.Add(RandomJpWord);
            RandomTest.DropDownItems.Add(RandomJpGrammar);
            RandomTest.DropDownItems.Add(RandomGoin);
            RandomTest.DropDownItems.Add(RandomWord);
            Settings.DropDownItems.Add(SetNumber);
            Settings.DropDownItems.Add(SetEngType);
            Settings.DropDownItems.Add(SetAutoPlay);
            Settings.DropDownItems.Add(SetAutoLog);
            Settings.DropDownItems.Add(ResetLearingStatus);
            SelectBook.DropDownItems.Add(CET4_1);
            SelectBook.DropDownItems.Add(CET4_3);
            SelectBook.DropDownItems.Add(CET6_1);
            SelectBook.DropDownItems.Add(CET6_3);
            SelectBook.DropDownItems.Add(GMAT_3);
            SelectBook.DropDownItems.Add(GRE_2);
            SelectBook.DropDownItems.Add(IELTS_3);
            SelectBook.DropDownItems.Add(TOEFL_2);
            SelectBook.DropDownItems.Add(SAT_2);
            SelectBook.DropDownItems.Add(KaoYan_1);
            SelectBook.DropDownItems.Add(KaoYan_2);
            SelectBook.DropDownItems.Add(Level4_1);
            SelectBook.DropDownItems.Add(Level4luan_2);
            SelectBook.DropDownItems.Add(Level8_1);
            SelectBook.DropDownItems.Add(Level8luan_2);
            
            GotoHtml.DropDownItems.Add(Shortcuts);
            GotoHtml.DropDownItems.Add(Use);
            GotoHtml.DropDownItems.Add(Site);
            GotoHtml.DropDownItems.Add(Pdf);
        }

        private void Begin_Click(object sender, EventArgs e)
        {
            if (!System.IO.Directory.Exists("Log"))  {
                System.IO.Directory.CreateDirectory("Log");
            }

            StudySessionState lastStudy = studyStateService.GetLastStudy(Se.DataBase);
            if (lastStudy.studySource != StudySessionStateService.SourceLegacy)
            {
                ResumeImportedContentStudy();
                return;
            }

            if (!string.IsNullOrWhiteSpace(lastStudy.legacyTableName))
            {
                Select.TABLE_NAME = lastStudy.legacyTableName;
                Se.UpdateBookName(lastStudy.legacyTableName);
            }

            WordType Words = new WordType();
            Words.Number = Select.WORD_NUMBER;

            StartLegacyStudyThread(CreateSelectedStudyStart(), Words);
        }

        private ParameterizedThreadStart CreateSelectedStudyStart()
        {
            if (Select.TABLE_NAME == "Goin")
                return new ParameterizedThreadStart(PushGoinWords.OrderGoin);
            if (Select.TABLE_NAME == "StdJp_Mid")
                return new ParameterizedThreadStart(PushJpWords.Recitation);

            return new ParameterizedThreadStart(PushWords.RecitationSM2);
        }

        private void StartLegacyStudyThread(ParameterizedThreadStart studyStart, WordType words)
        {
            studyStateService.SaveLegacy(Se.DataBase, Select.TABLE_NAME);
            importedContentSession.CancelActiveSession();
            legacyStudySession.CancelActiveSession();
            thread = legacyStudySession.StartThread(token =>
            {
                words.CancellationToken = token;
                studyStart(words);
            });
        }

        private void ResumeImportedContentStudy()
        {
            try
            {
                legacyStudySession.CancelActiveSession();
                Task sessionTask = importedContentSession.RunAsync(async token =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    ImportedContentStudyService studyService = CreateImportedContentStudyService();
                    await studyService.RunSavedStudyAsync(Se.DataBase, token);
                });
                ReportImportedContentSessionFailure(sessionTask, "继续上次学习失败");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    "继续上次学习失败：\n" + ex.Message,
                    "Nihongo ToastFish",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void SetNumber_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(pushWords.SetWordNumber));
            thread.Start();
        }

        private void SetEngType_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(pushWords.SetEngType));
            thread.Start();
        }


        

        private void ImportWords_Click(object sender, EventArgs e)
        {
            OpenFileDialog Dialog = new OpenFileDialog();
            Dialog.Filter = "xlsx files (*.xlsx)|*.xlsx|xls files (*.xls)|*.xls";
            if (Dialog.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
            {
                return;
            }
            String FileName = Dialog.FileName;
            CreateLog Log = new CreateLog();
            WordType Words = new WordType();
            Words.Number = Select.WORD_NUMBER;
            object lstObj = Log.ImportExcel(FileName);
            string typeObj = lstObj.ToString();
            string typeWord= typeof(List<Word>).ToString();
            string typeJpWord = typeof(List<JpWord>).ToString();
            string typeCustWord = typeof(List<CustomizeWord>).ToString();
            try
            {
                if (typeObj == typeWord)
                {
                    Words.WordList = (List<Word>)lstObj;
                    Select.TABLE_NAME = "GRE_2";
                }
                else if (typeObj == typeJpWord)
                {
                    Words.JpWordList = (List<JpWord>)lstObj;
                    Select.TABLE_NAME = "StdJp_Mid";
                }
                else if (typeObj == typeCustWord)
                {
                    Words.CustWordList = (List<CustomizeWord>)lstObj;
                    Select.TABLE_NAME = "自定义";
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("导入文件出错！");
                    return;
                }
            }
            catch {
                System.Windows.Forms.MessageBox.Show("导入文件出错！");
                return;
            }
            
            if (!Directory.Exists("Log")){
                System.IO.Directory.CreateDirectory("Log");
            }
            

            StartLegacyStudyThread(CreateImportedStudyStart(), Words);
        }

        private ParameterizedThreadStart CreateImportedStudyStart()
        {
            if (Select.TABLE_NAME == "Goin")
                return new ParameterizedThreadStart(PushGoinWords.OrderGoin);
            if (Select.TABLE_NAME == "StdJp_Mid")
                return new ParameterizedThreadStart(PushJpWords.Recitation);
            if (Select.TABLE_NAME == "自定义")
                return new ParameterizedThreadStart(PushCustomizeWords.Recitation);

            return new ParameterizedThreadStart(PushWords.Recitation);
        }

        private void SelectBook_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem curitem = sender as ToolStripMenuItem;
            if (curitem != null && curitem.OwnerItem !=null)
            {
                foreach (ToolStripItem itemi in SelectBook.DropDownItems)
                {
                    (itemi as ToolStripMenuItem).Checked = false;
                }
                foreach (ToolStripItem itemi in SelectJpBook.DropDownItems)
                {
                    (itemi as ToolStripMenuItem).Checked = false;
                }
            }
            curitem.Checked = true;
           // (sender as ToolStripMenuItem).Checked = !(sender as ToolStripMenuItem).Checked;
            string TempName = "";
            if (sender.ToString() == "四级核心词汇")
                TempName = "CET4_1";
            else if (sender.ToString() == "四级完整词汇")
                TempName = "CET4_3";
            else if (sender.ToString() == "六级核心词汇")
                TempName = "CET6_1";
            else if (sender.ToString() == "六级完整词汇")
                TempName = "CET6_3";
            else if (sender.ToString() == "GMAT词汇")
                TempName = "GMAT_3";
            else if (sender.ToString() == "GRE词汇")
                TempName = "GRE_2";
            else if (sender.ToString() == "IELTS词汇")
                TempName = "IELTS_3";
            else if (sender.ToString() == "TOEFL词汇")
                TempName = "TOEFL_2";
            else if (sender.ToString() == "SAT词汇")
                TempName = "SAT_2";
            else if (sender.ToString() == "考研必考词汇")
                TempName = "KaoYan_1";
            else if (sender.ToString() == "考研完整词汇")
                TempName = "KaoYan_2";
            else if (sender.ToString() == "专四真题高频词")
                TempName = "Level4_1";
            else if (sender.ToString() == "专四核心词汇")
                TempName = "Level4luan_2";
            else if (sender.ToString() == "专八真题高频词")
                TempName = "Level8_1";
            else if (sender.ToString() == "专八核心词汇")
                TempName = "Level8luan_2";
            else if (sender.ToString() == "顺序五十音")
                TempName = "Goin";
            else if (sender.ToString() == "标准日本语中级词汇")
            {
                TempName = "StdJp_Mid";
                bool Flag = false;
                SpeechSynthesizer synth = new SpeechSynthesizer();
                foreach (InstalledVoice voice in synth.GetInstalledVoices())
                {
                    VoiceInfo info = voice.VoiceInfo;
                    if (info.Culture.IetfLanguageTag == "ja-JP")
                        Flag = true;
                }
                if(Flag == false)
                    System.Windows.Forms.MessageBox.Show("检测到您未安装日语语音包，请去“设置”->“时间和语言”->“语音”->“添加语音”中安装日本语，以免影响正常使用。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (sender.ToString() == "随机五十音测试")
                TempName = "Goin";
            Select.TABLE_NAME = TempName;
            Se.UpdateBookName(TempName);
            if (!isRestoringMenuSelection)
                studyStateService.SaveLegacy(Se.DataBase, TempName);
            Se.UpdateTableCount();
            if (isRestoringMenuSelection)
                return;
            //if (sender.ToString() == "顺序五十音")
            //{
            //     int Progress = Se.GetGoinProgress();
            //     PushWords.PushMessage("当前词库：" + sender.ToString() + "\n当前进度：" + Progress.ToString() + "/104");
            // }
            // else
            //{
            List<int> res = Se.SelectCount();
                pushWords.PushMessage("当前词库：" + sender.ToString() + "\n当前进度：" + res[0].ToString() + "/" + res[1].ToString());
           // }
        }

        private void ShowStartupStudyStatus()
        {
            try
            {
                ImportedContentStudyService studyService = CreateImportedContentStudyService();
                string message = studyService.GetSavedStudyStatusMessage(Se.DataBase);
                pushWords.PushMessage(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ShowStartupStudyStatus failed: " + ex.Message);
            }
        }

        private void RandomWordTest_Click(object sender, EventArgs e)
        {
            if (Select.TABLE_NAME == "StdJp_Mid" || Select.TABLE_NAME == "Goin")
                Select.TABLE_NAME = "GRE_2";

            WordType words = new WordType();
            words.Number = Select.WORD_NUMBER;
            StartLegacyStudyThread(new ParameterizedThreadStart(pushWords.UnorderWord), words);
        }

        private void RandomGoinTest_Click(object sender, EventArgs e)
        {
            Select.TABLE_NAME = "Goin";
            Se.UpdateBookName("Goin");

            WordType words = new WordType();
            words.Number = Select.WORD_NUMBER;
            StartLegacyStudyThread(new ParameterizedThreadStart(PushGoinWords.UnorderGoin), words);
        }

        private void RandomJpWordTest_Click(object sender, EventArgs e)
        {
            Select.TABLE_NAME = "StdJp_Mid";

            WordType words = new WordType();
            words.Number = Select.WORD_NUMBER;
            StartLegacyStudyThread(new ParameterizedThreadStart(PushJpWords.UnorderWord), words);
        }

        private void Notebook_Click(object sender, EventArgs e)
        {
            NotebookWindow window = new NotebookWindow(Se.DataBase);
            if (this.IsVisible)
                window.Owner = this;
            window.Show();
            window.Activate();
        }

        private void AddGrammarPracticeMenus(ToolStripMenuItem parent)
        {
            string[] levels = { "N5", "N4", "N3", "N2", "N1" };
            foreach (string level in levels)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(level + " 语法练习");
                item.Tag = level;
                item.Click += new EventHandler(RandomJpGrammarTest_Click);
                parent.DropDownItems.Add(item);
            }
        }

        private void RandomJpGrammarTest_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            string level = item == null ? "N5" : item.Tag as string;
            ShowGrammarPractice(string.IsNullOrWhiteSpace(level) ? "N5" : level);
        }

        private void ShowGrammarPractice(string jlptLevel)
        {
            try
            {
                legacyStudySession.CancelActiveSession();
                Task sessionTask = importedContentSession.RunAsync(async token =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    ImportedContentStudyService studyService = CreateImportedContentStudyService();
                    await studyService.RunFirstGrammarPracticeAsync(Se.DataBase, jlptLevel, token);
                });
                ReportImportedContentSessionFailure(sessionTask, "日语语法练习失败");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    "日语语法练习失败：\n" + ex.Message,
                    "Nihongo ToastFish",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void AddBuiltinContentMenus(ToolStripMenuItem parent)
        {
            string[] levels = { "N5", "N4", "N3", "N2", "N1" };
            foreach (string level in levels)
            {
                ToolStripMenuItem levelMenu = new ToolStripMenuItem("内置 " + level + " 内容");
                levelMenu.DropDownItems.Add(CreateBuiltinContentMenuItem(level, "词汇预览", ImportedContentStudyMode.Vocabulary));
                levelMenu.DropDownItems.Add(CreateBuiltinContentMenuItem(level, "语法预览", ImportedContentStudyMode.Grammar));
                levelMenu.DropDownItems.Add(CreateBuiltinContentMenuItem(level, "例句预览", ImportedContentStudyMode.Example));
                parent.DropDownItems.Add(levelMenu);
            }
        }

        private ToolStripMenuItem CreateBuiltinContentMenuItem(string level, string text, ImportedContentStudyMode mode)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Tag = mode + "|" + level;
            item.Click += new EventHandler(BuiltinContentMenu_Click);
            return item;
        }

        private void BuiltinContentMenu_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            string tag = item == null ? string.Empty : item.Tag as string;
            string[] parts = string.IsNullOrEmpty(tag) ? new string[0] : tag.Split('|');
            if (parts.Length != 2)
                return;

            ImportedContentStudyMode mode = (ImportedContentStudyMode)Enum.Parse(typeof(ImportedContentStudyMode), parts[0]);
            ShowImportedContentPreview(mode, parts[1]);
        }

        private void BuiltinN5Preview_Click(object sender, EventArgs e)
        {
            ShowImportedContentPreview(ImportedContentStudyMode.Vocabulary, "N5");
        }

        private void BuiltinN5GrammarPreview_Click(object sender, EventArgs e)
        {
            ShowImportedContentPreview(ImportedContentStudyMode.Grammar, "N5");
        }

        private void BuiltinN5ExamplePreview_Click(object sender, EventArgs e)
        {
            ShowImportedContentPreview(ImportedContentStudyMode.Example, "N5");
        }

        private void ShowImportedContentPreview(ImportedContentStudyMode mode, string jlptLevel)
        {
            try
            {
                legacyStudySession.CancelActiveSession();
                Task sessionTask = importedContentSession.RunAsync(async token =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    ImportedContentStudyService studyService = CreateImportedContentStudyService();
                    await studyService.RunFirstStudyAsync(Se.DataBase, mode, jlptLevel, token);
                });
                ReportImportedContentSessionFailure(sessionTask, "内置日语内容预览失败");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    "内置日语内容预览失败：\n" + ex.Message,
                    "Nihongo ToastFish",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void ReportImportedContentSessionFailure(Task sessionTask, string title)
        {
            sessionTask.ContinueWith(task =>
            {
                Exception exception = task.Exception == null ? null : task.Exception.GetBaseException();
                if (exception == null)
                    return;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    System.Windows.Forms.MessageBox.Show(
                        title + "：\n" + exception.Message,
                        "Nihongo ToastFish",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }));
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private ImportedContentStudyService CreateImportedContentStudyService()
        {
            return new ImportedContentStudyService(
                null,
                null,
                null,
                null,
                null,
                null,
                PushWords.HotKeytObservable,
                MapNavigationHotKeyAction);
        }

        private string MapNavigationHotKeyAction(string events)
        {
            if (events == "previous")
                return ToastFish.Services.Notifications.NotificationAction.Previous;
            if (events == "next")
                return ToastFish.Services.Notifications.NotificationAction.Next;
            if (events == "cancel")
                return ToastFish.Services.Notifications.NotificationAction.Cancel;
            if (events == "add-note")
                return ToastFish.Services.Notifications.NotificationAction.AddNote;
            if (events == "details")
                return ToastFish.Services.Notifications.NotificationAction.Details;
            return string.Empty;
        }

        public  void ResetLearingStatus_Click(object sender, EventArgs e)
        {
           string TableName;
           bool isok= TablelDictionary.TryGetValue(Select.TABLE_NAME, out TableName);
            if (!isok) {
                return;
            }
            
            DialogResult result = System.Windows.Forms.MessageBox.Show(
            $"是否要重置“{TableName}”的学习进度?", $"进度重置：{Select.TABLE_NAME}",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result==System.Windows.Forms.DialogResult.Yes)
            {
               
                try {
                    Se.ResetTableCount();
                    pushWords.PushMessage($"重置{TableName}完成！");
                    //System.Windows.Forms.MessageBox.Show($"重置{TableName}完成！");

                }
                catch {
                    pushWords.PushMessage($"重置{TableName}出错！");
                   // System.Windows.Forms.MessageBox.Show($"重置{TableName}出错！");
                }                
            }
            //this.Se. 
        }
        private void ShortCuts_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MessageBox.Show(
                "ALT+Q：继续上次学习\n" +
                "ALT+~：播放当前发音\n" +
                "ALT+1 到 ALT+4：对应点击通知按钮 1 到 4\n" +
                "ALT+A：上一个\n" +
                "ALT+D：下一个\n" +
                "ALT+W：关闭当前弹窗并暂停\n" +
                "ALT+S：添加当前内容到笔记本\n" +
                "ALT+E：打开当前内容详情\n" +
                "CTRL+ALT+J：继续上次学习\n" +
                "CTRL+ALT+V：从 N5 词汇开始\n" +
                "CTRL+ALT+G：从 N5 语法开始\n" +
                "CTRL+ALT+E：从 N5 例句开始\n" +
                "CTRL+ALT+P：暂停当前学习\n" +
                "CTRL+ALT+O：显示/隐藏主窗口",
                "Nihongo ToastFish");
        }

        private void AutoPlay_Click(object sender, EventArgs e)
        {
            //sender as ToolStripMenuItem).Checked = !(sender as ToolStripMenuItem).Checked;
            if (Select.AUTO_PLAY == 0)
            {
                Select.AUTO_PLAY = 1;
                (sender as ToolStripMenuItem).Checked = true;
            }               
            else
            {
                Select.AUTO_PLAY = 0;
                (sender as ToolStripMenuItem).Checked = false;
            }
            Se.UpdateGlobalConfig();
        }

        private void AutoLog_Click(object sender, EventArgs e)
        {
            //(sender as ToolStripMenuItem).Checked = !(sender as ToolStripMenuItem).Checked;
            if (Select.AUTO_LOG == 0)
            {
                Select.AUTO_LOG = 1;
                (sender as ToolStripMenuItem).Checked = true;
            }
            else
            {
                Select.AUTO_LOG = 0;
                (sender as ToolStripMenuItem).Checked = false;
            }
            Se.UpdateGlobalConfig();
        }

        private void HowToUse_Click(object sender, EventArgs e)
        {
            OpenProjectWebsite();
        }

        private void OpenProjectWebsite()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ProjectWebsiteUrl,
                UseShellExecute = true
            });
        }

        private void Site_Click(object sender, EventArgs e)
        {
            OpenProjectWebsite();
        }
        private void OpenPdf_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(".\\Resources\\Star.pdf");
        }
        private void ExitApp_Click(object sender, EventArgs e)
        {
            legacyStudySession.CancelActiveSession();
            importedContentSession.CancelActiveSession();
            ToastNotificationManagerCompat.History.Clear();
            Environment.Exit(0);
        }

        private void Start_Click(object sender, EventArgs e)
        {
            //StartWithWindows.SetMeStart(true);
            String startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Nihongo ToastFish.lnk");
            (sender as ToolStripMenuItem).Checked = !(sender as ToolStripMenuItem).Checked;
            StartWithWindows.CreateShortcut(startupPath);
        }
        #endregion
    }
}

