using System;
using System.Linq;
using System.Collections.Generic;
using statementReader.Contracts;
using org.pdfclown.files;
using org.pdfclown.tools;
using org.pdfclown.documents.contents;
using org.pdfclown.documents;
using System.Drawing;

namespace statementReader.Business
{
    public class StatementExtractor
    {
        /**
          <summary>This sample demonstrates how to retrieve text content along with its graphic attributes
          (font, font size, text color, text rendering mode, text bounding box, and so on) from a PDF document;
          text is automatically sorted and aggregated.</summary>
        */
        private string account = "9610";
        private string flag = "Date Description Check No.Additions Subtractions Balance";
        private string flag1 = "Date Description Check";
        private string flag2 = "Date\tDescription";
        private string begining = "Beginning balance on ";
        private string eof = "Ending balance on ";

        public List<Transaction> Extract(Document document)
        {
            var extractor = new TextExtractor();
            var inProgress = false;
            var transactions = new List<Transaction>();
            var pgCnt = 0;
            var year = 1977;

            foreach (var page in document.Pages)
            {
                pgCnt++;
                IList<ITextString> pageTextStrings = new List<ITextString>();
                try
                {
                    pageTextStrings = extractor.Extract(page)[TextExtractor.DefaultArea];
                }
                catch
                {
                    continue;
                }
                var headerSplit = pageTextStrings[0].Text.Split(' ').ToList();
                var dashIdx = headerSplit.IndexOf("-");
                if(dashIdx > -1)
                {
                    int.TryParse(headerSplit[dashIdx - 1], out year);
                }
                var accountPage = pageTextStrings.Any(x => x.Text.Contains(account));
                var hasBalances = pageTextStrings.Any(x => x.Text.Contains(begining));

                inProgress = (!inProgress && accountPage && hasBalances) || inProgress;

                if(!inProgress)
                {
                    continue;
                }

                var hasEof = pageTextStrings.Any(y => y.Text.Contains(eof));
                inProgress = !hasEof;

                var startIdx = -1;
                var contText = pageTextStrings.FirstOrDefault(y => y.Text.Contains(flag1));
                if (hasBalances)
                {
                    startIdx = pageTextStrings.IndexOf(pageTextStrings.First(y => y.Text.Contains(begining))) + 1;
                }
                else if (contText != null)
                {
                    startIdx = pageTextStrings.IndexOf(contText) +1;
                }
                else
                {
                    throw new Exception("Could not determine where to start!");
                }

                Transaction transactionNowAndPrevious = null;
                for (var i = startIdx; i < pageTextStrings.Count; i++)
                {
                    var textString = pageTextStrings[i];
                    var textParts = textString.Text.Trim().Split(' ');
                    long eofTest;
                    if (textParts.Length == 1 && !string.IsNullOrEmpty(textParts[0]) && long.TryParse(textParts[0], out eofTest))
                    {
                        // end of page
                        continue;
                    }
                    if(textString.Text.Contains(eof))
                    {
                        break;
                    }

                    decimal amnt = 0;
                    decimal balance = 0;
                    DateTime dateTest = DateTime.MinValue;
                    if (textParts.Length > 1 && textParts[textParts.Length - 2].Contains(".") && decimal.TryParse(textParts[textParts.Length - 2], out amnt) && decimal.TryParse(textParts[textParts.Length - 1], out balance))
                    {
                        if (textParts[0].Contains("/"))
                        {
                            var dateString = $"{textParts[0]}/{year}";
                            DateTime.TryParse(dateString, out dateTest);
                        }

                       var desc = textParts.ToList();
                        desc.RemoveAt(textParts.Length - 1);
                        desc.RemoveAt(textParts.Length - 2);
                        desc.RemoveAt(0);
                        desc = desc.Select(x => x.Trim().Replace(',', '|')).ToList();

                        var tstChrs = textString.TextChars.Where(y => y.Virtual == true).ToList();
                        var tst = tstChrs.Count > 2 ? tstChrs[tstChrs.Count - 3] : tstChrs[0];
                        var typ = tst.Box.Right < 433 ? TransactionType.Deposit : TransactionType.Withdrawal;

                        int cardTst = -1;
                        int? card = null;
                        if ((desc.Count > 1 && desc[desc.Count - 2].ToLower() == "card" || desc[desc.Count - 1].Trim().Length == 4) && int.TryParse(desc[desc.Count - 1], out cardTst))
                        {
                            card = cardTst;
                        }

                        transactionNowAndPrevious = new Transaction
                        {
                            Id = Guid.NewGuid(),
                            Amount = typ == TransactionType.Deposit ? amnt : amnt*-1,
                            Date = dateTest,
                            Description = string.Join(" ", desc),
                            Type = typ,
                            Card = card
                        };

                    }
                    else if (textParts.Length > 0 && decimal.TryParse(textParts[textParts.Length - 1], out amnt) && DateTime.TryParse($"{textParts[0]}/{year}", out dateTest))
                    {
                        // No balance
                        var desc = textParts.ToList();
                        desc.RemoveAt(textParts.Length - 1);
                        desc.RemoveAt(0);
                        desc = desc.Select(x => x.Trim().Replace(',', '|')).ToList();

                        int cardTst = -1;
                        int? card = null;
                        if ((desc.Count > 1 && desc[desc.Count - 2].ToLower() == "card" || desc[desc.Count-1].Trim().Length == 4)&& int.TryParse(desc[desc.Count - 1], out cardTst))
                        {
                            card = cardTst;
                        }

                        var tstChrs = textString.TextChars.Where(y => y.Virtual == true).ToList();
                        var tst = tstChrs.Count > 1 ? tstChrs[tstChrs.Count - 2] : tstChrs[0];
                        var typ = tst.Box.Right < 433 ? TransactionType.Deposit : TransactionType.Withdrawal;

                        transactionNowAndPrevious = new Transaction
                        {
                            Id = Guid.NewGuid(),
                            Amount = typ == TransactionType.Deposit ? amnt : amnt * -1,
                            Date = dateTest,
                            Description = string.Join(" ", desc),
                            Type = typ,
                            Card = card
                        };
                    }
                    else
                    {
                        int cardTst = -1;
                        int? card = null;
                        var desc = textString.Text.Trim().Split(' ').ToList();
                        if ((desc.Count > 1 && desc[desc.Count - 2].ToLower() == "card" || desc[desc.Count-1].Trim().Length == 4) && int.TryParse(desc[desc.Count - 1], out cardTst))
                        {
                            card = cardTst;
                        }

                        transactionNowAndPrevious = transactionNowAndPrevious ?? new Transaction();
                        transactionNowAndPrevious.Card = card;
                        transactionNowAndPrevious.Description += $" {textString.Text}";
                        transactions.Add(transactionNowAndPrevious);
                    }
                }
             }
            return transactions;
        }

