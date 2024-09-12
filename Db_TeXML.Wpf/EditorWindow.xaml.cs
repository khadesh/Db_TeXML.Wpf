using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Diagnostics;
using System.Data;
using System.Collections;
using System.IO;
using System.Windows.Media.Animation;
using System.Text.RegularExpressions;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using _WcfMyBatisUtilLibrary;
using _WcfMyBatisServiceLibrary;
using Db_TeXML.Wpf.parser;
using Db_TeXML.Wpf.parser.tokens._base;
using NUnit.Framework;
using _CommonLib;

namespace Db_TeXML.Wpf
{
    [TestFixture, RequiresSTA]
    public partial class EditorWindow : BaseWindow
    {
        #region Static Variables
        /// <summary>
        /// Directory where project information should be saved.
        /// </summary>
        public static string ProjectsDirectory = ".\\projects\\";
        #endregion

        #region Private Variables (For Database ComboBox Selection Change)
        /// <summary>
        /// Is the port txb enabled for the current selected db.
        /// </summary>
        private bool[] TxbPortIsEnabled = new bool[] { true, true, true, true };

        /// <summary>
        /// Default ports for each database.
        /// </summary>
        private string[] TxbPortDefault = new string[] { "5423", "1433", "3306", "1521" };

        /// <summary>
        /// Is the server txb enabled for the current selected db.
        /// </summary>
        private bool[] TxbServerIsEnabled = new bool[] { true, true, true, true };

        /// <summary>
        /// Default values for server.
        /// </summary>
        private string[] TxbServerDefault = new string[] { "localhost", "localhost", "localhost", "localhost" };

        /// <summary>
        /// A boolean to disable overwriting of text during a selection changed event.
        /// </summary>
        private bool SuppressSelectionChangedEvents = false;
        #endregion

        #region Public / Private Variables
        /// <summary>
        /// Used to help resize the window.
        /// </summary>
        private double BACKFLOAT_BOTTOM_MARGIN;

        /// <summary>
        /// Two types of editors, one for input and one for output.
        /// </summary>
        private enum TabType { INPUT, OUTPUT };

        /// <summary>
        /// Used to know which tab is currently being executed in the event of an error.
        /// </summary>
        private string currentExecutingTab = "";

        /// <summary>
        /// The current sql namespace.
        /// </summary>
        private string SqlNamespace = "PostgresDbHandler";
        private string[] SqlNamespaces = new string[] { "PostgresDbHandler", "SqlServerDbHandler", "MySqlDbHandler", "OracleDbHandler" };
        
        /// <summary>
        /// The current selected database type.
        /// </summary>
        private SQLMap.DatabaseType DbType = SQLMap.DatabaseType.POSTGRES;
        private SQLMap.DatabaseType[] DbTypes = new SQLMap.DatabaseType[] { SQLMap.DatabaseType.POSTGRES, SQLMap.DatabaseType.SQLSERVER, SQLMap.DatabaseType.MYSQL, SQLMap.DatabaseType.ORACLE };

        /// <summary>
        /// Hash table containing connection details from past connected databases.
        /// </summary>
        public Hashtable connectionDetails = new Hashtable();

        /// <summary>
        /// Hashtable containing all editors.
        /// 
        /// Usage Format:
        ///   Hashtable[string editorTag, ICSharpCode.AvalonEdit.TextEditor editor]
        ///   
        /// Sample data:
        ///   Hashtable["Script1", instanceof(ICSharpCode.AvalonEdit.TextEditor)]
        ///   Hashtable["Output1", instanceof(ICSharpCode.AvalonEdit.TextEditor)]
        ///   * Where Script1 indicates the first tabs input script
        /// </summary>
        private Hashtable avalonTextEditors = new Hashtable();

        /// <summary>
        /// A HashSet of input text editors.
        /// </summary>
        HashSet<TextEditor> textEditorsInput = new HashSet<TextEditor>();

        /// <summary>
        /// A HashSet of output text editors.
        /// </summary>
        HashSet<TextEditor> textEditorsOutput = new HashSet<TextEditor>();
        private int numScripts = 1;

        /// <summary>
        /// An instance of a parse engine, used for parsing scripts to generate output.
        /// </summary>
        private ParseEngine parseEngine = null;

        /// <summary>
        /// Definitions for input text editors defining rules for word highlighting and color coding syntax.
        /// </summary>
        private IHighlightingDefinition dbtexmlHightlightingDefinition = null;

        /// <summary>
        /// Defines how the code should be folded.
        /// </summary>
        private DbTeXMLFoldingStrategy foldingStrategy = new DbTeXMLFoldingStrategy();

        /// <summary>
        /// Hashtable containing instances of ColorizeAvalonEdit2, 
        /// needed to call updates for recoloring the scripts header area.
        /// </summary>
        Hashtable colorizeAvalonEdit2Hash = new Hashtable();

        /// <summary>
        /// Hashtable of folding managers, 
        /// an instance of each scripts folding manager needs to be accessed 
        /// on the folding threads ticks.
        /// </summary>
        Hashtable foldingManagers = new Hashtable();

        /// <summary>
        /// A popup window containing autocomplete values for scripts.
        /// </summary>
        private CompletionWindow completionWindow = null;

