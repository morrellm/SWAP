using System;
using System.Collections;
using System.ComponentModel;
using System.Net.Mime;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Reflection;
using HTTP;
using PHP_PARSER;

namespace SimpleHttpServer
{
    
    //Singleton
    public class Program : IConnectionListener
    {
        private bool _run = true;
        public const string hr = "--------------------------------------------------------------------";

        private static Program _uInst = null;

        private int _threadMax = 10;
        public int ThreadMax
        {
            get
            {
                return _threadMax;
            }
            set
            {
                if (value > 0)
                    _threadMax = value;
                else
                    throw new ArgumentException("Thread max must be > 0!!");
            }
        }
        private ArrayList _currentConnections = new ArrayList();
        public ArrayList CurrentConnections
        {
            get
            {
                return _currentConnections;
            }
            set
            {
                _currentConnections = value;
            }
        }

        private string _myAddr = "155.92.105.192";
        public string MyAddress
        {
            get
            {
                return _myAddr;
            }
            set
            {
                _myAddr = value;
            }
        }

        private string _workingPath = Directory.GetCurrentDirectory();
        private string _resourcePath = "\\Resources\\";
        public string ResourcePath 
        {
            get
            {
                return _resourcePath;
            }
            set
            {
                if (value[0] == '\\' && value[value.Length - 1] == '\\')//checks for proper format
                {
                    _resourcePath = value;
                }
                else
                {
                    throw new ArgumentException("Improper path format! must include a '\\' at the beginning and end of a resource path.");//throws exception if data bad format
                }
                
            }
        }
        //default 404 page file name
        private string _page404Fname = "404_page.html";
        public string Page404
        {
            get
            {
                return _page404Fname;
            }
            set
            {
                if (value.Contains(".html") || value.Contains(".htm"))
                    _page404Fname = value;
            }
        }
        //default page (index.html)
        private string _homePage = "home_page.html";
        public string HomePage
        {
            get
            {
                return _homePage;
            }
            set
            {
                if (value.Contains(".html") || value.Contains(".htm"))
                    _homePage = value;
            }
        }

        //this is a token that if found in a requested resource, access will be denied
        private string _lockedPageToken = "LOCKED_FOLDER";
        public string LockedPageToken
        {
            get
            {
                return _lockedPageToken;
            }
            set
            {
                _lockedPageToken = value;
            }
        }

        //default access denied page
        private string _accessDenied = "no_access.html";
        public string AccessDenied
        {
            get
            {
                return _accessDenied;
            }
            set
            {
                if (value.Contains(".html") || value.Contains(".htm"))
                    _accessDenied = value;
            }
        }
        
        private string _phpLoc = "C:\\PHP\\";
        public string PhpLoc
        {
            get
            {
                return _phpLoc;
            }
            set
            {
                _phpLoc = value;
            }
        }
        private int served = 0;

        //CONSTRUCTOR********
        private Program()
        {
            _uInst = this;
            var server = new HttpServer(this);//creates a new server for the program to use
            //sets up the working path, and the resource path
            _workingPath = _workingPath.Substring(0, _workingPath.IndexOf("\\SWAP\\", _workingPath.IndexOf("\\SWAP\\")+1));

            ConfigParser p = new ConfigParser();

            var parsed = p.ParseFile(_workingPath + "\\swap_config.cnfg");

            if (!parsed)
            {
                Console.WriteLine("Failed to parse config file...\nPress enter to exit.");
                Console.Read();
                Environment.Exit(1);
            }

            _resourcePath = _workingPath + _resourcePath;

           
            Console.WriteLine("Server resource path initialized.\nResource path = " + _resourcePath);
            if (!PHP_SAPI.initParser(PhpLoc))
            {
                Console.WriteLine("Failed to find php.exe ar location "+PhpLoc);
                Console.WriteLine("Press enter to exit...");
                Console.Read();
                Environment.Exit(1);
            }
            Console.WriteLine("Server PHP parser initialized.");
            server.Start();
            Console.WriteLine("Server Started.");

            Thread uiThread = new Thread(() =>new SwapUI(this));
            uiThread.Start();
        }

        public static Program instance()
        {
            Program ret = null;
            if (_uInst == null)
            {
                _uInst = new Program();
                ret = _uInst;
            }
            else
            {
                ret = _uInst;
            }

            return ret;
        }

        public static void Main(String[] args)
        {
            instance();
        }

