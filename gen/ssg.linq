<Query Kind="Program" />

void Main()
{
    Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
	var basePath = @"D:\Projects\git\lacti.github.io";
	var templatePath = Path.Combine(basePath, "template");
	
	var articleTemplate = File.ReadAllText(Path.Combine(templatePath, "blog-post.html"));
	var indexTemplate = File.ReadAllText(Path.Combine(templatePath, "list.html"));
	
	var savePath = Path.Combine(basePath, "a");
	var articles = LoadArticle(Path.Combine(basePath, "src"));
	var recent = string.Join("",
		articles.OrderByDescending(e => e.WriteDate).Take(7).Select(e => string.Format(RelatedTemplateItem, e.Title, e.WriteDate.ToString("M. d."), e.Url)));
	foreach (var article in articles)
	{
		Console.WriteLine(article.Url);
		// title, datetime, content, related, recent
		var related = "";
		if (article.Related.Count > 0)
		{
			related = RelatedTemplateBegin 
				+ string.Join("", article.Related.Select(e => string.Format(RelatedTemplateItem, e.Title, e.WriteDate.ToString("M. d."), e.Url)))
				+ RelatedTemplateEnd;
		}
		
		File.WriteAllText(Path.Combine(savePath, article.FileName), 
			articleTemplate.Replace("{{title}}", article.Title)
				.Replace("{{datetime}}", article.FormalTime)
				.Replace("{{content}}", MakeContent(article.Content))
				.Replace("{{related}}", related)
				.Replace("{{recent}}", recent), Encoding.UTF8);
	}
	
	var articleItems = string.Join("\n",
		articles.OrderByDescending(e => e.WriteDate).Select(e => string.Format(ListItemTemplate, e.Title, e.FormalTime, e.Url)));
	File.WriteAllText(Path.Combine(basePath, "index.html"), 
		indexTemplate.Replace("{{article}}", articleItems)
			.Replace("{{recent}}", recent), Encoding.UTF8);
	/*
	var indexes = string.Join("\n",
		indexTuples.OrderBy(e => e.Item1).Select(e => string.Format("        <li><a href=\"a/{1}.html\">{0}</a></li>\n", e.Item1, e.Item2)));
	File.WriteAllText(Path.Combine(basePath, "index.html"), indexTemplate.Replace("{{list_item}}", indexes), Encoding.UTF8);
	*/
}

class Article
{
	public string FileName;
	public string Title;
	public DateTime WriteDate;
	public string Content;
	public readonly List<Article> Related = new List<Article>();
	
	public string Url { get { return "/a/" + FileName; } }
	public string FormalTime { get { return WriteDate.ToString("MMMM dd, yyyy") + " at " + WriteDate.ToString("h:mm tt"); } }
}

List<Article> LoadArticle(string path)
{
	var articles = new List<Article>();
	foreach (var file in Directory.GetFiles(path, "*.txt"))
	{
		var lines = File.ReadAllLines(file, Encoding.UTF8);
		articles.Add(new Article
		{
			FileName = Path.GetFileNameWithoutExtension(file) + ".html",
			Title = lines[0],
			WriteDate = DateTime.Parse(lines[1]),
			Content = string.Join(Environment.NewLine, lines.Skip(2))
		});
	}
	
	// make related
	foreach (var tuple in articles.Select(e => Tuple.Create(e.Title.Contains("#") ? e.Title.Substring(0, e.Title.IndexOf("#")).Trim() : e.Title.Trim(), e)).GroupBy(e => e.Item1))
	{
		var groupedArticles = tuple.Select(e => e.Item2).ToArray();
		foreach (var each in groupedArticles)
			each.Related.AddRange(groupedArticles.Where(e => e != each));
	}
	return articles;
}

static readonly Regex CodeSectionRegex = new Regex(@"\[code lang=""([^""]+)""\s*\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
string MakeContent(string original)
{
	var resultBuffer = new StringBuilder();
	var isCode = false;
	foreach (var line in original.Replace("\r", "").Split('\n'))
	{
		if (line.Contains("[code lang=\"")) isCode = true;
		if (!isCode)
		{
			resultBuffer.Append(line + "<br />\n");
		}
		else
		{
			resultBuffer.Append(line.Replace("<", "&lt;").Replace(">", "&gt;") + "\n");
		}
		
		if (line.Contains("[/code]")) isCode = false;
	}
	var content = resultBuffer.ToString();
	content = CodeSectionRegex.Replace(content, match => string.Format("<pre class=\"brush: {0}\">", match.Groups[1].Value));
	content = content.Replace("[/code]", "</pre>");
	if (!content.Contains("<a"))
	{
		content = ConvertUrlsToLinks(content);
	}
	return content;
}

static readonly Regex UrlToLinkRegex = new Regex(@"((www\.|(http|https|ftp|news|file)+\:\/\/)[&#95;.a-z0-9-]+\.[a-z0-9\/&#95;:@=.+?,##%&~-]*[^.|\'|\# |!|\(|?|,| |>|<|;|\)])", RegexOptions.IgnoreCase);
private string ConvertUrlsToLinks(string msg)
{
	return UrlToLinkRegex.Replace(msg, "<a href=\"$1\">$1</a>").Replace("href=\"www", "href=\"http://www");
}

const string RelatedTemplateBegin = @"
                <div class=""well"">
					<h4>Related</h4>
					<ul>";
const string RelatedTemplateItem = @"
						<li><a href=""{2}"">{0}</a> <span class=""date"">{1}</span></li>";
const string RelatedTemplateEnd = @"
					</ul>
                </div>
                <!-- /well -->";
				
const string RecentTemplateItem = @"
						<li><a href=""{2}"">{0}</a> <span class=""date"">{1}</span></li>";
						
const string ListItemTemplate = @"
                <h4><a href=""{2}"">{0}</a></h4>
                <p>
                    <span class=""glyphicon glyphicon-time""></span> Posted on {1}</p>
                <hr>";