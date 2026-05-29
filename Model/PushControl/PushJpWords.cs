using System;
using System.Collections.Generic;
using ToastFish.Model.SqliteControl;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Speech.Synthesis;
using System.Threading;
using ToastFish.Model.Log;
using ToastFish.Services.Notifications;

namespace ToastFish.Model.PushControl
{
    class PushJpWords : PushWords
    {
        public JpWord GetRandomWord(List<JpWord> WordList)
        {
            Random Rd = new Random();
            int Index = Rd.Next(WordList.Count);
            return WordList[Index];
        }

        public string GetJapaneseVoiceName()
        {
            SpeechSynthesizer synth = new SpeechSynthesizer();
            foreach (InstalledVoice voice in synth.GetInstalledVoices())
            {
                VoiceInfo info = voice.VoiceInfo;
                if (info.Culture.IetfLanguageTag == "ja-JP")
                    return info.Name;
            }
            return "";
        }

        public void PushOneWord(JpWord CurrentWord)
        {
            ToastNotificationManagerCompat.History.Clear();
            string OneLine = CurrentWord.headWord + "  (" + CurrentWord.hiragana + ")";
            if (CurrentWord.Phone != -1)
                OneLine += "  重音：" + CurrentWord.Phone.ToString();
            OneLine += "\n" + CurrentWord.tranCN;
            new ToastContentBuilder()
            .AddText(OneLine)
            .AddText(CurrentWord.pos)

            .AddButton(new ToastButton()
                .SetContent("记住了！")
                .AddArgument("action", "succeed")
                .SetBackgroundActivation())

            .AddButton(new ToastButton()
                .SetContent("暂时跳过..")
                .AddArgument("action", "fail")
                .SetBackgroundActivation())

            .AddButton(new ToastButton()
                .SetContent("发音")
                .AddArgument("action", "voice")
                .SetBackgroundActivation())
            .Show();
        }

        public void PushOneTransQuestion(JpWord CurrentWord, string B, string C)
        {
            string Question = CurrentWord.tranCN;
            string A = CurrentWord.headWord;

            Random Rd = new Random();
            int AnswerIndex = Rd.Next(3);
            QUESTION_CURRENT_RIGHT_ANSWER = AnswerIndex;

            if (AnswerIndex == 0)
            {
                new ToastContentBuilder()
               .AddText("翻译\n" + Question)

               .AddButton(new ToastButton()
                   .SetContent("A." + A)
                   .AddArgument("action", "0")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("B." + B)
                   .AddArgument("action", "1")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("C." + C)
                   .AddArgument("action", "2")
                   .SetBackgroundActivation())

               .Show();
            }
            else if (AnswerIndex == 1)
            {
                new ToastContentBuilder()
                .AddText("翻译\n" + Question)

               .AddButton(new ToastButton()
                   .SetContent("A." + B)
                   .AddArgument("action", "0")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("B." + A)
                   .AddArgument("action", "1")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("C." + C)
                   .AddArgument("action", "2")
                   .SetBackgroundActivation())
               .Show();
            }
            else if (AnswerIndex == 2)
            {
                new ToastContentBuilder()
                .AddText("翻译\n" + Question)

               .AddButton(new ToastButton()
                   .SetContent("A." + C)
                   .AddArgument("action", "0")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("B." + B)
                   .AddArgument("action", "1")
                   .SetBackgroundActivation())

               .AddButton(new ToastButton()
                   .SetContent("C." + A)
                   .AddArgument("action", "2")
                   .SetBackgroundActivation())
               .Show();
            }
        }