        private string[] ParseHttpAddr(string addr)
        {
            int startInd = 0;

            //gets rid of unnecessary http:// or https:// in web address
            if (addr.Contains("http://"))
            {
                startInd += "http://".Length;
            }
            else if (addr.Contains("https://"))
            {
                startInd += "https://".Length;
            }

            //seprates the host and resource
            char[] toSep = { '/' };
            string[] tokens = addr.Substring(startInd).Split(toSep, 2);

            return tokens;
        }

        public void Notify(TcpClient connection)
        {
            Console.WriteLine("Program: Connection Received!");
            bool block = false;
            HttpRequest request = new HttpRequest(connection);
            Console.WriteLine("Program: Request Read, and Processed!");
            served++;
                
            //TODO not working with chrome!!!
            //this loop waits for a connection to be freed

            _currentConnections.Add(request);
            Thread t = new Thread(() => new RequestHandler(ref request));
            t.Start();
        }

    }
    /*
     ****************************
     * CLASS START ConfigParser
     ****************************
     */
    public class ConfigParser
    {
        public bool ParseFile(string fname)
        {
            bool fail = false;
            Program inst = Program.instance();

            if (File.Exists(fname))
            {
                var fs = new FileStream(fname, FileMode.Open, FileAccess.Read);
                Console.WriteLine("Reading config file...");

                //creates byte array to store the file from the file stream
                byte[] content = new byte[fs.Length];
                //reads the file
                fs.Read(content, 0, (int)fs.Length);
                //closes file
                fs.Close();
                string file = "";

                //this loop converts the byte[] to a string
                for (int i = 0; i < content.Length; i++)
                {
                    file += (char)content[i];
                }

                //divides the file's contents into lines
                string[] line = file.Split('\n');

                //parses line-by-line
                string curStr;
                string[] token;
                for (int i = 0; i < line.Length; i++)
                {
                    curStr = line[i].Trim();
                    if (curStr.Length != 0 && !curStr.Substring(0, 1).Equals("#"))
                    {
                        token = curStr.Split('=');
                        for (int j = 0; j < token.Length; j++)
                        {
                            token[j] = token[j].Trim();
                        }
                        if (token.Length == 2)//must be 2 or something went wrong
                        {
                            PropertyInfo pi = inst.GetType().GetProperty(token[0]);//attempts to get the specified property
                            if (pi != null)//if the property exists it is set
                            {
                                try
                                {
                                    if (pi.GetValue(inst).GetType().Equals("".GetType()))//if property type is a string
                                        pi.SetValue(inst, token[1]);
                                    else//if it is a number
                                        pi.SetValue(inst, Int32.Parse(token[1]));
                                    Console.WriteLine("'" + token[0] + "' set to " + pi.GetValue(inst));
                                }
                                catch (TargetInvocationException tie)//if the data is not accepted by the setter method
                                {
                                    Console.WriteLine("\nInvalid format on line " + i + " " + tie.InnerException.Message + "\n");
                                    fail = true;
                                }
                                catch (TargetException te)
                                {
                                    Console.WriteLine("Target Exception on line " + i + ", Property: " + token[0] + "\n" + te.Message);
                                    fail = true;
                                }

                            }
                            else//otherwise an error message is printed because property doesn't exist
                            {
                                Console.WriteLine("\nNon-existant property " + token[0] + " on line " + i + "\n");
                                fail = true;
                            }

                        }
                        else//if token.Length != 2 then the syntax was not correct
                        {
                            Console.WriteLine("\nFailed to parse line " + i + "! \"" + curStr + "\"\n");
                            fail = true;
                        }

                    }
                }

            }

            return !fail;
        }
    }
    /*
     ****************************
     * CLASS START SwapUI
     ****************************
     */
    class SwapUI
    {
        private Program _program = null;

        public SwapUI(Program prg)
        {
            if (prg != null)
            {
                _program = prg;
                Console.WriteLine(Program.hr);
                Console.WriteLine("Welcome to SWAP v0.2");
                Console.WriteLine(Program.hr + "\n");
                Console.WriteLine("Use command 'lscmd' for command list.\n");
                InputLoop();
            }
            else
            {
                Console.WriteLine("Failed to start UI!!!\nEnter to exit...");
                Environment.Exit(1);
            }
        }

        private void InputLoop()
        {
            Console.Write("\nSWAP> ");
            string input = Console.ReadLine();
            Console.WriteLine();//a buffer between the typed cmd and the output
            ParseInput(input);

            InputLoop();
        }

