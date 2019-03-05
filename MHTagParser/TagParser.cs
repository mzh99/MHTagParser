using System.Collections.Generic;
using System.Text;

namespace OCSS.MHTagParser {
   //
   // Generic Tag Parser - can be used for SGML, or subsets like HTML.
   // Translated from the original Delphi version.
   //
   // Notes:
   // ----------------------
   // This parser doesn't handle special cases with TagEndChar embedded within a tag. This could be the case for SGML Declarations like comments.
   // This class also doesn't validate matching of tags/markers, etc. If you want a validator, look elsewhere...
   //
   // Sample Usage:
   // --------------
   // TagParser TP = new TagParser();
   // TP.Content = "<html><head>Heading</head><body><p>para 1</p><p>para 2<form><input name='test' id='myid'></form></p></body></html>";
   // TP.ParseContent();
   // Console.WriteLine("Tags: " + TP.TagCount.ToString());
   // for (int z = 0; z < TP.TagCount; z++) {
   //    Console.WriteLine("Tag=" + TP.Tag(z) + " AfterText=" + TP.TagPostText(z) + " PreText=" + TP.TagPreText(z));
   // }
   // Console.WriteLine("Pos of Body=" + TP.FindTag("BODY").ToString());
   // Console.WriteLine("Pos of xyz=" + TP.FindTag("XYZ").ToString());
   // Console.WriteLine("Pos of p,2=" + TP.FindTag("P",0,2).ToString());
   // Console.WriteLine("Count of <p>=" + TP.SpecificTagCount("P").ToString());
   //


   public class TagRec {
      public readonly string TagName;
      public readonly int StartByte;
      public readonly int EndByte;
      public readonly int EleStart;
      public readonly int EleEnd;

      public TagRec(string name, int startPos, int endPos, int eleStart, int eleEnd) {
         this.TagName = name;
         this.StartByte = startPos;
         this.EndByte = endPos;
         this.EleStart = eleStart;
         this.EleEnd = eleEnd;
      }
   }
   /// <summary>Generic Tag Parser</summary>
   public class TagParser {

      static readonly char TAGTOKEN_START = '<';
      static readonly char TAGTOKEN_END = '>';
      // static readonly char TAGTOKEN_SPACE = ' ';
      static readonly char ATTRIB_SEP = '=';
      static readonly char SINGLE_QUOTE = '\'';
      static readonly char DOUBLE_QUOTE = '"';

      public bool CaseSensitiveTags { get; private set; }
      public bool CaseSensitiveAttributes { get; private set; }
      public char TagStartChar { get; private set; }
      public char TagEndChar { get; private set; }
      public int TagCount { get { return TagRecs.Count; } }
      public string Content { get; set; }

      private List<TagRec> TagRecs;

      public TagParser(): this(false, false, TAGTOKEN_START, TAGTOKEN_END) { }

      public TagParser(bool caseSensTags, bool caseSensAttribs, char tagStartChar, char tagEndChar) {
         this.CaseSensitiveTags = caseSensTags;
         this.CaseSensitiveAttributes = caseSensAttribs;
         this.TagStartChar = tagStartChar;
         this.TagEndChar = tagEndChar;
         this.TagRecs = new List<TagRec>();
      }

      /// <summary>returns the Tag text for Tag# at Index</summary>
      /// <param name="tagNum"></param>
      /// <returns></returns>
      public string Tag(int tagNum) {
         if ((tagNum < 0) || (tagNum >= TagRecs.Count))
            return string.Empty;
         return TagRecs[tagNum].TagName;
      }

      public TagRec GetTagInfo(int tagNum) {
         if (tagNum < 0 || tagNum >= TagRecs.Count)
            return null;
         return TagRecs[tagNum];

      }

      /// <summary>Returns the index of the Tag named TagName starting the search with index StartSearch</summary>
      /// <param name="tagName"></param>
      /// <param name="startSearch"></param>
      /// <param name="numOccur"></param>
      /// <returns>if StartSearch is less than zero or greater than the number of tags, or tag is not found, -1 is returned</returns>
      public int FindTag(string tagName, int startSearch = 0, int numOccur = 1) {
         if ((startSearch >= 0) && (startSearch < TagRecs.Count) && (numOccur > 0)) {
            if (CaseSensitiveTags == false)
               tagName = tagName.ToUpper();
            for (int z = startSearch; z < TagRecs.Count; z++) {
               if (((TagRec) TagRecs[z]).TagName == tagName) {
                  numOccur--;
                  if (numOccur == 0)
                     return z;
               }
            }
         }
         return -1;
      }

      /// <summary>Returns the count of tags matching TagName starting with index StartSearch</summary>
      /// <param name="tagName"></param>
      /// <param name="startSearch"></param>
      /// <returns>if StartSearch is less than zero or greater than the number of tags, or tag is not found, 0 is returned</returns>
      public int SpecificTagCount(string tagName, int startSearch = 0) {
         int cnt = 0;
         if ((startSearch >= 0) && (startSearch < TagRecs.Count)) {
            if (CaseSensitiveTags == false)
               tagName = tagName.ToUpper();
            for (int z = startSearch; z < TagRecs.Count; z++) {
               if (((TagRec) TagRecs[z]).TagName == tagName)
                  cnt++;
            }
         }
         return cnt;
      }

