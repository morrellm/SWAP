using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Timers;
using HTTP;

namespace PHP_PARSER
{
    /*
     * Aim is to support PHP 5.5.4
     * The way it works:
     * 1) the parser is setup using a configuration file of a known location
     * 2) when an outside program wants to use the php parser it passes in a
     */
    public static class PHP_SAPI
    {
        private static string _phpLoc = "C:\\PHP\\";
        public static string GATE_INTER = "CGI/1.1";
        private static Process _parser = null;
        private static int _tempNum = 0;

       /* public const string[] knownSuperGlobal = { "$_SERVER['PHP_SELF']",              //Returns the location of the executing script
                                                   "$_SERVER['GATEWAY_INTERFACE']", 	//Returns the version of the Common Gateway Interface (CGI) the server is using
                                                   "$_SERVER['SERVER_ADDR']", 	        //Returns the IP address of the host server
                                                   "$_SERVER['SERVER_NAME']" ,	        //Returns the name of the host server (such as www.w3schools.com)
                                                   "$_SERVER['SERVER_SOFTWARE']", 	    //Returns the server identification string (such as Apache/2.2.24)
                                                   "$_SERVER['SERVER_PROTOCOL']", 	    //Returns the name and revision of the information protocol (such as HTTP/1.1)
                                                   "$_SERVER['REQUEST_METHOD']", 	    //Returns the request method used to access the page (such as POST)
                                                   "$_SERVER['REQUEST_TIME']", 	        //Returns the timestamp of the start of the request (such as 1377687496)
                                                   "$_SERVER['QUERY_STRING']",  	    //Returns the query string if the page is accessed via a query string
                                                   "$_SERVER['HTTP_ACCEPT']", 	        //Returns the Accept header from the current request
                                                   "$_SERVER['HTTP_ACCEPT_CHARSET']", 	//Returns the Accept_Charset header from the current request (such as utf-8,ISO-8859-1)
                                                   "$_SERVER['HTTP_HOST']", 	        //Returns the Host header from the current request
                                                   "$_SERVER['HTTP_REFERER']",   	    //Returns the complete URL of the current page (not reliable because not all user-agents support it)
                                                   "$_SERVER['HTTPS']", 	            //Is the script queried through a secure HTTP protocol
                                                   "$_SERVER['REMOTE_ADDR']",   	    //Returns the IP address from where the user is viewing the current page
                                                   "$_SERVER['REMOTE_HOST']",   	    //Returns the Host name from where the user is viewing the current page
                                                   "$_SERVER['REMOTE_PORT']",   	    //Returns the port being used on the user's machine to communicate with the web server
                                                   "$_SERVER['SCRIPT_FILENAME']", 	    //Returns the absolute pathname of the currently executing script
                                                   "$_SERVER['SERVER_ADMIN']",  	    //Returns the value given to the SERVER_ADMIN directive in the web server configuration file (if your script runs on a virtual host, it will be the value defined for that virtual host) (such as someone@w3scholls.com)
                                                   "$_SERVER['SERVER_PORT']", 	        //Returns the port on the server machine being used by the web server for communication (such as 80)
                                                   "$_SERVER['SERVER_SIGNATURE']", 	    //Returns the server version and virtual host name which are added to server-generated pages
                                                   "$_SERVER['PATH_TRANSLATED']", 	    //Returns the file system based path to the current script
                                                   "$_SERVER['SCRIPT_NAME']", 	        //Returns the path of the current script
                                                   "$_SERVER['SCRIPT_URI']", 	        //Returns the URI of the current page
                                                   "$_POST['(.*?)']",                   //POST superglobal with a variable name in it (Obtained from a HTTP post method body)
                                                   "$_GET['(.*?)']",                    //GET superglobal with a variable name in it (Obtained from)
                                                   "$_REQUEST['(.*?)]"                  //REQUEST superglobal with a variable name in it ()
                                                 };
        */
        /// <summary>
        /// This must be called before any php parsing can occur.
        /// A config file should have been included with this parser.
        /// Also, a help file should have been provided to show how the config
        /// file is parsed.
        /// </summary>
        /// <param name="configFname">This is the location of the parser's configuration file</param>
        /// <returns></returns>
        public static bool initParser(string configFname)
        {
            bool result = false;

            if (File.Exists(configFname + "php.exe"))
            {
                //sets php location
                _phpLoc = configFname;
                //start parser process
                ProcessStartInfo psi = new ProcessStartInfo(_phpLoc + "php.exe");

                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                _parser = new Process();

                _parser.StartInfo = psi;

                _parser.Start();
                result = true;
            }

            return result;
        }
        

