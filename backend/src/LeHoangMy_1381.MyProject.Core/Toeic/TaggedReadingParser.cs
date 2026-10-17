using Abp.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace LeHoangMy_1381.MyProject.Toeic;

public class ReadingTag
{
    public string Tag { get; init; }
    public int Paragraph { get; init; }
    public Dictionary<string, string> Attributes { get; init; } = new();
    public List<ReadingTag> Children { get; } = new();
    public List<string> Lines { get; } = new();
    public List<byte[]> Images { get; } = new();
    public string Text => string.Join("\n", Lines).Trim();
    public string Attr(string name) => Attributes.TryGetValue(name, out var value) ? value : throw Error($"Thiếu thuộc tính {name}.");
    public int Number(string name) => int.TryParse(Attr(name), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : throw Error($"{name} phải là số nguyên.");
    public bool Flag(string name) => Attr(name) switch { "true" => true, "false" => false, _ => throw Error($"{name} phải là true/false.") };
    public IEnumerable<ReadingTag> All(string tag) => Children.SelectMany(c => (c.Tag == tag ? new[] { c } : Array.Empty<ReadingTag>()).Concat(c.All(tag)));
    public ReadingTag One(string tag) { var nodes = All(tag).ToList(); return nodes.Count == 1 ? nodes[0] : throw Error($"Cần đúng một {tag}."); }
    public UserFriendlyException Error(string message) => new($"Paragraph {Paragraph} ({Tag}): {message}");
}

// Native .NET parser: no Office, shell, Python process, or external relationships.
public static class TaggedReadingParser
{
    private static readonly Regex TagRegex = new(@"^\[\[(/?)([A-Z_]+)(.*?)\]\]$", RegexOptions.Compiled);
    private static readonly Regex AttributeRegex = new("([a-z_]+)=\"([^\"<>]*)\"", RegexOptions.Compiled);
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly Dictionary<string, string[]> Parents = new()
    {
        ["EXAM"] = new[] { "" }, ["PART"] = new[] { "EXAM" }, ["GROUP"] = new[] { "PART" },
        ["BLOCK"] = new[] { "GROUP" }, ["GAP_REF"] = new[] { "GROUP" }, ["QUESTION"] = new[] { "PART", "GROUP" },
        ["STEM"] = new[] { "QUESTION" }, ["OPTION"] = new[] { "QUESTION" }, ["ANSWER"] = new[] { "QUESTION" }, ["HINT"] = new[] { "QUESTION" }
    };
    private static readonly int[][] GroupBounds = {
        new[] { 6,131,134 },new[] { 6,135,138 },new[] { 6,139,142 },new[] { 6,143,146 },
        new[] { 7,147,148 },new[] { 7,149,150 },new[] { 7,151,152 },new[] { 7,153,154 },new[] { 7,155,157 },
        new[] { 7,158,160 },new[] { 7,161,163 },new[] { 7,164,167 },new[] { 7,168,171 },new[] { 7,172,175 },
        new[] { 7,176,180 },new[] { 7,181,185 },new[] { 7,186,190 },new[] { 7,191,195 },new[] { 7,196,200 }
    };
    private static XDocument ReadXml(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name) ?? throw new UserFriendlyException($"DOCX thiếu {name}.");
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 32 * 1024 * 1024 });
        return XDocument.Load(reader);
    }
    public static ReadingTag Parse(byte[] data)
    {
        if (data == null || data.Length == 0 || data.Length > 25 * 1024 * 1024) throw new UserFriendlyException("Chọn DOCX không quá 25 MB.");
        try
        {
            using var stream = new MemoryStream(data, false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            if (archive.Entries.Count > 2000 || archive.Entries.Sum(e => e.Length) > 128L * 1024 * 1024) throw new UserFriendlyException("DOCX vượt giới hạn giải nén.");
            var document = ReadXml(archive, "word/document.xml");
            var rels = ReadXml(archive, "word/_rels/document.xml.rels").Root!.Elements().ToDictionary(e => (string)e.Attribute("Id")!, e => e);
            ReadingTag root = null;
            var stack = new Stack<ReadingTag>();
            var finished = false;
            var index = 0;
            foreach (var paragraph in document.Root!.Element(W + "body")!.Descendants(W + "p"))
            {
                index++;
                var text = string.Concat(paragraph.Descendants().Select(e => e.Name == W + "t" ? e.Value : e.Name == W + "tab" ? "\t" : e.Name == W + "br" ? "\n" : "")).Trim();
                var match = TagRegex.Match(text);
                if (root == null && (!match.Success || match.Groups[1].Value != "" || match.Groups[2].Value != "EXAM" || !text.Contains("id=\"RC06\""))) continue;
                if (finished)
                {
                    if (match.Success && match.Groups[1].Value == "" && match.Groups[2].Value == "EXAM") throw new UserFriendlyException($"Paragraph {index}: nhiều EXAM.");
                    continue;
                }
                if (match.Success)
                {
                    var closing = match.Groups[1].Value == "/";
                    var tag = match.Groups[2].Value;
                    var raw = match.Groups[3].Value;
                    if (!Parents.ContainsKey(tag)) throw new UserFriendlyException($"Paragraph {index}: tag không hỗ trợ {tag}.");
                    if (closing)
                    {
                        if (!string.IsNullOrWhiteSpace(raw) || stack.Count == 0 || stack.Peek().Tag != tag) throw new UserFriendlyException($"Paragraph {index}: đóng tag sai {tag}.");
                        stack.Pop();
                        if (tag == "EXAM") finished = true;
                        continue;
                    }
                    var attrs = new Dictionary<string, string>();
                    foreach (Match attribute in AttributeRegex.Matches(raw))
                        if (!attrs.TryAdd(attribute.Groups[1].Value, attribute.Groups[2].Value)) throw new UserFriendlyException($"Paragraph {index}: thuộc tính trùng.");
                    if (!string.IsNullOrWhiteSpace(AttributeRegex.Replace(raw, ""))) throw new UserFriendlyException($"Paragraph {index}: thuộc tính sai định dạng.");
                    var parent = stack.Count == 0 ? "" : stack.Peek().Tag;
                    if (!Parents[tag].Contains(parent)) throw new UserFriendlyException($"Paragraph {index}: {tag} không thuộc {parent}.");
                    var node = new ReadingTag { Tag = tag, Attributes = attrs, Paragraph = index };
                    if (stack.Count > 0) stack.Peek().Children.Add(node); else root = node;
                    if (tag != "ANSWER" && tag != "GAP_REF") stack.Push(node);
                    continue;
                }
                if (text.StartsWith("[[")) throw new UserFriendlyException($"Paragraph {index}: tag sai định dạng.");
                var images = paragraph.Descendants(A + "blip").ToList();
                if (stack.Count > 0 && new[] { "STEM", "OPTION", "HINT", "BLOCK" }.Contains(stack.Peek().Tag))
                {
                    var node = stack.Peek();
                    if (text.Length > 0) node.Lines.Add(text);
                    foreach (var image in images)
                    {
                        var reference = (string)image.Attribute(R + "embed");
                        if (reference == null || !rels.TryGetValue(reference, out var relationship) || (string)relationship.Attribute("TargetMode") == "External") throw node.Error("Ảnh thiếu relationship hoặc là external.");
                        var target = (string)relationship.Attribute("Target") ?? "";
                        if (!target.StartsWith("media/", StringComparison.Ordinal) || target.Contains("..") || target.Contains('\\') || target.Contains(':')) throw node.Error("Đường dẫn ảnh không an toàn.");
                        var entry = archive.GetEntry("word/" + target) ?? throw node.Error("Không tìm thấy ảnh nhúng.");
                        using var imageStream = entry.Open();
                        using var bytes = new MemoryStream();
                        imageStream.CopyTo(bytes);
                        node.Images.Add(bytes.ToArray());
                    }
                }
                else if (images.Count > 0) throw new UserFriendlyException($"Paragraph {index}: ảnh ngoài BLOCK.");
                // Source notes and editorial Question 131 labels are deliberately excluded.
            }
            if (root == null || !finished || stack.Count != 0) throw new UserFriendlyException("Thiếu hoặc chưa đóng EXAM RC06.");
            Validate(root);
            return root;
        }
        catch (InvalidDataException) { throw new UserFriendlyException("File không phải DOCX ZIP hợp lệ."); }
        catch (XmlException) { throw new UserFriendlyException("XML bên trong DOCX không hợp lệ."); }
    }
    private static void Require(ReadingTag n, string names) { foreach (var name in names.Split(' ')) _ = n.Attr(name); }
    private static void Check(bool condition, ReadingTag n, string message) { if (!condition) throw n.Error(message); }
    public static void Validate(ReadingTag root)
    {
        Require(root, "id version source duration_minutes question_count title");
        Check(root.Number("duration_minutes") == 75 && root.Number("question_count") == 100, root, "Đề phải có 100 câu/75 phút.");
        Check(root.Attr("version").Length is > 0 and <= 30 && root.Attr("title").Length is > 0 and <= 250, root, "Version/title không hợp lệ.");
        var parts = root.All("PART").ToList();
        Check(parts.Select(p => p.Attr("id")).SequenceEqual(new[] { "P5", "P6", "P7" }), root, "Cần P5, P6, P7 đúng thứ tự.");
        var questions = root.All("QUESTION").ToList();
        Check(questions.Count == 100 && questions.Select(q => q.Attr("id")).Distinct().Count() == 100 && questions.Select(q => q.Number("number")).OrderBy(n => n).SequenceEqual(Enumerable.Range(101,100)), root, "Cần đủ số câu duy nhất 101–200.");
        foreach (var part in parts)
        {
            Require(part, "id shuffle_questions question_count");
            var pn = int.Parse(part.Attr("id")[1..], CultureInfo.InvariantCulture);
            var count = pn == 5 ? 30 : pn == 6 ? 16 : 54;
            Check(part.Number("question_count") == count && part.All("QUESTION").Count() == count, part, "Sai số câu Part.");
            Check(!part.Flag("shuffle_questions") || pn == 5, part, "Không xáo câu trong Part 6/7.");
            var groups = part.All("GROUP").ToList();
            var bounds = GroupBounds.Where(b => b[0] == pn).ToList();
            Check(groups.Count == bounds.Count, part, "Sai số nhóm bài đọc.");
            for (var i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                Require(g, "id part from to shuffle_group shuffle_questions");
                Check(g.Attr("part") == part.Attr("id") && g.Number("from") == bounds[i][1] && g.Number("to") == bounds[i][2] && !g.Flag("shuffle_questions"), g, "Sai ranh giới/thứ tự nhóm.");
                _ = g.Flag("shuffle_group");
                Check(g.All("QUESTION").Select(q => q.Number("number")).SequenceEqual(Enumerable.Range(bounds[i][1], bounds[i][2] - bounds[i][1] + 1)), g, "Câu không khớp nhóm.");
                var blocks = g.All("BLOCK").ToList();
                Check(blocks.Count > 0, g, "Nhóm thiếu bài đọc.");
                foreach (var b in blocks)
                {
                    Require(b, "id type source_pdf_page");
                    Check(b.Number("source_pdf_page") is >= 1 and <= 31, b, "Trang PDF không hợp lệ.");
                    if (b.Attr("type") == "image")
                    {
                        Require(b, "storage mime");
                        Check(b.Attr("storage") == "embedded" && b.Images.Count == 1 && b.Text == "", b, "BLOCK cần một ảnh nhúng, không có văn bản phụ.");
                        var bytes = b.Images[0];
                        Check(ReadingImageValidator.Mime(bytes)==b.Attr("mime"), b, "Ảnh nhúng bị hỏng hoặc MIME/chữ ký ảnh sai.");
                    }
                    else Check(b.Attr("type") == "text" && b.Text.Length > 0 && b.Images.Count == 0, b, "BLOCK text không hợp lệ.");
                }
                var gaps = g.All("GAP_REF").ToList();
                Check(pn == 6 ? gaps.Select(r => r.Attr("question_id")).OrderBy(s => s).SequenceEqual(g.All("QUESTION").Select(q => q.Attr("id")).OrderBy(s => s)) : gaps.Count == 0, g, "Sai/mất GAP_REF.");
                foreach (var gap in gaps)
                    Check(gap.Attr("question_id") == $"Q{gap.Number("gap_label")}" && blocks.Any(b => b.Attr("id") == gap.Attr("source_block")), gap, "GAP_REF sai câu/block.");
            }
            foreach (var q in part.All("QUESTION"))
            {
                Require(q, "id number part group shuffle_options shuffle_question");
                var n = q.Number("number");
                var expectedPart = n <= 130 ? 5 : n <= 146 ? 6 : 7;
                var group = groups.SingleOrDefault(g => g.Number("from") <= n && n <= g.Number("to"));
                Check(pn == expectedPart && q.Attr("id") == $"Q{n}" && q.Attr("part") == part.Attr("id") && q.Attr("group") == (group?.Attr("id") ?? "none"), q, "Sai ID/Part/group.");
                Check(!q.Flag("shuffle_question") || pn == 5, q, "Không xáo câu nhóm.");
                _ = q.Flag("shuffle_options");
                var stem = q.One("STEM"); var hint = q.One("HINT"); var answer = q.One("ANSWER");
                Check((pn == 6 || stem.Text.Length > 0) && hint.Text.Length is > 0 and <= 8000 && stem.Images.Count == 0 && hint.Images.Count == 0, q, "STEM/HINT bị thiếu hoặc chứa ảnh.");
                var options = q.All("OPTION").ToList();
                Check(options.Select(o => o.Attr("label")).OrderBy(s => s).SequenceEqual(new[] { "A","B","C","D" }), q, "Cần đúng 4 lựa chọn A/B/C/D.");
                foreach (var o in options) Check(o.Attr("id") == $"{q.Attr("id")}-{o.Attr("label")}" && o.Text.Length > 0 && o.Images.Count == 0, o, "OPTION sai ID/nội dung.");
                Check(options.Any(o => o.Attr("id") == answer.Attr("option_id")), answer, "Đáp án không thuộc câu.");
            }
        }
        foreach (var name in new[] { "GROUP", "BLOCK", "OPTION" }) Check(root.All(name).Select(n => n.Attr("id")).Distinct().Count() == root.All(name).Count(), root, $"ID {name} trùng.");
        Check(root.All("BLOCK").Count(b => b.Attr("type") == "image") == 27, root, "Reading V06 cần đủ 27 ảnh.");
    }
}
