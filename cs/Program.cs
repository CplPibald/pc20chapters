using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace pc20chapters {
static class Program {

    public const string PC20CHAPTERSVERSION = "1.2.0";
    public const string TITLETEXT = "Podcasting 2.0 Chapters Generator by SirBemrose";

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main() {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
} // Program

public class MainForm : Form
{
    public static string saveFileName = null;
    public static List<Chapter> chapters = new List<Chapter>();
    public static Panel chaptersPanel = new Panel();

    Button saveBtn = null;

    public MainForm()
    {
        Text = Program.TITLETEXT;
        KeyPreview = true;

        chaptersPanel.AutoScroll = true;
        chaptersPanel.Location = new Point(0, 40);
        Controls.Add(chaptersPanel);
        Resize += (o, a) => {
            var s = ((Form)o).ClientSize;
            chaptersPanel.Size = new Size(s.Width, s.Height - 40);
        };

        // global buttons
        Util.createButton("Open...", new Point(10, 5), new Size(75, 25), this, (o,a) => load());
        saveBtn = Util.createButton("Save", new Point(95, 5), new Size(75, 25), this, (o,a) => {if (null != saveFileName) {_ = save(saveFileName);}});
        saveBtn.Enabled = false;
        Util.createButton("Save As...", new Point(180, 5), new Size(75, 25), this, (o,a) => save());
        var nchBtn = Util.createButton("New Chapter", new Point(265, 5), new Size(95, 25), this, 
            (o,a) => { var chbx = createChapter(); adjustRows(); chbx.startTimeBox.Focus(); });
        AcceptButton = nchBtn;

        Size = new Size(900, 600);

        //if (file) { load(file); }

    }

    public void load () { 

        var ofd = new OpenFileDialog();
        ofd.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
        ofd.DefaultExt = "json";
        ofd.Filter = "JSON files (*.json)|*.json|All files|*";
        
        if (ofd.ShowDialog() != DialogResult.OK) {
            return;
        }

        _ = load(ofd.FileName); 
    }
    public async Task load (string file) {

        // chapters.Clear();
        chaptersPanel.Controls.Clear();  // TODO: pretty sure this is leaking something

        var serializerOptions = new JsonSerializerOptions {
            Converters = { new ChapterJsonConverter() }
        };

        using System.IO.FileStream fs = System.IO.File.OpenRead(file);
        DocumentWrapper doc = await JsonSerializer.DeserializeAsync<DocumentWrapper>(fs, serializerOptions);

        chapters = doc.chapters;
        foreach (var ch in chapters) {
            var chbx = new ChapterBox(ch);
            MainForm.chaptersPanel.Controls.Add(chbx);
        }

        adjustRows();

        saveFileName = file;
        Text = saveFileName + " - " + Program.TITLETEXT;
        saveBtn.Enabled = true;
    }

    public void save () { 

        var sfd = new System.Windows.Forms.SaveFileDialog();
        sfd.InitialDirectory = System.IO.Directory.GetCurrentDirectory();
        
        if (sfd.ShowDialog() != DialogResult.OK) {
            return;
        }
        _ = save(sfd.FileName);
    }

    public async Task save (string file) {
        // TODO: check for invalid chapters
        //       Ignore if no startTime
        // TODO: sort output by startTime

        var serializerOptions = new JsonSerializerOptions {
            Converters = { new ChapterJsonConverter() }
        };

        var doc = new DocumentWrapper();
        doc.chapters = chapters;
        doc.version = Program.PC20CHAPTERSVERSION;

        using System.IO.FileStream fs = System.IO.File.Create(file);
        await JsonSerializer.SerializeAsync(fs, doc, serializerOptions);
        await fs.DisposeAsync();

        saveFileName = file;
        Text = saveFileName + " - " + Program.TITLETEXT;
        saveBtn.Enabled = true;
    }

    public ChapterBox createChapter() {
        var ch = new Chapter();
        chapters.Add(ch);

        var chbx = new ChapterBox(ch);
        MainForm.chaptersPanel.Controls.Add(chbx);

        return chbx;
    } 

    public static void adjustRows() {
        
        List<Control> clist = new List<Control>();
        clist.AddRange(chaptersPanel.Controls.OfType<Control>());

        var rows = clist.OrderBy(chbx => {
            return ((ChapterBox)chbx).chapter.startTime;
        });
        int rowYpx = 10;
        foreach (var row in rows) {
            int h = ((ChapterBox)row).expanded ? 140 : 65;
            row.Location = new Point(10, rowYpx);
            row.Size = new Size(800, h);
            rowYpx += h;
        }
    }

    public static void removeChapter (ChapterBox chbx) {
        // TODO: this probably leaks event handlers.  Don't care.
        // Unless you have thousands of chapters, you won't run out of memory.
        chaptersPanel.Controls.Remove(chbx);
        chapters.Remove(chbx.chapter);
        adjustRows();
    }
} // MainForm

public class ChapterBox : GroupBox {
    public Button deleteButton;
    public Button dropButton;
    public bool expanded;
    public TextBox startTimeBox;
    public TextBox endTimeBox;
    public TextBox titleBox;
    public TextBox urlBox;
    public TextBox imgBox;
    public CheckBox tocBox;

    public Chapter chapter;

    public ChapterBox (Chapter ch) {
        chapter = ch;
        Location = new Point(10, 10);
        Size = new Size(800, 60);

        var ypx = 10;
        var xpx = 5;
        var boxheight = 25;
        expanded = false;
        deleteButton = Util.createButton("x", new Point(5, 0), new Size(15, 15), this, (o,_) => { MainForm.removeChapter((ChapterBox)(((Button)o).Tag)); }, this);
        dropButton = Util.createButton("v", new Point(5, 25), new Size(15, 15), this, (o,_) => { ((ChapterBox)(((Button)o).Tag)).toggleExpanded(); }, this);

        // Disabling expand functionality, because the form doesn't need that much space right now
        // But leaving the code in because I sweated over it and it works
        dropButton.Visible = false;
        // deleteButton.Visible = false;
       
        Util.createLabel("Start (hh:mm:ss):", new Point(xpx += 20, ypx), new Size(100, boxheight), this);
        startTimeBox = Util.createInput(new Point(xpx += 100, ypx), new Size(50, boxheight), this);
        startTimeBox.LostFocus += (o,a) => { chapter.startTime = validateTimestamp((TextBox)o); MainForm.adjustRows(); };
        startTimeBox.Text = ch.startTime.ToString();

        Util.createLabel("Title:", new Point(xpx += 60, ypx), new Size(40, boxheight), this);
        titleBox = Util.createInput(new Point(xpx += 40, ypx), new Size(240, boxheight), this);
        titleBox.LostFocus += (o,a) => { chapter.title = ((TextBox)o).Text; };
        titleBox.Text = ch.title;

        Util.createLabel("Url:", new Point(xpx += 250, ypx), new Size(35, boxheight), this);
        urlBox = Util.createInput(new Point(xpx += 35, ypx), new Size(240, boxheight), this);
        urlBox.LostFocus += (o,a) => { chapter.url = ((TextBox)o).Text; };
        urlBox.Text = ch.url;

        ypx += boxheight;
        xpx = 5;
        Util.createLabel("End (hh:mm:ss):", new Point(xpx += 20, ypx), new Size(100, boxheight), this);
        endTimeBox = Util.createInput(new Point(xpx += 100, ypx), new Size(50, boxheight), this);
        endTimeBox.LostFocus += (o,a) => { chapter.endTime = validateTimestamp((TextBox)o); };
        endTimeBox.Text = ch.endTime.ToString();

        Util.createLabel("Img:", new Point(xpx += 60, ypx), new Size(40, boxheight), this);
        imgBox = Util.createInput(new Point(xpx += 40, ypx), new Size(240, boxheight), this);
        imgBox.LostFocus += (o,a) => { chapter.img = ((TextBox)o).Text; };
        imgBox.Text = ch.img;

        tocBox = Util.createCheckbox("Toc",  new Point(xpx += 250, ypx), new Size(60, boxheight), this, true);
        tocBox.CheckedChanged += (o,a) => { chapter.toc = ((CheckBox)o).Checked; };
        tocBox.Checked = ch.toc;

    }

    int validateTimestamp(TextBox inputbox) {

        string t = inputbox.Text;

        var hhmmss = new Regex(@"^\d+\:\d+(\:\d+)?$");
        Match m = hhmmss.Match(t);
        if (m.Success) {
            // convert from HH:MM:SS
            int sec = 0;
            foreach (string field in t.Split(':')) {
                sec = sec * 60 + Util.intOrElse(field, 0);
            }
            inputbox.Text = sec.ToString();
            return sec;
        }
        Int32 n = 0;
        inputbox.Text = Int32.TryParse(t, out n) ? n.ToString() : "";
        return n;
    }

    public void toggleExpanded () {
        // TODO: Toggle visibility of any controls that are in bottom half of box
        expanded = !expanded;
        MainForm.adjustRows();
    }

} // ChapterBox

public class Chapter {

    public int startTime { get; set; }
    public int endTime { get; set; }
    public string title { get; set; }
    public string img { get; set; }
    public string url { get; set; }
    public bool toc { get; set; }

    public Chapter () {
        toc = true;
    }
} // Chapter

public class ChapterJsonConverter : JsonConverter<Chapter> {

    public override Chapter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var ch = new Chapter();

        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject) {
                return ch;
            }

            if (reader.TokenType == JsonTokenType.PropertyName) {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName) {
                    case "startTime":
                        ch.startTime = reader.GetInt32();
                        break;
                    case "endTime":
                        ch.endTime = reader.GetInt32();
                        break;
                    case "title":
                        ch.title = reader.GetString();
                        break;
                    case "img":
                        ch.img = reader.GetString();
                        break;
                    case "url":
                        ch.url = reader.GetString();
                        break;
                    case "toc":
                        ch.toc = reader.GetBoolean();
                        break;
                }
            }
        }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Chapter ch, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("startTime", ch.startTime);

        if (ch.endTime > 0) {
            writer.WriteNumber("endTime", ch.endTime);
        }

        if (!String.IsNullOrEmpty(ch.title)) {
            writer.WriteString("title", ch.title);
        }

        if (!String.IsNullOrEmpty(ch.img)) {
            writer.WriteString("img", ch.img);
        }

        if (!String.IsNullOrEmpty(ch.url)) {
            writer.WriteString("url", ch.url);
        }

        if (!ch.toc) {
            writer.WriteBoolean("toc", false);
        }

        writer.WriteEndObject();
    }
} // ChapterJsonConverter

