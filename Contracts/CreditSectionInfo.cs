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
        public int CardLast4 { get; }
        public int Year { get; }
        public SectionType CurrentSectionType { get; private set; }
        public bool GetPurchases { get; private set; }
        public bool GetFees { get; private set; }
        public bool GetInterest { get; private set; }
        public bool GetPayments { get; private set; }
        public bool GetCredits { get; private set; }
        public int IdxMin { get; set; }
//        public int IdxTransactions { get; private set; }
        public int IdxPurchases { get; private set; }
        public int IdxFees { get; private set; }
        public int IdxInterest { get; private set; }
        public int IdxPayments { get; private set; }
        public int IdxCredit { get; private set; }

        private const string TransactionFlag = "Transactions";
        private const string MinIdxFlag = "Trans Post";
        private const string PaymentFlag = "Payments";
        private const string CreditFlag = "Other Credits";
        private const string PurchaseFlag = "Purchases, Balance Transfers & Other Charges";
        private const string FeesFlag = "TOTAL FEES CHARGED FOR THIS PERIOD";
        private const string InterestFlag = "TOTAL INTEREST CHARGED FOR THIS PERIOD";
        private const string EndOfData = "Totals Year-to-Date";
        
        /*
         * Possible EndOfData flags:
         * 5596      YKG 1 7 17 161226 0 PAGE 2 of 2 10 5583 2000 VSE2
         *     - not sure if 5996 is consistent across all years but it is across 2016, 2017.
         *     - same with YKG
         *     - the 161226 seems to be a formatted date: 12/26/2016
         *
         *
         * LinesToSkip:
         * Minimum Payment
         * New Balance
         * ARVADA CO 80007-6704
         *
         * Skip these also, doubling up Interest and Fees
         *     - INTEREST CHARGED FOR THIS PERIOD
         *     - TOTAL FEES CHARGED IN 2016 
         *     - TOTAL INTEREST CHARGED IN 2016
         *     - PURCHASES 7.65% $0.00 30
         *     - CASH ADVANCES 7.65% $0.00 30
         *     - credit balance
         */

        public CreditSectionInfo(IList<ITextString> pageText, int lastFour, int year)
        {
            pageTextStrings = pageText;
            CardLast4 = lastFour;
            Year = year;
            InitializeInfo();
        }

        private void InitializeInfo()
        {
            IdxMin = GetIndexFromFlag(MinIdxFlag);
            if (IdxMin == -1)
            {
                ResetIndexes();
            }
            else
            {
                IdxFees = GetIndexFromFlag(FeesFlag, true);
                IdxCredit = GetIndexFromFlag(CreditFlag, true);
                IdxInterest = GetIndexFromFlag(InterestFlag, true);
                IdxPayments = GetIndexFromFlag(PaymentFlag, true);
                IdxPurchases = GetIndexFromFlag(PurchaseFlag, true);
            }
            SetBoolsFromIndexes();
            SetNexSection();
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

            IdxMin = -1;
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
            return CurrentSectionIdx++;
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

        private int GetIndexFromFlag(string flag, bool addOne = false)
        {
            if (flag == PurchaseFlag)
            {
                addOne = true;
            }
            var match = GetTextStringMatch(flag);
            var add = addOne ? 1 : 0;
            var retVal = match == null
                ? -1
                : GetIndex(match) + add;
            return retVal;
        }

        private int GetIndex(ITextString textString)
        {
            return pageTextStrings.IndexOf(textString);
        }
        private ITextString GetTextStringMatch(string flag)
        {
            return pageTextStrings.FirstOrDefault(y => y.Text.Contains(flag) && pageTextStrings.IndexOf(y) >= IdxMin);
        }
    }
}
