namespace TitleSizeRetriever
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Windows.Forms;
	using System.Xml.Linq;

	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	using Shameless.Resources;
	using Shameless.Tickets;
	using Shameless.Utils;

	using HtmlDocument = HtmlAgilityPack.HtmlDocument;

	public static class TitleSizeRetrieverMain
	{
		public static void Main()
		{
			var html = new HtmlDocument();
			html.Load("tmds.htm");

			var titlesFromTmds = html.DocumentNode
				.Descendants("table")
				.ToArray()[1]
				.Descendants("tr")
				.Select(title => title.Descendants("td"))
				.Select(a => a.ToArray()[0].InnerText)
				.Where(a => a.StartsWith("0004"))
				.ToArray();

			var titlesFromDatabase = JObject.FromObject(JsonConvert.DeserializeObject(File.ReadAllText(Files.SizesPath)))
				.Properties()
				.Select(a => a.Name)
				.ToArray();

			var missingTitles = titlesFromTmds.Except(titlesFromDatabase).ToArray();

			Console.WriteLine(titlesFromTmds.Length);
			Console.WriteLine(titlesFromDatabase.Length);
			Console.WriteLine(missingTitles.Length);
			
			const string bigJsonInvalid = "sizes_invalid.json";

			////var titles = DatabaseParser.ParseFromDatabase(Files.DbPath, Files.SizesPath);

			////var titlesWithKeys = DatabaseParser.ParseFromDatabase(Files.DbPath, Files.SizesPath);
			////var bigJsonForComparison = JObject.FromObject(JsonConvert.DeserializeObject(File.ReadAllText(bigJson)));
			// var titles = // ParseDatabase("community.xml");

			////// test
			////var missingTitles = titles.Except(titlesWithKeys).ToList();
			////Console.WriteLine(missingTitles.Count);
			////Console.WriteLine(string.Join("\r\n", missingTitles));
			////Environment.Exit(0);
			////// test

			////// test
			////var sizes = JObject.FromObject(JsonConvert.DeserializeObject(File.ReadAllText(bigJson)));
			////var totalSize = sizes.Properties().Select(a => long.Parse(a.Value.ToString())).Sum();
			////Console.WriteLine("Total size of all the eShop: " + DatabaseParser.HumanReadableFileSize(totalSize));
			////Environment.Exit(0);
			////// test

			var processed = 0;
			foreach (var title in missingTitles)
			{
				Console.Write($"\r{processed++}/{missingTitles.Length} ({(double)processed / missingTitles.Length * 100:F2}%)");

				var sizesJson = JObject.FromObject(JsonConvert.DeserializeObject(File.ReadAllText(Files.SizesPath)));

				var sizesJsonInvalid = JObject.FromObject(JsonConvert.DeserializeObject(File.ReadAllText(bigJsonInvalid)));

				if (sizesJson[title] == null && sizesJsonInvalid[title] == null)
				{
					long size;

					try
					{
						size = CDNUtils.GetTitleSize(title);
					}
					catch (WebException ex)
					{
						Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");

						var statusCode = (int)((HttpWebResponse)ex.Response).StatusCode;

						sizesJsonInvalid[title] = statusCode;

						File.WriteAllText(bigJsonInvalid, JsonConvert.SerializeObject(sizesJsonInvalid));
						Console.WriteLine(title + ": " + statusCode);

						continue;
					}

					sizesJson[title] = size;

					Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");

					// File.WriteAllText(Files.SizesPath, JsonConvert.SerializeObject(sizesJson));
					// File.AppendAllText("titles.txt", title.TitleId + ": " + size + Environment.NewLine);

					File.WriteAllText(Files.SizesPath, JsonConvert.SerializeObject(sizesJson));
					Console.WriteLine(title + ": " + DatabaseParser.HumanReadableFileSize(size));
				}
			}

			var json = File.ReadAllText(Files.SizesPath);
			var invalidJson = File.ReadAllText(bigJsonInvalid);

			File.WriteAllText(Files.SizesPath, JsonPrettifier.FormatJson(json));
			File.WriteAllText(bigJsonInvalid, JsonPrettifier.FormatJson(invalidJson));

			Console.WriteLine("Finished at " + DateTime.Now);
			Console.Beep(300, 2000);
		}

		private static Nintendo3DSTitle[] ParseDatabase(string groovyCiaPath)
		{
			var xmlFile = XElement.Load(groovyCiaPath);
			var titlesFound = new List<Nintendo3DSTitle>();

			foreach (var node in xmlFile.Nodes())
			{
				var titleInfo = node as XElement;

				if (titleInfo == null)
				{
					continue;
				}

				Func<string, string> titleData = tag => titleInfo.Element(tag).Value.Trim();
				var titleId = titleData("titleid");
				var name = titleData("name");
				var empty = string.Empty;

				var foundTicket = new Nintendo3DSTitle(titleId, empty, name, empty, empty, empty, 0);

				if (!titlesFound.Exists(a => Equals(a, foundTicket)))
				{
					titlesFound.Add(foundTicket);
				}
			}

			return titlesFound.ToArray();
		}
	}
}