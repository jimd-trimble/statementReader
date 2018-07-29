using System;
using System.Linq;
using System.Collections.Generic;
using statementReader.Contracts;
using org.pdfclown.files;
using org.pdfclown.tools;
using org.pdfclown.documents.contents;
using org.pdfclown.documents;

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
        //private string flag = "Date Description Check No.Additions Subtractions Balance";
        private string flag1 = "Date Description Check";
        //private string flag2 = "Date\tDescription";
        private string begining = "Beginning balance on ";
        private string eof = "Ending balance on ";
        private string filePath = "";

        #region Main Extract Method.
        public List<Transaction> Extract(Document document)
        {
            var extractor = new TextExtractor();
            var inProgress = false;
            var transactions = new List<Transaction>();
            var pgCnt = 0;
            var year = 1977;
            var transactionsStart = flag1;

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
                if (dashIdx > -1)
                {
                    int.TryParse(headerSplit[dashIdx - 1], out year);
                }
                var accountPage = pageTextStrings.Any(x => x.Text.Contains(account));
                var hasBalances = pageTextStrings.Any(x => x.Text.Contains(begining));

                inProgress = (!inProgress && accountPage && hasBalances) || inProgress;

                if (!inProgress)
                {
                    continue;
                }

                var hasEof = pageTextStrings.Any(y => y.Text.Contains(eof));
                inProgress = !hasEof;

                var startIdx = -1;
                var contText = pageTextStrings.FirstOrDefault(y => y.Text.Contains(transactionsStart));
                if (hasBalances)
                {
                    startIdx = pageTextStrings.IndexOf(pageTextStrings.First(y => y.Text.Contains(begining))) + 1;
                }
                else if (contText != null)
                {
                    startIdx = pageTextStrings.IndexOf(contText) + 1;
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
                    if (textString.Text.Contains(eof))
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
                            Amount = typ == TransactionType.Deposit ? amnt : amnt * -1,
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
                        if ((desc.Count > 1 && desc[desc.Count - 2].ToLower() == "card" || desc[desc.Count - 1].Trim().Length == 4) && int.TryParse(desc[desc.Count - 1], out cardTst))
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
                        if ((desc.Count > 1 && desc[desc.Count - 2].ToLower() == "card" || desc[desc.Count - 1].Trim().Length == 4) && int.TryParse(desc[desc.Count - 1], out cardTst))
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
#endregion


        public List<Transaction> ExtractCredit(Document document)
        {
            if (account.Length < 5)
            {
                throw new Exception("Invalid account number, must be longer than 5 digits.");
            }
            var last4 = account.Remove(account.Length - 5);

            var extractor = new TextExtractor();
            var transactions = new List<Transaction>();
            var pgCnt = 0;
            var year = 1977;
            var yearHeader = "Statement Billing Period";
            var accountFlag = "$Ending in {last4}";


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
                var dashIdx = headerSplit.IndexOf(yearHeader);
                if (dashIdx > -1)
                {
                    //int.TryParse(headerSplit[dashIdx - 1], out year);
                    var tst1 = headerSplit[dashIdx + 1];
                    var parseYear = tst1.Split('/');
                    if (parseYear.Length >= 3)
                    {
                        if (!int.TryParse(parseYear[2], out year))
                        {
                            var stop = "stop!";
                        }
                    }
                }
                var pageInfo = new CreditSectionInfo(pageTextStrings);
                var accountPage = pageTextStrings.Any(x => x.Text.Contains(accountFlag));

                if (!accountPage)
                {
                    continue;
                }

                var startIdx = -1;

                //var contText = pageTextStrings.FirstOrDefault(y => y.Text.Contains(flag1));
                if (hasBalances)
                {
                    startIdx = pageTextStrings.IndexOf(pageTextStrings.First(y => y.Text.Contains(lbegining))) + 1;
                }
                else if (contText != null)
                {
                    startIdx = pageTextStrings.IndexOf(contText) + 1;
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
                    if (textString.Text.Contains(eof))
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
                            Amount = typ == TransactionType.Deposit ? amnt : amnt * -1,
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
                        if ((desc.Count > 1 && desc[desc.Count - 2].ToLower() == "card" || desc[desc.Count - 1].Trim().Length == 4) && int.TryParse(desc[desc.Count - 1], out cardTst))
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
                        if ((desc.Count > 1 && desc[desc.Count - 2].ToLower() == "card" || desc[desc.Count - 1].Trim().Length == 4) && int.TryParse(desc[desc.Count - 1], out cardTst))
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

        public List<Transaction> Run(string pAccount, string pFilePath, bool creditStatment = false)
        {
            account = pAccount;
            filePath = pFilePath;
            List<Transaction> retVal;
            using (File file = new File(filePath))
            {
                var document = file.Document;

                retVal = creditStatment
                    ? ExtractCredit(document)
                    : Extract(document);
            }
            return retVal;
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
    }
}
