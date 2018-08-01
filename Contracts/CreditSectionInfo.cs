using System;
using System.Linq;
using System.Collections.Generic;
using org.pdfclown.documents.contents;

namespace statementReader.Contracts
{

    public enum SectionType
    {
        Purchases,
        Fees,
        Interest,
        Payments,
        Credits,
/*
        All,
*/
        None
    }

    public class CreditSectionInfo
    {
        public static string EndOfSectionFlag { get; } = "FOR THIS PERIOD";
        public IList<ITextString> pageTextStrings { get; }
        public int CurrentSectionIdx { get; private set; }
        public SectionType CurrentSectionType { get; private set; }
        public bool GetPurchases { get; private set; }
        public bool GetFees { get; private set; }
        public bool GetInterest { get; private set; }
        public bool GetPayments { get; private set; }
        public bool GetCredits { get; private set; }
        public int IdxTransactions { get; private set; }
        public int IdxPurchases { get; private set; }
        public int IdxFees { get; private set; }
        public int IdxInterest { get; private set; }
        public int IdxPayments { get; private set; }
        public int IdxCredit { get; private set; }

        private const string TransactionFlag = "Transactions";
        private const string PaymentFlag = "Payments";
        private const string CreditFlag = "Other Credits";
        private const string PurchaseFlag = "Purchases, Balance Transfers & Other Charges";
        private const string FeesFlag = "Fees Charged";
        private const string InterestFlag = "Interest Charged";
        private const string EndOfData = "Totals Year-to-Date";

        public CreditSectionInfo(IList<ITextString> pageText)
        {
            pageTextStrings = pageText;
            InitializeInfo();
        }

        private void InitializeInfo()
        {
            IdxTransactions = GetIndexFromFlag(TransactionFlag);
            if (IdxTransactions == -1)
            {
                ResetIndexes();
            }
            else
            {
                IdxFees = GetIndexFromFlag(FeesFlag);
                IdxCredit = GetIndexFromFlag(CreditFlag);
                IdxInterest = GetIndexFromFlag(InterestFlag);
                IdxPayments = GetIndexFromFlag(PaymentFlag);
                IdxPurchases = GetIndexFromFlag(PurchaseFlag);
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

            IdxTransactions = -1;
            IdxPurchases = -1;
            IdxFees = -1;
            IdxInterest = -1;
            IdxPayments = -1;
            IdxCredit = -1;
        }

        public bool LastPage()
        {
            return GetIndexFromFlag(EndOfData) > -1;
        }

        /// <summary>
        /// Updates the indices and bools based on type passed in.
        /// </summary>
        /// <returns>The index for the beginning of the next section.</returns>
        /// <param name="section">The section just completed.</param>
        public int GetNextIdx()
        {
            switch(CurrentSectionType)
            {
                case SectionType.Fees:
                    {
                        GetFees = false;
                        IdxFees = -1;
                        break;
                    }
                case SectionType.Interest:
                    {
                        GetInterest = false;
                        IdxInterest = -1;
                        break;
                    }
                case SectionType.Payments:
                    {
                        GetPayments = false;
                        IdxPayments = -1;
                        break;
                    }
                case SectionType.Credits:
                    {
                        GetCredits = false;
                        IdxCredit = -1;
                        break;
                    }
                case SectionType.Purchases:
                    {
                        GetPurchases = false;
                        IdxPurchases = -1;
                        break;
                    }
                case SectionType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            SetNexSection();
            return CurrentSectionIdx;
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
            if(GetPayments)
            {
                CurrentSectionType = SectionType.Payments;
                CurrentSectionIdx = IdxPayments;
            }
            else if (GetCredits)
            {
                CurrentSectionType = SectionType.Credits;
                CurrentSectionIdx = IdxCredit;
            }
            else if (GetPurchases)
            {
                CurrentSectionType = SectionType.Purchases;
                CurrentSectionIdx = IdxPurchases;
            }
            else if (GetFees)
            {
                CurrentSectionType = SectionType.Fees;
                CurrentSectionIdx = IdxFees;
            }
            else if (GetInterest)
            {
                CurrentSectionType = SectionType.Interest;
                CurrentSectionIdx = IdxInterest;
            }
            else
            {
                CurrentSectionType = SectionType.None;
                CurrentSectionIdx = -1;
            }
        }

/*
        private void ResetBools()
        {
            GetPurchases = false;
            GetFees = false;
            GetInterest = false;
            GetPayments = false;
            GetCredits = false;
        }
*/


        private void SetBoolsFromIndexes()
        {
            GetPurchases = IdxPurchases > -1;
            GetFees = IdxFees > -1; 
            GetInterest = IdxInterest > -1;
            GetPayments = IdxPayments > -1;
            GetCredits = IdxCredit > -1;
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
