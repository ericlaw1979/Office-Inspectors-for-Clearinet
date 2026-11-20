using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Be.Windows.Forms;
using BlockParser;
using Clearinet;
using MAPIInspector.Parsers;
using static MapiInspector.MAPIParser;

namespace MapiInspector
{
    /// <summary>
    /// MAPIInspector Class
    /// </summary>
    public class MAPIInspector
    {
        private readonly TrafficDirection direction;

        public MAPIInspector(TrafficDirection direction)
        {
            this.direction = direction;
        }

        /// <summary>
        /// The JsonResult is used to save the Json string which converted by parse result
        /// </summary>
        public static string JsonResult = string.Empty;

        /// <summary>
        /// The JsonFile is used to set a file name to save JsonResult
        /// </summary>
        public static string JsonFile = "Json.txt";

        /// <summary>
        /// The JsonFile is used to set a file name to save error messages when automation test
        /// </summary>
        public static string ErrorFile = "Error.txt";

        /// <summary>
        /// Gets or sets the Tree View control where displayed the MAPI message.
        /// </summary>
        public TreeView MAPIViewControl { get; set; }

        /// <summary>
        /// Gets or sets the control collection where displayed the MAPI parsed message and corresponding hex data.
        /// </summary>
        public MAPIControl MAPIControl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the frame has been changed.
        /// </summary>
        public bool bDirty { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the frame is read-only.
        /// </summary>
        public bool bReadOnly { get; set; }

        /// <summary>
        /// Gets the direction of the traffic
        /// </summary>
        public TrafficDirection Direction => direction;

        /// <summary>
        /// Gets a value indicating whether the message is MAPI protocol message.
        /// </summary>
        public bool IsMapihttp
        {
            get
            {
                if (session == null) return false;
                if (Direction == TrafficDirection.In)
                {
                    return session.RequestHeaders.ExistsAndContains("Content-Type", "application/mapi-http");
                }

                string responseCode = session.ResponseHeaders["X-ResponseCode"];
                if (responseCode == "0")
                {
                    return session.ResponseHeaders.ExistsAndContains("Content-Type", "application/mapi-http");
                }
                return responseCode.Length != 0 && session.ResponseHeaders.ExistsAndContains("Content-Type", "text/html");
            }
        }

        internal Exchange session { get; set; }

        /// <summary>
        /// Gets or sets the raw bytes from the frame
        /// </summary>
        private byte[] rawBody { get; set; }

        public void AddToTab(TabPage o)
        {
            o.Text = "MAPI";
            MAPIControl = new MAPIControl();
            MAPIControl.Inspector = this;
            o.Controls.Add(MAPIControl);
            MAPIControl.Size = o.Size;
            MAPIControl.Dock = DockStyle.Fill;
            MAPIViewControl = MAPIControl.MAPITreeView;
            MAPIControl.MAPIHexBox.VScrollBarVisible = true;
            MAPIViewControl.AfterSelect -= TreeView_AfterSelect;
            MAPIViewControl.AfterSelect += TreeView_AfterSelect;
            ResetParserState();
        }

        /// <summary>
        /// Represents the method, which is used to handle the AfterSelect event of a TreeView.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">A System.Windows.Forms.TreeViewEventArgs that contains the event data.</param>
        public void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            int request = sender.ToString().LastIndexOf("Request");
            int response = sender.ToString().LastIndexOf("Response");

            if (e.Node.Tag == null)
            {
                MAPIControl.MAPIHexBox.Select(0, 0);
                MAPIControl.CROPSHexBox.Select(0, 0);
            }
            else
            {
                var pos = e.Node.Tag as BaseStructure.Position;
                if (pos != null)
                {
                    if (pos.IsCompressedXOR)
                    {
                        if (pos.IsAuxiliaryPayload)
                        {
                            MAPIControl.CROPSHexBox.ByteProvider = new StaticByteProvider(AuxPayLoadCompressedXOR);
                        }
                        else
                        {
                            if (request > response)
                            {
                                MAPIControl.CROPSHexBox.ByteProvider = new StaticByteProvider(InputPayLoadCompressedXOR[pos.BufferIndex]);
                            }
                            else
                            {
                                MAPIControl.CROPSHexBox.ByteProvider = new StaticByteProvider(OutputPayLoadCompressedXOR[pos.BufferIndex]);
                            }
                        }

                        MAPIControl.CROPSHexBox.Select(pos.StartIndex, pos.Offset);
                        MAPIControl.MAPIHexBox.Select(0, 0);
                        MAPIControl.CROPSHexBox.Visible = true;
                        ToolTip toolTip = new ToolTip();
                        toolTip.SetToolTip(MAPIControl.CROPSHexBox, "This is decompressed payload data.");
                        MAPIControl.SplitContainer.Panel2Collapsed = false;
                    }
                    else
                    {
                        MAPIControl.MAPIHexBox.Select(pos.StartIndex, pos.Offset);
                        MAPIControl.CROPSHexBox.Visible = false;
                        MAPIControl.SplitContainer.Panel2Collapsed = true;
                    }
                }
            }
        }

