using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.IO;
using System.Diagnostics;


//[module:CLSCompliant(true)]
namespace DGVPrinterHelper //AllocationRequest
{
    #region Supporting Classes

    /// <summary>
    /// Setup and implements logs for internal logging
    /// </summary>
    class LogManager
    {
        /// <summary>
        /// Path to log file
        /// </summary>
        private String basepath;
        public String BasePath
        {
            get { return basepath; }
            set { basepath = value; }
        }

        /// <summary>
        /// Header for log file name
        /// </summary>
        private String logheader;
        public String LogNameHeader
        {
            get { return logheader; }
            set { logheader = value; }
        }

        private int useFrame = 1;

        /// <summary>
        /// Define logging message categories
        /// </summary>
        public enum Categories
        {
            Info = 1,
            Warning,
            Error,
            Exception
        }

        /// <param name="userbasepath"></param>
        /// <param name="userlogname"></param>
        public LogManager(String userbasepath, String userlogname)
        {
            BasePath = String.IsNullOrEmpty(userbasepath) ? "." : userbasepath;
            LogNameHeader = String.IsNullOrEmpty(userlogname) ? "MsgLog" : userlogname;

            Log(Categories.Info, "********************* New Trace *********************");
        }

       
        /// <param name="category"></param>
        /// <param name="msg"></param>
        public void Log(Categories category, String msg)
        {
            // get call stack
            StackTrace stackTrace = new StackTrace();

            // get calling method name
            String caller = stackTrace.GetFrame(useFrame).GetMethod().Name;

            // log it
            LogWriter.Write(caller, category, msg, BasePath, LogNameHeader);

            // reset frame pointer
            useFrame = 1;
        }

      
        /// <param name="msg"></param>
        public void LogInfoMsg(String msg)
        {
            useFrame++; // bump up the stack frame pointer to skip this entry
            Log(Categories.Info, msg);
        }

        /// <param name="msg"></param>
        public void LogErrorMsg(String msg)
        {
            useFrame++; // bump up the stack frame pointer to skip this entry
            Log(Categories.Error, msg);
        }

    
        /// <param name="ex"></param>
        public void Log(Exception ex)
        {
            useFrame++; // bump up the stack frame pointer to skip this entry
            Log(Categories.Exception, String.Format("{0} from {1}", ex.Message, ex.Source));
        }
    }


    class LogWriter
    {
       
 
        private static String LogFileName(String name)
        {
            return String.Format("{0}_{1:yyyyMMdd}.Log", name, DateTime.Now);
        }

        /// <param name="from"></param>
        /// <param name="category"></param>
        /// <param name="msg"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        public static void Write(String from, LogManager.Categories category, String msg, String path, String name)
        {
            StringBuilder line = new StringBuilder();
            line.Append(DateTime.Now.ToShortDateString().ToString());
            line.Append("-");
            line.Append(DateTime.Now.ToLongTimeString().ToString());
            line.Append(", ");
            line.Append(category.ToString().PadRight(6, ' '));
            line.Append(",");
            line.Append(from.PadRight(13, ' '));
            line.Append(",");
            line.Append(msg);
            StreamWriter w = new StreamWriter(path + "\\" + LogFileName(name), true);
            w.WriteLine(line.ToString());
            w.Flush();
            w.Close();
        }
    }



    public class DGVCellDrawingEventArgs : EventArgs
    {
        public Graphics g;
        public RectangleF DrawingBounds;
        public DataGridViewCellStyle CellStyle;
        public int row;
        public int column;
        public Boolean Handled;

        public DGVCellDrawingEventArgs(Graphics g, RectangleF bounds, DataGridViewCellStyle style,
            int row, int column)
            : base()
        {
            this.g = g;
            DrawingBounds = bounds;
            CellStyle = style;
            this.row = row;
            this.column = column;
            Handled = false;
        }
    }


    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void CellOwnerDrawEventHandler(object sender, DGVCellDrawingEventArgs e);

    /// Hold Extension methods
    
    public static class Extensions
    {
  
        /// <typeparam name="?"></typeparam>
        /// <param name="list"></param>
        /// <param name="g"></param>
        /// <param name="pagewidth"></param>
        /// <param name="pageheight"></param>
        /// <param name="margins"></param>
        public static void DrawImbeddedImage<T>(this IEnumerable<T> list,
            Graphics g, int pagewidth, int pageheight, Margins margins)
        {
            foreach (T t in list)
            {
                if (t is DGVPrinter.ImbeddedImage)
                {
                    DGVPrinter.ImbeddedImage ii = (DGVPrinter.ImbeddedImage)Convert.ChangeType(t, typeof(DGVPrinter.ImbeddedImage));
                    
                    g.DrawImage(ii.theImage,
                        new Rectangle(ii.upperleft(pagewidth, pageheight, margins),
                        new Size(ii.theImage.Width, ii.theImage.Height)));
                }
            }
        }

    }

    #endregion


    

    public class DGVPrinter
    {
        public enum Alignment { NotSet, Left, Right, Center }
        public enum Location { Header, Footer, Absolute }
        public enum SizeType { CellSize, StringSize, Porportional }
        public enum PrintLocation { All, FirstOnly, LastOnly, None }

        //---------------------------------------------------------------------
        // internal classes/structs
        //---------------------------------------------------------------------
        #region Internal Classes


        enum paging { keepgoing, outofroom, datachange };

  
        public class ImbeddedImage
        {
            public Image theImage { get; set; }
            public Alignment ImageAlignment { get; set; }
            public Location ImageLocation { get; set; }
            public Int32 ImageX { get; set; }
            public Int32 ImageY { get; set; }

            internal Point upperleft(int pagewidth, int pageheight, Margins margins)
            {
                int y = 0;
                int x = 0;

    
                if (ImageLocation == Location.Absolute)
                    return new Point(ImageX, ImageY);

  
                switch (ImageLocation)
                {
                    case Location.Header:
                        y = margins.Top;
                        break;
                    case Location.Footer:
                        y = pageheight - theImage.Height - margins.Bottom;
                        break;
                    default:
                        throw new ArgumentException(String.Format("Unkown value: {0}", ImageLocation));
                }


                switch (ImageAlignment)
                {
                    case Alignment.Left:
                        x = margins.Left;
                        break;
                    case Alignment.Center:
                        x = (int)(pagewidth / 2 - theImage.Width / 2) + margins.Left;
                        break;
                    case Alignment.Right:
                        x = (int)(pagewidth - theImage.Width) + margins.Left;
                        break;
                    case Alignment.NotSet:
                        x = ImageX;
                        break;
                    default:
                        throw new ArgumentException(String.Format("Unkown value: {0}", ImageAlignment));
                }

                return new Point(x, y);
            }
        }

        public IList<ImbeddedImage> ImbeddedImageList = new List<ImbeddedImage>();

        
        class PageDef
        {
            public PageDef(Margins m, int count, int pagewidth)
            {
                columnindex = new List<int>(count);
                colstoprint = new List<object>(count);
                colwidths = new List<float>(count);
                colwidthsoverride = new List<float>(count);
                coltotalwidth = 0;
                margins = (Margins)m.Clone();
                pageWidth = pagewidth;
            }

            public List<int> columnindex;
            public List<object> colstoprint;
            public List<float> colwidths;
            public List<float> colwidthsoverride;
            public float coltotalwidth;
            public Margins margins;
            private int pageWidth;

            public int printWidth
            {
                get { return pageWidth - margins.Left - margins.Right; }
            }
        }
        IList<PageDef> pagesets;
        int currentpageset = 0;


        public class PrintDialogSettingsClass
        {
            public bool AllowSelection = true;
            public bool AllowSomePages = true;
            public bool AllowCurrentPage = true;
            public bool AllowPrintToFile = false;
            public bool ShowHelp = true;
            public bool ShowNetwork = true;
            public bool UseEXDialog = true;
        }


        public class rowdata
        {
            public DataGridViewRow row = null;
            public float height = 0;
            public bool pagebreak = false;
            public bool splitrow = false;
        }
        #endregion

        #region global variables


        DataGridView dgv = null;


        PrintDocument printDoc = null;

        // logging 
        LogManager Logger = null;

        // print status items
        Boolean EmbeddedPrinting = false;
        List<rowdata> rowstoprint;
        IList colstoprint;          // divided into pagesets for printing
        int lastrowprinted = -1;
        int currentrow = -1;
        int fromPage = 0;
        int toPage = -1;
        const int maxPages = 2147483647;

      
        int pageHeight = 0;
        float staticheight = 0;
        float rowstartlocation = 0;
        int pageWidth = 0;
        int printWidth = 0;
        float rowheaderwidth = 0;
        int CurrentPage = 0;
        int totalpages;
        PrintRange printRange;


        //private float headerHeight = 0;
        private float footerHeight = 0;
        private float pagenumberHeight = 0;
        private float colheaderheight = 0;
        //private List<float> rowheights;
        private List<float> colwidths;
        //private List<List<SizeF>> cellsizes;

        #endregion

        #region properties

        #region global properties



  
        protected Boolean enablelogging;
        public Boolean EnableLogging
        {
            get { return enablelogging; }
            set
            {
                enablelogging = value;
                if (enablelogging)
                {
                    Logger = new LogManager(".", "DGVPrinter");
                }
            }
        }



        public String LogDirectory
        {
            get
            {
                if (null != Logger)
                    return Logger.BasePath;
                else
                    return null;
            }
            set
            {
                if (null == Logger)
                    EnableLogging = true;
                Logger.BasePath = value;
            }
        }


    

        public event CellOwnerDrawEventHandler OwnerDraw;


        /// </summary>
        //public Form Owner
        //{ get; set; }
        protected Form _Owner = null;
        public Form Owner
        {
            get { return _Owner; }
            set { _Owner = value; }
        }


        //public Double PrintPreviewZoom
        //{ get; set; }
        protected Double _PrintPreviewZoom = 1.0;
        public Double PrintPreviewZoom
        {
            get { return _PrintPreviewZoom; }
            set { _PrintPreviewZoom = value; }
        }



        public PrinterSettings PrintSettings
        {
            get { return printDoc.PrinterSettings; }
        }


        private PrintDialogSettingsClass printDialogSettings = new PrintDialogSettingsClass();
        public PrintDialogSettingsClass PrintDialogSettings
        {
            get { return printDialogSettings; }
        }

        private String printerName;
        public String PrinterName
        {
            get { return printerName; }
            set { printerName = value; }
        }


        public PrintDocument printDocument
        {
            get { return printDoc; }
            set { printDoc = value; }
        }

        private Icon ppvIcon = null;
        public Icon PreviewDialogIcon
        {
            get { return ppvIcon; }
            set { ppvIcon = value; }
        }

        private PrintPreviewDialog previewdialog = null;
        public PrintPreviewDialog PreviewDialog
        {
            get { return previewdialog; }
            set { previewdialog = value; }
        }

        private Boolean printHeader = true;
        public Boolean PrintHeader
        {
            get { return printHeader; }
            set { printHeader = value; }
        }

