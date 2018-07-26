using System;
using System.Collections.Generic;
using org.pdfclown;
using statementReader.Contracts;

namespace statementReader.Business
{
    public class Reader
    {
        private string docPath;
        private string account;
        public Reader(string pDocPath, string pAccount)
        {
            docPath = pDocPath;
            account = pAccount;
        }

        public List<Transaction> DoWork()
        {
            var file = new org.pdfclown.files.File(docPath);
            var page11 = file.Document.Pages.Count >= 11 
                             ? file.Document.Pages[11]
                             : null;
            var data = page11.Contents.ContentContext.Contents[page11.Contents.ContentContext.Contents.Count - 1];
            var data2 = page11.Contents[page11.Contents.Count - 1];
            //org.pdfclown.objects.
            //GraphicsObject, ContentOjbect, OperationsObject("Show Text" || "Tj")
            //var textExtractor = new org.pdfclown.tools.TextExtractor();

            throw new NotImplementedException("Still need to implement Reader!");
        }
    }
}