        private async Task<string> PushOneTransQuestionAndWaitAsync(
            JpWord currentWord,
            string b,
            string c,
            CancellationToken cancellationToken)
        {
            string question = currentWord.tranCN;
            string a = currentWord.headWord;
            string[] choices = new string[3];

            Random random = new Random();
            int answerIndex = random.Next(3);
            QUESTION_CURRENT_RIGHT_ANSWER = answerIndex;

            if (answerIndex == 0)
            {
                choices[0] = "A." + a;
                choices[1] = "B." + b;
                choices[2] = "C." + c;
            }
            else if (answerIndex == 1)
            {
                choices[0] = "A." + b;
                choices[1] = "B." + a;
                choices[2] = "C." + c;
            }
            else
            {
                choices[0] = "A." + c;
                choices[1] = "B." + b;
                choices[2] = "C." + a;
            }

            return await notificationService.ShowQuestionAndWaitAsync(
                "翻译",
                question,
                choices,
                HotKeytObservable,
                MapPracticeNavigationHotKey,
                cancellationToken);
        }

        private string MapPracticeNavigationHotKey(string events)
        {
            if (events == NotificationAction.Previous)
                return NotificationAction.Previous;
            if (events == NotificationAction.Next)
                return NotificationAction.Next;
            if (events == NotificationAction.Cancel)
                return NotificationAction.Cancel;
            return string.Empty;
        }

        public static new void Recitation(Object Words)
        {
            WordType WordList = (WordType)Words;
            PushJpWords pushJpWords = new PushJpWords();
            Select Query = new Select();
            List<JpWord> RandomList;
            bool ImportFlag = true;

            if (WordList.JpWordList == null)
            {
                RandomList = Query.GetRandomJpWordList((int)WordList.Number);
                ImportFlag = false;
            }
            else
            {
                RandomList = WordList.JpWordList;
            }

            if (RandomList.Count == 0 && ImportFlag == false)
            {
                pushJpWords.PushMessage("好..好像词库里没有单词了，您就是摸鱼之王！");
                return;
            }
            else if (RandomList.Count == 0 && ImportFlag == true)
            {
                return;
            }
            List<JpWord> CopyList = pushJpWords.Clone<JpWord>(RandomList);

            if (ImportFlag == false)
            {
                CreateLog Log = new CreateLog();
                String LogName = "Log\\" + DateTime.Now.ToString().Replace('/', '-').Replace(' ', '_').Replace(':', '-') + "_日语.xlsx";
                Log.OutputExcel(LogName, RandomList, "日语");
            }
            
            JpWord CurrentWord = new JpWord();
            while (CopyList.Count != 0)
            {
                if (WordList.IsCancellationRequested)
                    return;

                if (pushJpWords.WORD_CURRENT_STATUS != 3)
                    CurrentWord = pushJpWords.GetRandomWord(CopyList);
                pushJpWords.PushOneWord(CurrentWord);

                pushJpWords.WORD_CURRENT_STATUS = 2;
                while (pushJpWords.WORD_CURRENT_STATUS == 2)
                {
                    if (WordList.IsCancellationRequested)
                        return;

                    var task = pushJpWords.ProcessToastNotificationRecitation(WordList.CancellationToken);
                    int result = task.Result;
                    if (result == 0)
                    {
                        pushJpWords.WORD_CURRENT_STATUS = 1;
                    }
                    else if (result == -1)
                    {
                        return;
                    }
                    else if (result == 1)
                    {
                        pushJpWords.WORD_CURRENT_STATUS = 0;
                    }
                    else if (result == 2)
                    {
                        pushJpWords.WORD_CURRENT_STATUS = 3;
                        SpeechSynthesizer synth = new SpeechSynthesizer();
                        try
                        {
                            synth.SelectVoice(pushJpWords.GetJapaneseVoiceName());
                        }
                        catch
                        {

                        }
                        synth.SpeakAsync(CurrentWord.hiragana);
                    }
                }
                if (pushJpWords.WORD_CURRENT_STATUS == 1)
                {
                    if (ImportFlag == false)
                    {
                        Query.UpdateWord(CurrentWord.wordRank);
                        Query.UpdateCount();
                    }
                    CopyList.Remove(CurrentWord);
                }
            }
            pushJpWords.PushMessage("背完了！接下来开始测验！");
            Thread.Sleep(3000);


            while (RandomList.Count != 0)
            {
                if (WordList.IsCancellationRequested)
                    return;

                ToastNotificationManagerCompat.History.Clear();
                Thread.Sleep(500);
                CurrentWord = pushJpWords.GetRandomWord(RandomList);
                List<JpWord> FakeWordList = Query.GetRandomJpWords(2);

                pushJpWords.PushOneTransQuestion(CurrentWord, FakeWordList[0].headWord, FakeWordList[1].headWord);

                pushJpWords.QUESTION_CURRENT_STATUS = 2;
                while (pushJpWords.QUESTION_CURRENT_STATUS == 2)
                {
                    if (WordList.IsCancellationRequested)
                        return;

                    var task = pushJpWords.ProcessToastNotificationQuestion(WordList.CancellationToken);
                    int result = task.Result;
                    if (result == 1)
                        pushJpWords.QUESTION_CURRENT_STATUS = 1;
                    else if (result == 0)
                        pushJpWords.QUESTION_CURRENT_STATUS = 0;
                    else if (result == -1)
                        pushJpWords.QUESTION_CURRENT_STATUS = -1;
                }

                if (pushJpWords.QUESTION_CURRENT_STATUS == 1)
                {
                    RandomList.Remove(CurrentWord);
                    Thread.Sleep(500);
                }
                else if (pushJpWords.QUESTION_CURRENT_STATUS == 0)
                {
                    //CopyList.Remove(CurrentWord);
                    new ToastContentBuilder()
                    .AddText("错误 正确答案：" + pushJpWords.AnswerDict[pushJpWords.QUESTION_CURRENT_RIGHT_ANSWER.ToString()] + '.' + CurrentWord.headWord)
                    .Show();
                    Thread.Sleep(3000);
                }
            }

            ToastNotificationManagerCompat.History.Clear();
            pushJpWords.PushMessage("结束了！恭喜！");
        }

