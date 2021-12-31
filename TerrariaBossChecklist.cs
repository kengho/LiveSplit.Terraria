using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace LiveSplit.Terraria {
    public partial class TerrariaBossChecklist : Form {

        private static readonly Dictionary<string, string> BossLookup = new Dictionary<string, string> {
            { "WallofFlesh", "wall_of_flesh" },
            { EBosses.EyeofCthulhu.ToString(), "cthulhuForm" },
            { EBosses.EaterofWorldsBrainofCthulhu.ToString(), "eaterBrainForm" },
            { EBosses.Skeletron.ToString(), "skeletron" },
            { EBosses.QueenBee.ToString(), "queen_bee" },
            { EBosses.KingSlime.ToString(), "king_slime" },
            { EBosses.Plantera.ToString(), "planteraForm" },
            { EBosses.Golem.ToString(), "golem" },
            { EBosses.DukeFishron.ToString(), "duke_fishron" },
            { EBosses.LunaticCultist.ToString(), "lunatic_cultist" },
            { EBosses.MoonLord.ToString(), "moon_lord" },
            { EBosses.EmpressofLight.ToString(), "empress_of_light" },
            { EBosses.QueenSlime.ToString(), "queen_slime" },
            { EBosses.TheDestroyer.ToString(), "the_destroyer" },
            { EBosses.TheTwins.ToString(), "twinsForm" },
            { EBosses.SkeletronPrime.ToString(), "skeletron_prime" },
        };

        private static Dictionary<string, bool> bossesDefeated = new Dictionary<string, bool> {
            { "WallofFlesh", false },
            { EBosses.EyeofCthulhu.ToString(), false },
            { EBosses.EaterofWorldsBrainofCthulhu.ToString(), false },
            { EBosses.Skeletron.ToString(), false },
            { EBosses.QueenBee.ToString(), false },
            { EBosses.KingSlime.ToString(), false },
            { EBosses.Plantera.ToString(), false },
            { EBosses.Golem.ToString(), false },
            { EBosses.DukeFishron.ToString(), false },
            { EBosses.LunaticCultist.ToString(), false },
            { EBosses.MoonLord.ToString(), false },
            { EBosses.EmpressofLight.ToString(), false },
            { EBosses.QueenSlime.ToString(), false },
            { EBosses.TheDestroyer.ToString(), false },
            { EBosses.TheTwins.ToString(), false },
            { EBosses.SkeletronPrime.ToString(), false },
            { EBosses.Deerclops.ToString(), false },
        };

        private const string SiteURL = "https://dryoshiyahu.github.io/terraria-boss-checklist/";

        private bool isRunning = false;

        private HashSet<int> bossOffsets;
        private bool isHardmode;

        public TerrariaBossChecklist() {
            InitializeComponent();
            WebBrowser.Url = new Uri(SiteURL);
        }

        private void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e) {
            HtmlDocument doc = WebBrowser.Document;
            HtmlElement head = doc.GetElementsByTagName("head")[0];
            HtmlElement script = doc.CreateElement("script");
            script.SetAttribute("text", @"
                function checkBoss(name) {
                    document.querySelector('.checklist .boss.' + name).click();
                }

                function resetBosses() {
                    document.querySelectorAll('.checklist .boss').forEach(function(e) {
                        if(e.getAttribute('style') && !e.style.backgroundImage.endsWith('dark.png"")')) {
                            e.click();
                        }
                    });
                }

                function getForm(name) {
                    return document.querySelector('#' + name + ' input:checked').value;
                }
            ");
            head.AppendChild(script);
        }

        private void WebBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e) {
            if(!e.Url.ToString().StartsWith(SiteURL)) {
                e.Cancel = true;
                Process.Start(e.Url.ToString());
            }
        }

        public string Url {
            get {
                string[] url = ((string)WebBrowser.Document.InvokeScript("getQueryUrl")).Split('?');
                return url.Length > 1 ? url[1] : "";
            }
            set {
                WebBrowser.Url = new Uri(SiteURL + (String.IsNullOrEmpty(value) ? "" : "?" + value));
            }
        }

        public void SetRunning(bool value) {
            isRunning = value;
            WebBrowser.Document.InvokeScript("resetBosses");

            // https://stackoverflow.com/questions/1070766/editing-dictionary-values-in-a-foreach-loop?rq=1
            List<string> bosses = new List<string>(bossesDefeated.Keys);
            for(int i = 0; i < bosses.Count; i++) {
                bossesDefeated[bosses[i]] = false;
            }
            UpdateBossesDefeatedFile();

            bossOffsets = new HashSet<int>(TerrariaEnums.AllBosses.Cast<int>());
            isHardmode = false;
        }

        public void Update(TerrariaMemory memory) {
            if(!isRunning) {
                return;
            }

            var bossesDefeatedChanged = false;

            foreach(int offset in bossOffsets.ToArray()) {
                if(memory.IsBossBeaten(offset)) {
                    bossOffsets.Remove(offset);
                    CheckBoss(TerrariaEnums.BossName(offset));
                    if (bossesDefeated[TerrariaEnums.BossName(offset)] == false) {
                        bossesDefeated[TerrariaEnums.BossName(offset)] = true;
                        bossesDefeatedChanged = true;
                    }

                }
            }

            if(!isHardmode && (memory.IsHardmode?.New ?? false)) {
                isHardmode = true;
                CheckBoss("WallofFlesh");
                if(bossesDefeated["WallofFlesh"] == false) {
                    bossesDefeated["WallofFlesh"] = true;
                    bossesDefeatedChanged = true;
                }
            }

            if(bossesDefeatedChanged) {
                UpdateBossesDefeatedFile();
            }
        }

        public void UpdateBossesDefeatedFile() {
            using(StreamWriter file = new StreamWriter(@"_bosses-defeated.csv")) {
                foreach(var entry in bossesDefeated) {
                    file.WriteLine("{0},{1}", entry.Key, entry.Value);
                }
                file.Close();
            }
        }

        public void CheckBoss(string bossName) {
            if(BossLookup.TryGetValue(bossName, out string outName)) {
                if(outName.EndsWith("Form")) {
                    outName = (string)WebBrowser.Document.InvokeScript("getForm", new object[] { outName });
                }
                WebBrowser.Document.InvokeScript("checkBoss", new string[] { outName });
            }
        }
    }
}