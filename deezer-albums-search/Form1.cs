
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Net;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;
using BrightIdeasSoftware;
using System.Drawing;
using System.Drawing.Imaging;
using Ookii.Dialogs.Wpf;

namespace deezer
{
    public partial class Form1 : Form
    {
        bool slozkaZParametru;
        string nazevCoveru;

        public Form1()
        {
            slozkaZParametru = false;
            nazevCoveru = "";
            InitializeComponent();
            // prohlížeč
            string[] args = Environment.GetCommandLineArgs();
            if (args != null)
            {
                if (args.Length > 0)
                {
                    if (args.Length > 4)
                    {
                        nazevCoveru = args[4];
                    }
                    if (args.Length > 3)
                    {
                        // cesta
                        string slozka = args[3];
                        if (File.Exists(slozka))
                        {
                            slozka = Path.GetDirectoryName(slozka);
                        }
                        if (Directory.Exists(slozka))
                        {
                            label3.Text = slozka;
                            slozkaZParametru = true;
                        }
                    }
                    if (args.Length > 2)
                    {
                        // umělec
                        textBox2.Text = args[2];
                    }
                    if (args.Length > 1)
                    {
                        // album
                        textBox1.Text = args[1];
                        button3_Click(null, null);
                    }
                }
            }
        }

        List<Album> nalezenaAlba = new List<Album>();

        private void ZiskejAlba(string adresa, bool smaz, bool vsechnyReleasy)
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
                    ZiskejAlba(adresa, smaz, vsechnyReleasy);
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
                // pokud nenajde allbum nebo ep, může vrátit i singl
                string typAlbumu = nalezeneAlbum.record_type.ToLower();
                if (vsechnyReleasy || typAlbumu == "album" || typAlbumu == "ep")
                {
                    // jedná se o album (nikoliv o singl)
                    // přidám nalezené album do seznamu
                    nalezenaAlba.Add(new Album(nalezeneAlbum.id));
                    treeListView1.SetObjects(nalezenaAlba);
                }
            }
            if (!String.IsNullOrEmpty(seznamNalezenychAlb.next))
            {
                // pokud existuje další stránka vyhledávání
                ZiskejAlba(seznamNalezenychAlb.next, false, vsechnyReleasy);
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

            button3.Enabled = true;
            if (!slozkaZParametru)
            {
                string cesta = NactiSoubor("cesta.txt");
                if (Directory.Exists(cesta))
                {
                    label3.Text = cesta.Trim();
                }
            }
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
            ZiskejAlba("https://api.deezer.com/search/album?q=artist:\"" + umelec + "\" album:\"" + album + "\"?access_token=", true, checkBox1.Checked);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            button3.Enabled = true;
            button3.Text = "search";
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

        private void Button2_Click(object sender, EventArgs e)
        {
            VistaFolderBrowserDialog vyberSlozky = new VistaFolderBrowserDialog();
            vyberSlozky.Description = "select directory to download files";
            vyberSlozky.UseDescriptionForTitle = true;

            // nastaví výchozí cestu
            if (Directory.Exists(label3.Text))
            {
                // jedná se o složku
                vyberSlozky.SelectedPath = label3.Text;
            }

            if ((bool)vyberSlozky.ShowDialog())
            {
                label3.Text = vyberSlozky.SelectedPath;
                slozkaZParametru = false;
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            var vybrano = treeListView1.SelectedObjects;
            if (vybrano == null)
            {
                return;
            }
            if (!Directory.Exists(label3.Text))
            {
                Button2_Click(null, null);
            }
            if (!Directory.Exists(label3.Text))
            {
                return;
            }

            int stazeno = 0;

            foreach (var asiAlbum in vybrano)
            {
                if (!(asiAlbum is Album))
                {
                    continue;
                }
                Album album = (Album)asiAlbum;
                string cesta = "";
                if (slozkaZParametru)
                {
                    cesta += "tracklist";
                }
                else
                {
                    cesta += album.Interpret + " - " + album.Datum.Split('-').First() + " " + album.Nazev;
                }
                cesta += ".txt";
                cesta = String.Join("", cesta.Split(Path.GetInvalidFileNameChars()));
                cesta = Path.Combine(label3.Text, cesta);
                string albumVysledek = "";
                foreach (var skladba in album.Skladby)
                {
                    string skladbaVysledek = skladba.Cislo + " " + skladba.Nazev;
                    string inter = skladba.Interpret;
                    if (!String.IsNullOrEmpty(inter))
                    {
                        skladbaVysledek += " (ft. " + inter + ")";
                    }
                    albumVysledek += skladbaVysledek + Environment.NewLine;
                }
                using (FileStream str = new FileStream(cesta, FileMode.Create, FileAccess.Write))
                {
                    using (StreamWriter zapisovacka = new StreamWriter(str))
                    {
                        zapisovacka.Write(albumVysledek);
                    }
                }
                if (File.Exists(cesta))
                {
                    stazeno++;
                }
            }
            toolStripStatusLabel1.Text = "successfully downloaded " + stazeno + " tracklists";
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            var vybrano = treeListView1.SelectedObjects;
            if (vybrano == null)
            {
                return;
            }
            if (!Directory.Exists(label3.Text))
            {
                Button2_Click(null, null);
            }
            if (!Directory.Exists(label3.Text))
            {
                return;
            }

            int stazeno = 0;

            foreach (var asiAlbum in vybrano)
            {
                if (!(asiAlbum is Album))
                {
                    continue;
                }
                Album album = (Album)asiAlbum;
                string cesta = "";
                if (!String.IsNullOrEmpty(nazevCoveru))
                {
                    cesta += nazevCoveru;
                }
                else if (slozkaZParametru)
                {
                    cesta += "cover";
                }
                else
                {
                    cesta += album.Interpret + " - " + album.Datum.Split('-').First() + " " + album.Nazev;
                }
                cesta += ".jpg";
                cesta = String.Join("", cesta.Split(Path.GetInvalidFileNameChars()));
                cesta = Path.Combine(label3.Text, cesta);

                try
                {
                    using (WebClient client = new WebClient())
                    {
                        using (Stream str = client.OpenRead(album.CoverNejvetsi))
                        {
                            Bitmap bitmap = new Bitmap(str);

                            if (bitmap != null)
                            {
                                bitmap.Save(cesta, ImageFormat.Jpeg);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Can't download cover");
                }
                if (File.Exists(cesta))
                {
                    stazeno++;
                }
            }
            toolStripStatusLabel1.Text = "successfully downloaded " + stazeno + " cover(s)";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Directory.Exists(label3.Text) && !slozkaZParametru)
            {
                UlozSoubor("cesta.txt", label3.Text);
            }
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

        private void label3_Click(object sender, EventArgs e)
        {
            string slozka = label3.Text;
            if (Directory.Exists(slozka))
            {
                Process.Start(slozka);
            }
        }
    }
}
