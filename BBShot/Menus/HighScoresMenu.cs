using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using System.Windows.Forms;

namespace Popgun.Menus
{
    public class HighScoresMenu : Menu
    {
        public struct Record
        {
            public String Name;
            public int Mode;
            public int Score;
        }

        public static String HighScoresFile = "Highscores.dat";

        public HighScoresMenu()
        {
            Id = "Xml/Menus/GamesList.xml";
        }

        public override void LoadContent()
        {
            if (File.Exists(HighScoresFile))
            {
                // Load the high scores file and add the data to the menu list
                Dictionary<int, List<Record>> scores = new Dictionary<int, List<Record>>();

                using (StreamReader reader = new StreamReader(File.OpenRead(HighScoresFile)))
                {
                    String allData = reader.ReadToEnd();
                    var data = allData.Split(new string[] { ";;" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in data)
                    {
                        var lineData = line.Split(new string[] { "@@" }, StringSplitOptions.RemoveEmptyEntries);
                        if (lineData.Length < 3) // Invalid line
                            continue;

                        try
                        {
                            Record record = new Record
                            {
                                Name = lineData[0],
                                Score = int.Parse(lineData[1]),
                                Mode = int.Parse(lineData[2]),
                            };

                            if (!scores.ContainsKey(record.Mode))
                                scores.Add(record.Mode, new List<Record>());

                            scores[record.Mode].Add(record);
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show("High scores file is corrupted!");
                            File.Delete(HighScoresFile);
                            break;
                        }
                    }
                }

                // Now sort it
                foreach (var kvp in scores)
                {
                    kvp.Value.Sort((r1, r2) =>
                    {
                        return r2.Score - r1.Score;
                    });
                }

                foreach (var kvp in scores)
                {
                    Items.Add(new MenuItem
                    {
                        Image = new Image("", Vector2.Zero, Vector2.One, text: kvp.Key + " Minutes")
                    });

                    for (int i = 0; i < Math.Min(kvp.Value.Count, 5); ++i)
                    {
                        Items.Add(new MenuItem
                        {
                            Image = new Image("", Vector2.Zero, Vector2.One, text: kvp.Value[i].Name + " - " + kvp.Value[i].Score)
                        });
                    }
                }
            }
            else
            {
                Items.Insert(0, new MenuItem
                {
                    Image = new Image("", Vector2.Zero, Vector2.One, text: "No high scores")
                });
            }

            base.LoadContent();
            AlignMenuItems();
        }
    }
}