      /// <summary>Returns the content between StartNum and EndNum</summary>
      /// <param name="startNum"></param>
      /// <param name="endNum"></param>
      /// <returns></returns>
      public string ContentBetween(int startNum, int endNum) {
         int startPos, endPos;

         if ((startNum >= 0) && (startNum < TagRecs.Count) && (endNum >= 0) && (endNum < TagRecs.Count) && (startNum < endNum)) {
            if (startNum + 1 == endNum) {
               return TagPostText(startNum);
            }
            else {
               startPos = ((TagRec) TagRecs[startNum]).EndByte + 1;
               endPos = ((TagRec) TagRecs[endNum - 1]).EndByte + 1;
               if (startPos < endPos) {
                  return Content.Substring(startPos, endPos - startPos);
               }
            }
         }
         return string.Empty;
      }

      /// <summary>Parses all Tag Attributes for a given tag</summary>
      /// <param name="tagNum">The index of the tag</param>
      /// <returns>IEnumerable of KeyValuePair</returns>
      //public IEnumerable<KeyValuePair<string, string>> ParseTagAttributes(int tagNum) {
      //   const char KEYVAL_SEP = '=';
      //   string oneKey, oneVal;

      //   var txt = GetTagAttributeTextRaw(tagNum);
      //   var tokens = txt.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      //   foreach (var token in tokens) {
      //      oneKey = token.StrSeg(1, 1, KEYVAL_SEP).Trim();
      //      if (CaseSensitiveAttributes == false)
      //         oneKey = oneKey.ToUpper();
      //      oneVal = token.StrSeg(2, int.MaxValue, KEYVAL_SEP).Trim().Unquote(new char[] { '"', '\'', '“', '”' });

      //      yield return new KeyValuePair<string, string>(oneKey, oneVal);
      //   }

      //}

      /// <summary>Parses all Tag Attributes for a given tag</summary>
      /// <param name="tagNum">The index of the tag</param>
      /// <returns>IEnumerable of KeyValuePair</returns>
      public IEnumerable<KeyValuePair<string, string>> ParseTagAttributes(int tagNum) {
         string oneTag, oneAttr, oneVal;
         int startPos, savePos;
         char endChar;

         oneTag = GetTagAttributeTextRaw(tagNum);
         while (oneTag.Length > 0) {
            startPos = 0;
            oneAttr = "";
            oneVal = "";
            // Find the first WHITESPACE or ATTRIB_SEP
            while (startPos + 1 <= oneTag.Length) {
               if (IsWhitespace(oneTag, startPos) || oneTag[startPos] == ATTRIB_SEP)
                  break;
               startPos++;
            }
            if (CaseSensitiveAttributes == false)
               oneAttr = oneTag.Substring(0, startPos).ToUpper();
            else
               oneAttr = oneTag.Substring(0, startPos);
            // Find the next non-whitespace char
            while (startPos + 1 <= oneTag.Length) {
               if (IsWhitespace(oneTag, startPos) == false)
                  break;
               startPos++;
            }
            if (startPos + 1 <= oneTag.Length) {
               if (oneTag[startPos] == ATTRIB_SEP) {
                  startPos++;
                  // Find the next non-whitespace (if any) after the ATTRIB_SEP
                  while (startPos + 1 <= oneTag.Length) {
                     if (IsWhitespace(oneTag, startPos) == false)
                        break;
                     startPos++;
                  }
                  if (startPos + 1 <= oneTag.Length) {
                     savePos = startPos;  // Save position
                     endChar = 'x';  // doesn't matter what this is set to
                     bool isQuoted = (oneTag[startPos] == DOUBLE_QUOTE) || (oneTag[startPos] == SINGLE_QUOTE);
                     if (isQuoted) {
                        endChar = oneTag[startPos];   // save the quote whether single or double
                        startPos++;
                     }
                     while (startPos + 1 <= oneTag.Length) {
                        if (oneTag[startPos] == endChar && isQuoted) {      // ending quote was found
                           startPos++;
                           break;
                        }
                        else {
                           if (IsWhitespace(oneTag, startPos) && isQuoted == false) {     // non-quoted attribute value end found
                              startPos++;
                              break;
                           }
                        }
                        startPos++;
                     }
                     oneVal = oneTag.Substring(savePos, startPos - savePos);
                     // Set marker to next non space char
                     while (startPos + 1 <= oneTag.Length) {
                        if (IsWhitespace(oneTag, startPos) == false)
                           break;
                        startPos++;
                     }
                  }
               }
            }
            oneTag = oneTag.Substring(startPos).TrimStart();
            if (oneAttr.Length > 0) {
               // Check if value has single/double quotes to enclose value
               if (oneVal.Length > 1) {
                  if (((oneVal[0] == DOUBLE_QUOTE) || (oneVal[0] == SINGLE_QUOTE)) && (oneVal[0] == oneVal[oneVal.Length - 1]))
                     oneVal = oneVal.Substring(1, oneVal.Length - 2);
               }
               yield return new KeyValuePair<string, string>(oneAttr, oneVal);
            }
         }
      }

