﻿using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Impworks.Utils.Strings;

namespace Bonsai.Code.Utils.Date
{
    /// <summary>
    /// A date with possibly unknown parts.
    /// </summary>
    public partial struct FuzzyDate: IEquatable<FuzzyDate>, IComparable<FuzzyDate>
    {
        #region Constructor

        public FuzzyDate(DateTime date)
            : this(date.Year, date.Month, date.Day)
        {
            
        }

        public FuzzyDate(int? year, int? month, int? day, bool isDecade = false)
        {
            if (day == null && month == null && year == null)
                throw new ArgumentNullException(nameof(year), "At least one of the date components must be specified.");

            if (day != null && month == null)
                throw new ArgumentNullException(nameof(month), "When day is specified, month must also be specified.");

            // ReSharper disable once ObjectCreationAsStatement
            // invokes internal validation
            new DateTime(year ?? 2000, month ?? 1, day ?? 1);

            IsDecade = isDecade;
            Year = year;
            Month = month;
            Day = day;

            _stringValue = null;
            _fullReadableDate = null;
            _shortReadableDate = null;
        }

        #endregion

        #region Fields

        /// <summary>
        /// Year component of the date.
        /// </summary>
        public readonly int? Year;

        /// <summary>
        /// Month component of the date.
        /// </summary>
        public readonly int? Month;

        /// <summary>
        /// Day component of the date.
        /// </summary>
        public readonly int? Day;

        /// <summary>
        /// Flag indicating that the year is approximate.
        /// </summary>
        public readonly bool IsDecade;

        /// <summary>
        /// Canonically formatted date.
        /// </summary>
        private string _stringValue;

        /// <summary>
        /// Cached short readable date.
        /// </summary>
        private string _shortReadableDate;

        /// <summary>
        /// Cached readable date.
        /// </summary>
        private string _fullReadableDate;

        #endregion

        #region Properties

        /// <summary>
        /// Short date format (day/month/year).
        /// </summary>
        public string ShortReadableDate => _shortReadableDate ?? (_shortReadableDate = GetShortReadableDate());

        /// <summary>
        /// Full readable description of the date.
        /// </summary>
        public string ReadableDate => _fullReadableDate ?? (_fullReadableDate = GetFullReadableDate());

        /// <summary>
        /// Readable year component of the date.
        /// </summary>
        public string ReadableYear
        {
            get
            {
                if (Year == null)
                    return null;

                return Year + (IsDecade ? "-е" : "");
            }
        }

        /// <summary>
        /// Returns the day and month if specified (for anniversaries).
        /// </summary>
        public DateTime? Date
        {
            get
            {
                if (Day == null || Month == null)
                    return null;

                return new DateTime(Year ?? DateTime.Now.Year, Month.Value, Day.Value);
            }
        }

        /// <summary>
        /// Returns the number of years since the date.
        /// Returns null for future years.
        /// </summary>
        public string GetAge(DateTime? relative = null)
        {
            var now = relative ?? DateTime.Now;

            if (!(now.Year > Year))
                return null;

            var years = now.Year - Year.Value - 1;

            if (IsDecade)
                return $"{years - 10}..{years + 1} {GetAgeWord(years + 1)}";

            if (now.Month == Month && Day == null)
                return $"{years}..{years + 1} {GetAgeWord(years + 1)}";

            // account for after-birthday
            if (now.Month > Month || (now.Month == Month && now.Day > Day))
                years++;

            return $"{years} {GetAgeWord(years)}";
        }

        #endregion

        #region Parse methods

        /// <summary>
        /// Formatting string.
        /// </summary>
        private static readonly Regex FormatRegex = new Regex(
            @"(?<year>[0-9]{3}[0-9?]|\?{4})\.(?<month>[0-9]{2}|\?\?)\.(?<day>[0-9]{2}|\?\?)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
        );

        /// <summary>
        /// Safely parses a FuzzyDate.
        /// </summary>
        public static FuzzyDate? TryParse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            return raw.TryParse(Parse);
        }

        /// <summary>
        /// Parses a fuzzy date from a string.
        /// </summary>
        public static FuzzyDate Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                throw new ArgumentNullException(nameof(raw));

            var match = FormatRegex.Match(raw);
            if(!match.Success)
                throw new ArgumentException("Date string was not in correct format!", nameof(raw));

            var year = match.Groups["year"].Value.TryParse<int?>();
            var month = match.Groups["month"].Value.TryParse<int?>();
            var day = match.Groups["day"].Value.TryParse<int?>();
            var isDecade = false;

            if (year == null)
            {
                // parse 199? as 1990
                year = match.Groups["year"].Value.Substring(0, 3).TryParse<int?>() * 10;
                isDecade = year != null;
            }