        public static new void UnorderWord(Object Num)
        {
            WordType wordType = Num as WordType;
            int Number = wordType == null ? (int)Num : wordType.Number;
            CancellationToken cancellationToken = wordType == null ? CancellationToken.None : wordType.CancellationToken;
            Select Query = new Select();
            PushJpWords pushJpWords = new PushJpWords();
            List<JpWord> TestList = Query.GetRandomJpWords(Number);

            CreateLog Log = new CreateLog();
            String LogName = "Log\\" + DateTime.Now.ToString().Replace('/', '-').Replace(' ', '_').Replace(':', '-') + "_随机日语单词.xlsx";
            Log.OutputExcel(LogName, TestList, "日语");

            JpWord CurrentWord = new JpWord();
            int currentIndex = 0;

            while (TestList.Count != 0)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                ToastNotificationManagerCompat.History.Clear();
                Thread.Sleep(500);
                if (currentIndex < 0)
                    currentIndex = TestList.Count - 1;
                if (currentIndex >= TestList.Count)
                    currentIndex = 0;

                CurrentWord = TestList[currentIndex];
                List<JpWord> FakeWordList = Query.GetRandomJpWords(2);

                string action = pushJpWords.PushOneTransQuestionAndWaitAsync(
                    CurrentWord,
                    FakeWordList[0].headWord,
                    FakeWordList[1].headWord,
                    cancellationToken).Result;

                if (action == NotificationAction.Cancel || action == string.Empty)
                    return;
                if (action == NotificationAction.Previous)
                {
                    currentIndex--;
                    continue;
                }
                if (action == NotificationAction.Next)
                {
                    currentIndex++;
                    continue;
                }

                if (action == pushJpWords.QUESTION_CURRENT_RIGHT_ANSWER.ToString())
                {
                    TestList.RemoveAt(currentIndex);
                    if (currentIndex >= TestList.Count)
                        currentIndex = 0;
                    Thread.Sleep(500);
                }
                else
                {
                    new ToastContentBuilder()
                    .AddText("错误 正确答案：" + pushJpWords.AnswerDict[pushJpWords.QUESTION_CURRENT_RIGHT_ANSWER.ToString()] + '.' + CurrentWord.headWord)
                    .Show();
                    Thread.Sleep(1200);
                    currentIndex++;
                }
            }
            ToastNotificationManagerCompat.History.Clear();
            pushJpWords.PushMessage("结束了！恭喜！");
        }
    }
}
