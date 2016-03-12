//-----------------------------------------------
// by Yegor Kuzmin
// описание находится в файле Pseudocode.txt
//-----------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace XmlToJson
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			string inputPath, outputPath, xmlData, jsonData;
			try{
				InitializeFields (args, out inputPath, out outputPath);
				if(ConfirmOutputFile(outputPath)){
					ReadFile (inputPath, out xmlData);
					ConvertXmlToJson (xmlData, out jsonData);
					WriteResultFile (outputPath, jsonData);
				}
				else{
					Console.WriteLine("Cancelled");
				}
			} catch(Exception exception){
				Console.WriteLine (exception.Message);
			} finally{
				Pause ();
			}
		}

		static void InitializeFields (string[] args, out string inputPath, out string outputPath)
		{
			if (args.Length < 2)
				throw new Exception ("One of the required parameters was not specified");
			inputPath = args [0];
			outputPath = args [1];
		}

		static bool ConfirmOutputFile(string path)
		{
			bool result = true;
			if (File.Exists (path)){
				if (!Confirm ("File exists. Overwrite?"))
					result = false;
			}
			return result;
		}

		static bool Confirm(string message){
			bool result;
			string answer;
			Console.Write(message + " [yes / No(default)]: ");
			answer = Console.ReadLine ();
			result = answer.StartsWith ("y", StringComparison.OrdinalIgnoreCase);
			return result;
		}

		static void ReadFile (string path, out string output)
		{
			output = File.ReadAllText (path);
		}

		static void ConvertXmlToJson(string xmlData, out string jsonData)
		{
			xmlData = RemoveXmlDeclaration (xmlData);
			jsonData = GetJson (xmlData);
			jsonData = RemoveExcessDelimeters (jsonData);
		}

		static string RemoveXmlDeclaration (string xmlData)
		{
			string result;
			result = Regex.Replace (xmlData, @"<\?.*\?>", "");
			return result;
		}

		static string GetJson(string data, string parent = "", bool parentIsArrayElement = false)
		{
			string result = "", content, tagName;
			string parentInQuotes = WrapIn (parent, "\"");
			bool arrayElement = false;
			var tagElements = new List<string> ();
			int distinctElementsCount = 0;
			do {
				tagName = ExtractFirstTagName (data);
				if(tagName != ""){
					tagElements = GetTagElements (data, tagName);
					arrayElement = tagElements.Count > 1;
					foreach (string element in tagElements) {
						content = ExtractElementContent (element, tagName);
						result += GetJson (content, tagName, arrayElement) + ",\n";
					}
					if (arrayElement) {
						result = parentInQuotes + ": " + WrapIn(result, "[\n", "\n]") + "\n";
						tagName = "";
					} else {
						data = RemoveFromStart (data, tagElements [0]);
						++distinctElementsCount;
					}
				}
			} while(tagName != "");
			if (distinctElementsCount > 1) {
				result = WrapIn(result, "{\n", "\n}");
				if (!parentIsArrayElement)
					result = parentInQuotes + ":\n" + result;
			}
			if (ContainsData (data)) {
				if (tagElements.Count == 0)
					result = WrapIn (data, "\"");
				if (!arrayElement && !parentIsArrayElement)
					result = parentInQuotes + ": " + result;
			}
			return result;
		}

		static string WrapIn(string data, string leftSide, string rightSide = null)
		{
			string result;
			if (rightSide == null)
				rightSide = leftSide;
			result = leftSide + data + rightSide;
			return result;
		}

		static string ExtractFirstTagName(string data)
		{
			string result;
			result = SubstringInBetween (data, "<", ">");
			return result;
		}

		static string SubstringInBetween(string data, string startBoundary, string endBoundary)
		{
			string result;
			int startIndex = data.IndexOf (startBoundary) + startBoundary.Length;
			int endIndex = data.IndexOf (endBoundary, startIndex);
			int length = endIndex - startIndex;
			if (length < 0)
				result = "";
			else
				result = data.Substring (startIndex, length);
			return result;
		}

		static List<string> GetTagElements(string data, string tag)
		{
			var result = new List<string>();
			string pattern = GetOpeningTagCode (tag) + @".+?" + GetClosingTagCode (tag);
			foreach(Match match in Regex.Matches(data, pattern, RegexOptions.Singleline))
				result.Add (match.Value);
			return result;
		}

		static string GetOpeningTagCode(string tagName)
		{
			string result;
			result = WrapIn (tagName, "<", ">");
			return result;
		}

		static string GetClosingTagCode(string tagName)
		{
			string result;
			result = WrapIn (tagName, "</", ">");
			return result;
		}

		static string ExtractElementContent(string data, string tag)
		{
			string result;
			string openingTag = GetOpeningTagCode (tag);
			string closingTag = GetClosingTagCode (tag);
			result = SubstringInBetween (data, openingTag, closingTag);
			return result;
		}

		static string RemoveFromStart(string data, string toBeRemoved)
		{
			string result;
			result = data.Remove(data.IndexOf(toBeRemoved), toBeRemoved.Length);
			return result;
		}

		static bool ContainsData(string data)
		{
			bool result;
			result = Regex.Match (data, @"\w").Success;
			return result;
		}

		static string RemoveExcessDelimeters (string jsonData)
		{
			string result = jsonData;
			result = RemoveCommasBefore (result, "}");
			result = RemoveCommasBefore (result, "]");
			result = RemoveCommasBeforeEOF (result);
			return result;
		}

		static string RemoveCommasBefore (string data, string delimeter)
		{
			string result;
			result = Regex.Replace (data, @"\s*?\,+?\s*?" + delimeter, "\n" + delimeter);
			return result;
		}

		static string RemoveCommasBeforeEOF (string data)
		{
			string result;
			result = Regex.Replace (data, @"\s*?\,+?\s*?\Z", "");
			return result;
		}

		static void WriteResultFile (string path, string data)
		{
			File.WriteAllText (path, data);
		}

		static void Pause ()
		{
			Console.WriteLine("Press enter to continue.");
			Console.ReadLine ();
		}
	}
}