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

        public bool Send(Stream strm, FileStream fs)//TODO 1/30/2014
        {
            bool result = false;

            var toSend = "";
            Console.WriteLine("" + fs.Length);
            toSend = GetHeaders() + ReadFile(fs);

            if (ContainsHeader("Content-Length"))
            {
                DeleteHeader("Transfer-Encoding");
            }

            //sends response
            SendString(strm, toSend);


            return result;
        }

        private string ReadFile(FileStream fs)
        {
            var fileContent = "";

            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, (int)fs.Length);
            for(int i = 0; i < fs.Length; i++)
            {
                fileContent += "" + (char)buffer[i];
            }

            return fileContent;
        }

        public bool SendChunked(Stream strm, FileStream fs)
        {
            bool result = true;
            var toSend = GetHeaders();
            //checks if the transfer encoding is correct
            if (!GetValue("Transfer-Encoding").Equals("chunked"))
            {
                return false;
            }
          
            //sending can now begin
            SendString(strm, toSend);
            var ind = 0;
            var chunkSize = 100;//10 Bytes chunk size
            bool stop = false;//stop sending?

            while (fs.Length > ind && !stop)
            {
                int remaining = (int)fs.Length - ind;

                if (remaining < chunkSize)
                {
                    chunkSize = remaining;
                }

                string chunk = Convert.ToString(chunkSize, 16) + "\r\n";//hexidecimal representation of the
                byte[] chunkBuffer = new byte[chunkSize];


                fs.Read(chunkBuffer, 0, chunkSize);
                for (int i = 0; i < chunkSize; i++)
                {
                    chunk += "" + (char)chunkBuffer[i];
                    
                }
                chunk += "\r\n";
                stop = !SendString(strm, chunk);
                

                ind += chunkSize;
            }
            if (!stop)
            {
                //sends the final zero
                string chunkEnder = "0\r\n";
                SendString(strm, chunkEnder);
            }

            return result;
        }

        private bool SendString(Stream strm, string strToSend)
        {
            bool result = false;

            byte[] curSend = new byte[strToSend.Length];

            for (int i = 0; i < strToSend.Length; i++)
            {
                char curChar = strToSend[i];
                curSend[i] = (byte)curChar;
            }

            try
            {
                strm.Write(curSend, 0, curSend.Length);
                strm.Flush();
                result = true;
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
            }

            return result;
        }

        //THIS METHOD NEEDS TO BE REFRACTORED OR MARKED AS DEPRECATED!
        /*public void SendErrorMessage(Stream strm, int errorCode)
        {
            var errorMessage = "";
            var htmlBody = "<!DOCTYPE HTML><html><head><style>footer{font-size:0.95em;text-align:center;}</style><title>SWAP Error 'errorCode'</title></head><body>'errorMessage'<br />" +
                           "<hr></body><footer>SWAP auto generated page.</footer></html>";
            switch (errorCode)
            {
                case 100:
                    errorMessage = "The lighter you requested could not be found on the server. Please ask coworker to borrow theirs.";
                    break;
                default:
                    errorCode = -1;
                    errorMessage = "null";
                    break;
            }

            if (errorCode != -1)
            {
                htmlBody = htmlBody.Replace("'errorCode'", "" + errorCode);
            }
            else
            {
                htmlBody = htmlBody.Replace("'errorCode'", "0-0");
            }

            htmlBody = htmlBody.Replace("'errorMessage'", "" + errorMessage);

            SetHeader("Content-Length", "" + htmlBody.Length);
            SetHeader("Content-Type", "text/html");
            Body = htmlBody;

            Send(strm);
        }*/
        /// <summary>
        /// This method sets the proper MIME type based upon what type of file was requested
        /// </summary>
        /// <param name="fileEnding"></param>
        /// <param name="response"></param>
        public bool SetContentType(string fileEnding, string fname)
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
            else if (fileEnding.Equals("txt"))
            {
                value = "text/plain";
            }
            else
            {
                value = "application/octet-stream";
                SetHeader("content-disposition", "attachment; filename=" + fname);
                isText = false;
            }

            this.SetHeader("Content-Type", value);

            return isText;
        }

        public bool DeleteHeader(string header)
        {
            bool result = false;

            if (ContainsHeader(header))
            {
                result = true;
                headers.Remove(header);
            }

            return result;
        }


        public bool ContainsHeader(string header)
        {
            bool result = false;

            if (headers.Contains(header))
            {
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

            str += "\r\n";//extra \r\n to seperate header from body

            return str;
        }

        public override string ToString()
        {
            var str = "";

            str += GetHeaders();

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