public class DocumentWrapper {

    public string version { get; set; }
    public List<Chapter> chapters { get; set; }

} // DocumentWrapper

public class Util {
    public static Button createButton (string label, Point pos, Size size, Control parent, EventHandler clickHandler, Object tag = null) {
        Button btn = new System.Windows.Forms.Button();
        btn.Text = label;
        btn.Location = pos;
        btn.Size = size;
        btn.Click += clickHandler;
        btn.Tag = tag;
        parent.Controls.Add(btn);
        return btn;
    }

    public static Label createLabel (string text, Point pos, Size size, Control parent) {
        Label t = new System.Windows.Forms.Label();
        t.Text = text;
        t.Location = pos;
        //t.TextAlign = align;
        t.AutoSize = false;
        t.Size = size;
        parent.Controls.Add(t);
        return t;
    }

    public static TextBox createInput (Point pos, Size size, Control parent, Object tag = null) {
        TextBox t = new System.Windows.Forms.TextBox();
        t.Location = pos;
        t.Size = size;
        t.Tag = tag;
        parent.Controls.Add(t);
        return t;
    }

    public static CheckBox createCheckbox(string label, Point pos, Size size, Control parent, bool value = false) {
        CheckBox btn = new System.Windows.Forms.CheckBox();
        btn.Text = label;
        btn.Location = pos;
        btn.Size = size;
        btn.Checked = value;
        parent.Controls.Add(btn);
        return btn;
    }

    public static int intOrElse(string s, int def) {
        Int32 n;
        return Int32.TryParse(s, out n) ? n : 0;
    }
} // Util

} // namespace
