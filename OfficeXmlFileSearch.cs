using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.IO.Packaging;

namespace BrowserClass
{

    public class SpecificDirectory
    {

        public string[,] LookUpWord(string nameKeyword, string nameStopword, string nameDirectory)
        {
            string sKeyWord = nameKeyword;
            string sStopWord = nameStopword;
            string sDirectory = nameDirectory;

            sStopWord = sStopWord.ToLower();
            sKeyWord = sKeyWord.ToLower();

            string sDocPath = Path.GetDirectoryName(sDirectory);
            // Looking for all the documents with the .docx extension
            string[] sDocName = Directory.GetFiles(sDocPath, "*.docx", SearchOption.AllDirectories);
            string[] sDocumentList = new string[1];
            string[] sDocumentText = new string[1];

            // Cycling the documents retrieved in the folder
            for (int i = 0; i < sDocName.Count(); i++)
            {
                string docWord = sDocName[i];

                // Opening the documents as read only, no need to edit them
                Package officePackage = Package.Open(docWord, FileMode.Open, FileAccess.Read);

                const String officeDocRelType = @"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";

                PackagePart corePart = null;
                Uri documentUri = null;

                // We are extracting the part with the document content within the files
                foreach (PackageRelationship relationship in officePackage.GetRelationshipsByType(officeDocRelType))
                {
                    documentUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), relationship.TargetUri);
                    corePart = officePackage.GetPart(documentUri);
                    break;
                }

                // Here enter the proper code
                if (corePart != null)
                {
                    string cpPropertiesSchema = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
                    string dcPropertiesSchema = "http://purl.org/dc/elements/1.1/";
                    string dcTermsPropertiesSchema = "http://purl.org/dc/terms/";

                    // Construction of a namespace manager to handle the different parts of the xml files
                    NameTable nt = new NameTable();
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);
                    nsmgr.AddNamespace("dc", dcPropertiesSchema);
                    nsmgr.AddNamespace("cp", cpPropertiesSchema);
                    nsmgr.AddNamespace("dcterms", dcTermsPropertiesSchema);

                    // Loading the xml document's text
                    XmlDocument doc = new XmlDocument(nt);
                    doc.Load(corePart.GetStream());

                    // I chose to directly load the inner text because I could not parse the way I wanted the document, but it works so far
                    string docInnerText = doc.DocumentElement.InnerText;
                    docInnerText = docInnerText.Replace("\\* MERGEFORMAT", ".");
                    docInnerText = docInnerText.Replace("DOCPROPERTY ", "");
                    docInnerText = docInnerText.Replace("Glossary.", "");

                    try
                    {
                        Int32 iPosKeyword = docInnerText.ToLower().IndexOf(sKeyWord);
                        Int32 iPosStopWord = docInnerText.ToLower().IndexOf(sStopWord);

                        if (iPosStopWord == -1)
                        {
                            iPosStopWord = docInnerText.Length;
                        }

                        if (iPosKeyword != -1 && iPosKeyword <= iPosStopWord)
                        {
                            // Redimensions the array if there was already a document loaded
                            if (sDocumentList[0] != null)
                            {
                                Array.Resize(ref sDocumentList, sDocumentList.Length + 1);
                                Array.Resize(ref sDocumentText, sDocumentText.Length + 1);
                            }
                            sDocumentList[sDocumentList.Length - 1] = docWord.Substring(sDocPath.Length, docWord.Length - sDocPath.Length);
                            // Taking the small context around the keyword
                            sDocumentText[sDocumentText.Length - 1] = ("(...) " + docInnerText.Substring(iPosKeyword, sKeyWord.Length + 60) + " (...)");
                        }

                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Console.WriteLine("Error reading inner text.");
                    }
                }
                // Closing the package to enable opening a document right after
                officePackage.Close();
            }

            if (sDocumentList[0] != null)
            {
                // Preparing the array for output
                string[,] sFinalArray = new string[sDocumentList.Length, 2];

                for (int i = 0; i < sDocumentList.Length; i++)
                {
                    sFinalArray[i, 0] = sDocumentList[i].Replace("\\", "");
                    sFinalArray[i, 1] = sDocumentText[i];
                }
                return sFinalArray;
            }
            else 
            {
                // Preparing the array for output
                string[,] sFinalArray = new string[1, 1];
                sFinalArray[0, 0] = "NO MATCH";
                return sFinalArray;
            }
        }
    }

}
