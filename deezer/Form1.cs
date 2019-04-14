﻿
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
                    nalezenaAlba.Add(new Album(nalezeneAlbum.id));
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

            button3.Enabled = true;
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
            ZiskejAlba("https://api.deezer.com/search/album?q=artist:\"" + umelec + "\" album:\"" + album + "\"?access_token=", true);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            button3.Enabled = true;
            button3.Text = "search";
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