        private float HeaderHeight
        {
            get
            {
                float headerheight = 0;
                headerheight += TitleHeight + SubTitleHeight;


                if ((bool)PrintColumnHeaders)
                {
                    headerheight += colheaderheight;
                }


                return headerheight;
            }
        }


        private Boolean printFooter = true;
        public Boolean PrintFooter
        {
            get { return printFooter; }
            set { printFooter = value; }
        }
        private Boolean? printColumnHeaders;
        public Boolean? PrintColumnHeaders
        {
            get { return printColumnHeaders; }
            set { printColumnHeaders = value; }
        }


        private Boolean? printRowHeaders = false;
        public Boolean? PrintRowHeaders
        {
            get { return printRowHeaders; }
            set { printRowHeaders = value; }
        }

        private Boolean keepRowsTogether = true;
        public Boolean KeepRowsTogether
        {
            get { return keepRowsTogether; }
            set { keepRowsTogether = value; }
        }

        private float keeprowstogethertolerance = 15;
        public float KeepRowsTogetherTolerance
        {
            get { return keeprowstogethertolerance; }
            set { keeprowstogethertolerance = value; }
        }

        #endregion

        // Title
        #region title properties

        // override flag
        bool overridetitleformat = false;


        float titleheight = 0;

        private String title;
        public String Title
        {
            get { return title; }
            set
            {
                title = value;
                if (docName == null)
                {
                    printDoc.DocumentName = value;
                }
            }
        }


        private String docName;
        public String DocName
        {
            get { return docName; }
            set { printDoc.DocumentName = value; docName = value; }
        }

        private Font titlefont;
        public Font TitleFont
        {
            get { return titlefont; }
            set { titlefont = value; }
        }

        private Color titlecolor;
        public Color TitleColor
        {
            get { return titlecolor; }
            set { titlecolor = value; }
        }

        private StringFormat titleformat;
        public StringFormat TitleFormat
        {
            get { return titleformat; }
            set { titleformat = value; overridetitleformat = true; }
        }

        

        public StringAlignment TitleAlignment
        {
            get { return titleformat.Alignment; }
            set
            {
                titleformat.Alignment = value;
                overridetitleformat = true;
            }
        }

  

        public StringFormatFlags TitleFormatFlags
        {
            get { return titleformat.FormatFlags; }
            set
            {
                titleformat.FormatFlags = value;
                overridetitleformat = true;
            }
        }

        
        private PrintLocation titleprint = PrintLocation.All;
        public PrintLocation TitlePrint
        {
            get { return titleprint; }
            set { titleprint = value; }
        }

        private float TitleHeight
        {
            get
            {
                if (PrintLocation.All == TitlePrint)
                    return titleheight + titlespacing;

                if ((PrintLocation.FirstOnly == TitlePrint) && (1 == CurrentPage))
                    return titleheight + titlespacing;

                if ((PrintLocation.LastOnly == TitlePrint) && (totalpages == CurrentPage))
                    return titleheight + titlespacing;

                return 0;
            }
        }

      
        private float titlespacing;
        public float TitleSpacing
        {
            get { return titlespacing; }
            set { titlespacing = value; }
        }

    
        private Brush titlebackground;
        public Brush TitleBackground
        {
            get { return titlebackground; }
            set { titlebackground = value; }
        }

 
        private Pen titleborder;
        public Pen TitleBorder
        {
            get { return titleborder; }
            set { titleborder = value; }
        }

        #endregion

        // SubTitle
        #region subtitle properties

        // override flat
        bool overridesubtitleformat = false;

  
        float subtitleheight = 0;

        private String subtitle;
        public String SubTitle
        {
            get { return subtitle; }
            set { subtitle = value; }
        }

        
        private Font subtitlefont;
        public Font SubTitleFont
        {
            get { return subtitlefont; }
            set { subtitlefont = value; }
        }

        private Color subtitlecolor;
        public Color SubTitleColor
        {
            get { return subtitlecolor; }
            set { subtitlecolor = value; }
        }

        private StringFormat subtitleformat;
        public StringFormat SubTitleFormat
        {
            get { return subtitleformat; }
            set { subtitleformat = value; overridesubtitleformat = true; }
        }

        

        public StringAlignment SubTitleAlignment
        {
            get { return subtitleformat.Alignment; }
            set
            {
                subtitleformat.Alignment = value;
                overridesubtitleformat = true;
            }
        }

        

        public StringFormatFlags SubTitleFormatFlags
        {
            get { return subtitleformat.FormatFlags; }
            set
            {
                subtitleformat.FormatFlags = value;
                overridesubtitleformat = true;
            }
        }



        private PrintLocation subtitleprint = PrintLocation.All;
        public PrintLocation SubTitlePrint
        {
            get { return subtitleprint; }
            set { subtitleprint = value; }
        }


        private float SubTitleHeight
        {
            get
            {
                if (PrintLocation.All == SubTitlePrint)
                    return subtitleheight + subtitlespacing;

                if ((PrintLocation.FirstOnly == SubTitlePrint) && (1 == CurrentPage))
                    return subtitleheight + subtitlespacing;

                if ((PrintLocation.LastOnly == SubTitlePrint) && (totalpages == CurrentPage))
                    return subtitleheight + subtitlespacing;

                return 0;
            }
        }

   

        private float subtitlespacing;
        public float SubTitleSpacing
        {
            get { return subtitlespacing; }
            set { subtitlespacing = value; }
        }


        private Brush subtitlebackground;
        public Brush SubTitleBackground
        {
            get { return subtitlebackground; }
            set { subtitlebackground = value; }
        }


        private Pen subtitleborder;
        public Pen SubTitleBorder
        {
            get { return subtitleborder; }
            set { subtitleborder = value; }
        }

        #endregion

     
        #region footer properties


        bool overridefooterformat = false;

        private String footer;
        public String Footer
        {
            get { return footer; }
            set { footer = value; }
        }
        private Font footerfont;
        public Font FooterFont
        {
            get { return footerfont; }
            set { footerfont = value; }
        }

        private Color footercolor;
        public Color FooterColor
        {
            get { return footercolor; }
            set { footercolor = value; }
        }

        
 
        private StringFormat footerformat;
        public StringFormat FooterFormat
        {
            get { return footerformat; }
            set { footerformat = value; overridefooterformat = true; }
        }
        public StringAlignment FooterAlignment
        {
            get { return footerformat.Alignment; }
            set
            {
                footerformat.Alignment = value;
                overridefooterformat = true;
            }
        }

        
  
        public StringFormatFlags FooterFormatFlags
        {
            get { return footerformat.FormatFlags; }
            set
            {
                footerformat.FormatFlags = value;
                overridefooterformat = true;
            }
        }

        private float footerspacing;
        public float FooterSpacing
        {
            get { return footerspacing; }
            set { footerspacing = value; }
        }

      
        private PrintLocation footerprint = PrintLocation.All;
        public PrintLocation FooterPrint
        {
            get { return footerprint; }
            set { footerprint = value; }
        }

        private float FooterHeight
        {
            get
            {
                float footerheight = 0;

                // return calculated height if we're printing the footer
                if ((PrintLocation.All == FooterPrint)
                    || ((PrintLocation.FirstOnly == FooterPrint) && (1 == CurrentPage))
                    || ((PrintLocation.LastOnly == FooterPrint) && (totalpages == CurrentPage)))
                {
                 
                    footerheight += footerHeight + FooterSpacing;
                }

                return footerheight;
            }
        }

        private Brush footerbackground;
        public Brush FooterBackground
        {
            get { return footerbackground; }
            set { footerbackground = value; }
        }

        
        private Pen footerborder;
        public Pen FooterBorder
        {
            get { return footerborder; }
            set { footerborder = value; }
        }

        #endregion

        #region page number properties


        bool overridepagenumberformat = false;

        private bool pageno = true;
        public bool PageNumbers
        {
            get { return pageno; }
            set { pageno = value; }
        }

        private Font pagenofont;
        public Font PageNumberFont
        {
            get { return pagenofont; }
            set { pagenofont = value; }
        }

    
   
        private Color pagenocolor;
        public Color PageNumberColor
        {
            get { return pagenocolor; }
            set { pagenocolor = value; }
        }

     
        private StringFormat pagenumberformat;
        public StringFormat PageNumberFormat
        {
            get { return pagenumberformat; }
            set { pagenumberformat = value; overridepagenumberformat = true; }
        }
 
    
        public StringAlignment PageNumberAlignment
        {
            get { return pagenumberformat.Alignment; }
            set
            {
                pagenumberformat.Alignment = value;
                overridepagenumberformat = true;
            }
        }

       
      
        public StringFormatFlags PageNumberFormatFlags
        {
            get { return pagenumberformat.FormatFlags; }
            set
            {
                pagenumberformat.FormatFlags = value;
                overridepagenumberformat = true;
            }
        }

 
        private bool pagenumberontop = false;
        public bool PageNumberInHeader
        {
            get { return pagenumberontop; }
            set { pagenumberontop = value; }
        }

     
     
        private bool pagenumberonseparateline = false;
        public bool PageNumberOnSeparateLine
        {
            get { return pagenumberonseparateline; }
            set { pagenumberonseparateline = value; }
        }

     
        private bool showtotalpagenumber = false;
        public bool ShowTotalPageNumber
        {
            get { return showtotalpagenumber; }
            set { showtotalpagenumber = value; }
        }

      
        private String pageseparator = " of ";
        public String PageSeparator
        {
            get { return pageseparator; }
            set { pageseparator = value; }
        }

        private String pagetext = "Page ";
        public String PageText
        {
            get { return pagetext; }
            set { pagetext = value; }
        }

        private String parttext = " - Part ";
        public String PartText
        {
            get { return parttext; }
            set { parttext = value; }
        }

        private PrintLocation pagenumberprint = PrintLocation.All;
        public PrintLocation PageNumberPrint
        {
            get { return pagenumberprint; }
            set { pagenumberprint = value; }
        }

      
        private float PageNumberHeight
        {
            get
            {
                if ((PrintLocation.All == PageNumberPrint)
                    || ((PrintLocation.FirstOnly == PageNumberPrint) && (1 == CurrentPage))
                    || ((PrintLocation.LastOnly == PageNumberPrint) && (totalpages == CurrentPage)))
                {
             
                 
                    if (pagenumberonseparateline)
                        return pagenumberHeight;
                    else if (pagenumberontop && 0 == TitleHeight && 0 == SubTitleHeight)
                    {
                        return pagenumberHeight;
                    }
                    else if (!pagenumberontop && 0 == FooterHeight)
                    {
                        return footerspacing + pagenumberHeight;
                    }
                }

                return 0;
            }
        }

        #endregion

    
        #region header cell properties

        private DataGridViewCellStyle rowheaderstyle;
        public DataGridViewCellStyle RowHeaderCellStyle
        {
            get { return rowheaderstyle; }
            set { rowheaderstyle = value; }
        }

       
 
