using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RDEditor
{
    public partial class RDEditorMain : Form
    {
        public RDEditorMain()
        {
            InitializeComponent();
            toolStrip1.Renderer = new ToolStripStripeRemoval();
            menuStrip1.Renderer = new SelectionRenderer();
            Editor.KeyDown += Editor_KeyDown; // UNDO REDO STACK
            Editor.DragDrop += new DragEventHandler(Editor_DragDrop); // DRAG DROP
            Editor.AllowDrop = true; // DRAG ROP
            args = Environment.GetCommandLineArgs();
        }

        // ********************************************************************** RENDERING
        // TOOLSTRIP STRIPE REMOVAL
        public class ToolStripStripeRemoval : ToolStripSystemRenderer
        {
            public ToolStripStripeRemoval() { }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                // ToolStripStripeRemoval
            }
        }

        // MENUSTRIP RENDERER
        private class SelectionRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs myMenu)
            {
                if (!myMenu.Item.Selected)
                    base.OnRenderMenuItemBackground(myMenu);
                else
                {
                    Rectangle menuRectangle = new Rectangle(Point.Empty, myMenu.Item.Size);
                    myMenu.Graphics.FillRectangle(Brushes.DodgerBlue, menuRectangle);
                    myMenu.Graphics.DrawRectangle(Pens.DeepSkyBlue, 1, 0, menuRectangle.Width - 2, menuRectangle.Height - 1);
                }
            }
        }


        // ********************************************************************** ONLOAD
        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("C:\\Program Files\\RDSoft\\RDExplorer.exe"))
            {
                toolStripSeparator5.Visible = true;
                RDExplorer.Visible = true;
            }
            if (args.Length > 1)
            {
                Editor.Clear();
                using (StreamReader sr = new StreamReader(args[1]))
                {
                    Editor.Text = sr.ReadToEnd();
                    sr.Close();
                }
                RDEditor.Properties.Settings.Default.LastFileOpened = args[1];
                tabPage1.Text = Path.GetFileName(args[1]);
                OriginalText = true;
                SetSyntax.Enabled = true;
            }
            else if (RDEditor.Properties.Settings.Default.AutoOpenLastFile == "ON")
            {
                if (RDEditor.Properties.Settings.Default.LastFileOpened != null && !string.IsNullOrWhiteSpace(RDEditor.Properties.Settings.Default.LastFileOpened))
                {
                    if (File.Exists(RDEditor.Properties.Settings.Default.LastFileOpened))
                    {
                        Editor.Clear();
                        using (StreamReader sr = new StreamReader(RDEditor.Properties.Settings.Default.LastFileOpened))
                        {
                            Editor.Text = sr.ReadToEnd();
                            sr.Close();
                        }
                        tabPage1.Text = Path.GetFileName(RDEditor.Properties.Settings.Default.LastFileOpened);
                        OriginalText = true;
                        SetSyntax.Enabled = true;
                    }
                }
            }
            else
            {
                RDEditor.Properties.Settings.Default.LastFileOpened = "";
            }
            if (RDEditor.Properties.Settings.Default.AutoTextSize == "ON")
            {
                Editor.Font = new System.Drawing.Font("Consolas", RDEditor.Properties.Settings.Default.TextSize);
                FontSize = RDEditor.Properties.Settings.Default.TextSize;
            }
            if (RDEditor.Properties.Settings.Default.WordWrap == "ON")
            {
                Editor.WordWrap = true;
            }
            else
            {
                Editor.WordWrap = false;
            }
            RDEditor.Properties.Settings.Default.Save();
            Editor.Focus();
        }

        // ********************************************************************** DECLARE STATIC VARIABLES BOOLEANS AND STACKS
        public static string Syntax { get; private set; }
        public string[] args;
        public string Run;
        public int FontSize = 10;
        public int line = 0;
        public int column = 0;
        public bool OriginalText = true;
        public bool UndoRedoClick = false;
        public bool PreviosTextState = false;
        public static bool isCharClosed = false;
        Stack<Func<object>> undoStack = new Stack<Func<object>>();
        Stack<Func<object>> redoStack = new Stack<Func<object>>();

        // ********************************************************************** UNDO/REDO STACK
        private void StackPush(object sender, Stack<Func<object>> stack)
        {
            if (UndoRedoClick != true)
            {
                RichTextBox RichTextBox = (RichTextBox)sender;
                var tBT = RichTextBox.Text(Editor.Text, Editor.SelectionStart);
                stack.Push(tBT);
            }
            UndoRedoClick = false;
        }

        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            // HOTKEY ZOOM IN AND OUT
            if (e.Control && e.KeyCode == Keys.Oemplus)
            {
                if (FontSize < 50)
                {
                    int originalIndex = Editor.SelectionStart;
                    int originalLength = Editor.SelectionLength;
                    FontSize = FontSize + 1;
                    Editor.SelectAll();
                    this.Editor.SelectionFont = new Font("Consolas", FontSize);
                    Editor.SelectionStart = originalIndex;
                    Editor.SelectionLength = originalLength;
                    TextSizeLabel.Text = "Text size: " + FontSize.ToString();
                    TextSizeLabel.Visible = true;
                    TextSizeShow.Enabled = true;
                    RDEditor.Properties.Settings.Default.TextSize = FontSize;
                    RDEditor.Properties.Settings.Default.Save();
                }
            }
            if (e.Control && e.KeyCode == Keys.OemMinus)
            {
                if (FontSize > 2)
                {
                    int originalIndex = Editor.SelectionStart;
                    int originalLength = Editor.SelectionLength;
                    FontSize = FontSize - 1;
                    Editor.SelectAll();
                    this.Editor.SelectionFont = new Font("Consolas", FontSize);
                    Editor.SelectionStart = originalIndex;
                    Editor.SelectionLength = originalLength;
                    TextSizeLabel.Text = "Text size: " + FontSize.ToString();
                    TextSizeLabel.Visible = true;
                    TextSizeShow.Enabled = true;
                    RDEditor.Properties.Settings.Default.TextSize = FontSize;
                    RDEditor.Properties.Settings.Default.Save();
                }
            }
            if (RDEditor.Properties.Settings.Default.AutoIndention == "ON")
            {
                int sel = Editor.SelectionStart;
                if (e.KeyCode == Keys.Enter)
                {
                    if (isCharClosed == true)
                    {
                        isCharClosed = false;
                    }
                }
            }

            // UNDO/REDO
            if (e.KeyCode == Keys.ControlKey && ModifierKeys == Keys.Control) { }
            else if (e.KeyCode == Keys.Z && ModifierKeys == Keys.Control)
            {
                if (undoStack.Count > 0)
                {
                    StackPush(sender, redoStack);
                    undoStack.Pop()();
                }
            }
            else if (e.KeyCode == Keys.Y && ModifierKeys == Keys.Control)
            {
                if (redoStack.Count > 0)
                {
                    StackPush(sender, undoStack);
                    redoStack.Pop()();
                }
            }
            else
            {
                redoStack.Clear();
                StackPush(sender, undoStack);
            }
        }


        // AUTO INDENTION
        private void Editor_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (RDEditor.Properties.Settings.Default.AutoIndention == "ON")
            {
                String s = e.KeyChar.ToString();
                int sel = Editor.SelectionStart;
                switch (s)
                {
                    case "(":
                        Editor.Text = Editor.Text.Insert(sel, "()");
                        e.Handled = true;
                        Editor.SelectionStart = sel + 1;
                        break;

                    case "{":
                        String t = "{}";
                        Editor.Text = Editor.Text.Insert(sel, t);
                        e.Handled = true;
                        Editor.SelectionStart = sel + t.Length - 1;
                        isCharClosed = true;
                        break;

                    case "[":
                        Editor.Text = Editor.Text.Insert(sel, "[]");
                        e.Handled = true;
                        Editor.SelectionStart = sel + 1;
                        break;

                    case "<":
                        Editor.Text = Editor.Text.Insert(sel, "<>");
                        e.Handled = true;
                        Editor.SelectionStart = sel + 1;
                        break;

                    case "\"":
                        Editor.Text = Editor.Text.Insert(sel, "\"\"");
                        e.Handled = true;
                        Editor.SelectionStart = sel + 1;
                        break;

                    case "'":
                        Editor.Text = Editor.Text.Insert(sel, "''");
                        e.Handled = true;
                        Editor.SelectionStart = sel + 1;
                        break;
                }
            }
        }

        // AUTOSAVE
        private void Editor_KeyUp(object sender, KeyEventArgs e)
        {
            if (RDEditor.Properties.Settings.Default.AutoSave == "OFF")
            {
                saveToolStripMenuItem.PerformClick();
            }
        }

        // ********************************************************************** CURSOR MAP
        private void CursorMap_Tick(object sender, EventArgs e)
        {
            line = 1 + Editor.GetLineFromCharIndex(Editor.GetFirstCharIndexOfCurrentLine());
            column = 1 + Editor.SelectionStart - Editor.GetFirstCharIndexOfCurrentLine();
            LineAndColumn.Text = "line: " + line.ToString() + " | column: " + column.ToString();
        }

        // ********************************************************************** TEXT CHANGED
        private void Editor_TextChanged(object sender, EventArgs e)
        {
            OriginalText = false;
        }

        // ********************************************************************** SEARCH
        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Search.Focus();
        }
        private void Search_TextChanged(object sender, EventArgs e)
        {
            string keywords = Search.Text;
            MatchCollection keywordMatches = Regex.Matches(Editor.Text, keywords);

            int originalIndex = Editor.SelectionStart;
            int originalLength = Editor.SelectionLength;

            // Stops blinking
            Search.Focus();

            Editor.SelectionStart = 0;
            Editor.SelectionLength = Editor.Text.Length;
            Editor.SelectionBackColor = Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));

            foreach (Match m in keywordMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.White;
                Editor.SelectionBackColor = Color.DodgerBlue;
            }

            Editor.SelectionStart = originalIndex;
            Editor.SelectionLength = originalLength;
            Editor.SelectionBackColor = Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
        }
        private void findAndReplaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Find.Focus();
        }
        private void ReplaceButton_Click(object sender, EventArgs e)
        {
            if (Find.Text != null && !string.IsNullOrWhiteSpace(Find.Text) && Replace.Text != null && !string.IsNullOrWhiteSpace(Replace.Text))
            {
                Editor.Text = Editor.Text.Replace(Find.Text, Replace.Text);
                Find.Text = "";
                Replace.Text = "";
            }
        }
        private void jumpToTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Editor.SelectionStart = 0;
            Editor.ScrollToCaret();
        }
        private void jumpToBottomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Editor.SelectionStart = Editor.Text.Length;
            Editor.ScrollToCaret();
        }

        // ********************************************************************** FILE
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (OriginalText == false)
            {
                Editor.Clear();
            }
        }
        private void NewToolStripButton_Click(object sender, EventArgs e)
        {
            newToolStripMenuItem.PerformClick();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            openfile.Title = "Open a file..";
            if (openfile.ShowDialog() == DialogResult.OK)
            {
                Editor.Clear();
                using (StreamReader sr = new StreamReader(openfile.FileName))
                {
                    Editor.Text = sr.ReadToEnd();
                    sr.Close();
                }
            }

            RDEditor.Properties.Settings.Default.LastFileOpened = openfile.FileName;
            RDEditor.Properties.Settings.Default.Save();

            tabPage1.Text = Path.GetFileName(RDEditor.Properties.Settings.Default.LastFileOpened);
            OriginalText = true;
            SetSyntax.Enabled = true;
        }
        private void OpenToolStripButton_Click(object sender, EventArgs e)
        {
            openToolStripMenuItem.PerformClick();
        }
        private void Editor_DragDrop(object sender, DragEventArgs e)
        {
            object FileName = e.Data.GetData("FileDrop");

            if (OriginalText == false)
            {
                DialogResult dialogResult = MessageBox.Show("Save the file before exiting?", "Save file", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    saveToolStripMenuItem.PerformClick();
                    Dispose(true);
                    Application.Exit();
                }
            }
            if (FileName != null)
            {
                var list = FileName as string[];

                if (list != null && !string.IsNullOrWhiteSpace(list[0]))
                {
                    Editor.Clear();
                    Editor.Text = File.OpenText(list[0]).ReadToEnd();
                }
                RDEditor.Properties.Settings.Default.LastFileOpened = list[0];
                RDEditor.Properties.Settings.Default.Save();

                tabPage1.Text = Path.GetFileName(RDEditor.Properties.Settings.Default.LastFileOpened);
                OriginalText = true;
                SetSyntax.Enabled = true;

            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists(RDEditor.Properties.Settings.Default.LastFileOpened))
            {
                if (OriginalText == false)
                {
                    StreamWriter txtoutput = new StreamWriter(RDEditor.Properties.Settings.Default.LastFileOpened);
                    txtoutput.Write(Editor.Text);
                    txtoutput.Close();
                    OriginalText = true;
                }
            }
            else
            {
                saveAsToolStripMenuItem.PerformClick();
            }
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog savefile = new SaveFileDialog();
            savefile.Title = "Save file as..";
            if (savefile.ShowDialog() == DialogResult.OK)
            {
                StreamWriter txtoutput = new StreamWriter(savefile.FileName);
                txtoutput.Write(Editor.Text);
                txtoutput.Close();
                RDEditor.Properties.Settings.Default.LastFileOpened = savefile.FileName;
                RDEditor.Properties.Settings.Default.Save();
                tabPage1.Text = Path.GetFileName(RDEditor.Properties.Settings.Default.LastFileOpened);
                OriginalText = true;
            }
        }
        private void SaveToolStripButton_Click(object sender, EventArgs e)
        {
            saveToolStripMenuItem.PerformClick();
        }

        private void PrintDocumentOnPrintPage(object sender, PrintPageEventArgs e)
        {
            e.Graphics.DrawString(this.Editor.Text, this.Editor.Font, Brushes.Black, 10, 25);
        }
        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrintDocument printDocument = new PrintDocument();
            printDocument.PrintPage += PrintDocumentOnPrintPage;
            printDocument.Print();
        }
        private void PrintToolStripButton_Click(object sender, EventArgs e)
        {
            printToolStripMenuItem.PerformClick();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (OriginalText == false)
            {
                DialogResult dialogResult = MessageBox.Show("Save the file before exiting?", "Save file", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    saveToolStripMenuItem.PerformClick();
                    Application.Exit();
                }
                else
                {
                    Application.Exit();
                }
            }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (OriginalText == false)
            {
                DialogResult dialogResult = MessageBox.Show("Save the file before exiting?", "Save file", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                base.OnFormClosing(e);
                if (dialogResult == DialogResult.Yes)
                {
                    saveToolStripMenuItem.PerformClick();
                    Dispose(true);
                    Application.Exit();
                }
                else
                {
                    Dispose(true);
                    Application.Exit();
                }
            }
        }


        // ********************************************************************** SYNTAX
        private void SetSyntax_Tick(object sender, EventArgs e)
        {
            // Language objects
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".bat") BatchLanguage.PerformClick();
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".c") CLanguage.PerformClick();
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".cs") CSharpLanguage.PerformClick();
            if (Path.GetFileName(RDEditor.Properties.Settings.Default.LastFileOpened) == "updater-script") EdifyLanguage.PerformClick();
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".java") JavaLanguage.PerformClick();
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".prop") PropLanguage.PerformClick();
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".sth") ShatchLanguage.PerformClick();
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".sh") ShellLanguage.PerformClick();
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".smali") SmaliLanguage.PerformClick();
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".xml") CLanguage.PerformClick();

            // other known file types
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".conf") PropLanguage.PerformClick();
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".plist") XmlLanguage.PerformClick();
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".sln") CSharpLanguage.PerformClick();

            SetSyntax.Enabled = false;
        }

        private void BatchLanguage_Click(object sender, EventArgs e)
        {
            PreviosTextState = OriginalText;
            string keywords = @"\b(assoc|attrib|break|bcdedit|cacls|chcp|chdir|chkdks|chkntfs|cls|cmd|color|comp|compact|convert|date|dir|diskpart|doskey|drivequery|endlocal|erase|fc|find|findstr|format|fsutil|ftype|gpresult|graftabl|help|icacls|label|md|mklink|mode|more|openfiles|path|pause|popd|print|prompt|pushd|rd|recover|ren|rename|replace|robocopy|set|setlocal|sc|schtasks|shift|shutdown|sort|subst|systeminfo|tasklist|taskkill|time|title|tree|type|ver|verify|vol|wmic|do|copy|xcopy|cd|mkdir|rmdir|del|move|goto|exit|echo|start|call|exist|for|if)\b";
            MatchCollection keywordMatches = Regex.Matches(Editor.Text, keywords);

            string types = @"\b(REM .+?|rem .+?)\b|REM|rem";
            MatchCollection typeMatches = Regex.Matches(Editor.Text, types);

            string methods = @"(:.+?$|REM\/\.+?\\*/)";
            MatchCollection methodMatches = Regex.Matches(Editor.Text, methods, RegexOptions.Multiline);

            string strings = "\".+?\"";
            MatchCollection stringMatches = Regex.Matches(Editor.Text, strings);

            int originalIndex = Editor.SelectionStart;
            int originalLength = Editor.SelectionLength;
            Color originalColor = Color.WhiteSmoke;

            // Stops blinking
            toolStrip1.Focus();

            Editor.SelectionStart = 0;
            Editor.SelectionLength = Editor.Text.Length;
            Editor.SelectionColor = originalColor;

            foreach (Match m in keywordMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.DodgerBlue;
            }

            foreach (Match m in typeMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Green;
            }

            foreach (Match m in methodMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Aqua;
            }

            foreach (Match m in stringMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.LightSalmon;
            }

            Editor.SelectionStart = originalIndex;
            Editor.SelectionLength = originalLength;
            Editor.SelectionColor = originalColor;

            Editor.Focus();
            Syntax = "Batch";
            OriginalText = PreviosTextState;
        }

        private void CLanguage_Click(object sender, EventArgs e)
        {
            PreviosTextState = OriginalText;

            string words1 = @"(auto|double|int|struct|break|else|fopen|long|fprintf|switch|case|enum|register|typedef|char|extern|return|union|const|float|short|unsigned|continue|for|static|void|default|goto|sizeof|volatile|do|if|signed|While|printf|getchar|putchar|gets|fgets|scanf|unsigned|print|fprint|fclose|exit)";
            MatchCollection words1matches = Regex.Matches(Editor.Text, words1);
            string words2 = @"\b(v0|v1|v2|v3|v4|v5|v6|v7|v8|v9|v10|v11|v12|v13|v14|v15|v16|v17|v18|v19|v20|v21|v22|v23|v24|v25|v26|v27|v28|v29|v30|v31|v32|v33|v34|v35|v36|v37|v38|v39|v40|v41|v42|v43|v44|v45|v46|v47|v48|v49|v50|p0|p1|p2|p3|p4|p5|p6|p7|p8|p9|p10|p11|p12|p13|p14|p15|p16|p17|p18|p19|p20|p21|p22|p23|p24|p25|p26|p27|p28|p29|p30)\b";
            MatchCollection words2matches = Regex.Matches(Editor.Text, words2);
            string comments = @"(\/\/.+?$|\/\*.+?\*\/)";
            MatchCollection commentsMatches = Regex.Matches(Editor.Text, comments, RegexOptions.Multiline);
            string comment = @"(#.+?$|\/\*.+?\*\/)";
            MatchCollection commentMatches = Regex.Matches(Editor.Text, comment, RegexOptions.Multiline);
            string strings = "\".+?\"";
            MatchCollection stringMatches = Regex.Matches(Editor.Text, strings);

            Color originalColor = Color.WhiteSmoke;
            int originalIndex = Editor.SelectionStart;
            int originalLength = Editor.SelectionLength;
            toolStrip1.Focus();
            Editor.SelectionColor = originalColor;

            foreach (Match m in words1matches)
            {
                    Editor.SelectionStart = m.Index;
                    Editor.SelectionLength = m.Length;
                    Editor.SelectionColor = Color.DodgerBlue;
            }
            foreach (Match m in words2matches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.MediumPurple;
            }
            foreach (Match m in commentMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Green;
            }
            foreach (Match m in commentsMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Green;
            }
            foreach (Match m in stringMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.LightSalmon;
            }

            Editor.SelectionStart = originalIndex;
            Editor.SelectionLength = originalLength;
            Editor.SelectionColor = originalColor;

            Editor.Focus();
            Syntax = "C";
            OriginalText = PreviosTextState;
        }

        private void CSharpLanguage_Click(object sender, EventArgs e)
        {
            PreviosTextState = OriginalText;

            string keywords = @"\b(class|bool|foreach|private|void|public|var|if|null|string|as|object|int|namespace|using|partial|public|new)\b";
            MatchCollection keywordMatches = Regex.Matches(Editor.Text, keywords);

            string types = @"\b(Console|Match|MatchCollection|Color|EventArgs|DragEventArgs|Application|DragEventHandler|StreamWriter|StreamReader|OpenFileDialog|SaveFileDialog|MessageBox|PrintDocument|PrintPageEventArgs|Regex)\b";
            MatchCollection typeMatches = Regex.Matches(Editor.Text, types);

            string comments = @"(\/\/.+?$|\/\*.+?\*\/)";
            MatchCollection commentMatches = Regex.Matches(Editor.Text, comments, RegexOptions.Multiline);

            string warnings = @"(#pragma .+?$|\/\*.+?\*\/)";
            MatchCollection warningsMatches = Regex.Matches(Editor.Text, warnings, RegexOptions.Multiline);

            string strings = "\".+?\"";
            MatchCollection stringMatches = Regex.Matches(Editor.Text, strings);

            int originalIndex = Editor.SelectionStart;
            int originalLength = Editor.SelectionLength;
            Color originalColor = Color.WhiteSmoke;

            // Stops blinking
            toolStrip1.Focus();

            Editor.SelectionStart = 0;
            Editor.SelectionLength = Editor.Text.Length;
            Editor.SelectionColor = originalColor;

            foreach (Match m in keywordMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.DodgerBlue;
            }

            foreach (Match m in typeMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Aqua;
            }

            foreach (Match m in warningsMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Gray;
            }

            foreach (Match m in commentMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Green;
            }

            foreach (Match m in stringMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.LightSalmon;
            }

            Editor.SelectionStart = originalIndex;
            Editor.SelectionLength = originalLength;
            Editor.SelectionColor = originalColor;

            Editor.Focus();
            Syntax = "CSharp";
            OriginalText = PreviosTextState;
        }

        private void EdifyLanguage_Click(object sender, EventArgs e)
        {
            PreviosTextState = OriginalText;
            string keywords = @"\b(assert|getprop|show_progress|ui_print|package_extract_file|package_extract_dir|run_program|set_perm|set_perm_recursive|delete|mount|symlink|run_program|set_metadata|set_metadata_recursive|write_raw_image|if|file_getprop|ifelse|format|set_progress|endif|is_mounted)\b";
            MatchCollection keywordMatches = Regex.Matches(Editor.Text, keywords);

            string comments = @"(#.+?$|#)";
            MatchCollection commentMatches = Regex.Matches(Editor.Text, comments, RegexOptions.Multiline);

            string strings = "\".+?\"";
            MatchCollection stringMatches = Regex.Matches(Editor.Text, strings);

            int originalIndex = Editor.SelectionStart;
            int originalLength = Editor.SelectionLength;
            Color originalColor = Color.LightSalmon;

            // Stops blinking
            toolStrip1.Focus();

            Editor.SelectionStart = 0;
            Editor.SelectionLength = Editor.Text.Length;
            Editor.SelectionColor = originalColor;

            foreach (Match m in keywordMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.DodgerBlue;
            }

            foreach (Match m in commentMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Green;
            }

            foreach (Match m in stringMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.WhiteSmoke;
            }

            Editor.SelectionStart = originalIndex;
            Editor.SelectionLength = originalLength;
            Editor.SelectionColor = originalColor;

            Editor.Focus();
            Syntax = "Edify";
            OriginalText = PreviosTextState;
        }

        private void JavaLanguage_Click(object sender, EventArgs e)
        {
            PreviosTextState = OriginalText;
            string keywords = @"\b(class|boolean|for|private|void|public|static|else|if|null|string|protected|as|object|throw|int|package|import|extends|implements|new|enum|false|true|try|catch|this|return|while|final|abstract|import|interface|native|strictfp|synchronized|transient|volatile)\b";
            MatchCollection keywordMatches = Regex.Matches(Editor.Text, keywords);

            string comments = @"(\/*\/.+?$|.+?\/*\/)";
            MatchCollection commentMatches = Regex.Matches(Editor.Text, comments, RegexOptions.Multiline);

            string strings = "\".+?\"";
            MatchCollection stringMatches = Regex.Matches(Editor.Text, strings);

            int originalIndex = Editor.SelectionStart;
            int originalLength = Editor.SelectionLength;
            Color originalColor = Color.WhiteSmoke;

            // Stops blinking
            toolStrip1.Focus();

            Editor.SelectionStart = 0;
            Editor.SelectionLength = Editor.Text.Length;
            Editor.SelectionColor = originalColor;

            foreach (Match m in keywordMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.DodgerBlue;
            }

            foreach (Match m in commentMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Green;
            }

            foreach (Match m in stringMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.LightSalmon;
            }

            Editor.SelectionStart = originalIndex;
            Editor.SelectionLength = originalLength;
            Editor.SelectionColor = originalColor;

            Editor.Focus();
            Syntax = "Java";
            OriginalText = PreviosTextState;
        }

        private void PropLanguage_Click(object sender, EventArgs e)
        {
            PreviosTextState = OriginalText;
            string keywords = @"(.+?=|.+? =)";
            MatchCollection keywordMatches = Regex.Matches(Editor.Text, keywords);

            string comments = @"(#.+?$)";
            MatchCollection commentMatches = Regex.Matches(Editor.Text, comments, RegexOptions.Multiline);

            int originalIndex = Editor.SelectionStart;
            int originalLength = Editor.SelectionLength;
            Color originalColor = Color.WhiteSmoke;

            // Stops blinking
            toolStrip1.Focus();

            Editor.SelectionStart = 0;
            Editor.SelectionLength = Editor.Text.Length;
            Editor.SelectionColor = originalColor;

            foreach (Match m in keywordMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.DodgerBlue;
            }

            foreach (Match m in commentMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Green;
            }

            Editor.SelectionStart = originalIndex;
            Editor.SelectionLength = originalLength;
            Editor.SelectionColor = originalColor;

            Editor.Focus();
            Syntax = "Prop";
            OriginalText = PreviosTextState;
        }

        private void ShatchLanguage_Click(object sender, EventArgs e)
        {
            PreviosTextState = OriginalText;
            string keywords = @"\b(COMMENT|VERBOSE|CLEAN|IMPORT|DIRECTORY|MAKE|HEADING|USE|DELETE|PROMPT|END|PRINT|BLANKLINE|FILE|WRITE|RAWCODE|PAUSE|MENU|START|ITEM|LOOP|CHOSEN|)\b";
            MatchCollection keywordMatches = Regex.Matches(Editor.Text, keywords);

            string comments = @"(#.+?$|\/\*.+?\*\/)";
            MatchCollection commentsMatches = Regex.Matches(Editor.Text, comments, RegexOptions.Multiline);

            string strings = "\".+?\"";
            MatchCollection stringMatches = Regex.Matches(Editor.Text, strings);

            int originalIndex = Editor.SelectionStart;
            int originalLength = Editor.SelectionLength;
            Color originalColor = Color.WhiteSmoke;

            // Stops blinking
            toolStrip1.Focus();

            Editor.SelectionStart = 0;
            Editor.SelectionLength = Editor.Text.Length;
            Editor.SelectionColor = originalColor;

            foreach (Match m in keywordMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.DodgerBlue;
            }

            foreach (Match m in commentsMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Green;
            }

            foreach (Match m in stringMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.LightSalmon;
            }

            Editor.SelectionStart = originalIndex;
            Editor.SelectionLength = originalLength;
            Editor.SelectionColor = originalColor;

            Editor.Focus();
            Syntax = "Shatch";
            OriginalText = PreviosTextState;
        }

        private void ShellLanguage_Click(object sender, EventArgs e)
        {
            PreviosTextState = OriginalText;
            string keywords = @"\b(fi|else|\||alias|apropos|apt-get|aptitude|aspell|awk|basename|bash|bc|bg|bind|break|builtin|bzip2|cal|case|cat|cd|cfdisk|chgrp|chmod|chown|chroot|chkconfig|cksum|clear|cmp|comm|command|continue|cp|cron|crontab|csplit|curl|cut|date|dc|dd|ddrescue|declare|df|diff|diff3|dig|dir|dircolors|dirname|dirs|dmesg|du|echo|egrep|eject|enable|env|ethtool|eval|exec|exit|expect|expand|export|expr|false|fdformat|fdisk|fg|fgrep|file|find|fmt|fold|for|format|free|fsck|ftp|function|fuser|gawk|getopts|grep|groupadd|groupdel|groupmod|groups|gzip|hash|head|help|history|hostname|htop|iconv|id|if|ifconfig|ifdown|ifup|import|install|ip|jobs|join|kill|killall|less|let|link|ln|local|locate|logname|logout|look|lpc|lpr|lprint|lprintd|lprintq|lprm|lsblk|ls|lsof|make|man|mkdir|mkfifo|mkisofs|mknod|more|most|mount|mtools|mtr|mv|mmv|nc|netstat|nice|nl|nohup|notify-send|nslookup|open|op|passwd|paste|pathchk|ping|pgrep|pkill|popd|pr|printcap|printenv|printf|ps|pushd|pv|pwd|quota|quotacheck|ram|rar|rcp|read|readarray|readonly|reboot|rename|renice|remsync|return|rev|rm|rmdir|rsync|screen|scp|sdiff|sed|select|seq|set|sftp|shift|shopt|shutdown|sleep|slocate|sort|source|split|ss|ssh|stat|strace|su|sudo|sum|suspend|sync|tail|tar|tee|test|time|timeout|times|touch|top|tput|traceroute|trap|tr|true|tsort|tty|type|ulimit|umask|umount|unalias|uname|unexpand|uniq|units|unrar|unset|unshar|until|uptime|useradd|userdel|usermod|users|uuencode|uudecode|v|vdir|vi|vmstat|wait|watch|wc|whereis|which|while|who|whoami|wget|write|xargs|xdg-open|xz|yes|zip)\b";
            MatchCollection keywordMatches = Regex.Matches(Editor.Text, keywords);

            string strings = "\".+?\"";
            MatchCollection stringMatches = Regex.Matches(Editor.Text, strings);

            string comments = @"(#.+?$|#)";
            MatchCollection commentMatches = Regex.Matches(Editor.Text, comments, RegexOptions.Multiline);

            int originalIndex = Editor.SelectionStart;
            int originalLength = Editor.SelectionLength;
            Color originalColor = Color.WhiteSmoke;

            // Stops blinking
            toolStrip1.Focus();

            Editor.SelectionStart = 0;
            Editor.SelectionLength = Editor.Text.Length;
            Editor.SelectionColor = originalColor;

            foreach (Match m in keywordMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.DodgerBlue;
            }

            foreach (Match m in stringMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.WhiteSmoke;
            }

            foreach (Match m in commentMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Green;
            }

            Editor.SelectionStart = originalIndex;
            Editor.SelectionLength = originalLength;
            Editor.SelectionColor = originalColor;

            Editor.Focus();
            Syntax = "Shell";
            OriginalText = PreviosTextState;
        }

        private void SmaliLanguage_Click(object sender, EventArgs e)
        {
            PreviosTextState = OriginalText;
            string words1 = @"(\.array-data|\.catch|\.param|\.catchall|\.class|\.end|\.end local|\.end locals|\.enum|\.epilogue|\.field|\.implements|\.line|\.local|\.locals|\.parameter|\.prologue|\.registers|\.restart|\.restart local|\.source|\.subannotation|\.super|\.end method|\.end\ annotation|\.end sparse-switch|\.end packed-switch|\.method|\.annotation|\.sparse-switch|\.packed-switch)";
            MatchCollection words1matches = Regex.Matches(Editor.Text, words1);

            string words2 = @"\b(abstract|bridge|constructor|declared-synchronized|enum|final|interface|native|private|protected|public|static|strictfp|synchronized|synthetic|system|transient|varargs|volatile)\b";
            MatchCollection words2matches = Regex.Matches(Editor.Text, words2);

            string words3 = @"\b(v0|v1|v2|v3|v4|v5|v6|v7|v8|v9|v10|v11|v12|v13|v14|v15|v16|v17|v18|v19|v20|v21|v22|v23|v24|v25|v26|v27|v28|v29|v30|v31|v32|v33|v34|v35|v36|v37|v38|v39|v40|v41|v42|v43|v44|v45|v46|v47|v48|v49|v50|p0|p1|p2|p3|p4|p5|p6|p7|p8|p9|p10|p11|p12|p13|p14|p15|p16|p17|p18|p19|p20|p21|p22|p23|p24|p25|p26|p27|p28|p29|p30)\b";
            MatchCollection words3matches = Regex.Matches(Editor.Text, words3);

            string properties = @"(.+?=|.+? =)";
            MatchCollection propertiesMatches = Regex.Matches(Editor.Text, properties);

            string strings = "\".+?\"";
            MatchCollection stringMatches = Regex.Matches(Editor.Text, strings);

            string comments = @"(#.+?$)";
            MatchCollection commentMatches = Regex.Matches(Editor.Text, comments, RegexOptions.Multiline);

            int originalIndex = Editor.SelectionStart;
            int originalLength = Editor.SelectionLength;
            Color originalColor = Color.WhiteSmoke;

            // Stops blinking
            toolStrip1.Focus();

            Editor.SelectionStart = 0;
            Editor.SelectionLength = Editor.Text.Length;
            Editor.SelectionColor = originalColor;

            foreach (Match m in propertiesMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.LightPink;
            }

            foreach (Match m in words1matches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.DodgerBlue;
            }

            foreach (Match m in words2matches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.MediumPurple;
            }

            foreach (Match m in words3matches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.LightSalmon;
            }

            foreach (Match m in stringMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.WhiteSmoke;
            }

            foreach (Match m in commentMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Green;
            }

            Editor.SelectionStart = originalIndex;
            Editor.SelectionLength = originalLength;
            Editor.SelectionColor = originalColor;

            Editor.Focus();
            Syntax = "Smali";
            OriginalText = PreviosTextState;
        }

        private void XmlLanguage_Click(object sender, EventArgs e)
        {
            PreviosTextState = OriginalText;
            string comments = @"(<!--.+?$|.+?-->|<!--)";
            MatchCollection commentMatches = Regex.Matches(Editor.Text, comments, RegexOptions.Multiline);

            string strings = "\".+?\"";
            MatchCollection stringMatches = Regex.Matches(Editor.Text, strings);

            string options = @" \b.+?=";
            MatchCollection optionsMatches = Regex.Matches(Editor.Text, options);

            string data = "<.+?>";
            MatchCollection dataMatches = Regex.Matches(Editor.Text, data);


            string start = "'.+?'";
            MatchCollection startMatches = Regex.Matches(Editor.Text, start);

            int originalIndex = Editor.SelectionStart;
            int originalLength = Editor.SelectionLength;
            Color originalColor = Color.WhiteSmoke;

            // Stops blinking
            toolStrip1.Focus();

            Editor.SelectionStart = 0;
            Editor.SelectionLength = Editor.Text.Length;
            Editor.SelectionColor = originalColor;

            foreach (Match m in dataMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.DodgerBlue;
            }

            foreach (Match m in stringMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.MediumPurple;
            }

            foreach (Match m in optionsMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.LightSalmon;
            }

            foreach (Match m in startMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.MediumPurple;
            }

            foreach (Match m in commentMatches)
            {
                Editor.SelectionStart = m.Index;
                Editor.SelectionLength = m.Length;
                Editor.SelectionColor = Color.Green;
            }

            Editor.SelectionStart = originalIndex;
            Editor.SelectionLength = originalLength;
            Editor.SelectionColor = originalColor;

            Editor.Focus();
            Syntax = "Xml";
            OriginalText = PreviosTextState;
        }

        // Selecting language
        private void batchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BatchLanguage.PerformClick();
        }
        private void cToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CLanguage.PerformClick();
        }
        private void cSharpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CSharpLanguage.PerformClick();
        }
        private void edifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EdifyLanguage.PerformClick();
        }
        private void javaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            JavaLanguage.PerformClick();
        }
        private void properyListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PropLanguage.PerformClick();
        }
        private void shatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShatchLanguage.PerformClick();
        }
        private void shellToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShellLanguage.PerformClick();
        }
        private void smaliToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SmaliLanguage.PerformClick();
        }
        private void xmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XmlLanguage.PerformClick();
        }
        private void batchToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            BatchLanguage.PerformClick();
        }
        private void cToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CLanguage.PerformClick();
        }
        private void cSharpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CSharpLanguage.PerformClick();
        }
        private void edifyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            EdifyLanguage.PerformClick();
        }
        private void javaToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            JavaLanguage.PerformClick();
        }
        private void propertyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PropLanguage.PerformClick();
        }
        private void shatchToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShatchLanguage.PerformClick();
        }
        private void shellToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShellLanguage.PerformClick();
        }
        private void smaliToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SmaliLanguage.PerformClick();
        }
        private void xmlToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            XmlLanguage.PerformClick();
        }

        // ********************************************************************** EDIT
        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndoRedoClick = true;
            if (undoStack.Count > 0)
            {
                StackPush(sender, redoStack);
                undoStack.Pop()();
            }
        }
        private void UndoToolStripButton_Click(object sender, EventArgs e)
        {
            undoToolStripMenuItem.PerformClick();
        }
        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndoRedoClick = true;
            if (redoStack.Count > 0)
            {
                redoStack.Pop()();
                StackPush(sender, undoStack);
                redoStack.Pop()();
            }
        }
        private void RedoToolStripButton_Click(object sender, EventArgs e)
        {
            redoToolStripMenuItem.PerformClick();
        }
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Editor.Clear();
        }
        private void ClearToolStripButton_Click(object sender, EventArgs e)
        {
            Editor.Clear();
        }
        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Editor.Cut();
        }
        private void CutToolStripButton_Click(object sender, EventArgs e)
        {
            Editor.Cut();
        }
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Editor.Copy();
        }
        private void CopyToolStripButton_Click(object sender, EventArgs e)
        {
            Editor.Copy();
        }
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Editor.Paste();
        }
        private void PasteToolStripButton_Click(object sender, EventArgs e)
        {
            Editor.Paste();
        }
        private void selectAllToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Editor.SelectAll();
        }
        private void SelectAllToolStripButton_Click(object sender, EventArgs e)
        {
            Editor.SelectAll();
        }
        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetSyntax.Enabled = true;
        }
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            SetSyntax.Enabled = true;
        }

        // ********************************************************************** CONSOLE AND EXECUTION
        private void Execute_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                Execute.Paste();
            }
            if (e.Control && e.KeyCode == Keys.C)
            {
                Execute.Copy();
            }

            if (e.KeyCode == Keys.Enter)
            {
                string execute = Execute.Text;
                string Command = execute.Split(' ').First();
                if (Command == "cd")
                {
                    string[] directory = execute.Split('"');
                    if (Directory.Exists(directory[1]))
                    {
                        Directory.SetCurrentDirectory(directory[1]);
                    }
                }
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = " /C \"" + execute + "\"";
                process.StartInfo = startInfo;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                Console.Text = Console.Text + Execute.Text;
                Console.Text = Console.Text + output;
                process.WaitForExit();
            }
        }
        private void Execute_TextChanged(object sender, EventArgs e)
        {
            Execute.SelectionStart = Execute.Text.Length;
            Execute.ScrollToCaret();
        }

        // ********************************************************************** CHANGE FONTSIZE
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Editor.ZoomFactor = 0.5F;
        }
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            Editor.ZoomFactor = 0.75F;
        }
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            Editor.ZoomFactor = 1;
        }
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            Editor.ZoomFactor = 1.5F;
        }
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            Editor.ZoomFactor = 2;
        }
        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            Editor.ZoomFactor = 3;
        }
        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            Editor.ZoomFactor = 5;
        }
        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            toolStripMenuItem2.PerformClick();
        }
        private void toolStripMenuItem10_Click(object sender, EventArgs e)
        {
            toolStripMenuItem3.PerformClick();
        }
        private void toolStripMenuItem11_Click(object sender, EventArgs e)
        {
            toolStripMenuItem4.PerformClick();
        }
        private void toolStripMenuItem12_Click(object sender, EventArgs e)
        {
            toolStripMenuItem5.PerformClick();
        }
        private void toolStripMenuItem13_Click(object sender, EventArgs e)
        {
            toolStripMenuItem6.PerformClick();
        }
        private void toolStripMenuItem14_Click(object sender, EventArgs e)
        {
            toolStripMenuItem7.PerformClick();
        }
        private void toolStripMenuItem15_Click(object sender, EventArgs e)
        {
            toolStripMenuItem8.PerformClick();
        }
        private void TextSizeShow_Tick(object sender, EventArgs e)
        {
            TextSizeLabel.Visible = false;
            TextSizeShow.Enabled = false;
        }

        // ********************************************************************** SETTINGS
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var settings = new Settings();
            settings.ShowDialog();
            ApplySettings.Enabled = true;
        }
        private void ApplySettings_Tick(object sender, EventArgs e)
        {
            if (RDEditor.Properties.Settings.Default.AutoSyntaxHighlight == "ON")
            {
                
            }
            if (RDEditor.Properties.Settings.Default.AutoSyntaxHighlight == "OFF")
            {
                
            }
            if (RDEditor.Properties.Settings.Default.WordWrap == "ON")
            {
                Editor.WordWrap = true;
            }
            else
            {
                Editor.WordWrap = false;
            }
            ApplySettings.Enabled = false;
        }

        // ********************************************************************** COMPILER
        private void compileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextSizeLabel.Text = "Compiling";
            TextSizeLabel.Visible = true;
            TextSizeShow.Enabled = true;
            string Compiler = "none";

            // SET COMPILER
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".sln")
            {
                if (File.Exists(RDEditor.Properties.Settings.Default.VisualStudioCompiler))
                {
                    Compiler = "\"" + RDEditor.Properties.Settings.Default.VisualStudioCompiler + "\" /build debug \"" + RDEditor.Properties.Settings.Default.LastFileOpened + "\"\"";
                    Run = Path.GetDirectoryName(RDEditor.Properties.Settings.Default.LastFileOpened) + "\\" + Path.GetFileNameWithoutExtension(RDEditor.Properties.Settings.Default.LastFileOpened) + "\\bin\\Debug\\" + Path.GetFileNameWithoutExtension(RDEditor.Properties.Settings.Default.LastFileOpened) + ".exe";
                }
            }
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".java")
            {
                if (File.Exists(RDEditor.Properties.Settings.Default.EclipseCompiler))
                {
                    Compiler = "\"" + RDEditor.Properties.Settings.Default.EclipseCompiler + "\" \"" + RDEditor.Properties.Settings.Default.LastFileOpened + "\" /build\"";
                    Run = Path.GetDirectoryName(RDEditor.Properties.Settings.Default.LastFileOpened) + "\\" + Path.GetFileNameWithoutExtension(RDEditor.Properties.Settings.Default.LastFileOpened) + ".java";
                }
            }
            if (Path.GetExtension(RDEditor.Properties.Settings.Default.LastFileOpened) == ".c")
            {
                if (File.Exists(RDEditor.Properties.Settings.Default.CCompiler))
                {
                    Compiler = "\"" + RDEditor.Properties.Settings.Default.CCompiler + "\" \"" + RDEditor.Properties.Settings.Default.LastFileOpened + "\" -o \"" + RDEditor.Properties.Settings.Default.LastFileOpened + ".exe \"";
                    Run = RDEditor.Properties.Settings.Default.LastFileOpened + ".exe";
                }
            }

            if (Compiler != "none")
            {
                saveToolStripMenuItem.PerformClick();
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C \"" + Compiler + "\"";
                process.StartInfo = startInfo;
                process.Start();
                Console.Text = Console.Text + process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                statusStrip3.BackColor = Color.Chartreuse;
                CompileTimer.Enabled = true;

                if (RDEditor.Properties.Settings.Default.ExecuteAfterCompile == "ON")
                {
                    if (RDEditor.Properties.Settings.Default.LastFileOpened != null && !string.IsNullOrWhiteSpace(RDEditor.Properties.Settings.Default.LastFileOpened))
                    {
                        if (File.Exists(Run))
                        {
                            Process runprogram = new Process();
                            ProcessStartInfo programinfo = new ProcessStartInfo();
                            programinfo.WindowStyle = ProcessWindowStyle.Hidden;
                            programinfo.CreateNoWindow = true;
                            programinfo.UseShellExecute = false;
                            programinfo.RedirectStandardOutput = true;
                            programinfo.FileName = "cmd.exe";
                            programinfo.Arguments = " /C \"\"" + Run + "\"\"";
                            runprogram.StartInfo = programinfo;
                            runprogram.Start();
                            string output = runprogram.StandardOutput.ReadToEnd();
                            Console.Text = Console.Text + "\n" + Directory.GetCurrentDirectory() + ">" + "\"" + RDEditor.Properties.Settings.Default.LastFileOpened + ".exe" + "\"\n" + output;
                            runprogram.WaitForExit();
                            TabSwitcher.SelectedIndex = 1;
                        }
                        else
                        {
                            statusStrip3.BackColor = Color.OrangeRed;
                            CompileTimer.Enabled = true;
                            DialogResult dialogResult = MessageBox.Show("The tool cannot find " + RDEditor.Properties.Settings.Default.LastFileOpened + ".exe\n\nThis could be a compiler error, however if your output file exist ignore this.", "Compile error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            if (Compiler == "none")
            {
                statusStrip3.BackColor = Color.OrangeRed;
                CompileTimer.Enabled = true;
                DialogResult dialogResult = MessageBox.Show("Compiler not set for this file format or file not recognised.\nRecognised formats: *.c *.java *.sln\n\nConfigure compiler settings in settings, if problems persist seek support.", "Compiler error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void CompileToolStripButton_Click(object sender, EventArgs e)
        {
            compileToolStripMenuItem.PerformClick();
        }
        private void CompileTimer_Tick(object sender, EventArgs e)
        {
            statusStrip3.BackColor = Color.DodgerBlue;
            CompileTimer.Enabled = false;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Process runprogram = new Process();
            ProcessStartInfo programinfo = new ProcessStartInfo();
            programinfo.WindowStyle = ProcessWindowStyle.Hidden;
            programinfo.CreateNoWindow = true;
            programinfo.UseShellExecute = false;
            programinfo.RedirectStandardOutput = true;
            programinfo.FileName = "C:\\Program Files\\RDSoft\\RDExplorer.exe";
            programinfo.Arguments = " ";
            runprogram.StartInfo = programinfo;
            runprogram.Start();
        }
    }
    public static class Extensions
    {
        // UNDO REDO STACK
        public static Func<RichTextBox> Text(this RichTextBox Editor, string text, int sel)
        {
            return () =>
            {
                Editor.Text = text;
                Editor.SelectionStart = sel;
                return Editor;
            };
        }
    }
}
