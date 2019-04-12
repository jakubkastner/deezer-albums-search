
using System;
using System.Collections.Generic;

using System.Windows.Forms;
using System.Net;

using Newtonsoft.Json;
using System.Linq;

using Gecko;
using System.IO;
using System.Text;
using System.Diagnostics;
using BrightIdeasSoftware;
using System.Drawing;

namespace deezer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            // prohlížeč
            Xpcom.Initialize("Firefox");
        }
        
        List<Album> nalezenaAlba = new List<Album>();
        string aplikaceID = "307004";
        string aplikaceSecret = "cf579ad333e74cdcf08329c3ad4d0f4c";
        string uzivatelAuth = "";
        string accessToken = "";
        string odkazAccessToken = "";
        string odkazInfoUzivatel = "";

        private void ZiskejAlba(string adresa, bool smaz)
        {
            // získá json soubor alba

            string ziskanyJson;
            using (WebClient klient = new WebClient())
            {
                ziskanyJson = klient.DownloadString(adresa);
            }
            Chyba chybaJson = new Chyba(ziskanyJson);
            if (chybaJson.JeChyba)
            {
                if (chybaJson.Kod == 4)
                {
                    ZiskejAlba(adresa, smaz);
                }
                return;
            }
            // získá seznam nalezených alb
            var seznamNalezenychAlb = JsonConvert.DeserializeObject<AlbumZiskej>(ziskanyJson);
            if (smaz)
            {
                treeListView1.ClearObjects();
                nalezenaAlba.Clear();
            }
            foreach (AlbumInformace nalezeneAlbum in seznamNalezenychAlb.data)
            {
                if (backgroundWorker1.CancellationPending)
                {
                    return;
                }
                if (nalezeneAlbum.record_type.ToLower() == "album")
                {
                    // jedná se o album (nikoliv o singl)
                    // přidám nalezené album do seznamu
                    nalezenaAlba.Add(new Album(nalezeneAlbum.id, accessToken));
                    treeListView1.SetObjects(nalezenaAlba);
                }
            }
            if (!String.IsNullOrEmpty(seznamNalezenychAlb.next))
            {
                // pokud existuje další stránka vyhledávání
                ZiskejAlba(seznamNalezenychAlb.next, false);
            }
        }

        private string OdstranZnaky(string text)
        {
            text.Replace(@"&", "")
                .Replace(".", "")
                .Replace(@"'", "")
                .Replace(@"\", "")
                .Replace("\"", "")
                .Replace(";", "")
                .Replace(":", "")
                .Replace("?", "")
                .Replace("!", "")
                .Trim();
            return text;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // rozbalení položek
            treeListView1.CanExpandGetter = delegate (Object x)
            {
                return (x is Album);
            };

            // přidání podpoložek
            treeListView1.ChildrenGetter = delegate (Object x)
            {
                if (x is Album)
                {
                    return ((Album)x).Skladby;
                }
                throw new ArgumentException("Should be Artist or Album");
            };

            HeaderFormatStyle vzhledNadpisu = new HeaderFormatStyle();
            vzhledNadpisu.SetBackColor(Color.FromArgb(77, 77, 77));
            vzhledNadpisu.SetForeColor(Color.White);
            treeListView1.HeaderFormatStyle = vzhledNadpisu;

            toolStripStatusLabel1.Text = "";
            // načte access token ze souboru
            accessToken = NactiSoubor("access_token.txt").Trim();

            // přihlásí uživatele
            PrihlasUzivatele();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // spuštění vyhledávání interpeta / alba

            if (button3.Text == "stop search")
            {
                backgroundWorker1.CancelAsync();
                button3.Text = "search";
            }
            else
            {
                if (!backgroundWorker1.IsBusy)
                {
                    button3.Text = "stop search";
                    backgroundWorker1.RunWorkerAsync();
                }
                else
                {
                    button3.Text = "stopping searching...";
                    button3.Enabled = false;
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            /// DODĚLAT
            /// nazastavuje se pokud dojde k chybě !!!!!

            if (backgroundWorker1.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            string umelec = OdstranZnaky(textBox1.Text);
            string album = OdstranZnaky(textBox2.Text);

            // získá ba umělce
            ZiskejAlba("https://api.deezer.com/search/album?q=artist:\"" + umelec + "\" album:\"" + album + "\"?access_token=" + accessToken, true);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            button3.Enabled = true;
            button3.Text = "search";
        }

        private void geckoWebBrowser1_DocumentCompleted(object sender, Gecko.Events.GeckoDocumentCompletedEventArgs e)
        {
            string url = "";

            if (geckoWebBrowser1.Url != null)
            {
                // zjistí aktuální adresu
                url = geckoWebBrowser1.Url.AbsoluteUri;
            }

            if (url.Contains("https://www.google.cz/?error_reason=user_denied"))
            {
                // uživatel zrušil přihlášení do aplikace
                MessageBox.Show("You must log in to your application through your Deezer account.");
                geckoWebBrowser1.Navigate("https://connect.deezer.com/oauth/auth.php?app_id=" + aplikaceID + "&redirect_uri=https://google.cz/");
            }
            else if (url.Contains("https://www.google.cz/?code="))
            {
                // získání Auth uživatele
                uzivatelAuth = url.Replace("https://www.google.cz/?code=", "");
                odkazAccessToken = "https://connect.deezer.com/oauth/access_token.php?app_id=" + aplikaceID + "&secret=" + aplikaceSecret + "&code=" + uzivatelAuth;
                geckoWebBrowser1.Navigate(odkazAccessToken);
            }
            else if (url == odkazAccessToken)
            {
                if (String.IsNullOrEmpty(uzivatelAuth))
                {
                    geckoWebBrowser1.Navigate("https://connect.deezer.com/oauth/auth.php?app_id=" + aplikaceID + "&redirect_uri=https://google.cz/");
                    return;
                }
                // získání access tokenu
                accessToken = geckoWebBrowser1.Document.Body.OuterHtml.Replace("<body>", "")
                                                                      .Replace("</body>", "");
                string[] accessTokenInfo = accessToken.Split('&');
                accessToken = accessTokenInfo.First().Replace("access_token=", "");
                string accessTokenPlatnost = accessTokenInfo.Last().Replace("expires=", "");
                PrihlasUzivatele();
            }
            else
            {
                treeListView1.Visible = false;
                geckoWebBrowser1.Visible = true;
            }
        }

        private void PrihlasUzivatele()
        {
            odkazInfoUzivatel = "https://api.deezer.com/user/me?access_token=" + accessToken;
            string ziskanyJson;
            using (WebClient klient = new WebClient())
            {
                ziskanyJson = klient.DownloadString(odkazInfoUzivatel);
            }
            Chyba chybaJson = new Chyba(ziskanyJson, false);
            if (chybaJson.JeChyba)
            {
                uzivatelAuth = "";
                geckoWebBrowser1.Navigate("https://connect.deezer.com/oauth/auth.php?app_id=" + aplikaceID + "&redirect_uri=https://google.cz/");
                return;
            }
            var uzivatel = JsonConvert.DeserializeObject<Uzivatel>(ziskanyJson);
            toolStripStatusLabel1.Text = "User succesfully logged in - " + uzivatel.name + " (" + uzivatel.id.ToString() + ")";
            toolStripStatusLabel1.Tag = uzivatel.link;
            button3.Enabled = true;
            button1.Text = "logout";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UlozSoubor("access_token.txt", accessToken);
        }

        private string NactiSoubor(string cesta)
        {
            // načtení dat ze souboru do comboboxu
            if (!File.Exists(cesta))
            {
                return "";
            }
            else
            {
                using (FileStream streamNacti = new FileStream(cesta, FileMode.Open))
                {
                    using (StreamReader nacti = new StreamReader(streamNacti, Encoding.Default))
                    {
                        return nacti.ReadToEnd();
                    }
                }
            }
        }

        private void UlozSoubor(string cesta, string zapis)
        {
            // uložení souboru
            // -> smaže již existující soubor a nahradí ho novým            
            if (File.Exists(cesta))
            {
                try
                {
                    File.Delete(cesta);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Saving the file");
                    return;
                }
            }
            using (FileStream streamUloz = new FileStream(cesta, FileMode.Append))
            {
                using (StreamWriter uloz = new StreamWriter(streamUloz, Encoding.Default))
                {
                    if (!String.IsNullOrEmpty(zapis))
                    {
                        uloz.WriteLine(zapis);
                    }
                }
            }
        }

        private void geckoWebBrowser1_Navigated(object sender, GeckoNavigatedEventArgs e)
        {

            string url = "";

            // zjistí aktuální adresu
            if (geckoWebBrowser1.Url != null)
            {
                url = geckoWebBrowser1.Url.AbsoluteUri;
            }
            if (url.Contains("https://connect.deezer.com/oauth/auth.php?app_id="))
            {
                treeListView1.Visible = false;
                geckoWebBrowser1.Visible = true;
            }
            else
            {
                treeListView1.Visible = true;
                geckoWebBrowser1.Visible = false;
            }
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            Process.Start(toolStripStatusLabel1.Tag.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "logout")
            {
                button3.Enabled = false;
                uzivatelAuth = "";
                toolStripStatusLabel1.Text = "";
                button1.Text = "login";
            }
            geckoWebBrowser1.Navigate("https://connect.deezer.com/oauth/auth.php?app_id=" + aplikaceID + "&redirect_uri=https://google.cz/");
        }

        private void treeListView1_CellEditFinished(object sender, CellEditEventArgs e)
        {
        }

        private void treeListView1_CellEditFinishing(object sender, CellEditEventArgs e)
        {
            switch (e.Column.AspectName)
            {
                /*case "Interpret":
                    e.RowObject*/
                default:
                    break;
            }
            treeListView1.RefreshObject((object)e.RowObject);
            treeListView1.RefreshItem((OLVListItem)e.ListViewItem);
        }
    }
}
