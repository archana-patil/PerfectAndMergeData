using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerfectAndMergeData
{
    public static class DataEnums
    {
        public enum Rules
        {
            CustomTransform,
            RemoveChars,
            ExtractWord,
            Normalise,
            ExtractLetters,
            ExtractName,
            TrimString,
            CustomExclude,
            CustomTransformLibrary
        }

        public enum RuleAttributes
        {
            LeftDelimiter,
            RightDelimiter,
            Mode,
            LookFor,
            ChangeTo,
            WholeWords,
            ELDirection,
            ELNumber,
            EWDirection,
            EWNumber,
            ExtractName,
            RemvVowels,
            RemvConsonants,
            RemvNumbers,
            RemvPunctuation,
            RemvOtherChars,
            Method,
            Category,
            CTLCategory
        }
    }
}
