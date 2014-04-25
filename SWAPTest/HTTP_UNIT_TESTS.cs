using System;
using SimpleHttpServer;
using PHP_PARSER;
using HTTP;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SWAPTest
{
    [TestClass]
    public class HTTP_UNIT_TESTS
    {
        [TestMethod]
        public void HttpResponseTestStatusCode()
        {
            HttpResponse rep;
            try
            {
                rep = new HttpResponse(0);
                Assert.Fail();
            }
            catch (NotImplementedException nie) { }//sucess

            rep = new HttpResponse(200);
            Assert.AreEqual(rep.GetStatusHeader(), "HTTP/1.1 200 OK");

            rep = new HttpResponse(206);
            Assert.AreEqual(rep.GetStatusHeader(), "HTTP/1.1 206 Partial Content");

            rep = new HttpResponse(301);
            Assert.AreEqual(rep.GetStatusHeader(), "HTTP/1.1 301 Moved Permanently");

            rep = new HttpResponse(400);
            Assert.AreEqual(rep.GetStatusHeader(), "HTTP/1.1 400 Bad Request");

            rep = new HttpResponse(403);
            Assert.AreEqual(rep.GetStatusHeader(), "HTTP/1.1 403 Forbidden");

            rep = new HttpResponse(404);
            Assert.AreEqual(rep.GetStatusHeader(), "HTTP/1.1 404 Not Found");

            rep = new HttpResponse(500);
            Assert.AreEqual(rep.GetStatusHeader(), "HTTP/1.1 500 Internal Server Error");

            rep = new HttpResponse(501);
            Assert.AreEqual(rep.GetStatusHeader(), "HTTP/1.1 501 Not Implemented");

            rep = new HttpResponse(502);
            Assert.AreEqual(rep.GetStatusHeader(), "HTTP/1.1 502 Bad Gateway");
           
        }

        [TestMethod]
        public void HttpResponseTestSetHeader()
        {

        }
    }
}
