﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Net.Mime;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using HTTP;

namespace SimpleHtmlCloud
{
    class Program : IConnectionListener
    {
        private bool _run = true;

        private static string _myAddr = "155.92.105.192";
        public static string MyAddress
        {
            get
            {
                return _myAddr;
            }
        }

        private static string _resourcePath = "Resources\\";
        public static string ResourcePath
        {
            get
            {
                return _resourcePath;
            }
        }
        private static string _page404Fname = "404_PAGE.html";
        public static string Page404
        {
            get
            {
                return _page404Fname;
            }
        }
        private static string _homePage = "home_page.html";
        public static string HomePage
        {
            get
            {
                return _homePage;
            }
        }
        private int served = 0;

        //CONSTRUCTOR********
        public Program()
        {
            var server = new HttpServer(this);
            server.Start();
        }

        public static void Main(String[] args)
        {
            new Program();
            Console.Read();
        }

        private static string[] ParseHttpAddr(string addr)
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
            Console.WriteLine("Total connections served: " + served);
            new RequestHandler(connection);
        }

    }
    /*
     ****************************
     * CLASS START HtmlGenerator
     ****************************
     */
    //TODO This class is what need to be worked on
    class HtmlGenerator
    {
        private static string _myAddr = "155.92.105.192";
        private static string _outputPath = "Resources\\";
        private static string _page404Fname = "404_PAGE.html";


        //Prepares a html directory list for a given directory
        public static HttpResponse PrepareHtmlDirectoryList(String directory)//TODO add this functionality (BASIC, FOR APPLICATION INITIALIZING CALL ONLY)
        {
            var content = "";
            HttpResponse response;


            //CHECK IF RESOURCE EXISTS

            if (File.Exists(directory))
            {
                //IF YES, THEN GENERATE HTML DIRECTORY PAGE AND RETURN IT
                response = new HttpResponse(200);
                //the preliminary html file tags
                content += "<html>\r\n" +
                           "<head>\n" +
                           "<title> M.I.A. Web File Viewer </title>\n" +
                           "<base href=\"http://" + _myAddr + "/\">" +
                           "<style>\n" +
                           "body\n" +
                           "{\n" +
                           "text-align:center;\n" +
                           "}\n" +
                           "a:link, a:visited\n" +
                           "{\n" +
                           "font-weight:bold;\n" +
                           "text-decoration:none;\n" +
                           "margin:8pt;\n" +
                           "}\n" +
                           "a:hover\n" +
                           "{\n" +
                           "color:#FF4400;\n" +
                           "font-size:150%;\n" +
                           "}\n" +
                           "a:active\n" +
                           "{\n" +
                           "color:#FFFF00;\n" +
                           "font-size:150%;\n" +
                           "width:180px;\n" +
                           "}\n" +
                           "</style>\n" +
                           "</head>\n" +
                           "    <body>\n";

                string[] files = FormatPaths(Directory.GetFiles(directory), directory);

                //creates file links
                for (int i = 0; i < files.Length; i++)
                {
                    content += "            <a href= \"" + files[i] + "\">File: " + files[i] +
                               "</href> <br> \r\n";
                }

                //closing html file tags
                content += "        </body>\r\n" +
                           "</html>";
                //adds content to body
                response.Body = content;
                //adds headers
                response.AddHeader("Content-Type", "text/html; charset=utf-8");
                response.AddHeader("Content-Length", "" + content.Length);
            }
            else
            {
                response = new HttpResponse(404);

                //ELSE RETURN A VALUE THAT MEANS THE RESOURCE COULD NOT BE FOUND
                if (File.Exists(_outputPath + _page404Fname)) //checks for a premade 404 page
                {
                    //file reading works! tested as proof of concept
                    var fs = File.OpenRead(_outputPath + _page404Fname);

                    byte[] contents = new byte[fs.Length];
                    fs.Read(contents, 0, (int)fs.Length);
                    foreach (byte utf8 in contents)
                    {
                        char utf8Char = (char)utf8;
                        content += "" + utf8Char;
                    }
                    response.Body = content;
                }
                else //otherwise it generates a M.I.A. default 404 page
                {
                    content += GeneratePage(404);
                    response.Body = content;
                }
                //add headers
                response.AddHeader("Content-Type", "text/html; charset=utf-8");
                response.AddHeader("Content-Length", "" + content.Length);
            }





            return response;
        }

        private static string GeneratePage(int statusCode)
        {
            var pageContent = "";
            pageContent += "<!DOCTYPE HTML>\n" +
                                "<html>\n" +
                                "<head>\n" +
                                "<title>M.I.A. 404 Error</title>\n" +
                                "<style>\n" +
                                "body\n" +
                                "{\n" +
                                "background-color:#953100;\n" +
                                "}\n" +
                                "h1\n" +
                                "{\n" +
                                "background-color:#F00000;\n" +
                                "color:#FFFFFF;\n" +
                                "font-size:87px;\n" +
                                "text-transform:uppercase;\n" +
                                "text-align:center;\n" +
                                "border-style:outset;\n" +
                                "border-color:#950004;\n" +
                                "border-width:0.1em;" +
                                "margin-top:0px;\n" +
                                "margin-bottom:0em;\n" +
                                "}\n" +
                                "\n" +
                                "p\n" +
                                "{\n" +
                                "margin-left:1em;\n" +
                                "margin-right:3em;\n" +
                                "margin-top:1em;\n" +
                                "font-size:16pt;\n" +
                                "}\n" +
                                "#author\n" +
                                "{\n" +
                                "font-size:0.75em;\n" +
                                "text-align:center;\n" +
                                "}\n" +
                                "</style>\n" +
                                "</head>\n";

            if (statusCode == 404)
            {
                pageContent += "<body>\n" +
                               "		<h1> 404 Error</h1>\n" +
                               "		<p>\n" +
                               "		The requested page does not exist.\n" +
                               "		<hr />\n" +
                               "		</p>\n" +
                               "		<p id=\"author\">\n" +
                               "		Page generated by M.I.A. Server File Viewer&copy(Version 0.0.21)\n" +
                               "		</p>\n" +
                               "	</body>\n" +
                               "</html>";
            }
            else
            {
                pageContent += "</html>\n";
            }


            return pageContent;
        }

        private static string GetGlobalIp()
        {
            //this is a website that tells you your global ip in plaintext format
            string response = "";

            var startInd = response.IndexOf("<body>") + "<body>".Length;
            var length = response.IndexOf("</", startInd) - startInd;

            response = response.Substring(startInd, length).Trim();

            string[] a = response.Split(':');

            var a2 = a[1].Trim();

            return a2;
        }
        //Prepares an appropriate file viewer html page for a given file name
        private static void PrepareHtmlFilePage(string fname)
        {
            var fileExt = Path.GetExtension(fname);

            string content = "<html>\r\n" +
                             "\t<title>" + fname + "</title>\r\n" +
                             "\t<header><b><u>File Viewer</u></b></header>\r\n" +
                             "\t\t<body>\r\n";

            if (File.Exists(fname))
            {
                var s = File.OpenRead(fname);

                if (fileExt.Contains("txt"))
                {
                    var sr = new StreamReader(s);

                    while (!sr.EndOfStream)
                    {
                        content += "" + (char)sr.Read();
                    }
                    content += "\r\n";
                }
                else if (fileExt.Contains("jpg") || fileExt.Contains("jpeg"))
                {
                    content += "<img src = \"" + fname + "\"></img>\r\n";
                }
                else
                {
                    content += "Unsupported File Type\r\n";
                }

                s.Close();
            }
            else
            {
                content += "File Could not be Opened.\r\n";
            }

            content += "\t\t</body>\r\n" +
                       "</html>";

            var os = File.Create(_outputPath + "Current_File.html");

            if (fileExt.Contains("txt"))
            {
                var sw = new StreamWriter(os);

                sw.Write(content);
                sw.Flush();

            }

            os.Close();

        }
        /// <summary>
        /// Gets the contents of a file and returns a string representation of the file's contents
        /// </summary>
        /// <param name="fname"></param>
        /// <returns></returns>
        private static string GetBody(string fname)
        {
            string body = "";
            StreamReader sr = File.OpenText(fname);

            while (!sr.EndOfStream)
            {
                body += "" + (char)sr.Read();
            }

            sr.Close();

            return body;
        }
        /// <summary>
        /// Formats a list of strings that represent file/directory paths
        /// </summary>
        /// <param name="unfPaths">A string array of unformatted file/directory paths</param>
        /// <returns>The formatted version of the unformatted string list</returns>
        private static string[] FormatPaths(string[] unfPaths, string directory)
        {
            string[] forPaths = new string[unfPaths.Length];

            for (int i = 0; i < unfPaths.Length; i++)
            {
                forPaths[i] = unfPaths[i].Replace(directory, "");
            }

            return forPaths;
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

        public HttpServer(IConnectionListener toNotify)
        {
            _toNotify = toNotify;
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
                var client = _server.AcceptTcpClient();
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

            if (!_isRunning)
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
        private readonly Stream _currentStream = null;

        /// <summary>
        /// This method takes a TcpClient in and attempts to read, process and respond to an HTTP/1.1 request
        /// </summary>
        /// <param name="client">A TcpClient (pressumably with an HTTP request) to handle</param>
        public RequestHandler(TcpClient client)
        {
            _currentStream = client.GetStream();
            HandleRequest();
        }

        /// <summary>
        /// This method handles the entirity of request processing
        /// </summary>
        private void HandleRequest()
        {
            string request = ReadRequest();

            var response = ProcessRequest(request);

            SendResponse(response);

            _currentStream.Close();
        }

        private void SendResponse(HttpResponse response)
        {
            var toSend = response.ToString();
            for (int i = 0; i < toSend.Length; i++)
            {
                char curChar = toSend[i];
                byte[] curSend = { (byte)curChar };

                _currentStream.Write(curSend, 0, 1);
                _currentStream.Flush();
            }

        }

        private HttpResponse ProcessRequest(string request)
        {
            HttpResponse response;
            var header = "";
            var body = "";

            var headerLength = request.IndexOf("\r\n\r\n") + "\r\n\r\n".Length;

            header = request.Substring(0, headerLength);

            body = request.Substring(headerLength);

            var resource = GetResource(header);


            if (resource.Equals("/"))
            {
                //goto homepage
                response = createResponse(Program.HomePage);//should be home page
            }
            else
            {
                //check if resource exists
                //if so fetch it
                //else 404 page
                response = new HttpResponse(200);//TODO
            }

            return response;
        }

        private HttpResponse createResponse(string resource)
        {
            HttpResponse response = null;
            if (File.Exists(Program.ResourcePath + resource))
            {
                response = new HttpResponse(200);
                string body = "";
                var fs = new FileStream(Program.ResourcePath + resource, FileMode.Open, FileAccess.Read);

                loadFile(fs);

                //if text type convert to string
                if (resource.EndsWith("html") || resource.EndsWith("htm") || resource.EndsWith("xml") ||
                    resource.EndsWith("css") || resource.EndsWith("js"))
                {
                    response.AddHeader("Content-Type", "text/");//TODO add resource type here
                }
                else if (resource.EndsWith("php"))
                {
                    response.AddHeader("Content-Type", "text/html");
                    //TODO php parsing goes here
                }
                //else print utf rep of bytes

                body = "";

                response.Body = body;
                response.AddHeader("Content-Length", "" + fs.Length);
                //TODO placeholder
                fs.Close();
            }
            else
            {
                //TODO 404 response
            }
            return response;
        }

        private byte[] loadFile(FileStream fs)
        {

            byte[] file = new byte[fs.Length];
            fs.Read(file, 0, (int)fs.Length);

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
            Console.WriteLine(header);

            //reads body(if any)
            //TODO add the ability to read a body (POST method)


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