using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;


namespace HTTP
{
    /*
     ****************
     * HttpResponse *
     ****************
     */

    public class HttpResponse
    {
        //TODO Add a constructor that takes in a string representation of a request and converts it to an HttpResponse object
        private Hashtable headers = new Hashtable();
        private const string STATUS_LINE = "status";

        private ArrayList _body = new ArrayList();

        public Object Body
        {
            get { return _body; }
            set
            {
                _body.Add(value);
            }
        }

        public HttpResponse(int statusCode)
        {
            SetStatusCode(statusCode);
        }

        public void SetStatusCode(int statusCode)
        {
            var headerStart = "HTTP/1.1 ";
            switch (statusCode)
            {
                case 200:
                    headerStart += "200 OK";
                    break;
                case 301:
                    headerStart += "301 Moved Permanently";
                    break;
                case 400:
                    headerStart += "400 Bad Request";
                    break;
                case 403:
                    headerStart += "403 Forbidden";
                    break;
                case 404:
                    headerStart += "404 Not Found";
                    break;
                case 500:
                    headerStart += "500 Internal Server Error";
                    break;
                case 501:
                    headerStart += "501 Not Implemented";
                    break;
                case 502:
                    headerStart += "502 Bad Gateway";
                    break;
                default:
                    throw new NotImplementedException("Status code: " + statusCode + " is not supported by this implementation of HttpRequest");
            }

            SetHeader(STATUS_LINE, headerStart);
        }

        public bool Send(Stream strm)//TODO 1/30/2014
        {
            throw new NotImplementedException("TODO!");
            bool result = false;

            var toSend = GetHeaders();

            for (int i = 0; i < toSend.Length; i++)
            {
                char curChar = toSend[i];
                byte[] curSend = { (byte)curChar };

                try
                {
                    strm.Write(curSend, 0, 1);
                    strm.Flush();
                }
                catch (IOException ioe)
                {
                    Console.Error.WriteLine("!-----------------------------------------------------------------! \n" +
                                            "!  An IOException occured while trying to send a response!        ! \n" +
                                            "!  Causes:                                                        ! \n" +
                                            "!  1) The client voulntarily closed the output stream             ! \n" +
                                            "!  2) This machine has lost connection to the Internet            ! \n" +
                                            "!  3) The client lost connection to the server and invoulntarily  ! \n" +
                                            "!     closed the output stream                                    ! \n" +
                                            "!-----------------------------------------------------------------!");
                    break;//breaks the loop because trying to send anymore bytes would result in another IOException.
                }

            }

            ArrayList body = (ArrayList)Body;

            for (int i = 0; i < body.Count; i++)
            {
                //TODO SEND BODY 
            }

            return result;
        }

        /// <summary>
        /// This method sets the proper MIME type based upon what type of file was requested
        /// </summary>
        /// <param name="fileEnding"></param>
        /// <param name="response"></param>
        public bool SetContentType(string fileEnding)
        {
            var value = "";
            var isText = true;

            //if text type convert to string
            if (fileEnding.Equals("html") || fileEnding.Equals("htm") || fileEnding.Equals("stm"))
            {
                value = "text/html";
            }
            else if (fileEnding.Equals("xml") || fileEnding.Equals("css"))
            {
                value = "text/" + fileEnding;
            }
            else if (fileEnding.Equals("php"))
            {
                value = "text/html";
                //TODO php parsing goes here
            }
            else if (fileEnding.Equals("jpg") || fileEnding.Equals("jpeg") || fileEnding.Equals("jpe"))
            {
                value = "image/jpeg";
                isText = false;
            }
            else if (fileEnding.Equals("gif") || fileEnding.Equals("bmp"))
            {
                value = "image/" + fileEnding;
                isText = false;
            }
            else if (fileEnding.Equals("ico"))
            {
                value = "image/x-icon";
                isText = false;
            }
            else if (fileEnding.Equals("svg"))
            {
                value = "image/svg+xml";
                isText = false;
            }
            else if (fileEnding.Equals("mp2") || fileEnding.Equals("mpa") || fileEnding.Equals("mpe") ||
                     fileEnding.Equals("mpeg") || fileEnding.Equals("mpg") || fileEnding.Equals("mpv2"))
            {
                value = "video/mpeg";
                isText = false;
            }
            else if (fileEnding.Equals("qt"))
            {
                value = "video/quicktime";
                isText = false;
            }
            else if (fileEnding.Equals("rtx"))
            {
                value = "text/richtext";
            }
            else if (fileEnding.Equals("rtf"))
            {
                value = "application/rtf";
                isText = false;
            }
            else if (fileEnding.Equals("mp3"))
            {
                value = "audio/mpeg";
                isText = false;
            }
            else if (fileEnding.Equals("snd"))
            {
                value = "audio/basic";
                isText = false;
            }
            else if (fileEnding.Equals("pdf"))
            {
                value = "application/pdf";
                isText = false;
            }
            else if (fileEnding.Equals("pps") || fileEnding.Equals("ppt"))
            {
                value = "application/vnd.ms-powerpoint";
                isText = false;
            }
            else if (fileEnding.Equals("swf"))
            {
                value = "application/x-shockwave-flash";
                isText = false;
            }
            else if (fileEnding.Equals("js"))
            {
                value = "application/x-javascript";
                isText = false;
            }
            else
            {
                value = "application/octet-stream";
                isText = false;
            }

            this.SetHeader("Content-Type", value);

            return isText;
        }