        public List<Transaction> Run(string pAccount, string pFilePath)
        {
            account = pAccount;
            filePath = pFilePath;
            List<Transaction> retVal;
            using (File file = new File(filePath))
            {
                var document = file.Document;

                retVal = Extract(document);
            }
            return retVal;
        }

        public List<Transaction> RunOld(int startPage, int endPage, bool usePageNum = false)
        {
            if (endPage <= startPage)
            {
                throw new Exception("End page <= Start page.");
            }

            var startIdx = startPage - 1;
            var cnt = endPage - startPage;
            var textStrings = new List<string>();
            var transactions = new List<Transaction>();

            // 1. Opening the PDF file...
            using (File file = new File(filePath))
            {
                var document = file.Document;

                //if (usePageNum)
                //{
                //if (endPage >= document.Pages.Count)
                //{
                //   throw new Exception("Endpage > total pages.");
                // }
                //return UsePageNum(startIdx, cnt, document);
                // }
                // 2. Text extraction from the document pages.
                var extractor = new TextExtractor();
                var foundAccount = false;
                var start = false;
                var pgCnt = 0;
                var lastAccntPage = -1;
                foreach (var page in document.Pages)
                {
                    pgCnt++;
                    //if (!PromptNextPage(page, false))
                    //{
                    //  break;
                    //}
                    IList<ITextString> pageTextStrings = new List<ITextString>();
                    try
                    {
                        pageTextStrings = extractor.Extract(page)[TextExtractor.DefaultArea];
                    }
                    catch
                    {
                        continue;
                    }
                    if (pageTextStrings == null || pageTextStrings.Count == 0)
                    {
                        continue;
                    }

                    if (lastAccntPage == -1 && !pageTextStrings.Any(x => x.Text.Contains(account)))
                    {
                        continue;
                    }
                    if (lastAccntPage > -1 && pgCnt - 1 != lastAccntPage)
                    {

                        continue;
                    }
                    lastAccntPage = pgCnt;
                    Transaction transactionNowAndPrevious = null;
                    foreach (ITextString textString in pageTextStrings)
                    {
                        if (textString.Text.Contains(account))
                        {
                            foundAccount = true;
                            continue;
                        }
                        if (textString.Text.Contains(eof))
                        {
                            foundAccount = false;
                            break;
                        }
                        if (!foundAccount)
                        {
                            continue;
                        }

                        if (textString.Text.Contains(flag))
                        {
                            start = true;
                            continue;
                        }
                        if (textString.Text.Contains(flag1))
                        {
                            start = true;
                            continue;
                        }
                        if (textString.Text.Contains(flag2))
                        {
                            start = true;
                            continue;
                        }
                        if (start && textString.Text.Contains("Beginning"))
                        {
                            continue;
                        }

                        var line = textString.Text.Trim().Split(' ');
                        long test;
                        if (line.Length == 1 && !string.IsNullOrEmpty(line[0]) && long.TryParse(line[0], out test))
                        {
                            start = false;
                        }
                        if (!start)
                        {
                            continue;
                        }



                        decimal amnt = 0;
                        decimal balance = 0;
                        var dateTest = DateTime.MinValue;
                        if (line.Length > 1 && decimal.TryParse(line[line.Length - 2], out amnt) && decimal.TryParse(line[line.Length - 1], out balance))
                        {
                            var desc = line.ToList();
                            DateTime.TryParse(line[0], out dateTest);
                            desc.RemoveAt(line.Length - 1);
                            desc.RemoveAt(line.Length - 2);
                            desc.RemoveAt(0);
                            desc = desc.Select(x => x.Trim().Replace(',', '|')).ToList();

                            transactionNowAndPrevious = new Transaction
                            {
                                Amount = amnt,
                                Date = dateTest,
                                Description = string.Join(" ", desc),
                                Misc = textString.Box.ToString(),
                                Type = textString.TextChars.First(x => x.Virtual == true).Box.Width < 120
                                                 ? TransactionType.Deposit
                                                 : TransactionType.Withdrawal
                            };

                        }
                        else if (line.Length > 0 && decimal.TryParse(line[line.Length - 1], out amnt) && DateTime.TryParse(line[0], out dateTest))
                        {
                            // No balance
                            var desc = line.ToList();
                            desc.RemoveAt(line.Length - 1);
                            desc.RemoveAt(0);
                            desc = desc.Select(x => x.Trim().Replace(',', '|')).ToList();

                            transactionNowAndPrevious = new Transaction
                            {
                                Amount = amnt,
                                Date = dateTest,
                                Description = string.Join(" ", desc),
                                Misc = textString.Box.ToString(),
                                Type = textString.TextChars.First(x => x.Virtual == true).Box.Width < 120
                                                 ? TransactionType.Deposit
                                                 : TransactionType.Withdrawal
                            };
                        }
                        else
                        {
                            transactionNowAndPrevious = transactionNowAndPrevious ?? new Transaction();
                            transactionNowAndPrevious.Description += $" {textString.Text}";
                            transactions.Add(transactionNowAndPrevious);
                        }
                    }
                }
            }
            return transactions;
        }