        public static String Parse(string pathname, HttpRequest request)
        {
            
            var body = "";
            if (File.Exists(pathname))
            {
                //200 have script, will parse
                string prepend = setSuperGlobals(request);
                string scriptPath = GetPathName(pathname);

                int curNum = _tempNum;//sets the current temp
                FileStream tfs = null;
                FileStream mfs = null;
                try
                {
                    tfs = File.Create(scriptPath + "temp" + curNum + ".php");//creates a temp file to be augmented with the server variables
                    _tempNum++;

                    mfs = File.OpenRead(pathname);
                    var buffer = new byte[prepend.Length + mfs.Length];
                    String output = prepend;

                    for (int i = 0; i < prepend.Length; i++)
                    {
                        buffer[i] = (byte)prepend[i];
                    }


                    var startOffset = prepend.Length;
                    mfs.Read(buffer, startOffset, (int)mfs.Length);


                    tfs.Write(buffer, 0, buffer.Length);

                    mfs.Close();
                    tfs.Close();

                    _parser.StartInfo.Arguments = "\"" + scriptPath + "temp" + curNum + ".php\"";//sets argument
                    _parser.Start();//uses specified args

                    StreamReader sr = _parser.StandardOutput;
                    int timeout = 1000;
                    bool stop = false;
                    Timer timer = new Timer(timeout);
                    timer.Elapsed += delegate(Object sender, ElapsedEventArgs e)
                                     {
                                         stop = true;
                                         timer.Dispose();
                                     };
                    try
                    {
                        timer.Start();
                        while (!sr.EndOfStream && !stop)
                        {
                            body += sr.ReadLine();
                        }

                    }
                    catch (OutOfMemoryException oome)
                    {
                        //this means there was some error in the php and to write the body
                    }
                    finally
                    {
                        timer.Stop();
                    }


                    File.Delete(scriptPath + "temp" + curNum + ".php");
                    _tempNum--;
                }
                catch (IOException ioe)
                {
                    //this occurs if the File is being accessed by another process.
                    //if this is the case, the parser does nothing.
                }
                finally
                {
                    //closes any filestreams that were not closed due to a thrown exception
                    if (tfs != null)
                    {
                        tfs.Close();
                    }
                    if (mfs != null)
                    {
                        mfs.Close();
                    }
                }
            }
            else
            {
                body = "<html><body>404 file not found: "+pathname+"</body></html>";
            }

            return body;
        }
        /// <summary>
        /// This method sets superglobals to their correct values based upon the httpRequest invoking the php file.
        /// This method should be called before parsing a php file
        /// </summary>
        /// <param name="request">the request from which to extract the datas</param>
        private static String setSuperGlobals(HttpRequest request)
        {
            String phpPrepend = "<?php\n";

            //SERVER_ADDR, SERVER_NAME, SERVER_SOFTWARE-X, SERVER_PROTOCOL-X
            phpPrepend += "$_SERVER['SERVER_NAME'] = \"" + System.Environment.MachineName + "\";\n";
            phpPrepend += "$_SERVER['SERVER_SOFTWARE'] = \"Simple Webserver and PHP v0.2\";\n";
            phpPrepend += "$_SERVER['SERVER_ADDR'] = \"" + request.GetValue("Host") + "\";\n";//SERVER_ADDR
            phpPrepend += "$_SERVER['PHP_SELF'] = \"" + GetScriptName(request.Resource) + "\";\n";
            phpPrepend += "$_SERVER['SCRIPT_NAME'] = \""+GetScriptName(request.Resource)+"\";\n";
            phpPrepend += "$_SERVER['SERVER_PROTOCOL'] = \"HTTP/1.1\";\n";
            phpPrepend += "$_SERVER['GATEWAY_INTERFACE'] = \""+GATE_INTER+"\";\n";
            phpPrepend += "$_SERVER['HTTP_HOST'] = \""+request.GetValue("Host") +"\";\n";
            phpPrepend += "$_SERVER['HTTP_ACCEPT'] = \""+request.GetValue("Accept")+"\";\n";
            phpPrepend += "$_SERVER['HTTP_USER_AGENT'] = \"" + request.GetValue("User-Agent") + "\";\n";
            phpPrepend += "$_SERVER['HTTP_REFERER'] = \"" + request.GetValue("Referer") + "\";\n";
            phpPrepend += "$_SERVER['QUERY_STRING'] = \"" + request.GetQueryString() + "\";\n";
            phpPrepend += "$_SERVER['REQUEST_METHOD'] = \"" + request.MethodToString() + "\";\n";
            var method = request.RequestMethod;
            //REQUEST_METHOD, REQUEST_TIME

            //QUERY_STRING-X
            //sets queries if used
            Hashtable query = request.Query;
            if (query != null)
            {
                foreach (var key in query.Keys)
                {
                    phpPrepend += "$_GET['" + key + "'] = \"" + query[key] + "\";\n";
                }
            }
            //HTTP_ACCEPT, HTTP_ACCEPT_CHARSET
            //HTTP_HOST, HTTP_REFERER, HTTPS
            //REMOTE_ADDR, REMOTE_HOST, REMOTE_PORT
            //SCRIPT_FILENAME
            //SERVER_ADMIN, SERVER_PORT, SERVER_SIGNATURE
            //PATH_TRANSLATED, SCRIPT_NAME, SCRIPT_URI

            phpPrepend += "?>\n";

            return phpPrepend;
        }
        private static string GetScriptName(string res)
        {
            string ret = res;

            if (res.Contains("?"))
            {
                res = res.Substring(0, res.IndexOf("?"));
            }

            return res;
        }

        private static string GetPathName(string res)
        {
            string[] splt = res.Split('\\');
            string ret = "";

            for (int i = 0; i < splt.Length-1; i++)//disincludes the last section(filename), to get the folder of the resource
            {
                ret += splt[i] + "\\";
            }

            return ret;
        }

    }
}