        /// <summary>
        /// Method that returns a sorting hint
        /// </summary>
        /// <returns>An integer indicating where we should order ourselves</returns>
        public int GetOrder()
        {
            return 0;
        }

        /// <summary>
        /// Method Fiddler calls to clear the display
        /// </summary>
        public void Clear()
        {
            MAPIViewControl.Nodes.Clear();
            MAPIControl.CROPSHexBox.Visible = false;
            byte[] empty = new byte[0];
            MAPIControl.MAPIHexBox.ByteProvider = new StaticByteProvider(empty);
            MAPIControl.MAPIHexBox.ByteProvider.ApplyChanges();
            MAPIControl.SplitContainer.Panel2Collapsed = true;
        }

        public int ScoreForExchange(Exchange exchange)
        {
            session = exchange;
            BaseHeaders = Direction == TrafficDirection.In ? exchange.RequestHeaders : exchange.ResponseHeaders;
            return IsMapihttp ? 100 : 0;
        }

        public void AssignExchange(Exchange exchange, HTTPHeaders headers, byte[] body, bool readOnly)
        {
            session = exchange;
            BaseHeaders = headers;
            bReadOnly = readOnly;
            ResetParserState();
            this.body = body;
        }

        /// <summary>
        /// Gets or sets the body byte[], called by Fiddler with session byte[]
        /// </summary>
        public byte[] body
        {
            get
            {
                return rawBody;
            }

            set
            {
                rawBody = value;
                UpdateView();
            }
        }

        /// <summary>
        /// Display the object in Tree View
        /// </summary>
        /// <param name="obj">The object to display</param>
        /// <param name="bytesForHexview">The byte array provided for HexView</param>
        public void DisplayObject(Block obj, byte[] bytesForHexview)
        {
            if (obj == null)
            {
                return;
            }

            MAPIViewControl.BeginUpdate();
            try
            {
                var topNode = BaseStructure.AddBlock(obj, DoDebug);
                MAPIViewControl.Nodes.Add(topNode);
                topNode.ExpandAll();
                if (bytesForHexview != null)
                {
                    MAPIControl.MAPIHexBox.ByteProvider = new StaticByteProvider(bytesForHexview);
                    MAPIControl.MAPIHexBox.ByteProvider.ApplyChanges();
                }
                if (MAPIViewControl.Nodes.Count != 0)
                {
                    MAPIViewControl.Nodes[0].EnsureVisible();
                }
            }
            catch (Exception e)
            {
                var exBlock = BlockException.Create("Exception", e, 0);
                var topNode = BaseStructure.AddBlock(obj, DoDebug);
                MAPIViewControl.Nodes.Add(topNode);
                topNode.ExpandAll();
            }
            finally
            {
                MAPIViewControl.EndUpdate();
            }
        }

        private void ResetParserState()
        {
            ResetHandleInformation();
            Partial.ResetPartialContextInformation();
            Partial.ResetPartialParameters();
        }

        private bool DoDebug { get; set; } = false;

        public void ToggleDebug()
        {
            DoDebug = !DoDebug;
            UpdateView();
        }