        private void ParseInput(string toParse)
        {
            toParse = toParse.Trim();
            switch (toParse)
            {
                case "lscmd":
                    Console.WriteLine(Program.hr);
                    Console.WriteLine("Available Commands");
                    Console.WriteLine(Program.hr+"\n");
                    Console.WriteLine("lscmd   - Lists availabe server commands\n"+
                                      "concur  - Lists current connections to the server\n"+
                                      "ls      - Lists all available resources in the server's resoure\n          location.\n" +
                                      Program.hr + "\n" +
                                      "Unimplemented\n" +
                                      Program.hr + "\n\n" +
                                      "dc      - disconnect a given IP instantly.\n"+
                                      "susdc   - suspends and disconnects a given IP for a given time period.\n"+
                                      "susko   - suspends a given IP for a given time period, but does not disconnect\n          them upon use of command.\n"+
                                      "          NOTE: the suspension time doesn't start until after the IP disconnects          voluntarily.\n"+
                                      "pban    - disconnects a given IP and permenatly bans them from the server.\n"+
                                      "More in development...");
                    break;
                case "ls":
                    Program prg = Program.instance();
                    if (Directory.Exists(prg.ResourcePath))
                    {
                        string[] dir = Directory.GetDirectories(prg.ResourcePath);
                        string[] file = Directory.GetFiles(prg.ResourcePath);

                        //Directories are listed first
                        Console.WriteLine("\n" + Program.hr);
                        Console.WriteLine(prg.ResourcePath);
                        Console.WriteLine(Program.hr + "\n");

                        for (int i = 0; i < dir.Length; i++)
                        {
                            dir[i] = dir[i].Replace(prg.ResourcePath, "");
                            Console.WriteLine("Folder - " + dir[i]);
                        }
                        for (int i = 0; i < file.Length; i++)
                        {
                            file[i] = file[i].Replace(prg.ResourcePath, "");
                            Console.WriteLine("File - " + file[i]);
                        }
                        Console.WriteLine("\n");
                    }
                    else
                    {
                        //Resource path is invalid
                        Console.WriteLine("It appears that resource path " + prg.ResourcePath + " does exist or can't be accessed.");
                    }
                    break;
                case "concur":
                    //get current connnections
                    ArrayList con = _program.CurrentConnections;
                    Console.WriteLine("\n" + Program.hr);
                    Console.WriteLine("Current Connections @" + DateTime.Now);
                    Console.WriteLine(Program.hr + "\n");

                    for (int i = 0; i < con.Count; i++)
                    {
                        HttpRequest curCon = (HttpRequest) con[i];
                        
                        try
                        {
                            Console.WriteLine((i+1) + ": " + curCon.Connection.Client.RemoteEndPoint + " ---------- accessing: " + curCon.Resource);
                        }
                        catch (ObjectDisposedException ode)
                        {
                            //if the connection has been disposed of it is removed from the list
                            _program.CurrentConnections.RemoveAt(i);
                            i--;
                        }
                       
                    }
                    if (con.Count == 0)
                    {
                        Console.WriteLine("<No current connections>");
                    }
                    else
                    {
                        Console.WriteLine("<" + con.Count + " current connections>");
                    }

                        break;
                default:
                    Console.WriteLine("Unknown Command: "+toParse);
                    break;
            }
        }
    }
 
    /*
     ****************************
     * CLASS START HttpServer
     ****************************
     */
    class HttpServer
    {
        private TcpListener _server;
        private bool _isRunning = false;
        private readonly IConnectionListener _toNotify;
        private ArrayList _currentConnections;

        public ArrayList Connections
        {
            get
            {
                return _currentConnections;
            }
        }
        public HttpServer(IConnectionListener toNotify)
        {
            _toNotify = toNotify;
            _currentConnections = new ArrayList(1);
            InitConnection();
        }

        private void InitConnection()
        {
            _server = new TcpListener(IPAddress.Any, 80);
            Start();
        }

        /// <summary>
        /// internal use only, called as a thread when the server is started.
        /// </summary>
        private void Listen()
        {
            while (_isRunning)
            {
                Console.WriteLine("HttpServer: Connection Received!");
                TcpClient client = _server.AcceptTcpClient();
                _currentConnections.Add(client);
                Thread t = new Thread(() =>_toNotify.Notify(client));
                t.Start();
            }
        }

