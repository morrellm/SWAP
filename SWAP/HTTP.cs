using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Net.Sockets;
using System.Web;
using System.Text.RegularExpressions;


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
        private string CRLF = SimpleHttpServer.Program.CRLF;

        private int _chunkSize = 5243000;//default chunk size of 5MB

        public int ChunkSize
        {
            get
            {
                return _chunkSize;
            }
            set
            {
                _chunkSize = value;
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
                case 206:
                    headerStart += "206 Partial Content";
                    break;
                case 301:
                    headerStart += "301 Moved Permanently";
                    break;
                case 400:
                    headerStart += "400 Bad Request";
                    break;
                case 401:
                    headerStart += "401 Unauthorized";
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

        public bool Send(ref Stream strm, ref FileStream fs)
        {
            bool result = false;

            var toSend = "";
           // Console.WriteLine("" + fs.Length);
            toSend = GetHeaders();

            //sends response
            SendString(ref strm, ref toSend);
            SendFile(ref strm, ref fs);


            return result;
        }

        public bool Send(ref Stream strm, ref String body)
        {
            bool result = false;

            var headers = GetHeaders();

            //sends response
            SendString(ref strm, ref headers);
            SendString(ref strm, ref body);


            return result;
        }

        private bool SendFile(ref Stream strm, ref FileStream fs)
        {
            bool result = false;
            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, (int)fs.Length);
            try
            {
                strm.Write(buffer, 0, (int)buffer.Length);
                result = true;
            }
            catch (IOException ioe)
            {
               //lost connect to user, or internet.               
            }
            fs.Close();

            return result;
        }

        public bool SendChunked(ref Stream strm, ref FileStream fs)
        {
            bool result = true;
            var toSend = GetHeaders();
            //checks if the transfer encoding is correct
            if (!GetValue("Transfer-Encoding").Equals("chunked"))
            {
                return false;
            }
          
            //sending can now begin
            SendString(ref strm, ref toSend);
            var ind = 0;
            var chunkSize = _chunkSize;//predefined chunk size
            bool stop = false;//stop sending?

            while (!stop && fs.Length > ind)
            {
                int remaining = (int)fs.Length - ind;

                if (remaining < chunkSize)
                {
                    chunkSize = remaining;
                }

                string chunk = Convert.ToString(chunkSize, 16) + CRLF;//hexidecimal representation of the
                byte[] chunkBuffer = new byte[chunkSize];


                fs.Read(chunkBuffer, 0, chunkSize);
                bool tempStop3 = !SendString(ref strm, ref chunk);
                bool tempStop = !SendBytes(ref strm, ref chunkBuffer);
                bool tempStop2 = !SendString(ref strm, ref CRLF);
                if (tempStop || tempStop2 || tempStop3)
                {
                    stop = true;
                    fs.Close();
                }
                
                ind += chunkSize;
            }
            if (!stop)
            {
                //sends the final zero
                string chunkEnder = "0" + CRLF;
                SendString(ref strm, ref chunkEnder);
            }

            return result;
        }
        private bool SendBytes(ref Stream strm, ref byte[] bytesToSend)
        {
            bool result = false;

            try
            {
                strm.Write(bytesToSend, 0, bytesToSend.Length);
                strm.Flush();
                result = true;
            }
            catch (IOException ioe)
            {
                //lost connect to user, or internet.  
            }

            return result;
        }
        private bool SendString(ref Stream strm, ref string strToSend)
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
                //lost connect to user, or internet.  
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
        public string SetContentType(string fileEnding, string fname)
        {
            var value = "";

            //if text type convert to string
            if (fileEnding.Equals("html") || fileEnding.Equals("htm") || fileEnding.Equals("stm"))
            {
                value = "text/html; charset=utf-8";
            }
            else if (fileEnding.Equals("xml") || fileEnding.Equals("css"))
            {
                value = "text/" + fileEnding + "; charset=utf-8";
            }
            else if (fileEnding.Equals("jpg") || fileEnding.Equals("jpeg") || fileEnding.Equals("jpe"))
            {
                value = "image/jpeg";

            }
            else if (fileEnding.Equals("gif") || fileEnding.Equals("bmp"))
            {
                value = "image/" + fileEnding;

            }
            else if (fileEnding.Equals("ico"))
            {
                value = "image/x-icon";
                
            }
            else if (fileEnding.Equals("svg"))
            {
                value = "image/svg+xml";
                
            }
            else if (fileEnding.Equals("mp2") || fileEnding.Equals("mpa") || fileEnding.Equals("mpe") ||
                     fileEnding.Equals("mpeg") || fileEnding.Equals("mpg") || fileEnding.Equals("mpv2"))
            {
                value = "video/mpeg";
                
            }
            else if (fileEnding.Equals("mp4")){
                value = "video/mp4";
                
            }
            else if (fileEnding.Equals("qt"))
            {
                value = "video/quicktime";
                
            }
            else if (fileEnding.Equals("rtx"))
            {
                value = "text/richtext; charset=utf-8";
            }
            else if (fileEnding.Equals("rtf"))
            {
                value = "application/rtf";
                
            }
            else if (fileEnding.Equals("mp3"))
            {
                value = "audio/mpeg";
                
            }
            else if (fileEnding.Equals("snd"))
            {
                value = "audio/basic";
                
            }
            else if (fileEnding.Equals("pdf"))
            {
                value = "application/pdf";
                
            }
            else if (fileEnding.Equals("pps") || fileEnding.Equals("ppt"))
            {
                value = "application/vnd.ms-powerpoint";
                
            }
            else if (fileEnding.Equals("swf"))
            {
                value = "application/x-shockwave-flash";
                
            }
            else if (fileEnding.Equals("js"))
            {
                value = "application/x-javascript";
                
            }
            else if (fileEnding.Equals("txt"))
            {
                value = "text/plain; charset=utf-8";
            }
            else if (fileEnding.Equals("zip"))
            {
                value = "application/zip";
                
            }
            else
            {
                value = "application/octet-stream";
                SetHeader("content-disposition", "attachment; filename=" + fname);
                
            }

            this.SetHeader("Content-Type", value);

            return value;
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
        public string GetStatusHeader()
        {
            string ret = GetHeader(STATUS_LINE);
            ret = ret.Substring(ret.IndexOf(':') + 1).Trim();
            return ret;
        }
        public string GetHeader(string header)
        {
            string ret = null;
            if (ContainsHeader(header))
            {
                ret += header + ": " + headers[header];
            }
            return ret;
        }

        public string GetHeaders()
        {
            var str = "";

            str += headers[STATUS_LINE] + " " +CRLF;

            int count = 0;
            foreach (string headerName in headers.Keys)
            {
                //skips method line
                if (!headerName.Equals(STATUS_LINE))
                {
                    str += headerName.Trim() + ": " + count + " " + CRLF;
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

            str += CRLF;//extra line ender to seperate header from body

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
        private Hashtable headers = new Hashtable();
        private const string MethodKey = "method";
        private Method _requestMethod = Method.NULL;
        private String _resource = "/";
        private StreamReader _sr = null;
        private string CRLF = SimpleHttpServer.Program.CRLF;

        public String Resource
        {
            get
            {
                return _resource;
            }
            set
            {
                //add error checking
                _resource = HttpUtility.UrlDecode(value);
            }
        }
        

        public Method RequestMethod
        {
            get { return _requestMethod; }
        }

        private Hashtable _body = new Hashtable();
        public Hashtable Body
        {
            get { return _body; }
        }

        
        private TcpClient _connection = null;
        public TcpClient Connection
        {
            get
            {
                return _connection;
            }
        }

        private Hashtable _query = new Hashtable();
        public Hashtable Query
        {
            get { return _query; }
        }
  
        public enum Method { GET, HEAD, POST, OPTIONS, NULL };

        public String MethodToString()
        {
            Method meth = _requestMethod;
            String str = null;
            switch (meth)
            {
                case Method.GET:
                    str = "GET";
                    break;
                case Method.HEAD:
                    str = "HEAD";
                    break;
                case Method.POST:
                    str = "POST";
                    break;
                case Method.OPTIONS:
                    str = "OPTIONS";
                    break;
                case Method.NULL:
                default:
                    break;
            }
            return str;
        }

        public HttpRequest(TcpClient connection)
        {
            _connection = connection;
            _sr = new StreamReader(connection.GetStream());
            string header = ReadRequest();
            parseRequest(header);
        }
        
        private void parseRequest(string strReq)
        {
            var headerStr = strReq;
            string body = "";

            //splits the raw header string into a string array of header lines
            var headers = headerStr.Split(new string[] {CRLF}, StringSplitOptions.RemoveEmptyEntries);

            //first header is special (no ':')
            //does some special parsing of the first line
            string[] methodLine = null;//starts first line as null
            //if there is at least one header line
            if (headers.Length != 0)
            {
                methodLine = headers[0].Split(' ');//divide it by spaces
            }
               
            //if the method line is close to proper format parsing is attempted
            if (methodLine != null && methodLine.Length == 3)
            {
                _requestMethod = StringToMethod(methodLine[0]);
                _resource = methodLine[1];
            }
            else
            {
                _requestMethod = Method.NULL;//this is an indication of an invalid method
                _resource = "/";
            }

            //this loop will not add first header
            foreach (string header in headers)
            {
                string[] token = header.Split(new char[] {':'}, 2);
                if (token.Length == 2)
                {
                    AddHeader(token[0].Trim(), token[1].Trim());
                }
            }

                
            

            //checks for a query string
            if (_resource.Contains("?"))
            {
                string[] query;
                int startQuery = _resource.IndexOf("?") + 1;
                int endQuery = _resource.IndexOf("#");//accounts for fragments

                if (endQuery != -1)
                {
                    query = _resource.Substring(startQuery, endQuery - startQuery).Split('&');
                }
                else
                {
                    query = _resource.Substring(startQuery).Split('&');
                }


                //removes query and stores it to the request
                _resource = _resource.Substring(0, startQuery - 1);
                SetQuery(query, ref _query);
            }

            //handles content body here
            if (RequestMethod == Method.POST)
            {
                //if content type includes a ';' this code gets just the type and temporarily ignore any extra
                var contentType = GetValue("Content-Type").ToLower();
                contentType = contentType.Substring(0, contentType.IndexOf(";"));
               
                //this decides how to read/parse the content
                if (contentType.Equals("application/x-www-form-urlencoded"))
                {
                    //read the body (simple format)
                    int length = Int32.Parse(GetValue("Content-Length"));
                    for (int i = 0; i < length; i++)
                    {
                        if (_sr.EndOfStream)
                            break;
                        char cur = (char)_sr.Read();
                        body += "" + cur;
        

                    }

                    string[] parameters = body.Split('&');
                    if (parameters.Length > 0)
                    {
                        SetQuery(parameters, ref _body);
                    }

                }
                else if (contentType.Equals("multipart/form-data"))
                {
                    //other file encoding (not yet supported)
                    //TODO
                    var parts = MultipartParser.Parse(_sr, this);
                    foreach (var part in parts)
                        Console.WriteLine("Disposition: " + part.GetValue("Content-Disposition"));

                }
         
            }
        }
        /// <summary>
        /// Reads (presummed) request from this HttpRequest's TcpClient's stream
        /// </summary>
        /// <returns>A string array containing the header(index 0) and the body(index 1 if present)</returns>
        private string ReadRequest()
        {

            var header = new StringBuilder();


            //reads header
            while (!header.ToString().Contains(CRLF + CRLF))
            {
                char chur = (char)_sr.Read();
                header.Append(chur);
            }
  
            return header.ToString();
        }

        public bool IsValid()
        {
            //valid if the request method != null
            return (_requestMethod != Method.NULL);
        }

        public void Dispose()
        {
            if (_connection.Connected) //makes sure the stream hasn't been closed before attempting to close it
            { 
                _connection.GetStream().Close();
                _connection.Close();
            }
        }

        public void SetQuery(string[] query, ref Hashtable toSet)
        {
            for (int i = 0; i < query.Length; i++)
            {
                var tokens = query[i].Split('=');
                try
                {
                    toSet.Add(tokens[0], tokens[1]);
                }
                catch (IndexOutOfRangeException ioore)
                {
                    toSet.Add(tokens[0], "");
                }

            }
        }

        public void AddHeader(string header, string value)
        {
            headers.Add(header, value);
        }

        public Method StringToMethod(string meth)
        {
            Method method = Method.NULL;

            if (meth.ToLower().Equals("get"))
            {
                method = Method.GET;
            }
            else if (meth.ToLower().Equals("head"))
            {
                method = Method.HEAD;
            }
            else if (meth.ToLower().Equals("options"))
            {
                method = Method.OPTIONS;
            }
            else if (meth.ToLower().Equals("post"))
            {
                method = Method.POST;
            }

            return method;
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

        public string GetQueryString()
        {
            var query = "";

            int i = 0;
            try
            {
                foreach(string key in _query.Keys)
                {
                    query += key+"="+_query[key];
                    if (i != _query.Keys.Count - 1)
                    {
                        query += "&";
                    }
                    i++;
                }
            }
            catch (NullReferenceException)
            {
                //thrown if no queries are in the hashtable
            }

            return query;
        }

        public override string ToString()
        {
            string str = "";

            var mod = _resource;
            int queryIns = mod.LastIndexOf(" ");
            var query = GetQueryString();

            if (query != null && !query.Equals("")) 
            {
                mod = mod.Insert(queryIns, "?" + GetQueryString() + " ");
            }            

            str += _requestMethod + " " + mod + " HTTP/1.1" + CRLF;

            int count = 0;
            foreach (string headerName in headers.Keys)
            {
                //skips method line
                if (!headerName.Equals(MethodKey))
                {
                    str += headerName.Trim() + ": " + count + " " + CRLF;
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
            str += CRLF;

            var len = Body.Keys.Count;
            int ind = 0;
            foreach (var key in Body.Keys)
            {
                str += key + "=" + Body[key];
                ind++;
                if (ind != len)
                {
                    str += "&";
                }
            }
            //ending double CRLF
            str += CRLF + CRLF;

            return str;
        }
    }

    public static class MultipartParser
    {
        public static List<Multipart> Parse(StreamReader sr, HttpRequest request)
        {
            string CRLF = SimpleHttpServer.Program.CRLF;
            List<Multipart> data = new List<Multipart>(5);
            //gets the boundary delimiter
            var boundary = request.GetValue("Content-Type").ToLower();
            boundary = boundary.Substring(boundary.IndexOf(";"));
            boundary = boundary.Substring(boundary.IndexOf("=") + 1).Trim();
            //Console.WriteLine("Boundary = '" + boundary + "'");

            //gets the content length
            var lengthStr = request.GetValue("Content-Length");
            int length = Int32.Parse(lengthStr);
            //Console.WriteLine("Length: " + length);

            //ind is the current index in the multipart response body
            int ind = 0;
            //this is the main loop that reads each part
            //TODO finish
            while (ind < length)
            {
                var curPart = "";
                //this will read the header of the current part
                while(!curPart.Contains(CRLF + CRLF))
                {

                    curPart += "" + (char) sr.Read();
                    ind++;//marks that a char was read
                }

 

                //splits up the current part header
                var lines = curPart.Split(new []{CRLF}, StringSplitOptions.RemoveEmptyEntries);
                //this parses the header and creates a Multipart object
                Multipart mp = new Multipart();

                foreach(var line in lines)
                {
                    if (!line.Contains(boundary))
                    {
                        var tokens = line.Split(new []{':'}, 2);
                        mp.AddAttribute(tokens[0].Trim(),
                                        tokens[1].Trim());
                    }
                }
                //Console.WriteLine("Multipart Header:\n" + mp.ToString());
                //get rid of header
                curPart = "";

                //now read body
                while(!curPart.Contains("--" + boundary + "--" + CRLF) &&
                      !curPart.Contains("--" + boundary + CRLF)){

                    curPart += "" + (char)sr.Read();
                    ind++;//marks that is read a char
                    Console.WriteLine(ind + "/" + length);
                }

               // Console.WriteLine("Multipart Body:\n" + curPart.ToString());
                //gets rid of extra boundarys and CRLFs
                var content = curPart;
                content = content.Replace("--" + boundary + "--" + CRLF, "");//if it is the last part
                content = content.Replace("--" + boundary + CRLF, "");//if it isn't the last part

                //sets multipart's content
                mp.Content = content.Trim();

               // Console.WriteLine("Non-parsed: " + curPart.ToString() + CRLF + boundary);
                //Console.WriteLine("Parsed: \"" + content + "\"");
                if (curPart.Contains("--" + boundary + "--"))//if this is the last section
                {
                    //end it
                    //sr.ReadToEnd();
                    ind = length;
                }

               // Console.WriteLine(data.Count + mp.ToString());
                data.Add(mp);
            }
            
            return data;
        }
    }
    public class Multipart
    {
        string CRLF = SimpleHttpServer.Program.CRLF;
        private object _content = "";
        public object Content
        {
            set { _content = value; }
            get { return _content; }
        }
        private Hashtable _attributes = new Hashtable();
        //attribute modifiers***
        //this returns multipart to allow method chaining
        public Multipart AddAttribute(string name, string value){
            _attributes.Add(name, value);
            return this;
        }
        public string GetValue(string name)
        {
            return _attributes.Contains(name) ? (string)_attributes[name] : null;
        }
        //to string
        public override string ToString()
        {
            var str = new StringBuilder();

            foreach(var key in _attributes.Keys)
                str.Append(key + ": " + _attributes[key] + CRLF);

            str.Append(CRLF + "'" + _content + "'");

            return str.ToString();
        }
    }
}