        public bool ContainsHeader(string header)
        {
            bool result = false;
            
            if (headers.Contains(header)){
                result = true;
            }
            
            return result;
        }
        public void SetHeader(string header, string value)
        {
            if (!headers.Contains(header)) 
            {
                headers.Add(header, value);
            }
            else
            {
                headers[header] = value;
            }
            
        }

        public string GetValue(string header)
        {
            var value = "";

            var hasHeader = false;

            foreach (string key in headers.Keys)
            {
                if (header.ToLower().Trim().Equals(key.ToLower().Trim()))
                {
                    hasHeader = true;
                    break;
                }
            }

            if (hasHeader)
            {
                value = (string)headers[header];
            }

            return value;
        }

        public string GetHeaders()
        {
            var str = "";

            str += headers[STATUS_LINE] + "\r\n";

            int count = 0;
            foreach (string headerName in headers.Keys)
            {
                //skips method line
                if (!headerName.Equals(STATUS_LINE))
                {
                    str += headerName.Trim() + ": " + count + " \r\n";
                }
                count++;
            }

            count = 0;

            foreach (string headerContent in headers.Values)
            {
                //skips method line
                if (!headerContent.Equals(STATUS_LINE))
                {
                    str = str.Replace(" " + count + " ", " " + headerContent.Trim());
                }

                count++;
            }

            return str;
        }

        private string GetBody()
        {
            var str = "";
            
            foreach (object bodyItem in _body)//TODO this needs to properly add collections and non-collection items to the body
            {
                if (!_body.GetType().IsAssignableFrom(new ArrayList().GetType()) &&
                    !_body.GetType().IsAssignableFrom(new Hashtable().GetType()))
                {
                    str += bodyItem.ToString();
                }
                else
                {
                    foreach (object item in _body)
                    {
                        str += item.ToString();
                    }
                }

            }
            return str;
        }
        public override string ToString()
        {
            var str = "";

            str += GetHeaders();
            //header-body seperation
            str += "\r\n";
            str += GetBody();

            return str;
        }
    }
    /*
     ***************
     * HttpRequest *
     ***************
     */

    public class HttpRequest
    {
        //TODO Add a constructor that takes in a string representation of a request and converts it to an HttpRequest object
        private Hashtable headers = new Hashtable();
        private const string MethodKey = "method";
        private ArrayList _body = new ArrayList();

        public ArrayList Body
        {
            get { return _body; }
            set
            {
                var methodHeader = ((string)headers[MethodKey]);

                if (methodHeader.Contains("POST"))
                {
                    foreach (object con in value)
                    {
                        _body.Add(con);
                    }
                }
                else
                {
                    throw new NotSupportedException("You can put a body in a non-POST request method in this implementation");
                }
            }
        }

        public enum Method { Get, Head, Post, Options };


        public HttpRequest(Method meth, string resource)
        {
            SetRequestMethod(meth, resource);
        }

        public HttpRequest(Method meth, string resource, ArrayList bodyItems)
            : this(meth, resource)
        {
            Body = bodyItems;
        }

        public void SetRequestMethod(Method meth, string resource)
        {
            var methodHeader = "";

            switch (meth)
            {
                case Method.Get:
                    methodHeader += "GET";
                    break;
                case Method.Head:
                    methodHeader += "HEAD";
                    break;
                case Method.Post:
                    methodHeader += "POST";
                    break;
                case Method.Options:
                    methodHeader += "OPTIONS";
                    break;
                default:
                    //should be impossible to get here
                    throw new Exception("Congraulations, you managed to pass this method an invalid HttpRequest.Method enum type.");
            }

            methodHeader += " " + resource + " HTTP/1.1";

            AddHeader(MethodKey, methodHeader);
        }
        public void AddHeader(string header, string value)
        {
            headers.Add(header, value);
        }
        /// <summary>
        /// Attempts to get a specified header, if the header
        /// is in the found in this requests header it returns
        /// its value represented by a string. if the header is
        /// not in the table null is returned.
        /// </summary>
        /// <param name="header">the header of which to get a vakue for</param>
        /// <returns>the value of the specifed header or null(if the header isn't in this HttpResponse</returns>
        public String GetValue(string header)
        {
            String value = null;

            if (headers.Contains(header))
            {
                value = (String)headers[header];
            }
            return value;
        }

        public override string ToString()
        {
            string str = "";

            str += headers[MethodKey] + "\r\n";

            int count = 0;
            foreach (string headerName in headers.Keys)
            {
                //skips method line
                if (!headerName.Equals(MethodKey))
                {
                    str += headerName.Trim() + ": " + count + " \r\n";
                }
                count++;
            }

            count = 0;

            foreach (string headerContent in headers.Values)
            {
                //skips method line
                if (!headerContent.Equals(MethodKey))
                {
                    str = str.Replace(" " + count + " ", " " + headerContent.Trim());
                }

                count++;
            }

            //header-body seperation
            str += "\r\n";

            for (int i = 0; i < _body.Count; i++)
            {
                str += _body[i] + "\r\n";
            }


            return str;
        }
    }
}
