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
    class Program : IConnectionListener
    {
        private bool _run = true;
        public const string hr = "--------------------------------------------------------------------";

        private static Program _uInst = null;

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

            parseConfigFile(_workingPath + "\\swap_config.cnfg");


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

        private void parseConfigFile(string fname)
        {
            bool fail = false;
            Program inst = this;

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
                                PropertyInfo pi = GetType().GetProperty(token[0]);//attempts to get the specified property
                                if (pi != null)//if the property exists it is set
                                {
                                    try
                                    {
                                        pi.SetValue(inst, token[1]);
                                        Console.WriteLine("'" + token[0] + "' set to " + pi.GetValue(inst));
                                    }
                                    catch (TargetInvocationException tie)//if the data is not accepted by the setter method
                                    {
                                        Console.WriteLine("\nInvalid format on line "+ i +" "+tie.InnerException.Message+"\n");
                                        fail = true;
                                    }
                                    catch (TargetException te)
                                    {
                                        Console.WriteLine("Target Exception on line "+ i + ", Property: "+token[0] +"\n"+te.Message);
                                    }
                                    
                                }
                                else//otherwise an error message is printed because property doesn't exist
                                {
                                    Console.WriteLine("\nNon-existant property " + token[0] + " on line " + i+"\n");
                                    fail = true;
                                }

                            }
                            else//if token.Length != 2 then the syntax was not correct
                            {
                                Console.WriteLine("\nFailed to parse line " + i + "! \""+curStr+"\"\n");
                                fail = true;
                            }
                        
                    }
                }

            }
            else
            {
                Console.WriteLine("\nCould not find "+ fname +", Server could not be started!!!");
                fail = true;
            }
            if (fail)
            {
                Console.WriteLine("\nFailed to parse configuration file!");
                Console.WriteLine("Press Enter to Exit...");
                Console.Read();
                Environment.Exit(1);
            }
            else
            {
                Console.WriteLine("\nSuccessfully read config file!\n");
            }
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
            served++;
            _currentConnections.Add(connection);
            Thread t = new Thread(() => new RequestHandler(ref connection));
            t.Start();
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
                        TcpClient curCon = (TcpClient) con[i];
                        
                        try
                        {
                            Console.WriteLine((i+1) + ": " + curCon.Client.RemoteEndPoint);
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
                        Console.WriteLine("No current connections");
                    }
                    else
                    {
                        Console.WriteLine(con.Count + " current connections");
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
                TcpClient client = _server.AcceptTcpClient();
                _currentConnections.Add(client);
                _toNotify.Notify(client);
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
        private TcpClient _currentConnection = null;
        private Stream _currentStream = null;

        /// <summary>
        /// This method takes a TcpClient in and attempts to read, process and respond to an HTTP/1.1 request
        /// </summary>
        /// <param name="client">A TcpClient (pressumably with an HTTP request) to handle</param>
        public RequestHandler(ref TcpClient client)
        {
            _currentConnection = client;
            _currentStream = client.GetStream();
            HandleRequest();
        }
        /// <summary>
        /// This is the destructor, it removes the request handler from the Programs currentConnections data structure
        /// </summary>
        ~RequestHandler()
        {
            Program.instance().CurrentConnections.Remove(_currentConnection);
        }

        /// <summary>
        /// This method handles the entirity of request processing
        /// </summary>
        private void HandleRequest()
        {
            string request = ReadRequest();//DONE
            
            ProcessRequest(request);//DONE for now (2/4/2014)

            _currentStream.Close();//DONE
        }

        private void ProcessRequest(string request)
        {
            var header = "";
            var body = "";
            HttpRequest httpRequest = null;
            Program prg = Program.instance();

            var headerLength = request.IndexOf("\r\n\r\n") + "\r\n\r\n".Length;

            header = request.Substring(0, headerLength);

            body = request.Substring(headerLength);

            var resource = GetResource(header);

            httpRequest = setupRequest(ref header, ref body);

            if (resource.Contains("?"))
            {
                string[] query;
                int startQuery = resource.IndexOf("?")+1;
             //   Console.WriteLine("Resource: " + resource);
                query = resource.Substring(startQuery).Split('&');
                foreach( string va in query)
                {
             //       Console.WriteLine("Query: "+va);
                }
                //removes query and stores it to the request
                resource = resource.Substring(0, startQuery-1);
                httpRequest.SetQuery(query);
            }

            if (!resource.Contains(".") && !resource.Contains(prg.LockedPageToken))//not a file request, therefore defaults to home_page of requested directory
            {
                if (resource.Trim()[resource.Length - 1] != '/')//added extra / if it was not included in the request
                {
                    resource += "/";
                }
                //goto default home page
                var path = resource + prg.HomePage;
                path = path.Replace('/', '\\');
                path = path.Substring(1);//gets rid of extra \ at beginning of resource path
                SendResponse(ref path, ref httpRequest);//should be home page
            }
            else
            {
                resource = resource.Substring(1);//removes the / if it isn't the homepage request
                resource = resource.Replace("/", "\\");//replaces any remaining / in the url
                SendResponse(ref resource, ref httpRequest);
            }
        }
        private HttpRequest setupRequest(ref string header, ref string body)
        {
            var firstLine = header.Substring(0, header.IndexOf("\r\n"));
            var reqMethod = firstLine.Split(' ')[0];
            var resource = firstLine.Split(' ')[1];
            HttpRequest request = new HttpRequest(reqMethod, resource, body);

            string[] lines = header.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < lines.Length; i++)
            {
                //this ensures that if there is a ':' in the value section it doesn't affect the value
                
                string[] curTokens = lines[i].Split(new char[] {':'}, 2);
               
                try
                {
                    request.AddHeader(curTokens[0], curTokens[1]);
                }
                catch (IndexOutOfRangeException ioore)
                {
                    //header is not added if there are not two tokens
                }
                
            }

        //    Console.WriteLine("REQUEST OBJECT:\n---------------\n"+request);

            return request;
        }

        private void SendResponse(ref string resource, ref HttpRequest request)
        {
            HttpResponse response = null;
            Program prg = Program.instance();
     //       Console.Write(Program.ResourcePath + resource+" | Exists = ");
      //      Console.WriteLine(File.Exists(Program.ResourcePath + resource));

            if (File.Exists(prg.ResourcePath + resource) && !resource.Contains(prg.LockedPageToken))//200 OK
            {
               // Console.WriteLine("-------------------" + prg.ResourcePath + resource + "-----------------------");
                response = new HttpResponse(200);
                var fs = new FileStream(prg.ResourcePath + resource, FileMode.Open, FileAccess.Read);

                var fileEnding = getFileType(resource);
                if (fileEnding.Equals("php"))
                {
                    var body = PHP_SAPI.Parse(prg.ResourcePath + resource, request);
                    string[] rName = resource.Split('/');
                    response.SetContentType("html", rName[rName.Length - 1]);
                    response.SetHeader("Content-Length", ""+body.Length);
                    response.Send(ref _currentStream, ref body);
                }
                else
                {
                    bool isText = true;

                    if (fs.Length <= 30000000)//if the file is less than 30MB send normally
                    {
                        string[] rName = resource.Split('/');
                        isText = response.SetContentType(fileEnding, rName[rName.Length - 1]);//sets the Content-Type of a respones and checks if the type is text

               //         Console.Error.WriteLine("isText? -->" + isText);

                        response.SetHeader("Content-Length", "" + fs.Length);
                        response.Send(ref _currentStream, ref fs);
                    }
                    else//send chunked
                    {
                        string[] rName = resource.Split('/');
                        isText = response.SetContentType(fileEnding, rName[rName.Length - 1]);
                        response.SetHeader("Transfer-Encoding", "chunked");
                        response.SetHeader("Content-Length", "" + fs.Length);
                        response.SendChunked(ref _currentStream, ref fs);
                    }
                }
                
            }
            else if (!resource.Contains(prg.LockedPageToken))//404 File Not Found
            {
                response = new HttpResponse(404);

                //checks if user specified 404 page exists
                if (File.Exists(prg.ResourcePath + prg.Page404))
                {
                    
                    FileStream fs = File.Open(prg.ResourcePath + prg.Page404, FileMode.Open, FileAccess.Read);
                    response.SetHeader("Content-Type", "text/html");
                    response.SetHeader("Content-Length", "" + fs.Length);
                    response.Send(ref _currentStream, ref fs);
                    fs.Close();
                }
                else
                {
                    string page = "<html><body><h1>404 file not found:</h1> <p>" + resource + "</p></body></html>";
                    response.SetContentType("html", prg.Page404);
                    response.SetHeader("Content-Length", ""+page.Length);
                    response.Send(ref _currentStream, ref page);
                }


            }
            else//403 Access Denied
            {
                response = new HttpResponse(403);
                //checks if user specified 403 page exists
                if (File.Exists(prg.ResourcePath + prg.AccessDenied))
                {

                    FileStream fs = File.Open(prg.ResourcePath + prg.AccessDenied, FileMode.Open, FileAccess.Read);
                    response.SetHeader("Content-Type", "text/html");
                    response.SetHeader("Content-Length", "" + fs.Length);
                    response.Send(ref _currentStream, ref fs);
                    fs.Close();
                }
                else
                {
                    string page = "<html><body><h1>403 Access Denied:</h1> <p>" + resource + "</p></body></html>";
                    response.SetContentType("html", prg.AccessDenied);
                    response.SetHeader("Content-Length", "" + page.Length);
                    response.Send(ref _currentStream, ref page);
                }
            }
        }
        
        private string getFileType(string fileName)
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
        private char[] loadFile(FileStream fs)
        {
            char[] file = new char[fs.Length];
            byte[] buffer = new byte[1];
            for (int i = 0; i < file.Length; i++)
            {
                fs.Read(buffer, 0, 1);
                file[i] = (char)buffer[0];
            }
                

            return file;
        }

        private static string GetResource(string header)
        {
            int endFirstLine = header.IndexOf("\r\n");
            string firstLine = header.Substring(0, endFirstLine);

            string[] fLPart = firstLine.Split(' ');

            return fLPart[1];
        }

        private string ReadRequest()
        {
            var request = "";
            var sr = new StreamReader(_currentStream);

            
            var header = "";
            var body = "";
            

            //reads header
            while (!header.Contains("\r\n\r\n"))
            {
                header += "" + (char)sr.Read();

            }

            if (header.Contains("POST") || header.Contains("post")) 
            {
                while (!body.Contains("\r\n\r\n"))
                {
                    body += ""+(char)sr.Read();
                }
            }

            request = header + body;

            return request;
        }

        /// <summary>
        /// This method request an resource from an HTTP server and returns a string representation of the body
        /// </summary>
        /// <param name="addr">The Internet address to connect to.</param>
        /// <exception cref = "SocketException"> Thrown when you can't connect to the given address (no such host, or no internet connectivity </exception>
        /// <returns>A string representation of the response from addr</returns>

        private static string HttpBodyRequest(string addr)
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