        private StringFormat rowheadercellformat = null;
        public StringFormat GetRowHeaderCellFormat(DataGridView grid)
        {
          
            if ((null != grid) && (null == rowheadercellformat))
            {
                buildstringformat(ref rowheadercellformat, grid.Rows[0].HeaderCell.InheritedStyle,
                    headercellalignment, StringAlignment.Near, headercellformatflags,
                    StringTrimming.Word);
            }

       
            if (null == rowheadercellformat)
                rowheadercellformat = new StringFormat(headercellformatflags);

            return rowheadercellformat;
        }

       
        private String rowheadercelldefaulttext = "\t";
        public String RowHeaderCellDefaultText
        {
            get { return rowheadercelldefaulttext; }
            set { rowheadercelldefaulttext = value; }
        }

        private Dictionary<string, DataGridViewCellStyle> columnheaderstyles =
            new Dictionary<string, DataGridViewCellStyle>();
        public Dictionary<string, DataGridViewCellStyle> ColumnHeaderStyles
        {
            get { return columnheaderstyles; }
        }


        private StringFormat columnheadercellformat = null;
        public StringFormat GetColumnHeaderCellFormat(DataGridView grid)
        {
      
            if ((null != grid) && (null == columnheadercellformat))
            {
                buildstringformat(ref columnheadercellformat, grid.Columns[0].HeaderCell.InheritedStyle,
                    headercellalignment, StringAlignment.Near, headercellformatflags,
                    StringTrimming.Word);
            }


            if (null == columnheadercellformat)
                columnheadercellformat = new StringFormat(headercellformatflags);

            return columnheadercellformat;
        }

 
  
        private StringAlignment headercellalignment;
        public StringAlignment HeaderCellAlignment
        {
            get { return headercellalignment; }
            set { headercellalignment = value; }
        }


    
        private StringFormatFlags headercellformatflags;
        public StringFormatFlags HeaderCellFormatFlags
        {
            get { return headercellformatflags; }
            set { headercellformatflags = value; }
        }
        #endregion


        #region cell properties

   
        private StringFormat cellformat = null;
        public StringFormat GetCellFormat(DataGridView grid)
        {
        
            if ((null != grid) && (null == cellformat))
            {
                buildstringformat(ref cellformat, grid.Rows[0].Cells[0].InheritedStyle,
                    cellalignment, StringAlignment.Near, cellformatflags,
                    StringTrimming.Word);
            }


            if (null == cellformat)
                cellformat = new StringFormat(cellformatflags);

            return cellformat;
        }


 
        private StringAlignment cellalignment;
        public StringAlignment CellAlignment
        {
            get { return cellalignment; }
            set { cellalignment = value; }
        }


        
        private StringFormatFlags cellformatflags;
        public StringFormatFlags CellFormatFlags
        {
            get { return cellformatflags; }
            set { cellformatflags = value; }
        }

        private List<float> colwidthsoverride = new List<float>();
        private Dictionary<string, float> publicwidthoverrides = new Dictionary<string, float>();
        public Dictionary<string, float> ColumnWidths
        {
            get { return publicwidthoverrides; }
        }

        private Dictionary<string, DataGridViewCellStyle> colstyles =
            new Dictionary<string, DataGridViewCellStyle>();
        public Dictionary<string, DataGridViewCellStyle> ColumnStyles
        {
            get { return colstyles; }
        }

    
        private Dictionary<string, DataGridViewCellStyle> altrowcolstyles =
            new Dictionary<string, DataGridViewCellStyle>();
        public Dictionary<string, DataGridViewCellStyle> AlternatingRowColumnStyles
        {
            get { return altrowcolstyles; }
        }


   
        private List<int> fixedcolumns = new List<int>();
        private List<string> fixedcolumnnames = new List<string>();
        public List<string> FixedColumns
        {
            get { return fixedcolumnnames; }
        }

        private List<String> hidecolumns = new List<string>();
        public List<String> HideColumns
        {
            get { return hidecolumns; }
        }

   
        private object oldvalue = null;
        private String breakonvaluechange;
        public String BreakOnValueChange
        {
            get { return breakonvaluechange; }
            set { breakonvaluechange = value; }
        }

        #endregion

        #region page level properties

        public Margins PrintMargins
        {
            get { return PageSettings.Margins; }
            set { PageSettings.Margins = value; }
        }

  
        public PageSettings PageSettings
        {
            get { return printDoc.DefaultPageSettings; }
        }


    
        private bool porportionalcolumns = false;
        public bool PorportionalColumns
        {
            get { return porportionalcolumns; }
            set
            {
                porportionalcolumns = value;
                if (porportionalcolumns)
                    ColumnWidth = ColumnWidthSetting.Porportional;
                else
                    ColumnWidth = ColumnWidthSetting.CellWidth;
            }
        }

        private Alignment tablealignment = Alignment.NotSet;
        public Alignment TableAlignment
        {
            get { return tablealignment; }
            set { tablealignment = value; }
        }

        public enum RowHeightSetting { DataHeight, CellHeight }
        private RowHeightSetting _rowheight = RowHeightSetting.DataHeight;
        public RowHeightSetting RowHeight
        {
            get { return _rowheight; }
            set { _rowheight = value; }
        }


     
        public enum ColumnWidthSetting { DataWidth, CellWidth, Porportional }
        private ColumnWidthSetting _rowwidth = ColumnWidthSetting.CellWidth;
        public ColumnWidthSetting ColumnWidth
        {
            get { return _rowwidth; }
            set
            {
                _rowwidth = value;
                if (value == ColumnWidthSetting.Porportional)
                    porportionalcolumns = true;
                else
                    porportionalcolumns = false;
            }
        }

        #endregion


        #region

   
        private int PreviewDisplayWidth()
        {
            double displayWidth = printDoc.DefaultPageSettings.Bounds.Width
                + 3 * printDoc.DefaultPageSettings.HardMarginY;
            return (int)(displayWidth * PrintPreviewZoom);
        }


      
        private int PreviewDisplayHeight()
        {
            double displayHeight = printDoc.DefaultPageSettings.Bounds.Height
                + 3 * printDoc.DefaultPageSettings.HardMarginX;

            return (int)(displayHeight * PrintPreviewZoom);
        }


     
        protected virtual void OnCellOwnerDraw(DGVCellDrawingEventArgs e)
        {
            if (null != OwnerDraw)
                OwnerDraw(this, e);
        }



        protected DataGridViewCellStyle GetStyle(DataGridViewRow row, DataGridViewColumn col)
        {

            DataGridViewCellStyle colstyle = row.Cells[col.Index].InheritedStyle.Clone();


            if (ColumnStyles.ContainsKey(col.Name))
            {
                colstyle = ColumnStyles[col.Name];
            }

 
            if (0 != (row.Index & 1) && AlternatingRowColumnStyles.ContainsKey(col.Name))
            {
                colstyle = AlternatingRowColumnStyles[col.Name];
            }

            return colstyle;
        }

    

        protected int GetColumnIndex(string colname)
        {
            int i = 0;
            foreach (DataGridViewColumn col in colstoprint)
            {
                if (col.Name != colname)
                {
                    i++;
                }
                else
                {
                    break;
                }
            }


            if (i >= colstoprint.Count)
            {
                throw new Exception("Unknown Column Name: " + colname);
            }

            return i;
        }

        #endregion

        #endregion

        //---------------------------------------------------------------------
        // Constructor
        //---------------------------------------------------------------------
        
        public DGVPrinter()
        {

            printDoc = new PrintDocument();
   
            PrintMargins = new Margins(60, 60, 40, 40);


            pagenofont = new Font("Tahoma", 8, FontStyle.Regular, GraphicsUnit.Point);
            pagenocolor = Color.Black;
            titlefont = new Font("Tahoma", 18, FontStyle.Bold, GraphicsUnit.Point);
            titlecolor = Color.Black;
            subtitlefont = new Font("Tahoma", 12, FontStyle.Bold, GraphicsUnit.Point);
            subtitlecolor = Color.Black;
            footerfont = new Font("Tahoma", 10, FontStyle.Bold, GraphicsUnit.Point);
            footercolor = Color.Black;

  
            titlespacing = 0;
            subtitlespacing = 0;
            footerspacing = 0;


            buildstringformat(ref titleformat, null, StringAlignment.Center, StringAlignment.Center,
                StringFormatFlags.NoWrap | StringFormatFlags.LineLimit | StringFormatFlags.NoClip, StringTrimming.Word);
            buildstringformat(ref subtitleformat, null, StringAlignment.Center, StringAlignment.Center,
                StringFormatFlags.NoWrap | StringFormatFlags.LineLimit | StringFormatFlags.NoClip, StringTrimming.Word);
            buildstringformat(ref footerformat, null, StringAlignment.Center, StringAlignment.Center,
                StringFormatFlags.NoWrap | StringFormatFlags.LineLimit | StringFormatFlags.NoClip, StringTrimming.Word);
            buildstringformat(ref pagenumberformat, null, StringAlignment.Far, StringAlignment.Center,
                StringFormatFlags.NoWrap | StringFormatFlags.LineLimit | StringFormatFlags.NoClip, StringTrimming.Word);


            columnheadercellformat = null;
            rowheadercellformat = null;
            cellformat = null;


            Owner = null;
            PrintPreviewZoom = 1.0;


            headercellalignment = StringAlignment.Near;
            headercellformatflags = StringFormatFlags.LineLimit | StringFormatFlags.NoClip;
            cellalignment = StringAlignment.Near;
            cellformatflags = StringFormatFlags.LineLimit | StringFormatFlags.NoClip;
        }





        public void PrintDataGridView(DataGridView dgv)
        {
            if (EnableLogging) Logger.LogInfoMsg("PrintDataGridView process started");
            if (null == dgv) throw new Exception("Null Parameter passed to DGVPrinter.");
            if (!(typeof(DataGridView).IsInstanceOfType(dgv)))
                throw new Exception("Invalid Parameter passed to DGVPrinter.");

            // save the datagridview we're printing
            this.dgv = dgv;

            // display dialog and print
            if (DialogResult.OK == DisplayPrintDialog())
            {
                PrintNoDisplay(dgv);
            }
        }