        /// <summary>
        /// Attempt to start a given instance of an HttpServer, returns wheither the process was succesful
        /// The server will begin to listen for new TCP connections on port 80, and whenever a connection
        /// comes in the Notify Method on the ISocketListener will be called with the new socket passed in
        /// </summary>
        /// <returns>result - true if the server was successfully started, false if the server was already running (RARE: or something failed along the way).</returns>
        public bool Start()
        {
            var result = false;

            if (!_isRunning)//TODO When a connection is dropped, performance suffers for subsuquent connections FIX THIS!!!!!
            {
                _isRunning = true;
                _server.Start();
                result = true;
                var t = new Thread(Listen);
                t.Start();
            }

            return result;
        }
        /// <summary>
        /// Attempts to stop a given instance of an HttpServer, returns wheither successful
        /// </summary>
        /// <returns>result - true if the server was successfully stopped, false if the server was already stopped (RARE: or something failed along the way).</returns>
        public bool Stop()
        {
            var result = false;

            if (_isRunning)
            {
                _isRunning = false;
                _server.Stop();
                result = true;
            }

            return result;
        }
    }
    /*
     ****************************
     * CLASS START RequestHandler
     ****************************
     */
    class RequestHandler
    {
        private HttpRequest _request = null;

        /// <summary>
        /// This method takes a TcpClient in and attempts to read, process and respond to an HTTP/1.1 request
        /// </summary>
        /// <param name="client">A TcpClient (pressumably with an HTTP request) to handle</param>
        public RequestHandler(ref HttpRequest request)
        {
            _request = request;
            ProcessRequest();
            Dispose();
        }
        /// <summary>
        /// This is the destructor, it removes the request handler from the Programs currentConnections data structure
        /// </summary>
        public void Dispose()
        {
            Console.WriteLine("########################");
            Console.WriteLine("RequestHandler disposed --- Resource: " + _request.Resource);
            Console.WriteLine("########################");
            Program.instance().CurrentConnections.Remove(_request);
            _request.Dispose();
        }

        public void ProcessRequest()
        {
            Program prg = Program.instance();


            //if the resource contains a query, this block handls it
            var resource = _request.Resource;

            if (!resource.Contains("."))//not a file request, therefore defaults to server's default home page of requested directory
            {
                if (resource.Trim()[resource.Length - 1] != '/')//added extra / if it was not included in the request
                {
                    resource += "/";
                }
                //goto default home page
                var path = resource + prg.HomePage;
                path = path.Replace('/', '\\');
                path = path.Substring(1);//gets rid of extra \ at beginning of resource path
                _request.Resource = path;
                SendResponse();//should be home page
            }
            else//file request
            {
                resource = resource.Substring(1);//removes the / if it isn't the homepage request
                resource = resource.Replace("/", "\\");//replaces any remaining / in the url
                _request.Resource = resource;
                SendResponse();
            }
        }

        public void SendResponse()
        {
            HttpResponse response = null;
            Program prg = Program.instance();
            Stream strm = _request.Connection.GetStream();//gets the request's stream
            var resource = _request.Resource;

     //       Console.Write(Program.ResourcePath + resource+" | Exists = ");
      //      Console.WriteLine(File.Exists(Program.ResourcePath + resource));

            if (File.Exists(prg.ResourcePath + resource))//File exists
            {
                if (!resource.Contains(prg.LockedPageToken))//200 OK
                {
                    bool streamed = false;
                    // Console.WriteLine("-------------------" + prg.ResourcePath + resource + "-----------------------");
                    response = new HttpResponse(200);
                    
                    var fs = new FileStream(prg.ResourcePath + resource, FileMode.Open, FileAccess.Read);

                    if (_request.GetValue("Range") != null)
                    {
                        response.SetHeader("Accept-Ranges", "bytes");
                        streamed = true;
                    }

                    var fileEnding = getFileType(resource);//gets file ending
                    //if php, must be parsed before being sent
                    if (fileEnding.Equals("php"))
                    {
                        var body = PHP_SAPI.Parse(prg.ResourcePath + resource, _request);
                        string[] rName = resource.Split('/');
                        response.SetContentType("html", rName[rName.Length - 1]);
                        response.SetHeader("Content-Length", "" + body.Length);
                        response.Send(ref strm, ref body);
                    }
                    else
                    {

                        if (fs.Length <= 30000000 && !streamed)//if the file is less than 30MB send normally && the file was not requested to be streamed
                        {
                            string[] rName = resource.Split('/');
                            response.SetContentType(fileEnding, rName[rName.Length - 1]);//sets the Content-Type of a respones and checks if the type is text

                            //         Console.Error.WriteLine("isText? -->" + isText);

                            response.SetHeader("Content-Length", "" + fs.Length);
 
                            response.Send(ref strm, ref fs);
                        }
                        else//send chunked
                        {
                            string[] rName = resource.Split('/');
                            response.SetContentType(fileEnding, rName[rName.Length - 1]);
                            response.SetHeader("Transfer-Encoding", "chunked");
                            if (streamed)
                            {
                                response.SetHeader("Content-Range", "bytes 0-" + fs.Length + "/*");
                            }
                            response.SetHeader("Content-Length", "" + fs.Length);
                            response.SendChunked(ref strm, ref fs);
                        }
                    }
                }
                else//403 Forbidden
                {
                    response = new HttpResponse(403);
                    //checks if user specified 403 page exists
                    if (File.Exists(prg.ResourcePath + prg.AccessDenied))
                    {

                        FileStream fs = File.Open(prg.ResourcePath + prg.AccessDenied, FileMode.Open, FileAccess.Read);
                        response.SetHeader("Content-Type", "text/html");
                        response.SetHeader("Content-Length", "" + fs.Length);
                        response.Send(ref strm, ref fs);
                        fs.Close();
                    }
                    else
                    {
                        string page = "<html><body><h1>403 Access Denied:</h1> <p>" + resource + "</p></body></html>";
                        response.SetContentType("html", prg.AccessDenied);
                        response.SetHeader("Content-Length", "" + page.Length);
                        response.Send(ref strm, ref page);
                    }
                }
                
            }
            else//404 File Not Found
            {
                response = new HttpResponse(404);

                //checks if user specified 404 page exists
                if (File.Exists(prg.ResourcePath + prg.Page404))
                {
                    
                    FileStream fs = File.Open(prg.ResourcePath + prg.Page404, FileMode.Open, FileAccess.Read);
                    response.SetHeader("Content-Type", "text/html");
                    response.SetHeader("Content-Length", "" + fs.Length);
                    response.Send(ref strm, ref fs);
                    fs.Close();
                }
                else
                {
                    string page = "<html><body><h1>404 file not found:</h1> <p>" + resource + "</p></body></html>";
                    response.SetContentType("html", prg.Page404);
                    response.SetHeader("Content-Length", ""+page.Length);
                    response.Send(ref strm, ref page);
                }


            }
        }
        
