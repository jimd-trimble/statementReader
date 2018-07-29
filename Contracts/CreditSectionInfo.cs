using System;
using System.Linq;
using System.Collections.Generic;
using statementReader.Contracts;
using org.pdfclown.files;
using org.pdfclown.tools;
using org.pdfclown.documents.contents;
using org.pdfclown.documents;

namespace statementReader.Contracts
{

    public enum SectionType
    {
        Purchases,
        Fees,
        Interest,
        Payments,
        Credits,
        All,
        None
    }

    public class CreditSectionInfo
    {
        public static string endOfSection = "FOR THIS PERIOD";

        public int nextSectionIdx { get; private set; }
        public SectionType nextSectionType { get; private set; }
        public bool getPurchases { get; private set; }
        public bool getFees { get; private set; }
        public bool getInterest { get; private set; }
        public bool getPayments { get; private set; }
        public bool getCredits { get; private set; }
        public int idxTransactions { get; private set; }
        public int idxPurchases { get; private set; }
        public int idxFees { get; private set; }
        public int idxInterest { get; private set; }
        public int idxPayments { get; private set; }
        public int idxCredit { get; private set; }

        private IList<ITextString> pageTextStrings;
        private string transactionFlag = "Transactions";
        private string paymentFlag = "Payments";
        private string creditFlag = "Other Credits";
        private string purchaseFlag = "Purchases, Balance Transfers & Other Charges";
        private string feesFlag = "Fees Charged";
        private string interestFlag = "Interest Charged";
        private string endOfData = "Totals Year-to-Date";

        public CreditSectionInfo(IList<ITextString> pageText)
        {
            this.pageTextStrings = pageText;
            InitializeInfo();
        }

        private void InitializeInfo()
        {
            idxTransactions = GetIndexFromFlag(transactionFlag);
            if (idxTransactions == -1)
            {
                ResetIndexes();
            }
            else
            {
                idxFees = GetIndexFromFlag(feesFlag);
                idxCredit = GetIndexFromFlag(creditFlag);
                idxInterest = GetIndexFromFlag(interestFlag);
                idxPayments = GetIndexFromFlag(paymentFlag);
                idxPurchases = GetIndexFromFlag(purchaseFlag);
            }
            SetBoolsFromIndexes();
        }

        //private void UpdateInfo()
        //{
           //idxTransactions = pageTextStrings == null
            //    ? -1
            //    : pageTextStrings.IndexOf(pageTextStrings.First(y => y.Text.Contains(transactionFlag)));
            //if (idxTransactions == -1)
            //{
            //    ResetIndexes();
            //    SetBoolsFromIndexes();
            //    return;
            //}
            //idxPurchases = pageTextStrings.IndexOf(pageTextStrings.First(y => y.Text.Contains(purchaseFlag)));
            //idxFees = pageTextStrings.IndexOf(pageTextStrings.First(y => y.Text.Contains(feesFlag)));
            //idxInterest = pageTextStrings.IndexOf(pageTextStrings.First(y => y.Text.Contains(interestFlag)));
            //idxPayments = pageTextStrings.IndexOf(pageTextStrings.First(y => y.Text.Contains(paymentFlag)));
            //idxCredit = pageTextStrings.IndexOf(pageTextStrings.First(y => y.Text.Contains(creditFlag)));

            //SetBoolsFromIndexes();
            //return;
        //}

        private void ResetIndexes()
        {

            idxTransactions = -1;
            idxPurchases = -1;
            idxFees = -1;
            idxInterest = -1;
            idxPayments = -1;
            idxCredit = -1;
        }

        public bool LastPage()
        {
            return GetIndexFromFlag(endOfData) > -1;
        }

        /// <summary>
        /// Updates the indices and bools based on type passed in.
        /// </summary>
        /// <returns>The index for the beginning of the next section.</returns>
        /// <param name="section">The section just completed.</param>
        public int EndOfSection(SectionType section)
        {
            switch(section)
            {
                case SectionType.Fees:
                    {
                        getFees = false;
                        idxFees = -1;
                        break;
                    }
                case SectionType.Interest:
                    {
                        getInterest = false;
                        idxInterest = -1;
                        break;
                    }
                case SectionType.Payments:
                    {
                        getPayments = false;
                        idxPayments = -1;
                        break;
                    }
                case SectionType.Credits:
                    {
                        getCredits = false;
                        idxCredit = -1;
                        break;
                    }
                case SectionType.Purchases:
                    {
                        getPurchases = false;
                        idxPurchases = -1;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            SetNexSection();
            return nextSectionIdx;
        }

        private void SetNexSection()
        {
            /* From top-to-bottom
             * Payments
             * Credits
             * Purchases
             * Fees
             * Interest
            */
            if(getPayments)
            {
                nextSectionType = SectionType.Payments;
                nextSectionIdx = idxPayments;
            }
            else if (getCredits)
            {
                nextSectionType = SectionType.Credits;
                nextSectionIdx = idxCredit;
            }
            else if (getPurchases)
            {
                nextSectionType = SectionType.Purchases;
                nextSectionIdx = idxPurchases;
            }
            else if (getFees)
            {
                nextSectionType = SectionType.Fees;
                nextSectionIdx = idxFees;
            }
            else if (getInterest)
            {
                nextSectionType = SectionType.Interest;
                nextSectionIdx = idxInterest;
            }
            else
            {
                nextSectionType = SectionType.None;
                nextSectionIdx = -1;
            }
        }

        private void ResetBools()
        {
            getPurchases = false;
            getFees = false;
            getInterest = false;
            getPayments = false;
            getCredits = false;
        }


        private void SetBoolsFromIndexes()
        {
            getPurchases = idxPurchases > -1;
            getFees = idxFees > -1; 
            getInterest = idxInterest > -1;
            getPayments = idxPayments > -1;
            getCredits = idxCredit > -1;
        }

        private int GetIndexFromFlag(string flag)
        {
            var match = GetTextStringMatch(flag);
            return match == null
                ? -1
                : GetIndex(match);
        }

        private int GetIndex(ITextString textString)
        {
            return pageTextStrings.IndexOf(textString);
        }
        private ITextString GetTextStringMatch(string flag)
        {
            return pageTextStrings.FirstOrDefault(y => y.Text.Contains(flag));
        }
    }
}
