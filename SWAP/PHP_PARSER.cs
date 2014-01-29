using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HTTP;

namespace PHP_PARSER
{
    /*
     * Aim is to support PHP 5.1.0
     * The way it works:
     * 1) the parser is setup using a configuration file of a known location
     * 2) when an outside program wants to use the php parser it passes in a
     */
    public static class PHP_SAPI
    {
        public static string phpLoc = "";
        public static string GATE_INTER = "CGI/1.1";

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

            return result;
        }
        

        public static HttpResponse Parse(string fname, HttpRequest request)
        {
            HttpResponse response = null;
            if (File.Exists(fname))
            {
                //200 have script, will parse
                response = new HttpResponse(200);
                setSuperGlobals(request);
            }
            else
            {
                //404 script not found
                response = new HttpResponse(404);
            }

            return response;
        }
        /// <summary>
        /// This method sets superglobals to their correct values based upon the httpRequest invoking the php file.
        /// This method should be called before parsing a php file
        /// </summary>
        /// <param name="request">the request from which to extract the datas</param>
        private static void setSuperGlobals(HttpRequest request)
        {
            //SERVER_ADDR, SERVER_NAME, SERVER_SOFTWARE, SERVER_PROTOCOL
            request.GetValue("Host");//SERVER_ADDR
            request.GetValue("Accept");
            //REQUEST_METHOD, REQUEST_TIME
            //QUERY_STRING
            //HTTP_ACCEPT, HTTP_ACCEPT_CHARSET
            //HTTP_HOST, HTTP_REFERER, HTTPS
            //REMOTE_ADDR, REMOTE_HOST, REMOTE_PORT
            //SCRIPT_FILENAME
            //SERVER_ADMIN, SERVER_PORT, SERVER_SIGNATURE
            //PATH_TRANSLATED, SCRIPT_NAME, SCRIPT_URI
            

            
        }

    }
}