        public string getFileType(string fileName)
        {
            var type = "";
            var tokens = fileName.Split('.');

            var rawEnding = tokens[tokens.Length - 1];

            for (int i = 0; i < rawEnding.Length; i++)
            {
                char curChar = rawEnding[i];
                //if curChar is a letter or number
                int curCharInt = (int)curChar;
                if ((curCharInt >= (int)'a' && curCharInt <= (int)'z') || (curCharInt >= (int)'A' && curCharInt <= (int)'Z') || (curCharInt >= (int)'0' && curCharInt <= (int)'9'))
                {
                    type += curChar;
                }
                else if (curChar != '.')//if the curChar is not a letter or number or a period the type is finished
                {
                    break;
                }
            }
           // Console.WriteLine("File Type: "+type);
            return type;
        }

        public static string GetResource(string header)
        {
            int endFirstLine = header.IndexOf("\r\n");
            string firstLine = header.Substring(0, endFirstLine);

            string[] fLPart = firstLine.Split(' ');

            return fLPart[1];
        }

        /// <summary>
        /// This method request an resource from an HTTP server and returns a string representation of the body
        /// </summary>
        /// <param name="addr">The Internet address to connect to.</param>
        /// <exception cref = "SocketException"> Thrown when you can't connect to the given address (no such host, or no internet connectivity </exception>
        /// <returns>A string representation of the response from addr</returns>

        public static string HttpBodyRequest(string addr)
        {
            string response = "";
            TcpClient client = null;

            //trys to connect, if it can't it exits the program
            client = new TcpClient(addr, 80);


            string[] tokens = { "", "" };

            string host = tokens[0];
            string resource = "";

            if (tokens.Length > 1)
            {
                resource = "/" + tokens[1];
            }



            Stream s = client.GetStream();
            var sw = new StreamWriter(s);
            var sr = new StreamReader(s);

            string request = "GET " + resource + " HTTP/1.1\r\n" +
                             "Host: " + host + "\r\n" +
                             "Connection: close\r\n" +
                             "\r\n";

            sw.Write(request);
            sw.Flush();

            //reads request header
            while (!response.Contains("\r\n\r\n"))
            {
                response += "" + (char)sr.Read();
            }
            //gets rid of header
            response = "";
            //reads request body
            while (!sr.EndOfStream)
            {
                response += "" + (char)sr.Read();
            }

            return response;

        }
    }

}
/*
 *********************************
 * INTERFACE START ISocketListener
 *********************************
 */

interface IConnectionListener
{
    void Notify(TcpClient data);
}