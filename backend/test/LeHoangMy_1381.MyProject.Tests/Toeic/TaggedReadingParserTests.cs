using Abp.UI;
using LeHoangMy_1381.MyProject.Toeic;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace LeHoangMy_1381.MyProject.Tests.Toeic;

public class TaggedReadingParserTests
{
    private static string Source()
    {
        var path=new DirectoryInfo(AppContext.BaseDirectory);
        while(path!=null){var file=Path.Combine(path.FullName,"docs","TOEIC_Reading_v06_100_cau_co_TAG.docx");if(File.Exists(file))return file;path=path.Parent;}
        throw new FileNotFoundException("Actual tagged Word fixture not found.");
    }
    private static byte[] Mutate(Action<XDocument> mutation)
    {
        using var stream=new MemoryStream();stream.Write(File.ReadAllBytes(Source()));
        using(var zip=new ZipArchive(stream,ZipArchiveMode.Update,true))
        {
            var entry=zip.GetEntry("word/document.xml")!;XDocument xml;
            using(var input=entry.Open())xml=XDocument.Load(input);
            mutation(xml);entry.Delete();var replacement=zip.CreateEntry("word/document.xml");using var output=replacement.Open();xml.Save(output);
        }
        return stream.ToArray();
    }
    private static XNamespace W=>"http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    [Fact] public void Actual_Word_preserves_all_questions_and_embedded_images()
    {
        var root=TaggedReadingParser.Parse(File.ReadAllBytes(Source()));
        Assert.Equal(100,root.All("QUESTION").Count());Assert.Equal(400,root.All("OPTION").Count());Assert.Equal(19,root.All("GROUP").Count());Assert.Equal(27,root.All("BLOCK").Sum(b=>b.Images.Count));Assert.Equal(16,root.All("GAP_REF").Count());
        Assert.All(root.All("QUESTION").Where(q=>q.Attr("part")=="P6"),q=>Assert.Equal("",q.One("STEM").Text));
    }
    [Fact] public void Missing_closing_hint_rejects_entire_document()
    {
        var bytes=Mutate(xml=>xml.Descendants(W+"p").First(p=>p.Value=="[[/HINT]]").Remove());
        Assert.Throws<UserFriendlyException>(()=>TaggedReadingParser.Parse(bytes));
    }
    [Fact] public void Split_Word_runs_still_parse_the_same_tag()
    {
        var bytes=Mutate(xml=>{var p=xml.Descendants(W+"p").First(p=>p.Value.StartsWith("[[QUESTION id=\"Q101\""));var text=p.Value;p.RemoveNodes();p.Add(new XElement(W+"r",new XElement(W+"t",text[..12])),new XElement(W+"r",new XElement(W+"t",text[12..])));});
        Assert.Equal(100,TaggedReadingParser.Parse(bytes).All("QUESTION").Count());
    }
    [Fact] public void Duplicate_question_identity_is_rejected()
    {
        var bytes=Mutate(xml=>{var t=xml.Descendants(W+"t").First(t=>t.Value.Contains("id=\"Q102\""));t.Value=t.Value.Replace("id=\"Q102\"","id=\"Q101\"");});
        Assert.Throws<UserFriendlyException>(()=>TaggedReadingParser.Parse(bytes));
    }
    [Fact] public void Wrong_answer_reference_is_rejected()
    {
        var bytes=Mutate(xml=>{var t=xml.Descendants(W+"t").First(t=>t.Value.StartsWith("[[ANSWER option_id=\"Q101-"));t.Value="[[ANSWER option_id=\"Q200-A\"]]";});
        Assert.Throws<UserFriendlyException>(()=>TaggedReadingParser.Parse(bytes));
    }
    [Fact] public void Invalid_MIME_and_unsafe_shuffle_are_rejected()
    {
        var image=Mutate(xml=>{var t=xml.Descendants(W+"t").First(t=>t.Value.Contains("mime=\"image/png\""));t.Value=t.Value.Replace("image/png","text/html");});
        Assert.Throws<UserFriendlyException>(()=>TaggedReadingParser.Parse(image));
        var shuffle=Mutate(xml=>{var p=xml.Descendants(W+"p").First(p=>p.Value.StartsWith("[[QUESTION id=\"Q131\""));var text=p.Value.Replace("shuffle_question=\"false\"","shuffle_question=\"true\"");p.RemoveNodes();p.Add(new XElement(W+"r",new XElement(W+"t",text)));});
        Assert.Throws<UserFriendlyException>(()=>TaggedReadingParser.Parse(shuffle));
    }
    [Fact] public void Truncated_or_corrupted_PNG_is_rejected()
    {
        using var zip=new ZipArchive(File.OpenRead(Source()),ZipArchiveMode.Read);
        var entry=zip.Entries.First(e=>e.FullName.StartsWith("word/media/")&&e.FullName.EndsWith(".png"));
        using var stream=entry.Open();using var buffer=new MemoryStream();stream.CopyTo(buffer);var image=buffer.ToArray();
        Assert.Equal("image/png",ReadingImageValidator.Mime(image));
        Assert.Null(ReadingImageValidator.Mime(image[..8]));
        image[image.Length/2]^=1;
        Assert.Null(ReadingImageValidator.Mime(image));
    }
}
