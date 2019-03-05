﻿/*
 *  "GEDKeeper", the personal genealogical database editor.
 *  Copyright (C) 2009-2019 by Sergey V. Zhdanovskih.
 *
 *  This file is part of "GEDKeeper".
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Globalization;
using System.Text;
using BSLib;

namespace GKCommon.GEDCOM
{
    public sealed class EnumTuple : IComparable
    {
        public string Key;
        public int Value;

        public EnumTuple(string key, int value)
        {
            Key = key;
            Value = value;
        }

        public int CompareTo(object obj)
        {
            // GEDCOM enums is ASCII identifiers
            return string.Compare(Key, ((EnumTuple)obj).Key, StringComparison.OrdinalIgnoreCase);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public static class GEDCOMUtils
    {
        public static readonly TextInfo InvariantTextInfo = CultureInfo.InvariantCulture.TextInfo;
        public static readonly NumberFormatInfo InvariantNumberFormatInfo = NumberFormatInfo.InvariantInfo;

        #region Parse functions

        public static string TrimLeft(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";

            int len = str.Length;
            int i = 1;
            while (i <= len && str[i - 1] <= ' ') i++;

            string result;
            if (i > len) {
                result = "";
            } else {
                result = ((i != 1) ? str.Substring(i - 1) : str);
            }
            return result;
        }

        public static string TrimRight(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";

            int len = str.Length;
            int i = len;
            while (i > 0 && str[i - 1] <= ' ') i--;

            string result = ((i != len) ? str.Substring(0, i) : str);
            return result;
        }

        public static string ExtractDelimiter(string str, char delimiter = GEDCOMProvider.GEDCOM_DELIMITER)
        {
            if (str == null) {
                return string.Empty;
            }

            int strLen = str.Length;
            int strIdx = 0;

            // skip delimiters
            while (strIdx < strLen && str[strIdx] == delimiter) strIdx++;

            // check empty string
            if (strLen - strIdx == 0) {
                return string.Empty;
            }

            return str.Substring(strIdx, strLen - strIdx);
        }

        public static bool ExtractExpectedIdent(string str, string expectedIdent, out string remainder, bool removeIdent = true)
        {
            if (str == null) {
                remainder = string.Empty;
                return false;
            }

            int strLen = str.Length;
            int strIdx = 0;

            // check ident
            while (strIdx < strLen && ConvertHelper.IsLetterOrDigit(str[strIdx])) strIdx++;

            string ident = InvariantTextInfo.ToUpper(str.Substring(0, strIdx));
            if (expectedIdent.Equals(ident, StringComparison.Ordinal)) {
                if (removeIdent) {
                    if (strLen - strIdx == 0) {
                        remainder = string.Empty;
                    } else {
                        remainder = str.Substring(strIdx, strLen - strIdx);
                    }
                } else {
                    remainder = str;
                }
                return true;
            } else {
                remainder = str;
                return false;
            }
        }

        public static int ExtractExpectedIdents(string str, string[] expectedIdents, out string remainder, bool removeIdent = true)
        {
            if (str == null) {
                remainder = string.Empty;
                return -1;
            }

            int strLen = str.Length;
            int strIdx = 0;

            // check ident
            while (strIdx < strLen && ConvertHelper.IsLetterOrDigit(str[strIdx])) strIdx++;

            string ident = InvariantTextInfo.ToUpper(str.Substring(0, strIdx));
            int idx = Algorithms.BinarySearch<string>(expectedIdents, ident, string.CompareOrdinal);

            if (idx >= 0) {
                if (removeIdent) {
                    if (strLen - strIdx == 0) {
                        remainder = string.Empty;
                    } else {
                        remainder = str.Substring(strIdx, strLen - strIdx);
                    }
                } else {
                    remainder = str;
                }
                return idx;
            } else {
                remainder = str;
                return -1;
            }
        }

        public static string ExtractIdent(string str, out string ident, bool removeIdent = true)
        {
            if (str == null) {
                ident = string.Empty;
                return string.Empty;
            }

            int strLen = str.Length;
            int strIdx = 0;

            // check ident
            while (strIdx < strLen && ConvertHelper.IsLetterOrDigit(str[strIdx])) strIdx++;

            ident = InvariantTextInfo.ToUpper(str.Substring(0, strIdx));
            string remainder;
            if (removeIdent) {
                if (strLen - strIdx == 0) {
                    remainder = string.Empty;
                } else {
                    remainder = str.Substring(strIdx, strLen - strIdx);
                }
            } else {
                remainder = str;
            }
            return remainder;
        }

        public static string CleanXRef(string xref)
        {
            string result = xref;

            if (!string.IsNullOrEmpty(result)) {
                if (result[0] == '@') {
                    result = result.Remove(0, 1);
                }

                if (result.Length > 0 && result[result.Length - 1] == '@') {
                    result = result.Remove(result.Length - 1, 1);
                }
            }

            return result;
        }

        public static string EncloseXRef(string xref)
        {
            if (!string.IsNullOrEmpty(xref)) {
                if (xref[0] != '@') {
                    xref = "@" + xref;
                }

                if (xref[xref.Length - 1] != '@') {
                    xref += "@";
                }
            }
            return xref;
        }

        #endregion

        #region Special parsing routines

        // Line format: <level>_<@xref@>_<tag>_<value>
        public static int ParseTag(string str, out int tagLevel, out string tagXRef, out string tagName, out string tagValue)
        {
            tagLevel = 0;
            tagXRef = string.Empty;
            tagName = string.Empty;
            tagValue = string.Empty;

            int result = 0;
            var strTok = new GEDCOMParser(str, false);
            strTok.SkipWhitespaces();

            var token = strTok.CurrentToken; // already trimmed
            if (token == GEDCOMToken.EOL) {
                return -2;
            }
            if (token != GEDCOMToken.Number) {
                tagValue = str;
                return -1;
            }
            tagLevel = strTok.GetNumber();
            result += 1;

            token = strTok.Next();
            if (token != GEDCOMToken.Whitespace) {
                // syntax error
            }

            token = strTok.Next();
            if (token == GEDCOMToken.Symbol && strTok.GetSymbol() == '@') {
                token = strTok.Next();
                while (token != GEDCOMToken.Symbol && strTok.GetSymbol() != '@') {
                    tagXRef += strTok.GetWord();
                    token = strTok.Next();
                }

                // FIXME: check for errors
                //throw new EGEDCOMException(string.Format("The string {0} contains an unterminated XRef pointer", str));
                //throw new EGEDCOMException(string.Format("The string {0} is expected to start with an XRef pointer", str));
                result += 1;

                token = strTok.Next();
                strTok.SkipWhitespaces();
            }

            token = strTok.CurrentToken;
            if (token != GEDCOMToken.Word) {
                // syntax error
            }
            tagName = strTok.GetWord();
            result += 1;

            token = strTok.Next();
            if (token == GEDCOMToken.Whitespace) {
                tagValue = strTok.GetRest();
                result += 1;
            }

            return result;
        }

        // XRefPtr format: ...@<xref>@...
        public static string ParseXRefPointer(string str, out string xref)
        {
            xref = string.Empty;
            if (str == null) {
                return string.Empty;
            }

            char[] strChars = str.ToCharArray();
            int strLen = strChars.Length;

            // skip leading whitespaces
            int strBeg = 0;
            while (strBeg < strLen && strChars[strBeg] == ' ') strBeg++;

            // check empty string
            if (strLen - strBeg == 0) {
                return string.Empty;
            }

            int init = -1, fin = strBeg;
            for (int i = strBeg; i < strLen; i++) {
                char chr = strChars[i];
                if (chr == GEDCOMProvider.GEDCOM_POINTER_DELIMITER) {
                    if (init == -1) {
                        init = i;
                    } else {
                        fin = i;
                        xref = new string(strChars, init, fin + 1 - init);
                        fin += 1;
                        break;
                    }
                } else if (chr == '#' && i == init + 1) {
                    break;
                }
            }

            return new string(strChars, fin, strLen - fin);
        }

        // Time format: hour:minutes:seconds.fraction
        public static string ParseTime(string strValue, out byte hour, out byte minutes, out byte seconds, out short fraction)
        {
            hour = 0;
            minutes = 0;
            seconds = 0;
            fraction = 0;

            if (!string.IsNullOrEmpty(strValue)) {
                var strTok = new GEDCOMParser(strValue, true);

                hour = (byte)strTok.RequestNextInt();
                strTok.RequestNextSymbol(':');
                minutes = (byte)strTok.RequestNextInt();

                var tok = strTok.Next();
                if (tok == GEDCOMToken.Symbol && strTok.GetSymbol() == ':') {
                    seconds = (byte)strTok.RequestNextInt();

                    tok = strTok.Next();
                    if (tok == GEDCOMToken.Symbol && strTok.GetSymbol() == '.') {
                        fraction = (short)strTok.RequestNextInt();
                    }
                }

                strValue = strTok.GetRest();
            }

            //time.SetRawData(hour, minutes, seconds, fraction);

            return strValue;
        }

        // CutoutPosition format: x1 y1 x2 y2
        public static string ParseCutoutPosition(string strValue, out int x1, out int y1, out int x2, out int y2)
        {
            x1 = 0;
            y1 = 0;
            x2 = 0;
            y2 = 0;

            if (!string.IsNullOrEmpty(strValue)) {
                var parser = new GEDCOMParser(strValue, true);
                x1 = parser.RequestNextInt();
                y1 = parser.RequestNextInt();
                x2 = parser.RequestNextInt();
                y2 = parser.RequestNextInt();
            }
            return string.Empty;
        }

        // DateValue format: INT/FROM/TO/etc..._<date>
        public static GEDCOMCustomDate ParseDateValue(string str, GEDCOMTree owner, GEDCOMDateValue parent)
        {
            if (str == null) {
                return null;
            }

            string strDateType;
            str = ExtractDelimiter(str);
            str = ExtractIdent(str, out strDateType, false);

            int idx = Algorithms.BinarySearch<string>(GEDCOMCustomDate.GEDCOMDateTypes, strDateType, string.CompareOrdinal);
            var dateType = (idx < 0) ? GEDCOMDateType.SIMP : (GEDCOMDateType)idx;

            GEDCOMCustomDate result;
            switch (dateType) {
                case GEDCOMDateType.AFT:
                case GEDCOMDateType.BEF:
                case GEDCOMDateType.BET:
                    result = new GEDCOMDateRange(owner, parent);
                    break;
                case GEDCOMDateType.INT:
                    result = new GEDCOMDateInterpreted(owner, parent);
                    break;
                case GEDCOMDateType.FROM:
                case GEDCOMDateType.TO:
                    result = new GEDCOMDatePeriod(owner, parent);
                    break;
                default:
                    result = new GEDCOMDate(owner, parent);
                    break;
            }
            return result;
        }

        public static string ParseDate(string strValue, out GEDCOMApproximated approximated, out GEDCOMCalendar calendar, 
                                       out short year, out bool yearBC, out string yearModifier, out byte month, out byte day,
                                       out GEDCOMDateFormat dateFormat)
        {
            approximated = GEDCOMApproximated.daExact;
            calendar = GEDCOMCalendar.dcGregorian;
            year = GEDCOMDate.UNKNOWN_YEAR;
            yearBC = false;
            yearModifier = "";
            month = 0;
            day = 0;
            dateFormat = GEDCOMDateFormat.dfGEDCOMStd;

            var strTok = new GEDCOMParser(strValue, false);
            var token = strTok.Next();
            strTok.SkipWhitespaces();

            // extract approximated
            if (token == GEDCOMToken.Word) {
                string su = InvariantTextInfo.ToUpper(strTok.GetWord());
                int idx = Algorithms.IndexOf(GEDCOMCustomDate.GEDCOMDateApproximatedArray, su);
                if (idx >= 0) {
                    approximated = (GEDCOMApproximated)idx;
                    strTok.Next();
                    strTok.SkipWhitespaces();
                }
            }

            // extract escape
            token = strTok.CurrentToken;
            if (token == GEDCOMToken.Symbol && strTok.GetSymbol() == '@') {
                var escapeBuf = new StringBuilder();
                escapeBuf.Append(strTok.GetWord());
                do {
                    token = strTok.Next();
                    escapeBuf.Append(strTok.GetWord());
                } while (token != GEDCOMToken.Symbol || strTok.GetSymbol() != '@');
                // FIXME: check for errors

                var escapeStr = escapeBuf.ToString();
                int idx = Algorithms.IndexOf(GEDCOMCustomDate.GEDCOMDateEscapeArray, escapeStr);
                if (idx >= 0) {
                    calendar = (GEDCOMCalendar)idx;
                }

                strTok.Next();
                strTok.SkipWhitespaces();
            }

            // extract day
            token = strTok.CurrentToken;
            int dNum;
            if (token == GEDCOMToken.Number && ((dNum = strTok.GetNumber()) <= 31)) {
                day = (byte)dNum;
                token = strTok.Next();
            }

            // extract delimiter
            if (token == GEDCOMToken.Whitespace && strTok.GetSymbol() == ' ') {
                dateFormat = GEDCOMDateFormat.dfGEDCOMStd;
                token = strTok.Next();
            } else if (token == GEDCOMToken.Symbol && strTok.GetSymbol() == '.') {
                dateFormat = GEDCOMDateFormat.dfSystem;
                token = strTok.Next();
            }

            // extract month
            if (token == GEDCOMToken.Word) {
                //string[] monthes = GEDCOMDate.GetMonthNames(calendar);
                //int idx = Algorithms.IndexOf(monthes, strTok.GetWord());
                //month = (byte)(idx + 1);

                // in this case, according to performance test results, BinarySearch is more efficient
                // than a simple search or even a dictionary search
                int idx = BinarySearch(GEDCOMCustomDate.GEDCOMMonthValues, InvariantTextInfo.ToUpper(strTok.GetWord()));
                month = (byte)((idx < 0) ? 0 : idx);

                token = strTok.Next();
            } else if (dateFormat == GEDCOMDateFormat.dfSystem && token == GEDCOMToken.Number) {
                month = (byte)strTok.GetNumber();

                token = strTok.Next();
            }

            // extract delimiter
            if (dateFormat == GEDCOMDateFormat.dfSystem) {
                if (token == GEDCOMToken.Symbol && strTok.GetSymbol() == '.') {
                    token = strTok.Next();
                }
            } else {
                if (token == GEDCOMToken.Whitespace && strTok.GetSymbol() == ' ') {
                    token = strTok.Next();
                }
            }

            // extract year
            if (token == GEDCOMToken.Number) {
                year = (short)strTok.GetNumber();
                token = strTok.Next();

                // extract year modifier
                if (token == GEDCOMToken.Symbol && strTok.GetSymbol() == '/') {
                    token = strTok.Next();
                    if (token != GEDCOMToken.Number) {
                        // error
                    } else {
                        yearModifier = strTok.GetWord();
                    }
                    token = strTok.Next();
                }

                // extract bc/ad
                if (token == GEDCOMToken.Word && strTok.GetWord() == "B") {
                    token = strTok.Next();
                    if (token != GEDCOMToken.Symbol || strTok.GetSymbol() != '.') {
                        // error
                    }
                    token = strTok.Next();
                    if (token != GEDCOMToken.Word || strTok.GetWord() != "C") {
                        // error
                    }
                    token = strTok.Next();
                    if (token != GEDCOMToken.Symbol || strTok.GetSymbol() != '.') {
                        // error
                    }
                    strTok.Next();
                    yearBC = true;
                }
            }

            strValue = strTok.GetRest();

            return strValue;
        }

        #endregion

        #region GEDCOM Enums Parse

        /*
        public string GetStrValue(T enumVal)
        {
            #if PCL
            int idx = (int)Convert.ChangeType(enumVal, typeof(int), null);
            #else
            int idx = (int)((IConvertible)enumVal);
            #endif

            if (idx < 0 || idx >= fStrValues.Length) {
                return string.Empty;
            } else {
                return fStrValues[idx];
            }
        }

        public T GetEnumValue(string key)
        {
            if (!string.IsNullOrEmpty(key) && !fCaseSensitive) {
                key = key.Trim().ToLowerInvariant();
            }

            int result;
            if (fValues.TryGetValue(key, out result)) {
                #if PCL
                return (T)Convert.ChangeType(result, typeof(T), null);
                #else
                return (T)((IConvertible)result);
                #endif
            } else {
                return fDefaultValue;
            }
        }
         */

        public static string Enum2Str(IConvertible elem, string[] values)
        {
            int idx = (int)elem;

            if (idx < 0 || idx >= values.Length) {
                return string.Empty;
            } else {
                return values[idx];
            }
        }

        /**
         * Performance (100.000 iterations, random access):
         *  - if-based (origin): 430 (100 %), 417 (100 %)
         *  - simple-loop based (test): 449 (104 %), 437 (104.8 %)
         *  - dict-based (GEDCOMEnumHelper): 399 (92.8 %), 397 (92 %)
         *  - bin-search-based: 326 (75.8 %), 324 (77.7 %)
         * 
         * On all tests wins BinarySearch-based.
         */
        public static int Str2Enum(string val, string[] values, int defVal, bool normalize = true)
        {
            if (string.IsNullOrEmpty(val)) return defVal;

            if (normalize) {
                val = GEDCOMUtils.InvariantTextInfo.ToLower(val.Trim());
            }

            int idx = Algorithms.BinarySearch<string>(values, val, string.CompareOrdinal);
            return (idx < 0) ? defVal : idx;
        }

        #endregion

        #region GEDCOM Enums processing

        public static Encoding GetEncodingByCharacterSet(GEDCOMCharacterSet cs)
        {
            Encoding res = Encoding.Default;

            switch (cs) {
                case GEDCOMCharacterSet.csANSEL:
                    res = new AnselEncoding();
                    break;

                case GEDCOMCharacterSet.csASCII:
                    res = Encoding.GetEncoding(1251);
                    break;

                case GEDCOMCharacterSet.csUNICODE:
                    res = Encoding.Unicode;
                    break;

                case GEDCOMCharacterSet.csUTF8:
                    res = Encoding.UTF8;
                    break;
            }

            return res;
        }

        public static GEDCOMRestriction GetRestrictionVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GEDCOMRestriction.rnNone;

            GEDCOMRestriction res;
            str = str.Trim().ToLowerInvariant();
            
            if (str == "confidential")
            {
                res = GEDCOMRestriction.rnConfidential;
            }
            else if (str == "locked")
            {
                res = GEDCOMRestriction.rnLocked;
            }
            else if (str == "privacy")
            {
                res = GEDCOMRestriction.rnPrivacy;
            }
            else
            {
                res = GEDCOMRestriction.rnNone;
            }
            return res;
        }

        public static string GetRestrictionStr(GEDCOMRestriction value)
        {
            string s;

            switch (value) {
                case GEDCOMRestriction.rnConfidential:
                    s = "confidential";
                    break;

                case GEDCOMRestriction.rnLocked:
                    s = "locked";
                    break;

                case GEDCOMRestriction.rnPrivacy:
                    s = "privacy";
                    break;

                default:
                    s = "";
                    break;
            }

            return s;
        }

        public static GEDCOMPedigreeLinkageType GetPedigreeLinkageTypeVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GEDCOMPedigreeLinkageType.plNone;

            GEDCOMPedigreeLinkageType result;
            str = str.Trim().ToLowerInvariant();
            
            if (str == "adopted")
            {
                result = GEDCOMPedigreeLinkageType.plAdopted;
            }
            else if (str == "birth")
            {
                result = GEDCOMPedigreeLinkageType.plBirth;
            }
            else if (str == "foster")
            {
                result = GEDCOMPedigreeLinkageType.plFoster;
            }
            else if (str == "sealing")
            {
                result = GEDCOMPedigreeLinkageType.plSealing;
            }
            else
            {
                result = GEDCOMPedigreeLinkageType.plNone;
            }
            return result;
        }

        public static string GetPedigreeLinkageTypeStr(GEDCOMPedigreeLinkageType value)
        {
            string s;
            switch (value) {
                case GEDCOMPedigreeLinkageType.plAdopted:
                    s = "adopted";
                    break;
                case GEDCOMPedigreeLinkageType.plBirth:
                    s = "birth";
                    break;
                case GEDCOMPedigreeLinkageType.plFoster:
                    s = "foster";
                    break;
                case GEDCOMPedigreeLinkageType.plSealing:
                    s = "sealing";
                    break;
                default:
                    s = "";
                    break;
            }
            return s;
        }

        public static GEDCOMChildLinkageStatus GetChildLinkageStatusVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GEDCOMChildLinkageStatus.clNone;

            GEDCOMChildLinkageStatus result;
            str = str.Trim().ToLowerInvariant();
            
            if (str == "challenged")
            {
                result = GEDCOMChildLinkageStatus.clChallenged;
            }
            else if (str == "disproven")
            {
                result = GEDCOMChildLinkageStatus.clDisproven;
            }
            else if (str == "proven")
            {
                result = GEDCOMChildLinkageStatus.clProven;
            }
            else
            {
                result = GEDCOMChildLinkageStatus.clNone;
            }
            return result;
        }

        public static string GetChildLinkageStatusStr(GEDCOMChildLinkageStatus value)
        {
            string s;
            switch (value) {
                case GEDCOMChildLinkageStatus.clChallenged:
                    s = "challenged";
                    break;
                case GEDCOMChildLinkageStatus.clDisproven:
                    s = "disproven";
                    break;
                case GEDCOMChildLinkageStatus.clProven:
                    s = "proven";
                    break;
                default:
                    s = "";
                    break;
            }
            return s;
        }

        public static GKCommunicationType GetCommunicationTypeVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GKCommunicationType.ctVisit;

            GKCommunicationType result;
            str = str.Trim().ToLowerInvariant();
            
            if (str == "call")
            {
                result = GKCommunicationType.ctCall;
            }
            else if (str == "email")
            {
                result = GKCommunicationType.ctEMail;
            }
            else if (str == "fax")
            {
                result = GKCommunicationType.ctFax;
            }
            else if (str == "letter")
            {
                result = GKCommunicationType.ctLetter;
            }
            else if (str == "tape")
            {
                result = GKCommunicationType.ctTape;
            }
            else if (str == "visit")
            {
                result = GKCommunicationType.ctVisit;
            }
            else
            {
                result = GKCommunicationType.ctVisit;
            }
            return result;
        }

        public static string GetCommunicationTypeStr(GKCommunicationType value)
        {
            string s = "";
            switch (value) {
                case GKCommunicationType.ctCall:
                    s = "call";
                    break;
                case GKCommunicationType.ctEMail:
                    s = "email";
                    break;
                case GKCommunicationType.ctFax:
                    s = "fax";
                    break;
                case GKCommunicationType.ctLetter:
                    s = "letter";
                    break;
                case GKCommunicationType.ctTape:
                    s = "tape";
                    break;
                case GKCommunicationType.ctVisit:
                    s = "visit";
                    break;
            }
            return s;
        }

        public static GEDCOMMultimediaFormat GetMultimediaFormatVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GEDCOMMultimediaFormat.mfNone;

            GEDCOMMultimediaFormat result;
            str = str.Trim().ToLowerInvariant();
            
            if (str == "bmp")
            {
                result = GEDCOMMultimediaFormat.mfBMP;
            }
            else if (str == "gif")
            {
                result = GEDCOMMultimediaFormat.mfGIF;
            }
            else if (str == "jpg" || str == "jpeg")
            {
                result = GEDCOMMultimediaFormat.mfJPG;
            }
            else if (str == "ole")
            {
                result = GEDCOMMultimediaFormat.mfOLE;
            }
            else if (str == "pcx")
            {
                result = GEDCOMMultimediaFormat.mfPCX;
            }
            else if (str == "tif" || str == "tiff")
            {
                result = GEDCOMMultimediaFormat.mfTIF;
            }
            else if (str == "wav")
            {
                result = GEDCOMMultimediaFormat.mfWAV;
            }
            else if (str == "txt")
            {
                result = GEDCOMMultimediaFormat.mfTXT;
            }
            else if (str == "rtf")
            {
                result = GEDCOMMultimediaFormat.mfRTF;
            }
            else if (str == "avi")
            {
                result = GEDCOMMultimediaFormat.mfAVI;
            }
            else if (str == "tga")
            {
                result = GEDCOMMultimediaFormat.mfTGA;
            }
            else if (str == "png")
            {
                result = GEDCOMMultimediaFormat.mfPNG;
            }
            else if (str == "mpg" || str == "mpeg")
            {
                result = GEDCOMMultimediaFormat.mfMPG;
            }
            else if (str == "htm" || str == "html")
            {
                result = GEDCOMMultimediaFormat.mfHTM;
            }
            else if (str == "raw")
            {
                result = GEDCOMMultimediaFormat.mfRAW;
            }
            else if (str == "mp3")
            {
                result = GEDCOMMultimediaFormat.mfMP3;
            }
            else if (str == "wma")
            {
                result = GEDCOMMultimediaFormat.mfWMA;
            }
            else if (str == "psd")
            {
                result = GEDCOMMultimediaFormat.mfPSD;
            }
            else if (str == "pdf")
            {
                result = GEDCOMMultimediaFormat.mfPDF;
            }
            else if (str == "mp4")
            {
                result = GEDCOMMultimediaFormat.mfMP4;
            }
            else if (str == "ogv")
            {
                result = GEDCOMMultimediaFormat.mfOGV;
            }
            else if (str == "mka")
            {
                result = GEDCOMMultimediaFormat.mfMKA;
            }
            else if (str == "wmv")
            {
                result = GEDCOMMultimediaFormat.mfWMV;
            }
            else if (str == "mkv")
            {
                result = GEDCOMMultimediaFormat.mfMKV;
            }
            else if (str == "mov")
            {
                result = GEDCOMMultimediaFormat.mfMOV;
            }
            else
            {
                result = GEDCOMMultimediaFormat.mfUnknown;
            }
            return result;
        }

        public static string GetMultimediaFormatStr(GEDCOMMultimediaFormat value)
        {
            string s;
            switch (value) {
                case GEDCOMMultimediaFormat.mfBMP:
                    s = "bmp";
                    break;
                case GEDCOMMultimediaFormat.mfGIF:
                    s = "gif";
                    break;
                case GEDCOMMultimediaFormat.mfJPG:
                    s = "jpg";
                    break;
                case GEDCOMMultimediaFormat.mfOLE:
                    s = "ole";
                    break;
                case GEDCOMMultimediaFormat.mfPCX:
                    s = "pcx";
                    break;
                case GEDCOMMultimediaFormat.mfTIF:
                    s = "tif";
                    break;
                case GEDCOMMultimediaFormat.mfWAV:
                    s = "wav";
                    break;
                case GEDCOMMultimediaFormat.mfTXT:
                    s = "txt";
                    break;
                case GEDCOMMultimediaFormat.mfRTF:
                    s = "rtf";
                    break;
                case GEDCOMMultimediaFormat.mfAVI:
                    s = "avi";
                    break;
                case GEDCOMMultimediaFormat.mfTGA:
                    s = "tga";
                    break;
                case GEDCOMMultimediaFormat.mfPNG:
                    s = "png";
                    break;
                case GEDCOMMultimediaFormat.mfMPG:
                    s = "mpg";
                    break;
                case GEDCOMMultimediaFormat.mfHTM:
                    s = "htm";
                    break;
                case GEDCOMMultimediaFormat.mfRAW:
                    s = "raw";
                    break;
                case GEDCOMMultimediaFormat.mfMP3:
                    s = "mp3";
                    break;
                case GEDCOMMultimediaFormat.mfWMA:
                    s = "wma";
                    break;
                case GEDCOMMultimediaFormat.mfPSD:
                    s = "psd";
                    break;
                case GEDCOMMultimediaFormat.mfPDF:
                    s = "pdf";
                    break;
                case GEDCOMMultimediaFormat.mfMP4:
                    s = "mp4";
                    break;
                case GEDCOMMultimediaFormat.mfOGV:
                    s = "ogv";
                    break;
                case GEDCOMMultimediaFormat.mfMKA:
                    s = "mka";
                    break;
                case GEDCOMMultimediaFormat.mfWMV:
                    s = "wmv";
                    break;
                case GEDCOMMultimediaFormat.mfMKV:
                    s = "mkv";
                    break;
                case GEDCOMMultimediaFormat.mfMOV:
                    s = "mov";
                    break;
                default:
                    s = "";
                    break;
            }
            return s;
        }


        public static string[] MediaTypes = new string[] {
            "", "audio", "book", "card", "electronic", "fiche", "film", "magazine",
            "manuscript", "map", "newspaper", "photo", "tombstone", "video", "z" };

        public static GEDCOMMediaType GetMediaTypeVal(string str)
        {
            return (GEDCOMMediaType)Str2Enum(str, MediaTypes, (int)GEDCOMMediaType.mtUnknown);
        }

        public static string GetMediaTypeStr(GEDCOMMediaType value)
        {
            return GEDCOMUtils.Enum2Str(value, MediaTypes);
        }


        public static readonly string[] LangNames = new string[] {
            "",
            "Afrikaans", "Akkadian", "Albanian", "Amharic", "Ancient Greek", "Anglo-Saxon", "Arabic", "Armenian", "Assamese",
            "Belorusian", "Bengali", "Braj", "Bulgarian", "Burmese",
            "Cantonese", "Catalan", "Catalan_Spn", "Church-Slavic", "Czech",
            "Danish", "Dogri", "Dutch",
            "Eblaite", "English", "Esperanto", "Estonian",
            "Faroese", "Finnish", "French",
            "Georgian", "German", "Greek", "Gujarati",
            "Hattic", "Hawaiian", "Hebrew", "Hindi", "Hittite", "Hungarian", "Hurrian",
            "Icelandic", "Indonesian", "Italian",
            "Japanese",
            "Kannada", "Kazakh", "Khmer", "Konkani", "Korean",
            "Lahnda", "Lao", "Latin", "Latvian", "Lithuanian", "Luwian", 
            "Macedonian", "Maithili", "Malayalam", "Mandrin", "Manipuri", "Marathi", "Mewari", "Mitanni-Aryan", 
            "Navaho", "Nepali", "Norwegian",
            "Oriya", 
            "Pahari", "Palaic", "Pali", "Panjabi", "Persian", "Polish", "Portuguese", "Prakrit", "Pusto",
            "Rajasthani", "Romanian", "Russian", 
            "Sanskrit", "Serb", "Serbo_Croa", "Slovak", "Slovene", "Spanish", "Sumerian", "Swedish",
            "Tagalog", "Tamil", "Telugu", "Thai", "Tibetan", "Turkish",
            "Ukrainian", "Urdu",
            "Vietnamese", "Wendic",
            "Yiddish",
        };

        public static GEDCOMLanguageID GetLanguageVal(string str)
        {
            return (GEDCOMLanguageID)Str2Enum(str, LangNames, (int)GEDCOMLanguageID.Unknown, false);
        }

        public static string GetLanguageStr(GEDCOMLanguageID value)
        {
            return GEDCOMUtils.Enum2Str(value, LangNames);
        }


        public static GEDCOMNameType GetNameTypeVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GEDCOMNameType.ntNone;

            GEDCOMNameType result;
            str = str.Trim().ToLowerInvariant();
            
            if (str == "aka")
            {
                result = GEDCOMNameType.ntAka;
            }
            else if (str == "birth")
            {
                result = GEDCOMNameType.ntBirth;
            }
            else if (str == "immigrant")
            {
                result = GEDCOMNameType.ntImmigrant;
            }
            else if (str == "maiden")
            {
                result = GEDCOMNameType.ntMaiden;
            }
            else if (str == "married")
            {
                result = GEDCOMNameType.ntMarried;
            }
            else
            {
                result = GEDCOMNameType.ntNone;
            }
            return result;
        }

        public static string GetNameTypeStr(GEDCOMNameType value)
        {
            string s = "";
            switch (value) {
                case GEDCOMNameType.ntNone:
                    s = "";
                    break;
                case GEDCOMNameType.ntAka:
                    s = "aka";
                    break;
                case GEDCOMNameType.ntBirth:
                    s = "birth";
                    break;
                case GEDCOMNameType.ntImmigrant:
                    s = "immigrant";
                    break;
                case GEDCOMNameType.ntMaiden:
                    s = "maiden";
                    break;
                case GEDCOMNameType.ntMarried:
                    s = "married";
                    break;
            }
            return s;
        }

        public static GKResearchStatus GetStatusVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GKResearchStatus.rsDefined;

            GKResearchStatus result;
            str = str.Trim().ToLowerInvariant();
            
            if (str == "inprogress")
            {
                result = GKResearchStatus.rsInProgress;
            }
            else if (str == "onhold")
            {
                result = GKResearchStatus.rsOnHold;
            }
            else if (str == "problems")
            {
                result = GKResearchStatus.rsProblems;
            }
            else if (str == "completed")
            {
                result = GKResearchStatus.rsCompleted;
            }
            else if (str == "withdrawn")
            {
                result = GKResearchStatus.rsWithdrawn;
            }
            else
            {
                result = GKResearchStatus.rsDefined;
            }
            return result;
        }

        public static string GetStatusStr(GKResearchStatus value)
        {
            string s = "";
            switch (value) {
                case GKResearchStatus.rsDefined:
                    s = "defined";
                    break;
                case GKResearchStatus.rsInProgress:
                    s = "inprogress";
                    break;
                case GKResearchStatus.rsOnHold:
                    s = "onhold";
                    break;
                case GKResearchStatus.rsProblems:
                    s = "problems";
                    break;
                case GKResearchStatus.rsCompleted:
                    s = "completed";
                    break;
                case GKResearchStatus.rsWithdrawn:
                    s = "withdrawn";
                    break;
            }
            return s;
        }

        public static GEDCOMSpouseSealingDateStatus GetSpouseSealingDateStatusVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GEDCOMSpouseSealingDateStatus.sdsNone;

            GEDCOMSpouseSealingDateStatus result;
            str = GEDCOMUtils.InvariantTextInfo.ToUpper(str.Trim());
            
            if (str == "CANCELED")
            {
                result = GEDCOMSpouseSealingDateStatus.sdsCanceled;
            }
            else if (str == "COMPLETED")
            {
                result = GEDCOMSpouseSealingDateStatus.sdsCompleted;
            }
            else if (str == "EXCLUDED")
            {
                result = GEDCOMSpouseSealingDateStatus.sdsExcluded;
            }
            else if (str == "DNS")
            {
                result = GEDCOMSpouseSealingDateStatus.sdsDNS;
            }
            else if (str == "DNS/CAN")
            {
                result = GEDCOMSpouseSealingDateStatus.sdsDNSCAN;
            }
            else if (str == "PRE-1970")
            {
                result = GEDCOMSpouseSealingDateStatus.sdsPre1970;
            }
            else if (str == "SUBMITTED")
            {
                result = GEDCOMSpouseSealingDateStatus.sdsSubmitted;
            }
            else if (str == "UNCLEARED")
            {
                result = GEDCOMSpouseSealingDateStatus.sdsUncleared;
            }
            else
            {
                result = GEDCOMSpouseSealingDateStatus.sdsNone;
            }
            return result;
        }

        public static string GetSpouseSealingDateStatusStr(GEDCOMSpouseSealingDateStatus value)
        {
            string str;
            switch (value) {
                case GEDCOMSpouseSealingDateStatus.sdsCanceled:
                    str = "CANCELED";
                    break;
                case GEDCOMSpouseSealingDateStatus.sdsCompleted:
                    str = "COMPLETED";
                    break;
                case GEDCOMSpouseSealingDateStatus.sdsExcluded:
                    str = "EXCLUDED";
                    break;
                case GEDCOMSpouseSealingDateStatus.sdsDNS:
                    str = "DNS";
                    break;
                case GEDCOMSpouseSealingDateStatus.sdsDNSCAN:
                    str = "DNS/CAN";
                    break;
                case GEDCOMSpouseSealingDateStatus.sdsPre1970:
                    str = "PRE-1970";
                    break;
                case GEDCOMSpouseSealingDateStatus.sdsSubmitted:
                    str = "SUBMITTED";
                    break;
                case GEDCOMSpouseSealingDateStatus.sdsUncleared:
                    str = "UNCLEARED";
                    break;
                default:
                    str = "";
                    break;
            }
            return str;
        }

        public static GEDCOMOrdinanceProcessFlag GetOrdinanceProcessFlagVal(string su)
        {
            if (string.IsNullOrEmpty(su)) return GEDCOMOrdinanceProcessFlag.opNone;

            GEDCOMOrdinanceProcessFlag result;
            su = GEDCOMUtils.InvariantTextInfo.ToUpper(su.Trim());
            
            if (su == "YES")
            {
                result = GEDCOMOrdinanceProcessFlag.opYes;
            }
            else if (su == "NO")
            {
                result = GEDCOMOrdinanceProcessFlag.opNo;
            }
            else
            {
                result = GEDCOMOrdinanceProcessFlag.opNone;
            }
            return result;
        }

        public static string GetOrdinanceProcessFlagStr(GEDCOMOrdinanceProcessFlag value)
        {
            string str = "";
            switch (value) {
                case GEDCOMOrdinanceProcessFlag.opNone:
                    str = "";
                    break;
                case GEDCOMOrdinanceProcessFlag.opYes:
                    str = "yes";
                    break;
                case GEDCOMOrdinanceProcessFlag.opNo:
                    str = "no";
                    break;
            }
            return str;
        }

        public static string GetPriorityStr(GKResearchPriority value)
        {
            string str = "";
            switch (value) {
                case GKResearchPriority.rpNone:
                    str = "";
                    break;
                case GKResearchPriority.rpLow:
                    str = "low";
                    break;
                case GKResearchPriority.rpNormal:
                    str = "normal";
                    break;
                case GKResearchPriority.rpHigh:
                    str = "high";
                    break;
                case GKResearchPriority.rpTop:
                    str = "top";
                    break;
            }
            return str;
        }

        public static GKResearchPriority GetPriorityVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GKResearchPriority.rpNone;

            string su = str.Trim().ToLowerInvariant();
            GKResearchPriority result;

            if (su == "low")
            {
                result = GKResearchPriority.rpLow;
            }
            else if (su == "normal")
            {
                result = GKResearchPriority.rpNormal;
            }
            else if (su == "high")
            {
                result = GKResearchPriority.rpHigh;
            }
            else if (su == "top")
            {
                result = GKResearchPriority.rpTop;
            }
            else
            {
                result = GKResearchPriority.rpNone;
            }
            
            return result;
        }

        public static string GetCharacterSetStr(GEDCOMCharacterSet value)
        {
            string str = "";
            switch (value) {
                case GEDCOMCharacterSet.csASCII:
                    str = "ASCII";
                    break;
                case GEDCOMCharacterSet.csANSEL:
                    str = "ANSEL";
                    break;
                case GEDCOMCharacterSet.csUNICODE:
                    str = "UNICODE";
                    break;
                case GEDCOMCharacterSet.csUTF8:
                    str = "UTF-8";
                    break;
            }
            return str;
        }

        public static GEDCOMCharacterSet GetCharacterSetVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GEDCOMCharacterSet.csASCII;

            string su = GEDCOMUtils.InvariantTextInfo.ToUpper(str);
            GEDCOMCharacterSet result;

            if (su == "ASCII" || su == "ANSI" || su == "IBMPC") {
                result = GEDCOMCharacterSet.csASCII;
            } else if (su == "ANSEL") {
                result = GEDCOMCharacterSet.csANSEL;
            } else if (su == "UNICODE") {
                result = GEDCOMCharacterSet.csUNICODE;
            } else if (su == "UTF8" || su == "UTF-8") {
                result = GEDCOMCharacterSet.csUTF8;
            } else {
                result = GEDCOMCharacterSet.csASCII;
            }
            
            return result;
        }

        public static GEDCOMSex GetSexVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GEDCOMSex.svNone;

            str = GEDCOMUtils.InvariantTextInfo.ToUpper(str.Trim());
            GEDCOMSex result;

            if (str == "M") {
                result = GEDCOMSex.svMale;
            } else if (str == "F") {
                result = GEDCOMSex.svFemale;
            } else if (str == "U") {
                result = GEDCOMSex.svUndetermined;
            } else {
                result = GEDCOMSex.svNone;
            }
            
            return result;
        }

        public static string GetSexStr(GEDCOMSex value)
        {
            string str;
            
            switch (value) {
                case GEDCOMSex.svMale:
                    str = "M";
                    break;
                case GEDCOMSex.svFemale:
                    str = "F";
                    break;
                case GEDCOMSex.svUndetermined:
                    str = "U";
                    break;
                default:
                    str = "";
                    break;
            }
            
            return str;
        }


        public static GEDCOMBaptismDateStatus GetBaptismDateStatusVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GEDCOMBaptismDateStatus.bdsNone;

            string su = str.Trim().ToUpperInvariant();
            GEDCOMBaptismDateStatus result;

            if (su == "CHILD")
            {
                result = GEDCOMBaptismDateStatus.bdsChild;
            }
            else if (su == "COMPLETED")
            {
                result = GEDCOMBaptismDateStatus.bdsCompleted;
            }
            else if (su == "EXCLUDED")
            {
                result = GEDCOMBaptismDateStatus.bdsExcluded;
            }
            else if (su == "PRE-1970")
            {
                result = GEDCOMBaptismDateStatus.bdsPre1970;
            }
            else if (su == "STILLBORN")
            {
                result = GEDCOMBaptismDateStatus.bdsStillborn;
            }
            else if (su == "SUBMITTED")
            {
                result = GEDCOMBaptismDateStatus.bdsSubmitted;
            }
            else if (su == "UNCLEARED")
            {
                result = GEDCOMBaptismDateStatus.bdsUncleared;
            }
            else
            {
                result = GEDCOMBaptismDateStatus.bdsNone;
            }
            return result;
        }

        public static string GetBaptismDateStatusStr(GEDCOMBaptismDateStatus value)
        {
            string str = "";
            switch (value)
            {
                case GEDCOMBaptismDateStatus.bdsChild:
                    str = "CHILD";
                    break;

                case GEDCOMBaptismDateStatus.bdsCompleted:
                    str = "COMPLETED";
                    break;

                case GEDCOMBaptismDateStatus.bdsExcluded:
                    str = "EXCLUDED";
                    break;

                case GEDCOMBaptismDateStatus.bdsPre1970:
                    str = "PRE-1970";
                    break;

                case GEDCOMBaptismDateStatus.bdsStillborn:
                    str = "STILLBORN";
                    break;

                case GEDCOMBaptismDateStatus.bdsSubmitted:
                    str = "SUBMITTED";
                    break;

                case GEDCOMBaptismDateStatus.bdsUncleared:
                    str = "UNCLEARED";
                    break;
            }

            return str;
        }

        public static GEDCOMEndowmentDateStatus GetEndowmentDateStatusVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GEDCOMEndowmentDateStatus.edsNone;

            string su = str.Trim().ToUpperInvariant();
            GEDCOMEndowmentDateStatus result;

            if (su == "CHILD")
            {
                result = GEDCOMEndowmentDateStatus.edsChild;
            }
            else if (su == "COMPLETED")
            {
                result = GEDCOMEndowmentDateStatus.edsCompleted;
            }
            else if (su == "EXCLUDED")
            {
                result = GEDCOMEndowmentDateStatus.edsExcluded;
            }
            else if (su == "INFANT")
            {
                result = GEDCOMEndowmentDateStatus.edsInfant;
            }
            else if (su == "PRE-1970")
            {
                result = GEDCOMEndowmentDateStatus.edsPre1970;
            }
            else if (su == "STILLBORN")
            {
                result = GEDCOMEndowmentDateStatus.edsStillborn;
            }
            else if (su == "SUBMITTED")
            {
                result = GEDCOMEndowmentDateStatus.edsSubmitted;
            }
            else if (su == "UNCLEARED")
            {
                result = GEDCOMEndowmentDateStatus.edsUncleared;
            }
            else
            {
                result = GEDCOMEndowmentDateStatus.edsNone;
            }
            return result;
        }

        public static string GetEndowmentDateStatusStr(GEDCOMEndowmentDateStatus value)
        {
            string str = "";
            
            switch (value)
            {
                case GEDCOMEndowmentDateStatus.edsChild:
                    str = "CHILD";
                    break;

                case GEDCOMEndowmentDateStatus.edsCompleted:
                    str = "COMPLETED";
                    break;

                case GEDCOMEndowmentDateStatus.edsExcluded:
                    str = "EXCLUDED";
                    break;

                case GEDCOMEndowmentDateStatus.edsInfant:
                    str = "INFANT";
                    break;

                case GEDCOMEndowmentDateStatus.edsPre1970:
                    str = "PRE-1970";
                    break;

                case GEDCOMEndowmentDateStatus.edsStillborn:
                    str = "STILLBORN";
                    break;

                case GEDCOMEndowmentDateStatus.edsSubmitted:
                    str = "SUBMITTED";
                    break;

                case GEDCOMEndowmentDateStatus.edsUncleared:
                    str = "UNCLEARED";
                    break;
            }

            return str;
        }

        public static GEDCOMChildSealingDateStatus GetChildSealingDateStatusVal(string str)
        {
            if (string.IsNullOrEmpty(str)) return GEDCOMChildSealingDateStatus.cdsNone;

            string su = str.Trim().ToUpperInvariant();
            GEDCOMChildSealingDateStatus result;

            if (su == "BIC")
            {
                result = GEDCOMChildSealingDateStatus.cdsBIC;
            }
            else if (su == "EXCLUDED")
            {
                result = GEDCOMChildSealingDateStatus.cdsExcluded;
            }
            else if (su == "PRE-1970")
            {
                result = GEDCOMChildSealingDateStatus.cdsPre1970;
            }
            else if (su == "STILLBORN")
            {
                result = GEDCOMChildSealingDateStatus.cdsStillborn;
            }
            else if (su == "SUBMITTED")
            {
                result = GEDCOMChildSealingDateStatus.cdsSubmitted;
            }
            else if (su == "UNCLEARED")
            {
                result = GEDCOMChildSealingDateStatus.cdsUncleared;
            }
            else
            {
                result = GEDCOMChildSealingDateStatus.cdsNone;
            }

            return result;
        }

        public static string GetChildSealingDateStatusStr(GEDCOMChildSealingDateStatus value)
        {
            string str = "";

            switch (value)
            {
                case GEDCOMChildSealingDateStatus.cdsBIC:
                    str = "BIC";
                    break;

                case GEDCOMChildSealingDateStatus.cdsExcluded:
                    str = "EXCLUDED";
                    break;

                case GEDCOMChildSealingDateStatus.cdsPre1970:
                    str = "PRE-1970";
                    break;

                case GEDCOMChildSealingDateStatus.cdsStillborn:
                    str = "STILLBORN";
                    break;

                case GEDCOMChildSealingDateStatus.cdsSubmitted:
                    str = "SUBMITTED";
                    break;

                case GEDCOMChildSealingDateStatus.cdsUncleared:
                    str = "UNCLEARED";
                    break;
            }

            return str;
        }
        
        #endregion

        public static string EncodeUID(byte[] binaryKey)
        {
            var invNFI = GEDCOMUtils.InvariantNumberFormatInfo;

            StringBuilder result = new StringBuilder(36);
            byte checkA = 0;
            byte checkB = 0;

            int num = binaryKey.Length;
            for (int i = 0; i < num; i++) {
                byte val = binaryKey[i];
                checkA = unchecked((byte)(checkA + (uint)val));
                checkB = unchecked((byte)(checkB + (uint)checkA));
                result.Append(val.ToString("X2", invNFI));
            }

            result.Append(checkA.ToString("X2", invNFI));
            result.Append(checkB.ToString("X2", invNFI));

            return result.ToString();
        }

        public static int BinarySearch(EnumTuple[] array, string key)
        {
            int i = 0;
            int num = array.Length - 1;
            while (i <= num) {
                int num2 = i + (num - i >> 1);

                EnumTuple ekv = array[num2];
                int num3 = string.Compare(ekv.Key, key, StringComparison.OrdinalIgnoreCase);

                if (num3 == 0) {
                    return ekv.Value;
                }
                if (num3 < 0) {
                    i = num2 + 1;
                }
                else {
                    num = num2 - 1;
                }
            }
            return ~i;
        }
    }
}