        private List<string> UsePageNum(int startIdx, int cnt, Document document)
        {

            var textStrings = new List<string>();
            // 2. Text extraction from the document pages.
            var extractor = new TextExtractor();
            for (var i = 0; i < cnt; i++)
            {
                var page = document.Pages[startIdx + i];
                IList<ITextString> tmpTextStrings = extractor.Extract(page)[TextExtractor.DefaultArea];
                foreach (ITextString textString in tmpTextStrings)
                {
                    textStrings.Add(textString.Text);
                }
            }
            return textStrings;
        }

        protected string PromptChoice(IDictionary<string, string> options)
        {
            Console.WriteLine();
            foreach (KeyValuePair<string, string> option in options)
            {
                Console.WriteLine(
                    (option.Key.Equals("") ? "ENTER" : "[" + option.Key + "]")
                      + " " + option.Value
                    );
            }
            Console.Write("Please select: ");
            return Console.ReadLine();
        }

        protected bool PromptNextPage(Page page, bool skip)
        {
            int pageIndex = page.Index;
            if (pageIndex > 0 && !skip)
            {
                IDictionary<string, string> options = new Dictionary<string, string>();
                options[""] = "Scan next page";
                options["Q"] = "End scanning";
                if (!PromptChoice(options).Equals(""))
                    return false;
            }

            Console.WriteLine("\nScanning page " + (pageIndex + 1) + "...\n");
            return true;
        }
    }
}