        public void PrintPreviewDataGridView(DataGridView dgv)
        {
            if (EnableLogging) Logger.LogInfoMsg("PrintPreviewDataGridView process started");
            if (null == dgv) throw new Exception("Null Parameter passed to DGVPrinter.");
            if (!(typeof(DataGridView).IsInstanceOfType(dgv)))
                throw new Exception("Invalid Parameter passed to DGVPrinter.");


            this.dgv = dgv;


            if (DialogResult.OK == DisplayPrintDialog())
            {
                PrintPreviewNoDisplay(dgv);
            }
        }

  
   
    
        public DialogResult DisplayPrintDialog()
        {
            if (EnableLogging) Logger.LogInfoMsg("DisplayPrintDialog process started");

            PrintDialog pd = new PrintDialog();
            pd.UseEXDialog = printDialogSettings.UseEXDialog;
            pd.AllowSelection = printDialogSettings.AllowSelection;
            pd.AllowSomePages = printDialogSettings.AllowSomePages;
            pd.AllowCurrentPage = printDialogSettings.AllowCurrentPage;
            pd.AllowPrintToFile = printDialogSettings.AllowPrintToFile;
            pd.ShowHelp = printDialogSettings.ShowHelp;
            pd.ShowNetwork = printDialogSettings.ShowNetwork;


            pd.Document = printDoc;
            if (!String.IsNullOrEmpty(printerName))
                printDoc.PrinterSettings.PrinterName = printerName;

            // show the dialog and display the result
            return pd.ShowDialog();
        }

     
        /// <param name="dgv"></param>
        public void PrintNoDisplay(DataGridView dgv)
        {
            if (EnableLogging) Logger.LogInfoMsg("PrintNoDisplay process started");
            if (null == dgv) throw new Exception("Null Parameter passed to DGVPrinter.");
            if (!(dgv is DataGridView))
                throw new Exception("Invalid Parameter passed to DGVPrinter.");


            this.dgv = dgv;

            printDoc.PrintPage += new PrintPageEventHandler(PrintPageEventHandler);
            printDoc.BeginPrint += new PrintEventHandler(BeginPrintEventHandler);


            SetupPrint();
            printDoc.Print();
        }

       

        public void PrintPreviewNoDisplay(DataGridView dgv)
        {
            if (EnableLogging) Logger.LogInfoMsg("PrintPreviewNoDisplay process started");
            if (null == dgv) throw new Exception("Null Parameter passed to DGVPrinter.");
            if (!(dgv is DataGridView))
                throw new Exception("Invalid Parameter passed to DGVPrinter.");


            this.dgv = dgv;

            printDoc.PrintPage += new PrintPageEventHandler(PrintPageEventHandler);
            printDoc.BeginPrint += new PrintEventHandler(BeginPrintEventHandler);

            SetupPrint();


            if (null == PreviewDialog)
                PreviewDialog = new PrintPreviewDialog();

            PreviewDialog.Document = printDoc;
            PreviewDialog.UseAntiAlias = true;
            PreviewDialog.Owner = Owner;
            PreviewDialog.PrintPreviewControl.Zoom = PrintPreviewZoom;
            PreviewDialog.Width = PreviewDisplayWidth();
            PreviewDialog.Height = PreviewDisplayHeight();

            if (null != ppvIcon)
                PreviewDialog.Icon = ppvIcon;

            // show the dialog
            PreviewDialog.ShowDialog();
        }


    


        public Boolean EmbeddedPrint(DataGridView dgv, Graphics g, Rectangle area)
        {
            if (EnableLogging) Logger.LogInfoMsg("EmbeddedPrint process started");
   
            if ((null == dgv))
                throw new Exception("Null Parameter passed to DGVPrinter.");

 
            EmbeddedPrinting = true;


            this.dgv = dgv;



            Margins saveMargins = PrintMargins;
            PrintMargins.Top = area.Top;
            PrintMargins.Bottom = 0;
            PrintMargins.Left = area.Left;
            PrintMargins.Right = 0;


            pageHeight = area.Height + area.Top;
            printWidth = area.Width;
            pageWidth = area.Width + area.Left;


            PrintHeader = false;
            PrintFooter = false;
            pageno = false;

  
            SetupPrint();

       
            PrintPage(g);
            return false;
        }

        public void EmbeddedPrintMultipageSetup(DataGridView dgv, Rectangle area)
        {
            if (EnableLogging) Logger.LogInfoMsg("EmbeddedPrintMultipageSetup process started");

            if ((null == dgv))
                throw new Exception("Null Parameter passed to DGVPrinter.");


            EmbeddedPrinting = true;


            this.dgv = dgv;

    


            Margins saveMargins = PrintMargins;
            PrintMargins.Top = area.Top;
            PrintMargins.Bottom = 0;
            PrintMargins.Left = area.Left;
            PrintMargins.Right = 0;


            pageHeight = area.Height + area.Top;
            printWidth = area.Width;
            pageWidth = area.Width + area.Left;

         
            PrintHeader = false;
            PrintFooter = false;
            pageno = false;

            
            SetupPrint();
        }

 
        public void BeginPrintEventHandler(object sender, PrintEventArgs e)
        {
            if (EnableLogging) Logger.LogInfoMsg("BeginPrintEventHandler called. Printing started.");
            // reset counters since we'll go through this twice if we print from preview
            currentpageset = 0;
            lastrowprinted = -1;
            CurrentPage = 0;
        }

    
        public void PrintPageEventHandler(object sender, PrintPageEventArgs e)
        {
            if (EnableLogging) Logger.LogInfoMsg("PrintPageEventHandler called. Printing a page.");
            e.HasMorePages = PrintPage(e.Graphics);
        }


        void SetupPrint()
        {
            if (EnableLogging)
            {
                Logger.LogInfoMsg("SetupPrint process started");
                var m = printDoc.DefaultPageSettings.Margins;
                Logger.LogInfoMsg(String.Format("Initial Printer Margins are {0}, {1}, {2}, {3}", m.Left, m.Right, m.Top, m.Bottom));
            }

            if (null == PrintColumnHeaders)
                PrintColumnHeaders = dgv.ColumnHeadersVisible;

            if (null == PrintRowHeaders)
                PrintRowHeaders = dgv.RowHeadersVisible;

            if ((null == RowHeaderCellStyle) && (0 != dgv.Rows.Count))
                RowHeaderCellStyle = dgv.Rows[0].HeaderCell.InheritedStyle;


            if (null == columnheadercellformat)
                buildstringformat(ref columnheadercellformat, dgv.Columns[0].HeaderCell.InheritedStyle,
                    headercellalignment, StringAlignment.Near, headercellformatflags,
                    StringTrimming.Word);
            if (null == rowheadercellformat)
                buildstringformat(ref rowheadercellformat, RowHeaderCellStyle,
                    headercellalignment, StringAlignment.Near, headercellformatflags,
                    StringTrimming.Word);
            if (null == cellformat)
                buildstringformat(ref cellformat, dgv.DefaultCellStyle,
                    cellalignment, StringAlignment.Near, cellformatflags,
                    StringTrimming.Word);

            if (!EmbeddedPrinting)
            {
                int printareawidth;
                int hardx = (int)Math.Round(printDoc.DefaultPageSettings.HardMarginX);
                int hardy = (int)Math.Round(printDoc.DefaultPageSettings.HardMarginY);
                if (printDoc.DefaultPageSettings.Landscape)
                    printareawidth = (int)Math.Round(printDoc.DefaultPageSettings.PrintableArea.Height);
                else
                    printareawidth = (int)Math.Round(printDoc.DefaultPageSettings.PrintableArea.Width);


                pageHeight = printDoc.DefaultPageSettings.Bounds.Height;
                pageWidth = printDoc.DefaultPageSettings.Bounds.Width;

            
                PrintMargins = printDoc.DefaultPageSettings.Margins;

                PrintMargins.Right = (hardx > PrintMargins.Right) ? hardx : PrintMargins.Right;
                PrintMargins.Left = (hardx > PrintMargins.Left) ? hardx : PrintMargins.Left;
                PrintMargins.Top = (hardy > PrintMargins.Top) ? hardy : PrintMargins.Top;
                PrintMargins.Bottom = (hardy > PrintMargins.Bottom) ? hardy : PrintMargins.Bottom;

                printWidth = pageWidth - PrintMargins.Left - PrintMargins.Right;
                printWidth = (printWidth > printareawidth) ? printareawidth : printWidth;


                if (EnableLogging)
                {
                    Logger.LogInfoMsg(String.Format("Printer 'Hard' X limit is {0} and 'Hard' Y limit is {1}", hardx, hardy));
                    Logger.LogInfoMsg(String.Format("Printer height limit is {0} and width limit is {1}, print width is {2}",
                        pageHeight, pageWidth, printWidth));
                    Logger.LogInfoMsg(String.Format("Final overall margins are {0}, {1}, {2}, {3}",
                        PrintMargins.Left, PrintMargins.Right, PrintMargins.Top, PrintMargins.Bottom));
                    Logger.LogInfoMsg(String.Format("Table Alignment is {0}", TableAlignment.ToString()));
                }
            }




            printRange = printDoc.PrinterSettings.PrintRange;
            if (EnableLogging) Logger.LogInfoMsg(String.Format("PrintRange is {0}", printRange));


            if (PrintRange.SomePages == printRange)
            {

                fromPage = printDoc.PrinterSettings.FromPage;
                toPage = printDoc.PrinterSettings.ToPage;
            }
            else
            {

                fromPage = 0;
                toPage = maxPages;
            }

           
            SetupPrintRange();


            SetupColumns();

        


            measureprintarea(printDoc.PrinterSettings.CreateMeasurementGraphics());

            totalpages = Pagination();

        }

   
        