            return new FuzzyDate(year, month, day, isDecade);
        }

        #endregion

        #region Formatting helpers

        /// <summary>
        /// Renders the short readable date.
        /// </summary>
        private string GetShortReadableDate()
        {
            var sb = new StringBuilder();

            if (Day != null)
            {
                sb.Append(Day?.ToString("D2"));
                sb.Append("/");
            }

            if (Month != null)
            {
                sb.Append(Month?.ToString("D2") ?? "??");
                sb.Append("/");
            }

            if(Year == null)
                sb.Append("????");
            else if(IsDecade)
                sb.Append((Year.Value / 10) + "?");
            else
                sb.Append(Year.Value);

            return sb.ToString();
        }

        /// <summary>
        /// Renders the full readable date.
        /// </summary>
        private string GetFullReadableDate()
        {
            var sb = new StringBuilder();

            if(Day != null)
            {
                sb.Append(Day);
                sb.Append(" ");
            }

            if(Month != null)
            {
                sb.Append(
                    Day != null
                        ? MonthNamesGenitive[Month.Value]
                        : MonthNamesNominative[Month.Value]
                );
            }

            if(Year != null)
            {
                if(Month != null)
                    sb.Append(IsDecade ? ", " : " ");

                sb.Append(ReadableYear);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Creates the canonical formatting of the date.
        /// </summary>
        private string GetCanonicalString()
        {
            var sb = new StringBuilder();

            if(Year == null)
                sb.Append("????");
            else if(IsDecade)
                sb.Append(Year.Value / 10 + "?");
            else
                sb.Append(Year.Value);

            sb.Append(".");
            sb.Append(Month?.ToString("D2") ?? "??");

            sb.Append(".");
            sb.Append(Day?.ToString("D2") ?? "??");

            return sb.ToString();
        }

        /// <summary>
        /// Month names in nominative case (for month + year format).
        /// </summary>
        private static string[] MonthNamesNominative =
        {
            "",
            "январь",
            "февраль",
            "март",
            "апрель",
            "май",
            "июнь",
            "июль",
            "август",
            "сентябрь",
            "октябрь",
            "ноябрь",
            "декабрь"
        };

        /// <summary>
        /// Month names in genitive case (for day + month format).
        /// </summary>
        private static string[] MonthNamesGenitive =
        {
            "",
            "января",
            "февраля",
            "марта",
            "апреля",
            "мая",
            "июня",
            "июля",
            "августа",
            "сентября",
            "октября",
            "ноября",
            "декабря"
        };

        /// <summary>
        /// Returns the word in correct declension for the number of years.
        /// </summary>
        private static string GetAgeWord(int age)
        {
            var ones = age % 10;
            var tens = (age / 10) % 10;

            if (tens != 1)
            {
                if (ones == 1)
                    return "год";

                if (ones >= 2 && ones <= 4)
                    return "года";
            }

            return "лет";
        }

        #endregion

        #region IEquatable implementation

        public bool Equals(FuzzyDate other) => _stringValue == other._stringValue;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is FuzzyDate date && Equals(date);
        }

        public override int GetHashCode() => _stringValue.GetHashCode();

        public static bool operator ==(FuzzyDate left, FuzzyDate right) => left.Equals(right);
        public static bool operator !=(FuzzyDate left, FuzzyDate right) => !left.Equals(right);

        #endregion

        #region IComparable implementation

        /// <summary>
        /// Compares two dates for sorting.
        /// </summary>
        public int CompareTo(FuzzyDate other)
        {
            return string.Compare(_stringValue, other._stringValue, CultureInfo.InvariantCulture, CompareOptions.None);
        }

        public static bool operator >=(FuzzyDate first, FuzzyDate second)
        {
            return first.CompareTo(second) >= 1;
        }

        public static bool operator <=(FuzzyDate first, FuzzyDate second)
        {
            return first.CompareTo(second) <= 1;
        }

        public static bool operator >(FuzzyDate first, FuzzyDate second)
        {
            return first.CompareTo(second) > 1;
        }

        public static bool operator <(FuzzyDate first, FuzzyDate second)
        {
            return first.CompareTo(second) < 1;
        }

        public static bool operator >=(FuzzyDate? first, FuzzyDate? second)
        {
            return first != null
                   && second != null
                   && first.Value.CompareTo(second.Value) >= 1;
        }

        public static bool operator <=(FuzzyDate? first, FuzzyDate? second)
        {
            return first != null
                   && second != null
                   && first.Value.CompareTo(second.Value) <= 1;
        }

        public static bool operator >(FuzzyDate? first, FuzzyDate? second)
        {
            return first != null
                   && second != null
                   && first.Value.CompareTo(second.Value) > 1;
        }

        public static bool operator <(FuzzyDate? first, FuzzyDate? second)
        {
            return first != null
                   && second != null
                   && first.Value.CompareTo(second.Value) < 1;
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns the canonically formatted date.
        /// </summary>
        public override string ToString() => _stringValue ?? (_stringValue = GetCanonicalString());

        #endregion
    }
}
