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

    class HttpResponse
    {
        //TODO Add a constructor that takes in a string representation of a request and converts it to an HttpResponse object
        private Hashtable headers = new Hashtable();
        private const string StatusLine = "status";

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

            AddHeader(StatusLine, headerStart);
        }

        public void AddHeader(string header, string value)
        {
            headers.Add(header, value);
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

        public string GetHeader()
        {
            var str = "";

            str += headers[StatusLine] + "\r\n";

            int count = 0;
            foreach (string headerName in headers.Keys)
            {
                //skips method line
                if (!headerName.Equals(StatusLine))
                {
                    str += headerName.Trim() + ": " + count + " \r\n";
                }
                count++;
            }

            count = 0;

            foreach (string headerContent in headers.Values)
            {
                //skips method line
                if (!headerContent.Equals(StatusLine))
                {
                    str = str.Replace(" " + count + " ", " " + headerContent.Trim());
                }

                count++;
            }

            return str;
        }

        public string GetBody()
        {
            var str = "";

            for (int i = 0; i < _body.Count; i++)//TODO wtf is a LINQ-expression?!?!
            {
                str += _body[i] + "\r\n";
            }

            return str;
        }
        public override string ToString()
        {
            var str = "";

            str += GetHeader();
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

    class HttpRequest
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
