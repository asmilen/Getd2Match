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
using System.Xml;
using System.IO;
using System.Globalization;

namespace Getd2Match
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public class Match
        {
            public int id { get; set; }
            public string team_a { get; set; }
            public string team_b { get; set; }
            public string match_time { get; set; }
            public string rate_a { get; set; }
            public string rate_b { get; set; }
            public string name_a { get; set; }
            public string name_b { get; set; }
            public string result { get; set; }
            public string score { get; set; }
            public string league_id { get; set; }
            public int d2top_id { get; set; }
            public string status { get; set; }
            public string title { get; set; }
            public string type { get; set; }
            public string type_a { get; set; }
            public string type_b { get; set; }

            public Match()
            {
                league_id = "1";
                status = "2";
                type_a = "";
                type_b = "";
                name_a = "";
                name_b = "";
            }
        }

        public List<string> Team;

        private void button1_Click(object sender, EventArgs e)
        {
            // Duyet d2top lay tran dau
            int start_group = 50154;
            List<Match> match_group = new List<Match>();
            Team = File.ReadLines(@"C:\Users\Administrator\Desktop\teams.txt").ToList();

            while (start_group < 55350)
            {
                try
                {
                    string url = "http://www.dota2top.cn/match/" + start_group + "/show.do";
                    var match = getMatch(url);
                    match.d2top_id = start_group;
                    match_group.Add(match);
                    int c_group = start_group;
                    while ((match.name_a == match_group[0].name_a) && (match.name_b == match_group[0].name_b))
                    {
                        try
                        {
                            url = "http://www.dota2top.cn/match/" + ++c_group + "/show.do";
                            match = getMatch(url);
                            match.d2top_id = c_group;
                            if ((match.name_a == match_group[0].name_a) && (match.name_b == match_group[0].name_b))
                                match_group.Add(match);
                        }
                        catch (Exception ex)
                        {
                            richTextBox1.Text += c_group + Environment.NewLine;
                            match = new Match();
                        }
                    }
                        
                    //Check xem co phai doto hay k
                    if (checkValidGroup(match_group)) savetoDB(match_group);
                    start_group = c_group;
                    match_group = new List<Match>();
                }
                catch (Exception ex) {
                    richTextBox1.Text += start_group + Environment.NewLine;
                    start_group++;
                }
            }
            richTextBox1.Text += "done" + Environment.NewLine;
        }



        private void savetoDB(List<Match> match_group)
        {
            foreach (var match in match_group)
            {
                // Kiem tra xem co ten trong bang Team chua
                match.team_a = getIDbyName(match.name_a);
                match.team_b = getIDbyName(match.name_b);

                //Lay match type
                match.type = getMatchType(match);

                if (match.type_a.Contains("-")) match.type_b = match.type_a.Replace('-', '+');
                if (match.type_b.Contains("-")) match.type_a = match.type_b.Replace('-', '+');

                // Get match result
                match.result = getMatchResult(match);

                string content = "~" + match.team_a + "~ ~" + match.team_b + "~ ~" + match.match_time + "~ ~" + match.rate_a + "~ ~" + match.rate_b + "~ ~" + match.score 
                    + "~ ~" + match.league_id + "~ ~" + match.d2top_id.ToString() + "~ ~" + match.status + "~ ~" + match.title + "~ ~" + match.type + "~ ~" + match.type_a 
                    + "~ ~" + match.type_b + "~ ~" + match.result + "~";
                File.AppendAllText(@"C:\Users\Administrator\Desktop\matches.txt", content + Environment.NewLine);
            }
        }

        private string getMatchResult(Match match)
        {
            var scores = match.score.Split(':');
            int score_a = 0; int score_b = 0;
            bool flag = Int32.TryParse(scores[0].Trim(), out score_a);
            bool flag1 = false;
            if (scores.Length > 1 ) flag1 = Int32.TryParse(scores[1].Trim(), out score_b);

            if (flag && flag1)
            {
                if (match.type != "handicap")
                {
                    if (score_a > score_b) return match.team_a;
                    if (score_b > score_a) return match.team_b;
                    return "0";
                }
                else
                {
                    float fa;
                    bool temp = float.TryParse(match.type_a.Trim().Substring(1, 4), out fa);
                    if (temp)
                    {
                        if ((fa + score_a) > score_b) return match.team_a;
                        return match.team_b;
                    }
                }
            }
            return "-1";
        }

        private string getMatchType(Match match)
        {
            if (match.type_a.Contains("10Kills")) return "10 kills";
            if (match.type_b.Contains("10Kills")) return "10 kills";
            if (match.type_a.Contains("-")) return "handicap";
            if (match.type_b.Contains("-")) return "handicap";
            if (match.type_a.Contains("1st")) return "fb";
            if (match.type_b.Contains("1st")) return "fb";
            return "normal";
        }

        private string getIDbyName(string name)
        {
            int index = Team.IndexOf(name);
            if (index >= 0)
            {
                return (index+1).ToString();
            }
            else
            {
                Team.Add(name);
                File.AppendAllText(@"C:\Users\Administrator\Desktop\teams.txt", name + Environment.NewLine);
                return (Team.IndexOf(name)+1).ToString();
            }
        }

        private bool checkValidGroup(List<Match> match_group)
        {
            foreach (var match in match_group) {
                if (match.type_a.Contains("10Kills")) return true;
                if (match.type_b.Contains("10Kills")) return true;
            }
            return false;
        }

        private Match getMatch(string url)
        {
            var match = new Match();
            HtmlAgilityPack.HtmlDocument doc = new HtmlWeb().Load(url);

            var node = doc.DocumentNode.SelectSingleNode("//div[@class='rightRate' and @style='float: left']");
            match.rate_b = node.InnerText.Trim();

            node = doc.DocumentNode.SelectSingleNode("//div[@class='leftRate' and @style='float: right']");
            match.rate_a = node.InnerText.Trim();

            var multinode = doc.DocumentNode.SelectNodes("//div[@class='big bold']");
            match.name_a = multinode.ElementAt(0).InnerText.Trim();
            match.name_b = multinode.ElementAt(2).InnerText.Trim();

            node = doc.DocumentNode.SelectSingleNode("//span[@id='oldmatch_0_timespan']");
            match.match_time = "2016-" + node.InnerText.Trim();
            match.match_time = DateTime.ParseExact(match.match_time, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy HH:mm:ss");

            node = doc.DocumentNode.SelectSingleNode("//div[@class='small']");
            match.score = node.InnerText.Trim();

            node = doc.DocumentNode.SelectSingleNode("//span[@class='small']");
            match.title = node.InnerText.Trim();

            // Loc name a thanh name va type
            int index = match.name_a.IndexOf("[");
            if (index >=0)
            {
                match.type_a = match.name_a.Substring(index);
                match.name_a = match.name_a.Substring(0,index-1).Trim();
            }

            index = match.name_b.IndexOf("[");
            if (index >= 0)
            {
                match.type_b = match.name_b.Substring(index);
                match.name_b = match.name_b.Substring(0, index - 1).Trim();
            }

            return match;
        }

        private string file_get_content(string url)
        {
            string sContents = string.Empty;
            if (url.ToLower().IndexOf("http:") > -1)
            { // URL 
                System.Net.WebClient wc = new System.Net.WebClient();
                byte[] response = wc.DownloadData(url);
                sContents = System.Text.Encoding.ASCII.GetString(response);
            }
            else
            {
                // Regular Filename 
                System.IO.StreamReader sr = new System.IO.StreamReader(url);
                sContents = sr.ReadToEnd();
                sr.Close();
            }
            return sContents;
        }
    }
}