        private void SetupColumns()
        {
            // identify fixed columns by their column number in the print list
            foreach (string colname in fixedcolumnnames)
            {
                try
                {
                    fixedcolumns.Add(GetColumnIndex(colname));
                }
                catch// (Exception ex)
                {
                    // missing column, so add it to print list and retry
                    colstoprint.Add(dgv.Columns[colname]);
                    fixedcolumns.Add(GetColumnIndex(colname));
                }
            }

            // Adjust override list to have the same number of entries as colstoprint,
            foreach (DataGridViewColumn col in colstoprint)
                if (publicwidthoverrides.ContainsKey(col.Name))
                    colwidthsoverride.Add(publicwidthoverrides[col.Name]);
                else
                    colwidthsoverride.Add(-1);

        }

     
        private void SetupPrintRange()
        {
        
            SortedList temprowstoprint = null;
            SortedList tempcolstoprint = null;

            // rows to print (handles "selection" and "current page" options
            if (PrintRange.Selection == printRange)
            {
                temprowstoprint = new SortedList(dgv.SelectedCells.Count);
                tempcolstoprint = new SortedList(dgv.SelectedCells.Count);

                //if DGV has rows selected, it's easy, selected rows and all visible columns
                if (0 != dgv.SelectedRows.Count)
                {
                    temprowstoprint = new SortedList(dgv.SelectedRows.Count);
                    tempcolstoprint = new SortedList(dgv.Columns.Count);

                    // sort the rows into index order
                    temprowstoprint = new SortedList(dgv.SelectedRows.Count);
                    foreach (DataGridViewRow row in dgv.SelectedRows)
                        if (row.Visible && !row.IsNewRow)
                            temprowstoprint.Add(row.Index, row);

                    // sort the columns into display order
                    foreach (DataGridViewColumn col in dgv.Columns) if (col.Visible) tempcolstoprint.Add(col.DisplayIndex, col);
                }
                // if selected columns, then all rows, and selected columns
                else if (0 != dgv.SelectedColumns.Count)
                {
                    temprowstoprint = new SortedList(dgv.Rows.Count);
                    tempcolstoprint = new SortedList(dgv.SelectedColumns.Count);

                    foreach (DataGridViewRow row in dgv.Rows)
                        if (row.Visible && !row.IsNewRow)
                            temprowstoprint.Add(row.Index, row);

                    foreach (DataGridViewColumn col in dgv.SelectedColumns)
                        if (col.Visible)
                            tempcolstoprint.Add(col.DisplayIndex, col);
                }
       
                else
                {
                    
                    temprowstoprint = new SortedList(dgv.SelectedCells.Count);
                    tempcolstoprint = new SortedList(dgv.SelectedCells.Count);

                    
                    int displayindex, colindex, rowindex;
                    foreach (DataGridViewCell cell in dgv.SelectedCells)
                    {
                        displayindex = cell.OwningColumn.DisplayIndex;
                        colindex = cell.ColumnIndex;
                        rowindex = cell.RowIndex;

                
                        if (!temprowstoprint.Contains(rowindex))
                        {
                            DataGridViewRow row = dgv.Rows[rowindex];
                            if (row.Visible && !row.IsNewRow)
                                temprowstoprint.Add(rowindex, dgv.Rows[rowindex]);
                        }
            
                        if (!tempcolstoprint.Contains(displayindex))
                            tempcolstoprint.Add(displayindex, dgv.Columns[colindex]);
                    }
                }
            }
                        
            else if (PrintRange.CurrentPage == printRange)
            {
           
                temprowstoprint = new SortedList(dgv.DisplayedRowCount(true));
                tempcolstoprint = new SortedList(dgv.Columns.Count);

         
                for (int i = dgv.FirstDisplayedScrollingRowIndex;
                    i < dgv.FirstDisplayedScrollingRowIndex + dgv.DisplayedRowCount(true);
                    i++)
                {
                    DataGridViewRow row = dgv.Rows[i];
                    if (row.Visible) temprowstoprint.Add(row.Index, row);
                }

          
                foreach (DataGridViewColumn col in dgv.Columns) if (col.Visible) tempcolstoprint.Add(col.DisplayIndex, col);
            }
     
            else
            {
                temprowstoprint = new SortedList(dgv.Rows.Count);
                tempcolstoprint = new SortedList(dgv.Columns.Count);

                foreach (DataGridViewRow row in dgv.Rows) if (row.Visible && !row.IsNewRow) temprowstoprint.Add(row.Index, row);

             
                foreach (DataGridViewColumn col in dgv.Columns) if (col.Visible) tempcolstoprint.Add(col.DisplayIndex, col);
            }

        
            rowstoprint = new List<rowdata>(temprowstoprint.Count);
            foreach (object item in temprowstoprint.Values) rowstoprint.Add(new rowdata() { row = (DataGridViewRow)item });

            colstoprint = new List<DataGridViewColumn>(tempcolstoprint.Count);
            foreach (object item in tempcolstoprint.Values) colstoprint.Add(item);

       
            foreach (String columnname in HideColumns)
            {
                colstoprint.Remove(dgv.Columns[columnname]);
            }

            if (EnableLogging) Logger.LogInfoMsg(String.Format("Grid Printout Range is {0} columns", colstoprint.Count));
            if (EnableLogging) Logger.LogInfoMsg(String.Format("Grid Printout Range is {0} rows", rowstoprint.Count));

        }

       
        private void buildstringformat(ref StringFormat format, DataGridViewCellStyle controlstyle,
            StringAlignment alignment, StringAlignment linealignment, StringFormatFlags flags,
            StringTrimming trim)
        {
          
            if (null == format)
                format = new StringFormat();

          
            format.Alignment = alignment;
            format.LineAlignment = linealignment;
            format.FormatFlags = flags;
            format.Trimming = trim;

     
            if ((null != dgv) && (RightToLeft.Yes == dgv.RightToLeft))
                format.FormatFlags |= StringFormatFlags.DirectionRightToLeft;

           
            if (null != controlstyle)
            {
   
                DataGridViewContentAlignment cellalign = controlstyle.Alignment;
                if (cellalign.ToString().Contains("Center")) format.Alignment = StringAlignment.Center;
                else if (cellalign.ToString().Contains("Left")) format.Alignment = StringAlignment.Near;
                else if (cellalign.ToString().Contains("Right")) format.Alignment = StringAlignment.Far;

                if (cellalign.ToString().Contains("Top")) format.LineAlignment = StringAlignment.Near;
                else if (cellalign.ToString().Contains("Middle")) format.LineAlignment = StringAlignment.Center;
                else if (cellalign.ToString().Contains("Bottom")) format.LineAlignment = StringAlignment.Far;
            }
        }

       
        private SizeF calccellsize(Graphics g, DataGridViewCell cell, DataGridViewCellStyle cellstyle,
            float basewidth, float overridewidth, StringFormat format)
        {

            SizeF size = new SizeF(cell.Size);


            if ((RowHeightSetting.DataHeight == RowHeight) ||
                (ColumnWidthSetting.DataWidth == ColumnWidth) ||
                (ColumnWidthSetting.Porportional == ColumnWidth))
            {
                SizeF datasize;

          
                if (("DataGridViewImageCell" == dgv.Columns[cell.ColumnIndex].CellType.Name)
                    && ("Image" == cell.ValueType.Name || "Byte[]" == cell.ValueType.Name))
                {

                    Image img;

                    if ((null == cell.Value) || (typeof(DBNull) == cell.Value.GetType()))
                        return new SizeF(1, 1);

         
                    if ("Image" == cell.ValueType.Name || "Object" == cell.ValueType.Name)
                    {

                        img = (System.Drawing.Image)cell.Value;
                    }
                    else if ("Byte[]" == cell.ValueType.Name)
                    {
                 
                        ImageConverter ic = new ImageConverter();
                        img = (Image)ic.ConvertFrom((byte[])cell.Value);
                    }
                    else
                        throw new Exception(String.Format("Unknown image cell underlying type: {0} in column {1}",
                            cell.ValueType.Name, cell.ColumnIndex));

         
                    datasize = img.Size;
                }
                else
                {
                    float width = (-1 != overridewidth) ? overridewidth : basewidth;

                
                    datasize = g.MeasureString(cell.EditedFormattedValue.ToString(), cellstyle.Font,
                        new SizeF(width, maxPages), format);

 
                    if (printWidth < datasize.Width)
                        datasize = g.MeasureString(cell.FormattedValue.ToString(), cellstyle.Font,
                        new SizeF(pageWidth - cellstyle.Padding.Left - cellstyle.Padding.Right, maxPages),
                        format);
                }

            
                if (RowHeightSetting.DataHeight == RowHeight)
                    size.Height = datasize.Height + cellstyle.Padding.Top + cellstyle.Padding.Bottom;

              
                if ((ColumnWidthSetting.DataWidth == ColumnWidth) ||
                    (ColumnWidthSetting.Porportional == ColumnWidth))
                    size.Width = datasize.Width + cellstyle.Padding.Left + cellstyle.Padding.Right;
            }

            return size;
        }


        private void RecalcRowHeights(Graphics g, int colindex, float newcolwidth)
        {
            DataGridViewCell cell = null;
            float finalsize = 0F;


            for (int i = 0; i < rowstoprint.Count; i++)
            {
                cell = ((DataGridViewRow)rowstoprint[i].row).Cells[((DataGridViewColumn)colstoprint[colindex]).Index];

                if (RowHeightSetting.DataHeight == RowHeight)
                {
                    StringFormat currentformat = null;

          
                    DataGridViewCellStyle colstyle = GetStyle(((DataGridViewRow)rowstoprint[i].row), ((DataGridViewColumn)colstoprint[colindex]));

    
                    buildstringformat(ref currentformat, colstyle, cellformat.Alignment, cellformat.LineAlignment,
                        cellformat.FormatFlags, cellformat.Trimming);

                    SizeF size = calccellsize(g, cell, colstyle, newcolwidth, colwidthsoverride[colindex], currentformat);

                    finalsize = size.Height;
                }
                else
                {
                    finalsize = cell.Size.Height;
                }

                rowstoprint[i].height = (rowstoprint[i].height < finalsize ? finalsize : rowstoprint[i].height);
            }
        }


