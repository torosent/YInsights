using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace YInsights.Web.Extentions
{

    public class XmlResult : ActionResult
    {
        public XDocument Xml { get; private set; }
        public string ContentType { get; set; }
        //public Encoding Encoding { get; set; }

        public XmlResult(XDocument xml)
        {
            this.Xml = xml;
            this.ContentType = "text/xml";
        }


        public override Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.ContentType = this.ContentType;

            if (Xml != null)
            {
                Xml.Save(context.HttpContext.Response.Body, SaveOptions.DisableFormatting);
                return Task.FromResult(0);

            }
            else
            {
                return base.ExecuteResultAsync(context);
            }
        }
    }

    //public class Utf8StringWriter : StringWriter
    //{
    //    public override Encoding Encoding { get { return Encoding.UTF8; } }
    //}

}