        /// <summary>
        /// Update the view with parsed and diagnosed data
        /// </summary>
        private void UpdateView()
        {
            if (MAPIControl != null && MAPIControl.InvokeRequired)
            {
                CApp.UIInvokeAsync(UpdateView);
                return;
            }

            Clear();
            SessionExtensions.AllSessionsNavigator.Init();
            byte[] bytesForHexView;
            Block parserResult;
            IsLooperCall = false;
            TargetHandle = new Stack<Dictionary<RopIdType, Dictionary<int, uint>>>();
            ContextInformationCollection = new List<ContextInformation>();
            Partial.ResetPartialParameters();

            if (IsMapihttp)
            {
                try
                {
                    if (Direction == TrafficDirection.In)
                    {
                        parserResult = ParseHTTPPayload(session, TrafficDirection.In, out bytesForHexView);
                    }
                    else
                    {
                        // An X-ResponseCode of 0 (zero) means success from the perspective of the protocol transport, and the client SHOULD parse the response body based on the request that was issued.
                        if (BaseHeaders["X-ResponseCode"] != "0")
                        {
                            return;
                        }

                        parserResult = ParseHTTPPayload(session, TrafficDirection.Out, out bytesForHexView);
                    }

                    DisplayObject(parserResult, bytesForHexView);
                }
                catch (Exception e)
                {
                    var exception = BlockException.Create("Exception", e, 0);
                    DisplayObject(exception, null);
                }
                finally
                {
                    DecodingContext.Notify_handlePropertyTags = new Dictionary<uint, Dictionary<int, Tuple<string, string, string, PropertyTag[], string>>>();
                    DecodingContext.RowRops_handlePropertyTags = new Dictionary<uint, Dictionary<int, Tuple<string, string, string, PropertyTag[]>>>();
                    TargetHandle = new Stack<Dictionary<RopIdType, Dictionary<int, uint>>>();
                    ContextInformationCollection = new List<ContextInformation>();
                    IsLooperCall = true;
                }
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// Parse the sessions from capture file using the MAPI Inspector
        /// Used in test automation
        /// </summary>
        /// <param name="sessionsFromCore">The sessions which from FiddlerCore to parse</param>
        /// <param name="pathName">The path for save result file</param>
        /// <param name="autoCaseName">The test case name to parse</param>
        /// <param name="allRops">All ROPs contained in list</param>
        /// <returns>Parse result, true means success</returns>
        public bool ParseCaptureFile(Exchange[] exchanges, string pathName, string autoCaseName, out List<string> allRops)
        {
            var errorStringList = new List<string>();
            StringBuilder stringBuilder = new StringBuilder();
            SessionExtensions.AllSessionsNavigator.Init(exchanges);

            DecodingContext decodingContext = new DecodingContext();
            Partial.ResetPartialParameters();
            Partial.ResetPartialContextInformation();
            ResetHandleInformation();
            int i = 0;
            foreach (var session in SessionExtensions.AllSessionsNavigator)
            {
                ParsingSession = session;
                if (IsMapihttpWithoutUI())
                {
                    try
                    {
                        IsLooperCall = false;
                        Partial.ResetPartialParameters();
                        BaseHeaders = session.RequestHeaders;
                        object obj = ParseHTTPPayload(session, TrafficDirection.In, out byte[] bytes);
                        JsonResult += Utilities.ConvertCSharpToJson(i, isRequest: true, obj);
                        if (session.ResponseHeaders["X-ResponseCode"] == "0")
                        {
                            object obj2 = ParseHTTPPayload(session, TrafficDirection.Out, out byte[] bytes2);
                            JsonResult += Utilities.ConvertCSharpToJson(i, isRequest: false, obj2);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorStringList.Add($"{errorStringList.Count + 1}. Error: Exchange#{session.id} Error Message:{ex.Message}");
                    }
                    finally
                    {
                        DecodingContext.Notify_handlePropertyTags = new Dictionary<uint, Dictionary<int, Tuple<string, string, string, PropertyTag[], string>>>();
                        DecodingContext.RowRops_handlePropertyTags = new Dictionary<uint, Dictionary<int, Tuple<string, string, string, PropertyTag[]>>>();
                        TargetHandle = new Stack<Dictionary<RopIdType, Dictionary<int, uint>>>();
                        ContextInformationCollection = new List<ContextInformation>();
                        IsLooperCall = true;
                    }
                }

                if (i % 10 == 0 && JsonResult.Length != 0)
                {
                    string path = pathName + Path.DirectorySeparatorChar.ToString() + autoCaseName + "-" + JsonFile;
                    if (!File.Exists(path))
                    {
                        using (StreamWriter streamWriter = File.CreateText(path))
                        {
                            streamWriter.WriteLine(JsonResult);
                        }
                    }
                    else
                    {
                        using (StreamWriter streamWriter2 = File.AppendText(path))
                        {
                            streamWriter2.WriteLine(JsonResult);
                        }
                    }
                    JsonResult = string.Empty;
                }

                i++;
            }

            allRops = AllRopsList;
            foreach (string errorString in errorStringList)
            {
                stringBuilder.AppendLine(errorString);
            }
            if (stringBuilder.Length != 0)
            {
                string path2 = pathName + Path.DirectorySeparatorChar.ToString() + autoCaseName + "-" + ErrorFile;
                File.WriteAllText(path2, stringBuilder.ToString());
                return false;
            }
            return true;
        }
    }
}