        private void measureprintarea(Graphics g)
        {
            int i, j;
            colwidths = new List<float>(colstoprint.Count);
            footerHeight = 0;

            // temp variables
            DataGridViewColumn col;
            DataGridViewRow row;

           


            for (i = 0; i < colstoprint.Count; i++)
            {
                col = (DataGridViewColumn)colstoprint[i];

             

                
                DataGridViewCellStyle headercolstyle = col.HeaderCell.InheritedStyle.Clone();
                if (ColumnHeaderStyles.ContainsKey(col.Name))
                {
                    headercolstyle = columnheaderstyles[col.Name];

         
                    buildstringformat(ref currentformat, headercolstyle, cellformat.Alignment, cellformat.LineAlignment,
                        cellformat.FormatFlags, cellformat.Trimming);
                }
                else if (col.HasDefaultCellStyle)
                {
        
                    buildstringformat(ref currentformat, headercolstyle, cellformat.Alignment, cellformat.LineAlignment,
                        cellformat.FormatFlags, cellformat.Trimming);
                }
                else
                {
                    currentformat = columnheadercellformat;
                }

   
                SizeF size = col.HeaderCell.Size;

        
                float usewidth = 0;
                if (0 <= colwidthsoverride[i])
          
                    colwidths.Add(colwidthsoverride[i]);        
                else if ((ColumnWidthSetting.CellWidth == ColumnWidth) || (ColumnWidthSetting.Porportional == ColumnWidth))
                {
                    usewidth = col.HeaderCell.Size.Width;
       
                    size = calccellsize(g, col.HeaderCell, headercolstyle, usewidth, colwidthsoverride[i], columnheadercellformat);
                    colwidths.Add(col.Width);                      
                }
                else
                {
                    usewidth = printWidth;
              
                    size = calccellsize(g, col.HeaderCell, headercolstyle, usewidth, colwidthsoverride[i], columnheadercellformat);
                    colwidths.Add(size.Width);
                }

            
                if (RowHeightSetting.DataHeight == RowHeight)
                    colheaderheight = (colheaderheight < size.Height ? size.Height : colheaderheight);
                else
                    colheaderheight = col.HeaderCell.Size.Height;
            }

       

            if (pageno)
            {
                pagenumberHeight = (g.MeasureString("Page", pagenofont, printWidth, pagenumberformat)).Height;
            }

            if (PrintHeader)
            {
             
                titleheight = (g.MeasureString(title, titlefont, printWidth, titleformat)).Height;
                subtitleheight = (g.MeasureString(subtitle, subtitlefont, printWidth, subtitleformat)).Height;
            }

    
            if (PrintFooter)
            {
                if (!String.IsNullOrEmpty(footer))
                {
                    footerHeight += (g.MeasureString(footer, footerfont, printWidth, footerformat)).Height;
                }

                footerHeight += footerspacing;
            }

     
            for (i = 0; i < rowstoprint.Count; i++)
            {
                row = (DataGridViewRow)rowstoprint[i].row;


                if ((bool)PrintRowHeaders)
                {
                   
                    String rowheadertext = String.IsNullOrEmpty(row.HeaderCell.FormattedValue.ToString())
                        ? rowheadercelldefaulttext : row.HeaderCell.FormattedValue.ToString();

                    SizeF rhsize = g.MeasureString(rowheadertext,
                        row.HeaderCell.InheritedStyle.Font);
                    rowheaderwidth = (rowheaderwidth < rhsize.Width) ? rhsize.Width : rowheaderwidth;
                }

               
                for (j = 0; j < colstoprint.Count; j++)
                {
                    col = (DataGridViewColumn)colstoprint[j];

              
                    StringFormat currentformat = null;
                    DataGridViewCellStyle colstyle = GetStyle(row, col); 


                    buildstringformat(ref currentformat, colstyle, cellformat.Alignment, cellformat.LineAlignment,
                        cellformat.FormatFlags, cellformat.Trimming);

                    
                    float basewidth;

                  
                    if (0 <= colwidthsoverride[j])
           
                        basewidth = colwidthsoverride[j];
                    else if ((ColumnWidthSetting.CellWidth == ColumnWidth) || (ColumnWidthSetting.Porportional == ColumnWidth))
              
                        basewidth = colwidths[j];
                    else
                    {
                
                        basewidth = printWidth;

              
                        basewidth -= colstyle.Padding.Left + colstyle.Padding.Right;

       
                        SizeF size = calccellsize(g, row.Cells[col.Index], colstyle,
                            basewidth, colwidthsoverride[j], currentformat);

                        basewidth = size.Width;
                    }

            
                    if (!(0 <= colwidthsoverride[j]) && (ColumnWidthSetting.DataWidth == ColumnWidth))
                        colwidths[j] = colwidths[j] < basewidth ? basewidth : colwidths[j];
                }
            }



        
            pagesets = new List<PageDef>();
            pagesets.Add(new PageDef(PrintMargins, colstoprint.Count, pageWidth));
            int pset = 0;

            pagesets[pset].coltotalwidth = rowheaderwidth;

            for (j = 0; j < fixedcolumns.Count; j++)
            {
                int fixedcol = fixedcolumns[j];
                pagesets[pset].columnindex.Add(fixedcol);
                pagesets[pset].colstoprint.Add(colstoprint[fixedcol]);
                pagesets[pset].colwidths.Add(colwidths[fixedcol]);
                pagesets[pset].colwidthsoverride.Add(colwidthsoverride[fixedcol]);
                pagesets[pset].coltotalwidth += (colwidthsoverride[fixedcol] >= 0)
                    ? colwidthsoverride[fixedcol] : colwidths[fixedcol];
            }

            if (printWidth < (pagesets[pset].coltotalwidth))
            {
                throw new Exception("Fixed column widths exceed the page width.");
            }

            float columnwidth;
            for (i = 0; i < colstoprint.Count; i++)
            {
              
                if (fixedcolumns.Contains(i))
                    continue;

           
                columnwidth = (colwidthsoverride[i] >= 0)
                    ? colwidthsoverride[i] : colwidths[i];

                if (printWidth < (pagesets[pset].coltotalwidth + columnwidth) && i != 0)
                {
                    pagesets.Add(new PageDef(PrintMargins, colstoprint.Count, pageWidth));
                    pset++;

 
                    pagesets[pset].coltotalwidth = rowheaderwidth;


                    for (j = 0; j < fixedcolumns.Count; j++)
                    {
                        int fixedcol = fixedcolumns[j];
                        pagesets[pset].columnindex.Add(fixedcol);
                        pagesets[pset].colstoprint.Add(colstoprint[fixedcol]);
                        pagesets[pset].colwidths.Add(colwidths[fixedcol]);
                        pagesets[pset].colwidthsoverride.Add(colwidthsoverride[fixedcol]);
                        pagesets[pset].coltotalwidth += (colwidthsoverride[fixedcol] >= 0)
                            ? colwidthsoverride[fixedcol] : colwidths[fixedcol];
                    }

    
                    if (printWidth < (pagesets[pset].coltotalwidth))
                    {
                        throw new Exception("Fixed column widths exceed the page width.");
                    }
                }


                pagesets[pset].columnindex.Add(i);
                pagesets[pset].colstoprint.Add(colstoprint[i]);
                pagesets[pset].colwidths.Add(colwidths[i]);
                pagesets[pset].colwidthsoverride.Add(colwidthsoverride[i]);
                pagesets[pset].coltotalwidth += columnwidth;
            }

         
            if (RightToLeft.Yes == dgv.RightToLeft)
            {
                for (pset = 0; pset < pagesets.Count; pset++)
                {
                    pagesets[pset].columnindex.Reverse();
                    pagesets[pset].colstoprint.Reverse();
                    pagesets[pset].colwidths.Reverse();
                    pagesets[pset].colwidthsoverride.Reverse();
                }
            }

            for (i = 0; i < pagesets.Count; i++)
            {
                PageDef pageset = pagesets[i];
                if (EnableLogging)
                {
                    String columnlist = "";

                    Logger.LogInfoMsg(String.Format("PageSet {0} Information ----------------------------------------------", i));

            
                    for (int k = 0; k < pageset.colstoprint.Count; k++)
                        columnlist = String.Format("{0},{1}", columnlist,
                            ((DataGridViewColumn)(pageset.colstoprint[k])).Index);
                    Logger.LogInfoMsg(String.Format("Measured columns {0}", columnlist.Substring(1)));
                    columnlist = "";

          
                    for (int k = 0; k < pageset.colstoprint.Count; k++)
                        columnlist = String.Format("{0},{1}", columnlist, pageset.colwidths[k]);
                    Logger.LogInfoMsg(String.Format("Original Column Widths: {0}", columnlist.Substring(1)));
                    columnlist = "";

             
                    for (int k = 0; k < pageset.colstoprint.Count; k++)
                        columnlist = String.Format("{0},{1}", columnlist, pageset.colwidthsoverride[k]);
                    Logger.LogInfoMsg(String.Format("Overridden Column Widths: {0}", columnlist.Substring(1)));
                    columnlist = "";
                }

              
                AdjustPageSets(g, pageset);

          
                if (EnableLogging)
                {
                    String columnlist = "";

   
                    for (int k = 0; k < pageset.colstoprint.Count; k++)
                        columnlist = String.Format("{0},{1}", columnlist, pageset.colwidths[k]);
                    Logger.LogInfoMsg(String.Format("Final Column Widths: {0}", columnlist.Substring(1)));
                    columnlist = "";

                    Logger.LogInfoMsg(String.Format("pageset print width is {0}, total column width to be printed is {1}",
                        pageset.printWidth, pageset.coltotalwidth));
                }
            }
        }


