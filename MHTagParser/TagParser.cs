using System;
using System.Collections.Generic;
using System.Text;
using OCSS.StringUtil;
// Note: Can be decoupled from OCSS.StringUtil.MHString if two functions are replaced: StrSeg() and Unquote()

namespace OCSS.MHTagParser {
   //
   // Generic Tag Parser - can be used for SGML, or subsets like HTML.
   // Translated from the original Delphi version.
   // Created by: Mitch Howard
   // Created on: 1/11/10 (original version in 2003)
   // Copyright: (c) Mitch Howard, 2003-2010
   //
   // Notes:
   // ----------------------
   // This parser doesn't handle any special cases with ">" embedded in any tag.
   // ex) SGML/HTML Comment: "<!------> hello-->"
   // It is a legal comment tag with two comments; the first is empty and the second one contains "> hello".
   //
   // This class also doesn't validate matching of tags/markers, etc.
   // If you want a validator, look elsewhere...
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

      public TagRec(string Name, int StartPos, int EndPos) {
         TagName = Name;
         StartByte = StartPos;
         EndByte = EndPos;
      }
   }

   public class TagParser {

      static readonly char TAGTOKEN_START = '<';
      static readonly char TAGTOKEN_END = '>';
      static readonly char TAGTOKEN_SPACE = ' ';

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
         return ((TagRec) TagRecs[tagNum]).TagName;
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
      public IEnumerable<KeyValuePair<string, string>> ParseTagAttributes(int tagNum) {
         const char KEYVAL_SEP = '=';
         string oneKey, oneVal;

         var txt = GetTagAttributeTextRaw(tagNum);
         var tokens = txt.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
         foreach (var token in tokens) {
            oneKey = token.StrSeg(1, 1, KEYVAL_SEP).Trim();
            if (CaseSensitiveAttributes == false)
               oneKey = oneKey.ToUpper();
            oneVal = token.StrSeg(2, int.MaxValue, KEYVAL_SEP).Trim().Unquote(new char[] { '"', '\'', '“', '”' });

            yield return new KeyValuePair<string, string>(oneKey, oneVal);
         }

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
            cPos++;
            if (cPos + 1 >= Content.Length)
               break;
            // Skip any spaces before tag
            while ((cPos + 1 <= Content.Length) && (Content[cPos] == TAGTOKEN_SPACE)) {
               cPos++;
            }
            if (cPos + 1 >= Content.Length)
               break;
            if (Content[cPos] == TAGTOKEN_SPACE)
               cPos++;
            // Get the tag name
            OneTag.Clear();
            while ((cPos + 1 <= Content.Length) && (Content[cPos] != TAGTOKEN_SPACE) && (Content[cPos] != TagEndChar)) {
               OneTag.Append(Content[cPos]);
               cPos++;
            }
            // Skip any spaces after tag
            while ((cPos + 1 <= Content.Length) && (Content[cPos] == TAGTOKEN_SPACE)) {
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
                  if ((Content[cPos] == '\'') && (outOfDQuote))
                     outOfSQuote = !outOfSQuote;
                  cPos++;
               }
               TagRecs.Add(new TagRec(OneTag.ToString(), saveCPos, cPos));
            }
         }
      }
   }
}