      private bool IsWhitespace(char c) {
         return char.IsWhiteSpace(c);
      }

      private bool IsWhitespace(string str, int ndx) {
         return char.IsWhiteSpace(str, ndx);
      }

      /// <summary>returns the tag attributes raw string for a tag</summary>
      /// <param name="tagNum">The index of the tag</param>
      /// <returns>the raw text</returns>
      public string GetTagAttributeTextRaw(int tagNum) {
         int startPos, endPos;
         string oneTag;

         if ((tagNum >= 0) && (tagNum < TagRecs.Count)) {
            oneTag = ((TagRec) TagRecs[tagNum]).TagName;
            startPos = ((TagRec) TagRecs[tagNum]).StartByte;
            endPos = ((TagRec) TagRecs[tagNum]).EndByte;
            if (startPos < endPos) {
               return Content.Substring(startPos, endPos - startPos);
            }
         }
         return string.Empty;
      }

      public string TagPostText(int tagNum) {
         // returns the text following the Tag# at TagNum
         int sPos, ePos;

         if ((tagNum < 0) || (tagNum >= TagRecs.Count))
            return string.Empty;

         sPos = ((TagRec) TagRecs[tagNum]).EndByte + 1;
         if (tagNum == TagRecs.Count - 1) {
            ePos = Content.Length - 1;
         }
         else {
            ePos = sPos;
            for (int z = sPos; z < Content.Length; z++) {
               if (Content[z] == TagStartChar) {
                  ePos = z;
                  break;
               }
            }
         }
         if (ePos > sPos)
            return Content.Substring(sPos, ePos - sPos);

         return string.Empty;
      }

      public string TagPreText(int tagNum) {
         // returns the text preceeding the Tag# at TagNum
         int sPos, ePos;

         if ((tagNum < 0) || (tagNum >= TagRecs.Count))
            return string.Empty;

         ePos = ((TagRec) TagRecs[tagNum]).StartByte - 1;
         for (int z = ePos - 1; z >= 0; z--) {
            if (Content[z] == TagStartChar) {
               ePos = z;
               break;
            }
         }
         sPos = 1;
         for (int z = ePos - 1; z >= 0; z--) {
            if (Content[z] == TagEndChar) {
               sPos = z + 1;
               break;
            }
         }

         if (ePos > sPos)
            return Content.Substring(sPos, ePos - sPos);

         return string.Empty;
      }

      /// <summary>Parse text content</summary>
      public void ParseContent() {
         int cPos, saveCPos;
         int eleStart = 0;
         int eleEnd = 0;
         bool outOfDQuote, outOfSQuote;
         string tempUC;
         StringBuilder OneTag = new StringBuilder();

         TagRecs.Clear();
         if (Content.Length == 0)
            return;
         cPos = 0;
         while (cPos + 1 < Content.Length) {
            // Find the starting token
            while ((cPos + 1 <= Content.Length) && (Content[cPos] != TagStartChar)) {
               cPos++;
            }
            eleStart = cPos;  // save raw tag start position
            cPos++;
            if (cPos + 1 >= Content.Length)
               break;
            // Skip any whitespace before tag
            while ((cPos + 1 <= Content.Length) && IsWhitespace(Content, cPos)) {
               cPos++;
            }
            if (cPos + 1 >= Content.Length)
               break;
            if (IsWhitespace(Content, cPos))
               cPos++;
            // Get the tag name
            OneTag.Clear();
            while ((cPos + 1 <= Content.Length) && (IsWhitespace(Content, cPos) == false) && (Content[cPos] != TagEndChar)) {
               OneTag.Append(Content[cPos]);
               cPos++;
            }
            // Skip any spaces after tag
            while ((cPos + 1 <= Content.Length) && IsWhitespace(Content, cPos)) {
               cPos++;
            }
            if (CaseSensitiveTags == false) {
               tempUC = OneTag.ToString().ToUpper();
               OneTag.Clear();
               OneTag.Append(tempUC);
            }
            if (OneTag.Length > 0) {
               saveCPos = cPos;
               // Find end of tag
               outOfDQuote = true;
               outOfSQuote = true;
               while ((cPos + 1 <= Content.Length)) {
                  if ((Content[cPos] == TagEndChar) && (outOfDQuote) && (outOfSQuote))
                     break;
                  if ((Content[cPos] == '"') && (outOfSQuote))
                     outOfDQuote = !outOfDQuote;
                  if ((Content[cPos] == SINGLE_QUOTE) && (outOfDQuote))
                     outOfSQuote = !outOfSQuote;
                  cPos++;
               }
               eleEnd = cPos;    // save raw tag ending position
               TagRecs.Add(new TagRec(OneTag.ToString(), saveCPos, cPos, eleStart, eleEnd));
            }
         }
      }
   }
}