        private void AdjustPageSets(Graphics g, PageDef pageset)
        {
            int i;
            float fixedcolwidth = rowheaderwidth;
            float remainingcolwidth = 0;
            float ratio;

     
            for (i = 0; i < pageset.colwidthsoverride.Count; i++)
                if (pageset.colwidthsoverride[i] >= 0)
                    fixedcolwidth += pageset.colwidthsoverride[i];

         
            for (i = 0; i < pageset.colwidths.Count; i++)
                if (pageset.colwidthsoverride[i] < 0)
                    remainingcolwidth += pageset.colwidths[i];


            if ((porportionalcolumns || ColumnWidthSetting.Porportional == ColumnWidth) &&
                0 < remainingcolwidth)
                ratio = ((float)printWidth - fixedcolwidth) / (float)remainingcolwidth;
            else
                ratio = (float)1.0;

 
            pageset.coltotalwidth = rowheaderwidth;
            for (i = 0; i < pageset.colwidths.Count; i++)
            {
                if (pageset.colwidthsoverride[i] >= 0)
  
                    pageset.colwidths[i] = pageset.colwidthsoverride[i];
                else if (ColumnWidthSetting.Porportional == ColumnWidth)
        
                    pageset.colwidths[i] = pageset.colwidths[i] * ratio;
                else if (pageset.colwidths[i] > printWidth - pageset.coltotalwidth)
                    pageset.colwidths[i] = printWidth - pageset.coltotalwidth;

          
                RecalcRowHeights(g, pageset.columnindex[i], pageset.colwidths[i]);

                pageset.coltotalwidth += pageset.colwidths[i];

            }

            if (Alignment.Left == tablealignment)
            {
    
                pageset.margins.Right = pageWidth - pageset.margins.Left - (int)pageset.coltotalwidth;
                if (0 > pageset.margins.Right) pageset.margins.Right = 0;
            }
            else if (Alignment.Right == tablealignment)
            {
            
                pageset.margins.Left = pageWidth - pageset.margins.Right - (int)pageset.coltotalwidth;
                if (0 > pageset.margins.Left) pageset.margins.Left = 0;
            }
            else if (Alignment.Center == tablealignment)
            {
     
                pageset.margins.Left = (pageWidth - (int)pageset.coltotalwidth) / 2;
                if (0 > pageset.margins.Left) pageset.margins.Left = 0;
                pageset.margins.Right = pageset.margins.Left;
            }
        }

    
        private int Pagination()
        {
            float pos = 0;
            paging newpage = paging.keepgoing;

            CurrentPage = 1;

            staticheight = pageHeight - FooterHeight - pagesets[currentpageset].margins.Bottom; //PrintMargins.Bottom;

           
            staticheight -= PageNumberHeight;


            pos = PrintMargins.Top + HeaderHeight;


            if (!String.IsNullOrEmpty(breakonvaluechange))
            {
                oldvalue = rowstoprint[0].row.Cells[breakonvaluechange].EditedFormattedValue;
            }


            for (int currentrow = 0; currentrow < (rowstoprint.Count); currentrow++)
            {
 
                if (pos + rowstoprint[currentrow].height >= staticheight)
                {
                    newpage = paging.outofroom;
                }

       
                if ((!String.IsNullOrEmpty(breakonvaluechange)) &&
                    (!oldvalue.Equals(rowstoprint[currentrow].row.Cells[breakonvaluechange].EditedFormattedValue)))
                {
                    newpage = paging.datachange;
                    oldvalue = rowstoprint[currentrow].row.Cells[breakonvaluechange].EditedFormattedValue;
                }

  
                if (newpage != paging.keepgoing)
                {
     
                    rowstoprint[currentrow].pagebreak = true;

                    // count the page
                    CurrentPage++;


                    if (CurrentPage > toPage)
                    {

                        return toPage;
                    }

                 
                    if (KeepRowsTogether
                        || newpage == paging.datachange
                        || (newpage == paging.outofroom && (staticheight - pos) < KeepRowsTogetherTolerance))
                    {

                        pos = rowstoprint[currentrow].height;
                    }
                    else
                    {
         
                        rowstoprint[currentrow].splitrow = true;

      
                        pos = pos + rowstoprint[currentrow].height - staticheight;
                    }

    
                    staticheight = pageHeight - FooterHeight - pagesets[currentpageset].margins.Bottom; //PrintMargins.Bottom;

                
                    staticheight += PageNumberHeight;


                   
                    pos += PrintMargins.Top + HeaderHeight + PageNumberHeight;
                }
                else
                {
                  
                    pos += rowstoprint[currentrow].height;
                }

            
                newpage = paging.keepgoing;
            }

          
            return CurrentPage;
        }

    
        private bool DetermineHasMorePages()
        {
            currentpageset++;
            if (currentpageset < pagesets.Count)
            {
                return true;     
            }
            else
                return false;
        }

       
        private bool PrintPage(Graphics g)
        {
       
            int firstrow = 0;

       
            bool HasMorePages = false;


            bool printthispage = false;


            float printpos = pagesets[currentpageset].margins.Top;

         
            CurrentPage++;
            if (EnableLogging) Logger.LogInfoMsg(String.Format("Print Page processing page {0} -----------------------", CurrentPage));
            if ((CurrentPage >= fromPage) && (CurrentPage <= toPage))
                printthispage = true;

   
            staticheight = pageHeight - FooterHeight - pagesets[currentpageset].margins.Bottom;
            if (!pagenumberontop)
                staticheight -= PageNumberHeight;

         
            float used = 0;

    
            rowdata thisrow = null;

           
            rowdata nextrow = null;

   

            while (!printthispage)
            {
                if (EnableLogging) Logger.LogInfoMsg(String.Format("Print Page skipping page {0} part {1}", CurrentPage, currentpageset + 1));

     
                printpos = pagesets[currentpageset].margins.Top + HeaderHeight + PageNumberHeight;

      
                bool pagecomplete = false;
                currentrow = lastrowprinted + 1;


                firstrow = currentrow;

                do
                {
                    thisrow = rowstoprint[currentrow];

  
                    used = (thisrow.height - rowstartlocation) > (staticheight - printpos)
                            ? (staticheight - printpos) : thisrow.height - rowstartlocation;
                    printpos += used;

            
                    lastrowprinted++;
                    currentrow++;
                    nextrow = (currentrow < rowstoprint.Count) ? rowstoprint[currentrow] : null;
                    if (null != nextrow && nextrow.pagebreak) // pagebreak before the next row
                    {
                        pagecomplete = true;

                        if (nextrow.splitrow)
                        {
                            rowstartlocation += (nextrow.height - rowstartlocation) > (staticheight - printpos)
                                ? (staticheight - printpos) : nextrow.height - rowstartlocation;
                        }
                    }
                    else
                    {
                        
                        rowstartlocation = 0;
                    }

       
                    if ((0 == rowstartlocation) && lastrowprinted >= rowstoprint.Count - 1)
                        pagecomplete = true;

                } while (!pagecomplete);


                if (EnableLogging) Logger.LogInfoMsg(String.Format("Print Page skipped rows {0} to {1}", firstrow, currentrow));

      
                CurrentPage++;

                if ((CurrentPage >= fromPage) && (CurrentPage <= toPage))
                    printthispage = true;


                if (0 != rowstartlocation)
                {
     
                    HasMorePages = true;
                }
                
                else if ((lastrowprinted >= rowstoprint.Count - 1) || (CurrentPage > toPage))
                {
          
                    HasMorePages = DetermineHasMorePages();

                    lastrowprinted = -1;
                    CurrentPage = 0;

                    return HasMorePages;
                }
            }

            if (EnableLogging)
            {
                Logger.LogInfoMsg(String.Format("Print Page printing page {0} part {1}", CurrentPage, currentpageset + 1));
                var m = pagesets[currentpageset].margins;
                Logger.LogInfoMsg(String.Format("Current Margins are {0}, {1}, {2}, {3}", m.Left, m.Right, m.Top, m.Bottom));
            }



            ImbeddedImageList.Where(p => p.ImageLocation == Location.Absolute).DrawImbeddedImage(g, pagesets[currentpageset].printWidth,
                pageHeight, pagesets[currentpageset].margins);

 

      
            printpos = pagesets[currentpageset].margins.Top;


            if (PrintHeader)
            {
          
                ImbeddedImageList.Where(p => p.ImageLocation == Location.Header).DrawImbeddedImage(g, pagesets[currentpageset].printWidth,
                    pageHeight, pagesets[currentpageset].margins);


                if (pagenumberontop)
                {
                    printpos = PrintPageNo(g, printpos);
                }

     
                if (0 != TitleHeight && !String.IsNullOrEmpty(title))
                    printsection(g, ref printpos, title, titlefont,
                        titlecolor, titleformat, overridetitleformat,
                        pagesets[currentpageset],
                        titlebackground, titleborder);

   
                printpos += TitleHeight;


                if (0 != SubTitleHeight && !String.IsNullOrEmpty(subtitle))
                    printsection(g, ref printpos, subtitle, subtitlefont,
                        subtitlecolor, subtitleformat, overridesubtitleformat,
                        pagesets[currentpageset],
                        subtitlebackground, subtitleborder);


                printpos += SubTitleHeight;
            }


            if ((bool)PrintColumnHeaders)
            {

                printcolumnheaders(g, ref printpos, pagesets[currentpageset]);
            }


            bool continueprinting = true;
            currentrow = lastrowprinted + 1;


            firstrow = currentrow;

            if (currentrow >= rowstoprint.Count)
            {
  
                continueprinting = false;
            }

            while (continueprinting)
            {
                thisrow = rowstoprint[currentrow];

       
                used = printrow(g, printpos, (DataGridViewRow)(thisrow.row),
                    pagesets[currentpageset], rowstartlocation);
                printpos += used;

                lastrowprinted++;
                currentrow++;
                nextrow = (currentrow < rowstoprint.Count) ? rowstoprint[currentrow] : null;
                if (null != nextrow && nextrow.pagebreak)
                {
                    continueprinting = false;

                    if (nextrow.splitrow)
                    {
                        rowstartlocation += printrow(g, printpos, (DataGridViewRow)(nextrow.row),
                            pagesets[currentpageset], rowstartlocation);
                    }
                }
                else
                {
                  
                    rowstartlocation = 0;
                }


                if ((0 == rowstartlocation) && lastrowprinted >= rowstoprint.Count - 1)
                    continueprinting = false;

            }


            if (EnableLogging)
            {
                Logger.LogInfoMsg(String.Format("Print Page printed rows {0} to {1}", firstrow, currentrow));
                PageDef pageset = pagesets[currentpageset];
                String columnlist = "";

    
                for (int i = 0; i < pageset.colstoprint.Count; i++)
                    columnlist = String.Format("{0},{1}", columnlist,
                        ((DataGridViewColumn)(pageset.colstoprint[i])).Index);

                Logger.LogInfoMsg(String.Format("Print Page printed columns {0}", columnlist.Substring(1)));
            }

            if (PrintFooter)
            {
         
                ImbeddedImageList.Where(p => p.ImageLocation == Location.Footer).DrawImbeddedImage(g, pagesets[currentpageset].printWidth,
                    pageHeight, pagesets[currentpageset].margins);

                printpos = pageHeight - footerHeight - pagesets[currentpageset].margins.Bottom;  // - margins.Top

              
                printpos += footerspacing;

             
                if (!pagenumberontop)
                {
                    printpos = PrintPageNo(g, printpos);
                }

                if (0 != FooterHeight)
                    printfooter(g, ref printpos, pagesets[currentpageset]);
            }




            if (0 != rowstartlocation)
            {
      
                HasMorePages = true;
            }

            
            if ((CurrentPage >= toPage) || (lastrowprinted >= rowstoprint.Count - 1))
            {
              
                HasMorePages = DetermineHasMorePages();

                
                rowstartlocation = 0;
                lastrowprinted = -1;
                CurrentPage = 0;
            }
            else
            {
      
                HasMorePages = true;
            }

            return HasMorePages;
        }

        private float PrintPageNo(Graphics g, float printpos)
        {
            if (pageno)
            {
                String pagenumber = pagetext + CurrentPage.ToString(CultureInfo.CurrentCulture);
                if (showtotalpagenumber)
                {
                    pagenumber += pageseparator + totalpages.ToString(CultureInfo.CurrentCulture);
                }
                if (1 < pagesets.Count)
                    pagenumber += parttext + (currentpageset + 1).ToString(CultureInfo.CurrentCulture);

  
                printsection(g, ref printpos,
                    pagenumber, pagenofont, pagenocolor, pagenumberformat,
                    overridepagenumberformat, pagesets[currentpageset],
                    null, null);

   
                if (pagenumberonseparateline)
                    printpos += pagenumberHeight;
            }
            return printpos;
        }