        /// <summary>
        /// Tracks if a connection has been successfully established.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return parseEngine != null;
            }
        }

        /// <summary>
        /// Gets the tag of the last generated input tab.
        /// Used for accessing the avalonTextEditors more easily.
        /// </summary>
        public string LastInputTabName
        {
            get
            {
                if (numScripts > 1)
                {
                    return "Script" + (this.numScripts - 1).ToString();
                }
                return "";
            }
        }

        /// <summary>
        /// Gets the tag of the last generated output tab.
        /// Used for accessing the avalonTextEditors more easily.
        /// </summary>
        public string LastOutputTabName
        {
            get
            {
                if (numScripts > 1)
                {
                    return "Output" + (this.numScripts - 1).ToString();
                }
                return "";
            }
        }

        /// <summary>
        /// The commands list window, where a user can view all possible commands for the parser.
        /// </summary>
        CommandsListWindow commandsListWindow = null;

        /// <summary>
        /// The help window, where a user can read about how to use the parse engine.
        /// </summary>
        HelpWindow helpWindow = null;
        #endregion

        #region Constructor(s)
        public EditorWindow()
        {
            InitializeComponent();
                        
            this.loadConnectionDetails();

            this.initializeAutoComplete();

            this.initializeFoldingThread();

            this.initializeDbTypeComboBox();
        }

        /// <summary>
        /// Initializes the DbType ComboBox and ensures event driven internal value changing.
        /// </summary>
        private void initializeDbTypeComboBox()
        {
            this.cmbDatabaseType.SelectionChanged += cmbDatabaseType_SelectionChanged;

            // Ensure initial settings are correct:
            this.cmbDatabaseType_SelectionChanged(null, null);
        }

        /// <summary>
        /// When a new database is selected, the default port needs to be set, 
        /// and certain fields may need to be enabled / disabled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbDatabaseType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.DbType = this.DbTypes[this.cmbDatabaseType.SelectedIndex];
            this.SqlNamespace = this.SqlNamespaces[this.cmbDatabaseType.SelectedIndex];

            this.txbPort.IsEnabled = this.TxbPortIsEnabled[this.cmbDatabaseType.SelectedIndex];
            this.txbServer.IsEnabled = this.TxbServerIsEnabled[this.cmbDatabaseType.SelectedIndex];
            if (!this.SuppressSelectionChangedEvents)
            {
                this.txbPort.Text = this.TxbPortDefault[this.cmbDatabaseType.SelectedIndex];
                if (!this.txbServer.IsEnabled)
                {
                    this.txbServer.Text = this.TxbServerDefault[this.cmbDatabaseType.SelectedIndex];
                }
                else if (this.txbServer.Text.Equals(""))
                {
                    this.txbServer.Text = this.TxbServerDefault[this.cmbDatabaseType.SelectedIndex];
                }
            }
        }

        /// <summary>
        /// The window loaded event for initializing certain things like sizing after the window has been created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.setHeightWidthRatios();

            this.resizeComponents();

            this.txbDatabase.Focus();
        }

        /// <summary>
        /// Initializes the folding thread which checks the code folding strategy every X secs.
        /// </summary>
        private void initializeFoldingThread()
        {
            try
            {
                DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
                foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
                foldingUpdateTimer.Tick += foldingUpdateTimer_Tick;
                foldingUpdateTimer.Start();
            }
            catch { }
        }

        /// <summary>
        /// Code executed within the folding strategy check thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void foldingUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (foldingStrategy != null)
            {
                foreach (TextEditor boxKey in this.foldingManagers.Keys) {
                    foldingStrategy.UpdateFoldings(((FoldingManager)this.foldingManagers[boxKey]), boxKey.Document);
                }
            }
        }

        /// <summary>
        /// Creates and hides the auto-complete popup menu for the database name field.
        /// 
        /// Selecting an item in this list will set all database connection parameters.
        /// </summary>
        private void initializeAutoComplete()
        {
            this.txbDatabaseAutoCompleteListBox.Visibility = System.Windows.Visibility.Hidden;
            this.resetListBoxItems(true);
            this.txbDatabaseAutoCompleteListBox.SelectionChanged += this.txbDatabaseAutoCompleteListBox_SelectionChanged;
        }

        /// <summary>
        /// Sets data used later for resizing. 
        /// </summary>
        private void setHeightWidthRatios()
        {
            BACKFLOAT_BOTTOM_MARGIN = this.ActualHeight - (this.backfloat1.ActualHeight);
        }
        #endregion

        #region Resizing Methods & Events
        /// <summary>
        /// Resizes all the components appropriately.
        /// </summary>
        private void resizeComponents()
        {
            double width = this.Width;
            double height = this.Height;
            if (this.WindowState == WindowState.Maximized)
            {
                width = System.Windows.SystemParameters.PrimaryScreenWidth;
                height = System.Windows.SystemParameters.PrimaryScreenHeight;
            }

            this.tabControlIn.Width = (this.ActualWidth - 41) / 2;
            this.tabControlIn.Height = this.tabStack.ActualHeight;

            this.tabControlOut.Width = (this.ActualWidth - 41) / 2;
            this.tabControlOut.Height = this.tabStack.ActualHeight;

            this.backfloat1.Margin = new Thickness(-600.0, 0.0, 600.0, (this.ActualHeight - (this.backfloat1.ActualHeight)) - BACKFLOAT_BOTTOM_MARGIN);
            this.backdrop1.Margin = new Thickness(0.0, (this.ActualHeight - (this.backdrop1.ActualHeight + 10.0)), -2.0, 10.0 );
            this.btnRunParser.Margin = new Thickness(this.ActualWidth - 170.0, this.btnRunParser.Margin.Top, this.btnRunParser.Margin.Right, this.btnRunParser.Margin.Bottom);
            this.btnCommandsList.Margin = new Thickness(this.ActualWidth - 170.0, this.btnCommandsList.Margin.Top, this.btnCommandsList.Margin.Right, this.btnCommandsList.Margin.Bottom);

            this.btnExplore.Margin = new Thickness(this.ActualWidth - 270.0, this.btnExplore.Margin.Top, this.btnExplore.Margin.Right, this.btnExplore.Margin.Bottom);

            this.btnHelp.Margin = new Thickness(this.ActualWidth - 170.0, this.btnHelp.Margin.Top, this.btnHelp.Margin.Right, this.btnHelp.Margin.Bottom);

            this.groupBoxTop.Width = this.ActualWidth - 20.0;

            foreach (TextEditor editor in this.textEditorsInput)
            {
                editor.Width = this.tabControlIn.Width - 6;
            }

            foreach (TextEditor editor in this.textEditorsOutput)
            {
                editor.Width = this.tabControlOut.Width - 27;
            }
        }

        /// <summary>
        /// Resize components when the user resizes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaseWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.resizeComponents();
        }

        /// <summary>
        /// Resize the components when the windows is maximized, etc.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaseWindow_StateChanged(object sender, EventArgs e)
        {
            this.resizeComponents();
        }
        #endregion
        
        #region Button Run Parser
        /// <summary>
        /// Simple button click function.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRunParser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save the scripts to disk before executing them.
                this.saveAllScripts();

                // Execute the scripts.
                this.runAllScripts();
            }
            catch (Exception ex)
            {
                string tabInfo = "";
                if (!currentExecutingTab.Equals(""))
                {
                    tabInfo = "Error in " +currentExecutingTab + ": \n";
                }
                MessageBox.Show(tabInfo + Messenger.GetMessage("SyntaxError") + ex.Message, Messenger.GetMessage("SyntaxErrorTitle"), MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// Runs all the scripts in sequential order.
        /// </summary>
        private void runAllScripts()
        {
            currentExecutingTab = "";
            foreach (TabItem tabItem in this.tabControlIn.Items)
            {
                currentExecutingTab = tabItem.Header.ToString();
                if (currentExecutingTab.StartsWith("Script"))
                {
                    string outputTabItemName = currentExecutingTab.Replace("Script", "Output");
                    string textOutput = "";
                    if (parseEngine != null)
                    {
                        textOutput = parseEngine.ParseTokens(((TextEditor)this.avalonTextEditors[currentExecutingTab]).Text, currentExecutingTab, this.txbDatabase.Text);
                    }
                    ((TextEditor)this.avalonTextEditors[outputTabItemName]).Text = textOutput;
                    this.saveOutput(outputTabItemName, (parseEngine != null ? parseEngine.OutFileName : ""));
                }
            }
        }
        #endregion

        #region Button Explore Results
        /// <summary>
        /// Opens explorer for the current project for user to view saved results.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExplore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string resultsPath = EditorWindow.ProjectsDirectory + this.txbDatabase.Text + "\\Output\\";
                if (Directory.Exists(resultsPath))
                {
                    Process.Start("explorer.exe", resultsPath);
                }
            }
            catch
            {
                MessageBox.Show(Messenger.GetMessage("ResultsOpenError"), Messenger.GetMessage("ResultsOpenErrorTitle"), MessageBoxButton.OK);
            }
        }
        #endregion

        #region Save Input / Output
        /// <summary>
        /// Saves the output (results) to disk.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="output">the default save file name (.txt will be appended to it in the method)</param>
        /// <param name="filename">if a user supplied a desired name it will be used</param>
        private void saveOutput(string tag, string filename)
        {
            try
            {
                if (this.IsConnected)
                {
                    this.createDirectoryStructure();

                    if (filename.Equals(""))
                    {
                        filename = EditorWindow.ProjectsDirectory + this.txbDatabase.Text + "\\Output\\" + tag + ".txt";
                    }
                    else
                    {
                        filename = EditorWindow.ProjectsDirectory + this.txbDatabase.Text + "\\Output\\" + filename;
                    }

                    string contents = ((TextEditor)this.avalonTextEditors[tag]).Text;
                    FileManager.SaveFile(filename, contents);
                }
            }
            catch
            {
                MessageBox.Show(Messenger.GetMessage("ResultsSaveError"), Messenger.GetMessage("ResultsSaveErrorTitle"), MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// Saves all the user input scripts to disk.
        /// </summary>
        private void saveAllScripts()
        {
            try
            {
                if (this.IsConnected)
                {
                    this.createDirectoryStructure();

                    foreach (TabItem tabItem in this.tabControlIn.Items)
                    {
                        if (!tabItem.Header.ToString().Equals("+"))
                        {
                            string fileName = EditorWindow.ProjectsDirectory + this.txbDatabase.Text + "\\Scripts\\" + tabItem.Header.ToString() + ".db.texml";

                            string contents = ((TextEditor)this.avalonTextEditors[tabItem.Header.ToString()]).Text;
                            FileManager.SaveFile(fileName, contents);
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show(Messenger.GetMessage("ScriptsSaveError"), Messenger.GetMessage("ScriptsSaveErrorTitle"), MessageBoxButton.OK);
            }
        }
        
        /// <summary>
        /// Creates a default directory structure if one doesn't yet exist.
        /// </summary>
        private void createDirectoryStructure()
        {
            string rootDir = EditorWindow.ProjectsDirectory + this.txbDatabase.Text + "\\Scripts";
            if (!Directory.Exists(rootDir))
            {
                Directory.CreateDirectory(rootDir);
            }

            rootDir = EditorWindow.ProjectsDirectory + this.txbDatabase.Text + "\\Output";
            if (!Directory.Exists(rootDir))
            {
                Directory.CreateDirectory(rootDir);
            }
        }
        #endregion

        #region Load Scripts Local
        /// <summary>
        /// Loads a projects data, both inputs and outputs.
        /// </summary>
        private void loadScripts()
        {
            try
            {
                if (this.IsConnected)
                {
                    this.createDirectoryStructure();

                    string rootDir = EditorWindow.ProjectsDirectory + this.txbDatabase.Text + "\\Scripts";
                    if (Directory.Exists(rootDir))
                    {
                        string[] fileNames = Directory.GetFiles(rootDir, "Script*.db.texml");

                        foreach (string fileName in fileNames)
                        {
                            loadScript(fileName);
                        }

                        if (this.tabControlIn.Items.Count >= 2)
                        {
                            ((TabItem)this.tabControlIn.Items[1]).IsSelected = true;
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show(Messenger.GetMessage("ScriptsLoadError"), Messenger.GetMessage("ScriptsLoadErrorTitle"), MessageBoxButton.OK);
            }
        }

        private void loadScript(string fileName)
        {
            // Get the script contents:
            string rootDir = Regex.Replace(fileName, @"\\[^\\]*$", "");
            string contents = File.ReadAllText(fileName);

            // Get the output filename:
            string outputFileName = ParseEngine.parseHeaderDataFilename(contents.Split('\n'));
            outputFileName = (
                outputFileName.Equals("") || !File.Exists(rootDir.Replace("Scripts", "Output") + "\\" + outputFileName)
                ? fileName.Replace("\\Scripts", "\\Output").Replace("\\Script", "\\Output").Replace(".db.texml", ".txt") 
                : rootDir.Replace("Scripts", "Output") + "\\" + outputFileName
            );

            // Add tabs:
            this.addNewTabs();

            // Load the input file contents:
            ((TextEditor)this.avalonTextEditors[this.LastInputTabName]).Text = contents;

            // Load the output file contents:
            if (File.Exists(outputFileName))
            {
                contents = File.ReadAllText(outputFileName);
                ((TextEditor)this.avalonTextEditors[this.LastOutputTabName]).Text = contents;
            }
        }
        #endregion

        #region Connection Processing (Button Connect / Practice Events)
        /// <summary>
        /// Loads all past connection details from connections.txt into the connectionDetails Hashtable.
        /// 
        /// The first entry will always be loaded as the default connection.
        /// </summary>
        private void loadConnectionDetails()
        {
            bool isFirst = true;
            string connectionsFile = "connections.txt";
            if (File.Exists(connectionsFile))
            {
                foreach (var nextLine in File.ReadAllLines(connectionsFile))
                {
                    string[] details = nextLine.Split('|');
                    if (details.Length == 6)
                    {
                        connectionDetails[details[2]] = nextLine;
                    }
                    if (isFirst)
                    {
                        try
                        {
                            this.SuppressSelectionChangedEvents = true;
                            this.selectCmbIndexFromValue(details[5]);
                            this.txbServer.Text = details[0];
                            this.txbPort.Text = details[1];
                            this.txbDatabase.Text = details[2];
                            this.txbUserId.Text = details[3];
                            this.txbPassword.Password = details[4];
                            isFirst = false;
                            this.SuppressSelectionChangedEvents = false;
                        }
                        catch (Exception ex)
                        {
                            this.SuppressSelectionChangedEvents = false;
                            throw ex;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves all the connection details to disk.
        /// </summary>
        private void saveConnectionDetails()
        {
            // topKey is used to determine which entry needs to be at the top of the connections.txt saved output.
            // Since the top entry is always loaded first.
            string topKey = this.txbDatabase.Text;

            // Practice mode details are never saved to disk.
            if (!topKey.Equals("practice mode"))
            {
                string details =
                        this.txbServer.Text + "|" +
                        this.txbPort.Text + "|" +
                        this.txbDatabase.Text + "|" +
                        this.txbUserId.Text + "|" +
                        this.txbPassword.Password + "|" +
                        ((ComboBoxItem)this.cmbDatabaseType.SelectedItem).Content.ToString();

                // Save the current settings in the connectionDetails hash:
                connectionDetails[topKey] = details;

                // The top entry set to current details:
                string saveDetails = details;

                foreach (string key in connectionDetails.Keys)
                {
                    // Check to make sure the topKey isn't added twice to the connection.txt output.
                    if (!topKey.Equals(key))
                    {
                        saveDetails += "\r\n" + connectionDetails[key];
                    }
                }

                // Save the info to disk.
                FileManager.SaveFile(".\\connections.txt", saveDetails);
            }
        }

        /// <summary>
        /// Connect to a database and load the project or start a practice session.
        /// </summary>
        /// <param name="isPractice">true if you wish to start a practice session, false otherwise.</param>
        private void connectToDatabase(bool isPractice)
        {
            try
            {
                if (!this.IsConnected)
                {
                    if (isPractice)
                    {
                        // Set practice mode information:
                        this.cmbDatabaseType.SelectedIndex = 0;
                        this.txbServer.Text = "127.0.0.1";
                        this.txbPort.Text = "5432";
                        this.txbDatabase.Text = "practice mode";
                        this.txbUserId.Text = "postgres";
                        this.txbPassword.Password = "postgres";

                        // Get an instance of a practice mode parse engine:
                        this.parseEngine = new ParseEngine();

                        // Begin the connection success animation:
                        Storyboard sb = this.FindResource("ConnectedPracticeStoryboard") as Storyboard;
                        sb.Begin();
                    }
                    else
                    {
                        // Set the sqlmap connection details:
                        SQLMap.SetConnectionDetails(
                            this.txbServer.Text,
                            this.txbPort.Text,
                            this.txbDatabase.Text,
                            this.txbUserId.Text,
                            this.txbPassword.Password,
                            this.DbType
                        );

                        // Set the sql select parameters:
                        Hashtable parameters = new Hashtable();
                        parameters.Add("database", this.txbDatabase.Text);

                        // Find all postgres table information:
                        DataTable tableDt = DatabaseServiceProxy.SelectRequest(
                            this.SqlNamespace,
                            "select_information_schema_tables",
                            parameters
                        );

                        // Find all postgres column information:
                        DataTable columnDt = DatabaseServiceProxy.SelectRequest(
                            this.SqlNamespace,
                            "select_information_schema_columns",
                            parameters
                        );

                        // Get an instance of the parser for the connected database:
                        this.parseEngine = new ParseEngine(tableDt, columnDt);

                        // Connection success, save the details to disk:
                        this.saveConnectionDetails();

                        // Begin the connection success animation:
                        Storyboard sb = this.FindResource("ConnectedStoryboard") as Storyboard;
                        sb.Begin();
                    }

                    // Disable all connection fields and enable input/output tabs:
                    this.txbServer.IsEnabled = false;
                    this.txbPort.IsEnabled = false;
                    this.txbDatabase.IsEnabled = false;
                    this.txbUserId.IsEnabled = false;
                    this.txbPassword.IsEnabled = false;
                    this.cmbDatabaseType.IsEnabled = false;
                    this.tabControlIn.IsEnabled = true;
                    this.tabControlOut.IsEnabled = true;

                    // Change the connect buttons label:
                    this.btnConnect.Content = "Connected...";

                    // Load any scripts from earlier sessions.
                    this.loadScripts();
                }
            }
            catch (Exception ex)
            {
                // If a connection fails, remove any parseEngine references and display the appropriate error message:
                this.parseEngine = null;

                if (!isPractice)
                {
                    MessageBox.Show(Messenger.GetMessage("DatabaseConnectionError"), Messenger.GetMessage("DatabaseConnectionErrorTitle"), MessageBoxButton.OK);
                }
                else
                {
                    MessageBox.Show(Messenger.GetMessage("PracticeModeInitError"), Messenger.GetMessage("PracticeModeInitErrorTitle"), MessageBoxButton.OK);
                }
            }
        }

        private void btnPractice_Click(object sender, RoutedEventArgs e)
        {
            this.connectToDatabase(true);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            this.connectToDatabase(false);
        }
        #endregion

        #region AvalonEdit Text Editor Functionality

        #region Tabbing Functionality (Add/Remove Editors)
        /// <summary>
        /// [+] tab double click functionality, adds new input and output tabs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.addNewTabs();
            e.Handled = true;
        }

        /// <summary>
        /// Add an input and output tab, increment the tab counter, and resize components.
        /// </summary>
        private void addNewTabs()
        {
            if (this.numScripts < 8)
            {
                this.addNewTab(TabType.OUTPUT);
                this.addNewTab(TabType.INPUT);
                this.numScripts++;
                this.resizeComponents();
            }
        }

        /// <summary>
        /// Adds a TabItem to the input or output TabControl.
        /// </summary>
        /// <param name="type"></param>
        private void addNewTab(TabType type)
        {
            if (type == TabType.INPUT)
            {
                this.tabControlIn.Items.Add(this.getNewTab("Script" + numScripts.ToString(), type));
                this.tabControlIn.SelectedIndex = this.numScripts;
            }
            else if (type == TabType.OUTPUT)
            {
                this.tabControlOut.Items.Add(this.getNewTab("Output" + numScripts.ToString(), type));
                this.tabControlOut.SelectedIndex = this.numScripts;
            }
        }

        /// <summary>
        /// Gets a new TabItem object, containing a grid which contains an Avalon TextEditor. 
        /// </summary>
        /// <param name="tabName">key for the new avalonTextEditors hash entry</param>
        /// <param name="type">input or output tab</param>
        /// <returns>TabItem to be added to the appropriate input or output TabControl</returns>
        private TabItem getNewTab(string tabName, TabType type)
        {
            TabItem nextTab = new TabItem();
            nextTab.Header = tabName;
            Grid tabGrid = new Grid();
            TextEditor inputTextEditor = this.getNewTextEditor(type);

            if (type == TabType.INPUT)
            {
                textEditorsInput.Add(inputTextEditor);
                inputTextEditor.Margin = new Thickness(-3, 0, 0, 0);
            }
            if (type == TabType.OUTPUT)
            {
                textEditorsOutput.Add(inputTextEditor);
                inputTextEditor.Margin = new Thickness(-20, 0, 0, 0);
            }

            avalonTextEditors[tabName] = inputTextEditor;
            tabGrid.Children.Add(inputTextEditor);
            tabGrid.Margin = new Thickness(0, 0, 0, 0);
            nextTab.Content = tabGrid;
            return nextTab;
        }

        /// <summary>
        /// Gets a new TextEditor instance and instanciates all the various custom syntax, colorizing, highighting rules.
        /// </summary>
        /// <param name="type">input or output tab</param>
        /// <returns></returns>
        private TextEditor getNewTextEditor(TabType type)
        {
            TextEditor inputTextEditor = new TextEditor();

            // Courier New for evenly spaced text.
            inputTextEditor.FontFamily = new FontFamily("Courier New");
            inputTextEditor.WordWrap = true;

            if (type == TabType.INPUT)
            {
                inputTextEditor.ShowLineNumbers = true;

                // Add syntax highlighting support:
                inputTextEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.HighlightingDefinitions[0];
                this.loadHighlightingDefinitions(inputTextEditor);

                // Add selection highlighting support:
                inputTextEditor.TextArea.TextView.LineTransformers.Add(new ColorizeAvalonEdit());
                inputTextEditor.TextArea.SelectionChanged += inputTextEditor_TextArea_SelectionChanged;
                
                // Add header colorize support:
                ColorizeAvalonEdit2 colorize2 = new ColorizeAvalonEdit2();
                this.colorizeAvalonEdit2Hash[inputTextEditor] = colorize2;
                inputTextEditor.TextArea.TextView.LineTransformers.Add(colorize2);
                inputTextEditor.TextChanged += inputTextEditor_TextChanged;

                // Add code folding support:
                FoldingManager foldingManager = FoldingManager.Install(inputTextEditor.TextArea);
                foldingManagers[inputTextEditor] = foldingManager;

                // Add support for code completion:
                inputTextEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
            }

            inputTextEditor.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            return inputTextEditor;
        }

        /// <summary>
        /// When an input tab is changed, the corresponding output tab should be shown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControlIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i = 0; i < this.tabControlIn.Items.Count; i++)
            {
                if (((TabItem)this.tabControlIn.Items[i]).IsSelected)
                {
                    ((TabItem)this.tabControlOut.Items[i]).IsSelected = true;
                }
            }
        }
        #endregion

        #region Text Completion Events
        /// <summary>
        /// Handles displaying the code completion window when a user inputs text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == "." && completionWindow == null)
            {
                ICSharpCode.AvalonEdit.Editing.TextArea textArea = ((ICSharpCode.AvalonEdit.Editing.TextArea)sender);

                // Get the text to check for completion code:
                string checkText = textArea.Document.GetText(0, textArea.Caret.Offset);
                
                // Open code completion after the user has pressed dot:
                completionWindow = new CompletionWindow(((ICSharpCode.AvalonEdit.Editing.TextArea)sender));
                IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;

                // Check to see if the checkText ends with "tokenName." for all tokens.
                foreach (string tokenName in ParseEngine.TokenNameToTokenClassNameReflectionHash.Keys)
                {
                    if (checkText.EndsWith(tokenName + "."))
                    {
                        this.addMyCompletionDataForToken(data, tokenName, ParseEngine.TokenNameToTokenClassNameReflectionHash[tokenName].ToString());
                    }
                }

                // Only show the completion list if there is at least one match:
                if (data.Count > 0) 
                { 
                    completionWindow.Show();
                    completionWindow.Closed += delegate { completionWindow = null; };
                }
                else
                {
                    completionWindow = null;
                }
            }
        }

        /// <summary>
        /// Sets the completion data for a given token name.
        /// </summary>
        /// <param name="data">code completion data to be initialized</param>
        /// <param name="tokenName">a tokens name</param>
        /// <param name="tokenClassName">a tokens class name</param>
        private void addMyCompletionDataForToken(IList<ICompletionData> data, string tokenName, string tokenClassName)
        {
            ArrayList commandsList = new ArrayList();
            System.Type tokenType = System.Type.GetType(ParseEngine.Db_TeXML_TokenNamespace + "." + tokenClassName);
            if (ParseEngine.TokenTypeToReflectionCallbackHash.ContainsKey(tokenType))
            {
                Hashtable subhash = (Hashtable)ParseEngine.TokenTypeToReflectionCallbackHash[tokenType];
                foreach (string subkey in subhash.Keys)
                {
                    commandsList.Add(subkey);
                }
            }

            commandsList.Sort();
            foreach (string command in commandsList)
            {
                // Removes the token name from the completion data so when a user types "table.",
                // "table.name.sql" will appear as "name.sql" in the completion window.
                data.Add(new MyCompletionData(Regex.Replace(command, "^" + tokenName + @"\.", "")));
            }
        }
        
        /// <summary>
        /// Implements AvalonEdit ICompletionData interface to provide the entries in the completion drop down.
        /// </summary>
        public class MyCompletionData : ICompletionData
        {
            public string Text { get; private set; }
            public double Priority { get { return 1.0; } }
            public object Content { get { return this.Text; } }
            public object Description { get { return "Description for " + this.Text; } }
            public System.Windows.Media.ImageSource Image { get { return null; } }
            public MyCompletionData(string text) { this.Text = text; }

            public void Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ISegment completionSegment,
                EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, this.Text);
            }
        }
        #endregion

        #region DocumentColorizingTransformer(s) (Text Highlighting / Header Colorizing Support)
        /// <summary>
        /// Selected text needs to be updated everytime the users selection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void inputTextEditor_TextArea_SelectionChanged(object sender, EventArgs e)
        {
            ICSharpCode.AvalonEdit.Editing.TextArea textArea = (ICSharpCode.AvalonEdit.Editing.TextArea)sender;
            ColorizeAvalonEdit.SelectedText = textArea.Selection.GetText();
        }

        /// <summary>
        /// Supports italicizing all instances of the current selection in the current row.
        /// </summary>
        public class ColorizeAvalonEdit : DocumentColorizingTransformer
        {
            public static string SelectedText = "";
            protected override void ColorizeLine(DocumentLine line)
            {
                if (!SelectedText.Equals(""))
                {
                    int lineStartOffset = line.Offset;
                    string text = CurrentContext.Document.GetText(line);

                    int start = 0;
                    int index;
                    while ((index = text.IndexOf(ColorizeAvalonEdit.SelectedText, start)) >= 0)
                    {
                        base.ChangeLinePart(
                            lineStartOffset + index,
                            lineStartOffset + index + ColorizeAvalonEdit.SelectedText.Length,
                            (VisualLineElement element) =>
                            {
                                Typeface tf = element.TextRunProperties.Typeface;
                                element.TextRunProperties.SetTypeface(new Typeface(
                                    tf.FontFamily,
                                    FontStyles.Italic,
                                    FontWeights.Bold,
                                    tf.Stretch
                                ));
                            });
                        start = index + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Supports special coloring of the header area so the user can clearly see where it begins and ends.
        /// </summary>
        public class ColorizeAvalonEdit2 : ICSharpCode.AvalonEdit.Rendering.DocumentColorizingTransformer
        {
            public int NumHeaderLines = 0;

            public void UpdateNumHeaderLines(TextEditor editor)
            {
                string text = editor.Text;
                bool isHeaderArea = true;
                int numHeaderLines = 0;
                string[] lines = text.Split('\n');

                for (int i = 0; i < lines.Length && isHeaderArea; i++)
                {
                    if (lines[i].StartsWith("#"))
                    {
                        numHeaderLines++;
                    }
                    else
                    {
                        isHeaderArea = false;
                    }
                }

                this.NumHeaderLines = numHeaderLines;
                editor.TextArea.TextView.Redraw();
            }

            protected override void ColorizeLine(ICSharpCode.AvalonEdit.Document.DocumentLine line)
            {
                int lineStartOffset = line.Offset;
                string text = CurrentContext.Document.GetText(line);
                int start = 0;
                int index;
                if (line.LineNumber <= NumHeaderLines)
                {
                    if (text.Length > 0)
                    {
                        while ((index = text.IndexOf(text, start)) >= 0)
                        {
                            base.ChangeLinePart(
                                lineStartOffset + index, // startOffset
                                lineStartOffset + index + text.Length, // endOffset
                                (VisualLineElement element) =>
                                {
                                    element.TextRunProperties.SetBackgroundBrush(Brushes.LightGray);

                                });
                            start = index + 1; // search for next occurrence
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Text changed event for recoloring the header area.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void inputTextEditor_TextChanged(object sender, EventArgs e)
        {
            ((ColorizeAvalonEdit2)this.colorizeAvalonEdit2Hash[sender]).UpdateNumHeaderLines((TextEditor)sender);
        }
        #endregion

        #region Load Syntax Highlighting Definitions
        /// <summary>
        /// Returns a regular expression for determining blue colored syntax highlighting.
        /// </summary>
        /// <returns>a regex</returns>
        private string generateBlueTextRegex()
        {
            string regex = "";
            foreach (var key in ParseEngine.TokenTypeToReflectionCallbackHash.Keys)
            {
                string subkey = ((System.Type)key).Name.ToString().ToLower();

                regex += @"<" + subkey + @"\b|";
                regex += @"/" + subkey + @">|";
            }
            regex = "(" + regex.Substring(0, regex.Length - 1) + ")";
            return regex;
        }

        /// <summary>
        /// Returns a regular expression for determining green colored syntax highlighting.
        /// </summary>
        /// <returns>a regex</returns>
        private string generateDarkMagentaTextRegex()
        {
            string regex = "";
            foreach (var key in ParseEngine.TokenTypeToReflectionCallbackHash.Keys)
            {
                Hashtable subhash = (Hashtable)ParseEngine.TokenTypeToReflectionCallbackHash[key];

                foreach (string subkey in subhash.Keys)
                {
                    regex += @"" + subkey.Replace(@".",@"\.") + @"|";
                }
            }
            regex = "(" + regex.Substring(0, regex.Length - 1) + ")";
            return regex;
        }

        /// <summary>
        /// Manually sets the syntax highlighting definitions using system reflection.
        /// </summary>
        /// <param name="textEditor"></param>
        private void loadHighlightingDefinitions(TextEditor textEditor)
        {
            if (dbtexmlHightlightingDefinition == null)
            {
                using (StreamReader s = new StreamReader(@"syntax_definitions\Db_TeXML.xshd"))
                {
                    using (XmlTextReader reader = new XmlTextReader(s))
                    {
                        dbtexmlHightlightingDefinition = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        dbtexmlHightlightingDefinition.MainRuleSet.Rules[dbtexmlHightlightingDefinition.MainRuleSet.Rules.Count - 2].Regex = new Regex(this.generateBlueTextRegex());
                        dbtexmlHightlightingDefinition.MainRuleSet.Rules[dbtexmlHightlightingDefinition.MainRuleSet.Rules.Count - 1].Regex = new Regex(this.generateDarkMagentaTextRegex());
                    }
                }
            }
            textEditor.SyntaxHighlighting = dbtexmlHightlightingDefinition;
        }
        #endregion

        #endregion

        #region General GUI Events (drag window, mouse down, etc.)
        private void BaseWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void txbServer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.txbDatabase.Focus(); 
        }

        private void txbDatabase_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.txbPort.Focus();
        }

        private void txbPort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.txbUserId.Focus(); 
        }

        private void txbUserId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.txbPassword.Focus(); 
        }

        private void txbPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                this.btnConnect.Focus(); 
        }

        private void txbPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            this.txbPassword.SelectAll();
        }

        /// <summary>
        /// Closes all other open windows so the application can will properly end when the user closes it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BaseWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (commandsListWindow != null)
                {
                    this.commandsListWindow.Close();
                }
                if (helpWindow != null)
                {
                    this.helpWindow.Close();
                }
            }
            catch { }
        }
        #endregion

        #region Database Auto-complete Dropdown Logic
        /// <summary>
        /// Displays the auto-complete window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txbDatabase_GotFocus(object sender, RoutedEventArgs e)
        {
            this.txbDatabaseAutoCompleteListBox.Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// Hides the auto-complete window if the last key pressed was not the down arrow key.
        /// Otherwise focus should transfer to the auto-complete window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txbDatabase_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!this.txbDatabase.LastKeyPressedWasDownKey)
            {
                this.txbDatabaseAutoCompleteListBox.Visibility = System.Windows.Visibility.Hidden;
                this.txbDatabase.LastKeyPressedWasDownKey = false;
            }
        }

        /// <summary>
        /// Hides the auto-complete window if it loses focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txbDatabaseAutoCompleteListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            this.txbDatabaseAutoCompleteListBox.Visibility = System.Windows.Visibility.Hidden;
        }

        /// <summary>
        /// Resets the auto-complete window list items if the database name text field value changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txbDatabase_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.resetListBoxItems(false);
        }

        /// <summary>
        /// Resets the auto-complete window list items.
        /// </summary>
        /// <param name="matchAny"></param>
        private void resetListBoxItems(bool matchAny)
        {
            this.txbDatabaseAutoCompleteListBox.Items.Clear();

            foreach (string key in this.connectionDetails.Keys)
            {
                if (key.Contains(this.txbDatabase.Text) || matchAny)
                {
                    if (!key.Equals("practice mode"))
                    {
                        this.txbDatabaseAutoCompleteListBox.Items.Add(key);
                    }
                }
            }

            this.txbDatabaseAutoCompleteListBox.Height = this.txbDatabaseAutoCompleteListBox.Items.Count * 20;
        }

        /// <summary>
        /// Sets all connection information if a user selects an auto-complete entry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txbDatabaseAutoCompleteListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.txbDatabaseAutoCompleteListBox.SelectedItem != null)
            {
                string databaseName = this.txbDatabaseAutoCompleteListBox.SelectedItem.ToString();

                string[] values = ((string)this.connectionDetails[databaseName]).Split('|');

                try
                {
                    this.SuppressSelectionChangedEvents = true;
                    this.selectCmbIndexFromValue(values[5]);
                    this.txbServer.Text = values[0];
                    this.txbPort.Text = values[1];
                    this.txbDatabase.Text = values[2];
                    this.txbUserId.Text = values[3];
                    this.txbPassword.Password = values[4];
                    this.SuppressSelectionChangedEvents = false;
                }
                catch (Exception ex)
                {
                    this.SuppressSelectionChangedEvents = false;
                    throw ex;
                }

                this.resetListBoxItems(true);

                this.txbDatabaseAutoCompleteListBox.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        /// <summary>
        /// Searches the cmbDatabaseType ComboBox for a matching value and sets the index if it finds one.
        /// </summary>
        /// <param name="value"></param>
        private void selectCmbIndexFromValue(string value)
        {
            for (int i = 0; i < this.cmbDatabaseType.Items.Count; i++)
            {
                ComboBoxItem item = (ComboBoxItem)this.cmbDatabaseType.Items[i];
                if (item.Content.Equals(value))
                {
                    this.cmbDatabaseType.SelectedIndex = i;
                    break;
                }
            }
        }
        #endregion

        #region Commands List Window / Help Window Button Events
        private void btnCommandsList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (commandsListWindow == null)
                {
                    this.commandsListWindow = new CommandsListWindow();
                }
                this.commandsListWindow.Show();
            }
            catch
            {
                MessageBox.Show(Messenger.GetMessage("CommandWindowInitError"), Messenger.GetMessage("CommandWindowInitErrorTitle"), MessageBoxButton.OK);
            }
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (helpWindow == null)
                {
                    this.helpWindow = new HelpWindow();
                }
                this.helpWindow.Show();
            }
            catch
            {
                MessageBox.Show(Messenger.GetMessage("HelpWindowInitError"), Messenger.GetMessage("HelpWindowInitErrorTitle"), MessageBoxButton.OK);
            }
        }
        #endregion

        #region ************Unit Tests************
        [Test]
        public void test_errorMessagesProperlyDefined()
        {
            bool isException = false;
            try
            {
                doNothingMessageBox(Messenger.GetMessage("CommandWindowInitError"), Messenger.GetMessage("CommandWindowInitErrorTitle"), MessageBoxButton.OK);
                doNothingMessageBox(Messenger.GetMessage("HelpWindowInitError"), Messenger.GetMessage("HelpWindowInitErrorTitle"), MessageBoxButton.OK);
                doNothingMessageBox(Messenger.GetMessage("SyntaxError"), Messenger.GetMessage("SyntaxErrorTitle"), MessageBoxButton.OK);
                doNothingMessageBox(Messenger.GetMessage("ResultsOpenError"), Messenger.GetMessage("ResultsOpenErrorTitle"), MessageBoxButton.OK);
                doNothingMessageBox(Messenger.GetMessage("ResultsSaveError"), Messenger.GetMessage("ResultsSaveErrorTitle"), MessageBoxButton.OK);
                doNothingMessageBox(Messenger.GetMessage("ScriptsSaveError"), Messenger.GetMessage("ScriptsSaveErrorTitle"), MessageBoxButton.OK);
                doNothingMessageBox(Messenger.GetMessage("ScriptsLoadError"), Messenger.GetMessage("ScriptsLoadErrorTitle"), MessageBoxButton.OK);
                doNothingMessageBox(Messenger.GetMessage("DatabaseConnectionError"), Messenger.GetMessage("DatabaseConnectionErrorTitle"), MessageBoxButton.OK);
                doNothingMessageBox(Messenger.GetMessage("PracticeModeInitError"), Messenger.GetMessage("PracticeModeInitErrorTitle"), MessageBoxButton.OK);
                doNothingMessageBox(Messenger.GetMessage("CommandWindowInitError"), Messenger.GetMessage("CommandWindowInitErrorTitle"), MessageBoxButton.OK);
            }
            catch
            {
                isException = true;
            }
            Assert.AreEqual(isException, false);
        }

        private void doNothingMessageBox(string message, string title, MessageBoxButton buttons)
        {

        }
        #endregion
    }
}
