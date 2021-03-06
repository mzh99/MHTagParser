using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OCSS.MHTagParser;

namespace MHTagParser.Tests {

   [TestClass]
   public class UnitTest1 {

      private static readonly string HTML_CONTENT1 = @"<html><head>Heading</head><body><p>para 1</p><p>para 2<form><input name='test' id='myid'><input type='checkbox' id='vehicleid' name='vehicle' value='Boat' checked>I have a boat></form></p></body></html>";
      private static readonly string HTML_CONTENT2 = @"<html><head>Heading</head><body><!-- a comment here --></body></html>";
      private static readonly string HTML_CONTENT_SPACES = @"<p class =  ""dummy1 dummy2"" id=test>";

      private static readonly string XML_CONTENT1 = @"<?xml version=""1.0"" encoding=""UTF-8""?><svg class=""__svgcsssuper"" viewBox=""0 0 128 104"" preserveAspectRatio=""xMinYMin meet"" version=""1.0"" xmlns=""http://www.w3.org/2000/svg"" xmlns:cc=""http://creativecommons.org/ns#"" xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#""></svg>";
      private static readonly string XML_CONTENT2 = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?><svg" + "\n" +
         @"id=""svg99"" version=""1.1"" viewBox=""0 0 448 512""></svg>";

      /*
         for (int z = 0; z < TP.TagCount; z++) {
            Console.WriteLine("Tag=" + TP.Tag(z) + " AfterText=" + TP.TagPostText(z) + " PreText=" + TP.TagPreText(z));
         }
         Tag=HTML AfterText= PreText=
         Tag=HEAD AfterText=Heading PreText=
         Tag=/HEAD AfterText= PreText=Heading
         Tag=BODY AfterText= PreText=
         Tag=P AfterText=para 1 PreText=
         Tag=/P AfterText= PreText=para 1
         Tag=P AfterText=para 2 PreText=
         Tag=FORM AfterText= PreText=para 2
         Tag=INPUT AfterText= PreText=
         Tag=/FORM AfterText= PreText=
         Tag=/P AfterText= PreText=
         Tag=/BODY AfterText= PreText=
         Tag=/HTML AfterText= PreText=
      */

      [TestMethod]
      public void BasicParseIsSuccessful() {
         TagParser TP = new TagParser { Content = HTML_CONTENT1 };
         TP.ParseContent();
         Assert.AreEqual(14, TP.TagCount, "Tag count not 13");
         Assert.AreEqual(3, TP.FindTag("BODY"), "BODY tag not found at index 3");
         Assert.AreEqual(-1, TP.FindTag("XYZ"), "xyz (missing) tag not -1");
         Assert.AreEqual(6, TP.FindTag("p", 0, 2), "second P tag does not have index of 6");
         Assert.AreEqual(2, TP.SpecificTagCount("P"), "count of P tags is not 2");
         var inputNdx = TP.FindTag("INPUT");
         Assert.IsTrue(inputNdx >= 0, "input tag was not found");
         var rawElements = TP.GetTagAttributeTextRaw(inputNdx);
         Assert.AreEqual("name='test' id='myid'", rawElements, "raw element extract for input tag is incorrect.");
         Assert.AreEqual("HTML", TP.Tag(0), "First tag not HTML");
         Assert.AreEqual("HEAD", TP.Tag(1), "Second tag not HEAD");
         Assert.AreEqual("Heading", TP.TagPostText(1), "Text after second tag not Heading");
      }

      [TestMethod]
      public void ParseAttributesIsSuccessful() {
         TagParser TP = new TagParser { Content = HTML_CONTENT1 };
         TP.ParseContent();
         var inputNdx = TP.FindTag("INPUT", 0, 2);
         Assert.AreEqual(9, inputNdx, "input tag was not 9");
         var list = TP.ParseTagAttributes(inputNdx).ToArray();
         Assert.AreEqual(5, list.Length, "List of attributes not 5");
         Assert.AreEqual("TYPE", list[0].Key.ToUpper(), "First attribute not TYPE");
         Assert.AreEqual("CHECKBOX", list[0].Value.ToUpper(), "First value not CHECKBOX");
         Assert.AreEqual("CHECKED", list[4].Key.ToUpper(), "Fifth attribute not CHECKED");
         Assert.AreEqual(string.Empty, list[4].Value.ToUpper(), "Fifth value not empty");
      }

      [TestMethod]
      public void ParseAttributesWithSpacesInValueIsSuccessful() {
         TagParser TP = new TagParser(true, true, '<', '>') { Content = XML_CONTENT1 };
         TP.ParseContent();
         var svgNdx = TP.FindTag("svg");
         Assert.AreEqual(1, svgNdx, "svg tag index != 1");
         var kvps = TP.ParseTagAttributes(svgNdx).ToArray();
         Assert.AreEqual(8, kvps.Length, "array not 8 elements");
         Assert.AreEqual("class", kvps[0].Key, "class not first attribute");
         Assert.AreEqual("__svgcsssuper", kvps[0].Value, "class value incorrect");
         Assert.AreEqual("viewBox", kvps[1].Key, "viewBox not second attribute");
         Assert.AreEqual("0 0 128 104", kvps[1].Value, "viewBox value incorrect");
      }

      [TestMethod]
      public void ParseAttributesWithWhitespaceRightAfterElementIsSuccessful() {
         TagParser TP = new TagParser(true, true, '<', '>') { Content = XML_CONTENT2 };
         TP.ParseContent();
         var svgNdx = TP.FindTag("svg");
         Assert.AreEqual(1, svgNdx, "svg tag index != 1");
         var kvps = TP.ParseTagAttributes(svgNdx).ToArray();
         Assert.AreEqual(3, kvps.Length, "array not 3 elements");
         Assert.AreEqual("id", kvps[0].Key, "id not first attribute");
         Assert.AreEqual("svg99", kvps[0].Value, "id value incorrect");
         var tagName = TP.Tag(svgNdx);
         Assert.AreEqual("svg", tagName, "tag not svg");
         Assert.AreEqual(3, tagName.Length, "svg tag not 3 long");
      }

      [TestMethod]
      public void ParseAttributesWithoutSpacesIsSuccessful() {
         TagParser TP = new TagParser { Content = HTML_CONTENT_SPACES };
         TP.ParseContent();
         Assert.AreEqual(1, TP.TagCount, "Tag count not 0");
         Assert.AreEqual("P", TP.Tag(0), "Tag 0 not P");
         var kvps = TP.ParseTagAttributes(0).ToArray();
         Assert.AreEqual("CLASS", kvps[0].Key, "Key 0 incorrect");
         Assert.AreEqual("dummy1 dummy2", kvps[0].Value, "Value 0 incorrect");
         Assert.AreEqual("ID", kvps[1].Key, "Key 1 incorrect");
         Assert.AreEqual("test", kvps[1].Value, "Value 1 incorrect");
      }

      [TestMethod]
      public void RawTagOffsetsAreCorrect() {
         var content = File.ReadAllText(@"E:\Data\Source\CS\ParcusBudget\SourceAssets\test\reg-trash-alt-red.svg");
         TagParser TP = new TagParser(true, true, '<', '>') { Content = content };
         TP.ParseContent();
         var svgNdx = TP.FindTag("svg");
         Assert.AreEqual(1, svgNdx, "svg tag index != 1");
         var tag = TP.GetTagInfo(svgNdx);
         Assert.AreEqual(55, tag.EleStart, "Ele start not 56");
         Assert.AreEqual(346, tag.EleEnd, "Ele end not 346");
      }

   }

}