        private void printsection(Graphics g, ref float pos, string text,
            Font font, Color color, StringFormat format, bool useroverride, PageDef pageset,
            Brush background, Pen border)
        {
          
            SizeF printsize = g.MeasureString(text, font, pageset.printWidth, format);

         
            RectangleF printarea = new RectangleF((float)pageset.margins.Left, pos, (float)pageset.printWidth,
               printsize.Height);

    
            if (null != background)
            {
                g.FillRectangle(background, printarea);
            }

        
            if (null != border)
            {
                g.DrawRectangle(border, printarea.X, printarea.Y, printarea.Width, printarea.Height);
            }

           
            g.DrawString(text, font, new SolidBrush(color), printarea, format);
        }

     
        private void printfooter(Graphics g, ref float pos, PageDef pageset)
        {

            // print the footer
            printsection(g, ref pos, footer, footerfont, footercolor, footerformat,
                overridefooterformat, pageset, footerbackground, footerborder);
        }

       
        private void printcolumnheaders(Graphics g, ref float pos, PageDef pageset)
        {
            
            float xcoord = pageset.margins.Left + rowheaderwidth;

       
            Pen lines = new Pen(dgv.GridColor, 1);

 
            DataGridViewColumn col;
            for (int i = 0; i < pageset.colstoprint.Count; i++)
            {
                col = (DataGridViewColumn)pageset.colstoprint[i];

       
                float cellwidth = (pageset.colwidths[i] > pageset.printWidth - rowheaderwidth ?
                    pageset.printWidth - rowheaderwidth : pageset.colwidths[i]);

                // get column style
                DataGridViewCellStyle style = col.HeaderCell.InheritedStyle.Clone();
                if (ColumnHeaderStyles.ContainsKey(col.Name))
                {
                    style = ColumnHeaderStyles[col.Name];
                }

           
                RectangleF cellprintarea = new RectangleF(xcoord, pos, cellwidth, colheaderheight);

                DrawCell(g, cellprintarea, style, col.HeaderCell, 0, columnheadercellformat, lines);

                xcoord += pageset.colwidths[i];
            }

          
            pos += colheaderheight +
                (dgv.ColumnHeadersBorderStyle != DataGridViewHeaderBorderStyle.None ? lines.Width : 0);
        }

        
        private float printrow(Graphics g, float finalpos, DataGridViewRow row, PageDef pageset,
            float startlocation)
        {
        
            float xcoord = pageset.margins.Left;
            float pos = finalpos;

           
            Pen lines = new Pen(dgv.GridColor, 1);

            
            float rowwidth = (pageset.coltotalwidth > pageset.printWidth ? pageset.printWidth : pageset.coltotalwidth);

          
            float rowheight = (rowstoprint[currentrow].height - startlocation) > (staticheight - pos)
                ? (staticheight - pos) : rowstoprint[currentrow].height - startlocation;

         
            DataGridViewCellStyle rowstyle = row.InheritedStyle.Clone();
            DataGridViewCellStyle headerstyle = row.HeaderCell.InheritedStyle.Clone();

           
            RectangleF printarea = new RectangleF(xcoord, pos, rowwidth,
                rowheight);

          
            g.FillRectangle(new SolidBrush(rowstyle.BackColor), printarea);

            
            if ((bool)PrintRowHeaders)
            {
               
                RectangleF headercellprintarea = new RectangleF(xcoord, pos,
                    rowheaderwidth, rowheight);

                DrawCell(g, headercellprintarea, headerstyle, row.HeaderCell, startlocation,
                    rowheadercellformat, lines);

              
                xcoord += rowheaderwidth;
            }

            
            DataGridViewColumn col;
            for (int i = 0; i < pageset.colstoprint.Count; i++)
            {
                
                col = (DataGridViewColumn)pageset.colstoprint[i];
                DataGridViewCell cell = row.Cells[col.Index];


                float cellwidth = (pageset.colwidths[i] > pageset.printWidth - rowheaderwidth ?
                    pageset.printWidth - rowheaderwidth : pageset.colwidths[i]);

            
                if (cellwidth > 0)
                {
          
                    StringFormat finalformat = null;
                    Font cellfont = null;
                    DataGridViewCellStyle colstyle = GetStyle(row, col);

                
                    buildstringformat(ref finalformat, colstyle, cellformat.Alignment, cellformat.LineAlignment,
                        cellformat.FormatFlags, cellformat.Trimming);
                    cellfont = colstyle.Font;

                 
                    RectangleF cellprintarea = new RectangleF(xcoord, pos, cellwidth,
                        rowheight);

                    DrawCell(g, cellprintarea, colstyle, cell, startlocation, finalformat, lines);
                }
                // track horizontal space used
                xcoord += pageset.colwidths[i];
            }

            return rowheight;
        }

     
        Boolean DrawOwnerDrawCell(Graphics g, int rowindex, int columnindex, RectangleF rectf,
            DataGridViewCellStyle style)
        {
            DGVCellDrawingEventArgs args = new DGVCellDrawingEventArgs(g, rectf, style,
                rowindex, columnindex);
            OnCellOwnerDraw(args);
            return args.Handled;
        }


   
        void DrawCell(Graphics g, RectangleF cellprintarea, DataGridViewCellStyle style,
            DataGridViewCell cell, float startlocation, StringFormat cellformat, Pen lines)
        {

            if (!DrawOwnerDrawCell(g, cell.RowIndex, cell.ColumnIndex, cellprintarea, style))
            {

                RectangleF clip = g.ClipBounds;

             
                g.FillRectangle(new SolidBrush(style.BackColor), cellprintarea);


                RectangleF paddedcellprintarea = new RectangleF(cellprintarea.X + style.Padding.Left,
                    cellprintarea.Y + style.Padding.Top,
                    cellprintarea.Width - style.Padding.Right - style.Padding.Left,
                    cellprintarea.Height - style.Padding.Bottom - style.Padding.Top);


                g.SetClip(cellprintarea);


                RectangleF actualprint = new RectangleF(paddedcellprintarea.X, paddedcellprintarea.Y - startlocation,
                    paddedcellprintarea.Width, paddedcellprintarea.Height + startlocation);


                if (0 <= cell.RowIndex && 0 <= cell.ColumnIndex)
                {
                    if ("DataGridViewImageCell" == dgv.Columns[cell.ColumnIndex].CellType.Name)
                    {

                        DrawImageCell(g, (DataGridViewImageCell)cell, actualprint);
                    }
                    else if ("DataGridViewCheckBoxCell" == dgv.Columns[cell.ColumnIndex].CellType.Name)
                    {

                        DrawCheckBoxCell(g, (DataGridViewCheckBoxCell)cell, actualprint);
                    }
                    else
                    {
                     
                        g.DrawString(cell.FormattedValue.ToString(), style.Font,
                            new SolidBrush(style.ForeColor), actualprint, cellformat);
                    }
                }
                else
                {
                 
                    g.DrawString(cell.FormattedValue.ToString(), style.Font,
                        new SolidBrush(style.ForeColor), actualprint, cellformat);
                }

              
                g.SetClip(clip);

             
                if (dgv.CellBorderStyle != DataGridViewCellBorderStyle.None)
                    g.DrawRectangle(lines, cellprintarea.X, cellprintarea.Y, cellprintarea.Width, cellprintarea.Height);
            }
        }

       
        void DrawCheckBoxCell(Graphics g, DataGridViewCheckBoxCell checkboxcell, RectangleF rectf)
        {
            
            Image i = new Bitmap((int)rectf.Width, (int)rectf.Height);
            Graphics tg = Graphics.FromImage(i);

         
            CheckBoxState state = CheckBoxState.UncheckedNormal;
            if (checkboxcell.ThreeState)
            {
                if (((CheckState)checkboxcell.EditedFormattedValue) == CheckState.Checked)
                    state = CheckBoxState.CheckedNormal;
                else if (((CheckState)checkboxcell.EditedFormattedValue) == CheckState.Indeterminate)
                    state = CheckBoxState.MixedNormal;
            }
            else
            {
                if ((Boolean)checkboxcell.EditedFormattedValue)
                    state = CheckBoxState.CheckedNormal;
            }

           
            Size size = CheckBoxRenderer.GetGlyphSize(tg, state);
            int x = ((int)rectf.Width - size.Width) / 2;
            int y = ((int)rectf.Height - size.Height) / 2;

            
            CheckBoxRenderer.DrawCheckBox(tg, new Point(x, y), state);

      
            switch (checkboxcell.InheritedStyle.Alignment)
            {
                case DataGridViewContentAlignment.BottomCenter:
                    rectf.Y += y;
                    break;
                case DataGridViewContentAlignment.BottomLeft:
                    rectf.X -= x;
                    rectf.Y += y;
                    break;
                case DataGridViewContentAlignment.BottomRight:
                    rectf.X += x;
                    rectf.Y += y;
                    break;
                case DataGridViewContentAlignment.MiddleCenter:
                    break;
                case DataGridViewContentAlignment.MiddleLeft:
                    rectf.X -= x;
                    break;
                case DataGridViewContentAlignment.MiddleRight:
                    rectf.X += x;
                    break;
                case DataGridViewContentAlignment.TopCenter:
                    rectf.Y -= y;
                    break;
                case DataGridViewContentAlignment.TopLeft:
                    rectf.X -= x;
                    rectf.Y -= y;
                    break;
                case DataGridViewContentAlignment.TopRight:
                    rectf.X += x;
                    rectf.Y -= y;
                    break;
                case DataGridViewContentAlignment.NotSet:
                    break;
            }


            g.DrawImage(i, rectf);

            tg.Dispose();
            i.Dispose();
        }

        
        void DrawImageCell(Graphics g, DataGridViewImageCell imagecell, RectangleF rectf)
        {

            Image img;

            if ((null == imagecell.Value) || (typeof(DBNull) == imagecell.Value.GetType()))
                return;

            if ("Image" == imagecell.ValueType.Name)
            {
                
                img = (System.Drawing.Image)imagecell.Value;
            }
            else if ("Byte[]" == imagecell.ValueType.Name)
            {
                
                ImageConverter ic = new ImageConverter();
                img = (Image)ic.ConvertFrom((byte[])imagecell.Value);
            }
            else
                throw new Exception(String.Format("Unknown image cell underlying type: {0} in column {1}",
                    imagecell.ValueType.Name, imagecell.ColumnIndex));

          
            Rectangle src = new Rectangle();

            
            int dx = 0;
            int dy = 0;

           
            if ((DataGridViewImageCellLayout.Normal == imagecell.ImageLayout) ||
                (DataGridViewImageCellLayout.NotSet == imagecell.ImageLayout))
            {
  =
                dx = img.Width - (int)rectf.Width;
                dy = img.Height - (int)rectf.Height;

                if (0 > dx) rectf.Width = src.Width = img.Width; else src.Width = (int)rectf.Width;
                if (0 > dy) rectf.Height = src.Height = img.Height; else src.Height = (int)rectf.Height;

            }
            else if (DataGridViewImageCellLayout.Stretch == imagecell.ImageLayout)
            {
              
                src.Width = img.Width;
                src.Height = img.Height;

         
                dx = 0;
                dy = 0;
            }
            else 
            {
     
                src.Width = img.Width;
                src.Height = img.Height;

                float vertscale = rectf.Height / src.Height;
                float horzscale = rectf.Width / src.Width;
                float scale;


                if (vertscale > horzscale)
                {

                    scale = horzscale;
                    dx = 0;
                    dy = (int)((src.Height * scale) - rectf.Height);
                }
                else
                {
            
                    scale = vertscale;
                    dy = 0;
                    dx = (int)((src.Width * scale) - rectf.Width);
                }

           
                rectf.Width = src.Width * scale;
                rectf.Height = src.Height * scale;
            }

            switch (imagecell.InheritedStyle.Alignment)
            {
                case DataGridViewContentAlignment.BottomCenter:
                    if (0 > dy) rectf.Y -= dy; else src.Y = dy;
                    if (0 > dx) rectf.X -= dx / 2; else src.X = dx / 2;
                    break;
                case DataGridViewContentAlignment.BottomLeft:
                    if (0 > dy) rectf.Y -= dy; else src.Y = dy;
                    src.X = 0;
                    break;
                case DataGridViewContentAlignment.BottomRight:
                    if (0 > dy) rectf.Y -= dy; else src.Y = dy;
                    if (0 > dx) rectf.X -= dx; else src.X = dx;
                    break;
                case DataGridViewContentAlignment.MiddleCenter:
                    if (0 > dy) rectf.Y -= dy / 2; else src.Y = dy / 2;
                    if (0 > dx) rectf.X -= dx / 2; else src.X = dx / 2;
                    break;
                case DataGridViewContentAlignment.MiddleLeft:
                    if (0 > dy) rectf.Y -= dy / 2; else src.Y = dy / 2;
                    src.X = 0;
                    break;
                case DataGridViewContentAlignment.MiddleRight:
                    if (0 > dy) rectf.Y -= dy / 2; else src.Y = dy / 2;
                    if (0 > dx) rectf.X -= dx; else src.X = dx;
                    break;
                case DataGridViewContentAlignment.TopCenter:
                    src.Y = 0;
                    if (0 > dx) rectf.X -= dx / 2; else src.X = dx / 2;
                    break;
                case DataGridViewContentAlignment.TopLeft:
                    src.Y = 0;
                    src.X = 0;
                    break;
                case DataGridViewContentAlignment.TopRight:
                    src.Y = 0;
                    if (0 > dx) rectf.X -= dx; else src.X = dx;
                    break;
                case DataGridViewContentAlignment.NotSet:
                    if (0 > dy) rectf.Y -= dy / 2; else src.Y = dy / 2;
                    if (0 > dx) rectf.X -= dx / 2; else src.X = dx / 2;
                    break;
            }

         
            g.DrawImage(img, rectf, src, GraphicsUnit.Pixel);
        }
    }
}
