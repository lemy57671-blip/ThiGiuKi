using Abp.AspNetCore.Mvc.Authorization;
using Abp.UI;
using LeHoangMy_1381.MyProject.Authorization;
using LeHoangMy_1381.MyProject.Controllers;
using LeHoangMy_1381.MyProject.Reading;
using LeHoangMy_1381.MyProject.Toeic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LeHoangMy_1381.MyProject.Web.Controllers;

[AbpMvcAuthorize(PermissionNames.Pages_Exams)]
public class ReadingController : MyProjectControllerBase
{
    private readonly IReadingAdminAppService _admin;
    public ReadingController(IReadingAdminAppService admin) { _admin=admin; }
    public async Task<IActionResult> Index(bool dashboard = false)
    {
        var exams = await _admin.GetExamsAsync();
        var stats = new List<ExamStatistics>();
        foreach (var e in exams) stats.Add(await _admin.GetStatisticsAsync(e.Id));
        ViewBag.Dashboard=dashboard; ViewBag.TotalAttempts=stats.Sum(s => s.TotalAttempts);
        ViewBag.Average=stats.Sum(s => s.CompletedAttempts) == 0 ? (decimal?)null : stats.Sum(s => (s.AverageAccuracyPercent ?? 0)*s.CompletedAttempts)/stats.Sum(s => s.CompletedAttempts);
        return View(exams);
    }
    [HttpGet] public IActionResult Upload() => View();
    [HttpPost, RequestSizeLimit(27*1024*1024)]
    public async Task<IActionResult> Upload(IFormFile file, bool createNewVersion = false)
    {
        try
        {
            if (file == null || file.Length <= 0 || file.Length > 25*1024*1024 || !Path.GetExtension(file.FileName).Equals(".docx",System.StringComparison.OrdinalIgnoreCase)) throw new UserFriendlyException("Chọn file .docx không quá 25 MB.");
            using var bytes = new MemoryStream(); await file.CopyToAsync(bytes);
            var result = await _admin.ImportAsync(bytes.ToArray(),Path.GetFileName(file.FileName),createNewVersion);
            TempData["ReadingMessage"] = result.Status == "ALREADY_IMPORTED"
                ? "File này đã có phiên bản phù hợp. Đã mở lại đề hiện có, không tạo bản trùng."
                : $"Đã nhập đủ 100 câu và 27 ảnh vào bản nháp phiên bản {result.Version}. Cần rà soát đáp án trước khi xuất bản.";
            return RedirectToAction(nameof(Preview),new { id=result.ExamId });
        }
        catch (UserFriendlyException e) { ViewBag.ImportError=e.Message; ViewBag.CreateNewVersion=createNewVersion; return View(); }
    }
    [HttpPost] public async Task<IActionResult> DeleteDraft(ManageReadingExamInput input)
    {
        try
        {
            if (!ModelState.IsValid) throw new UserFriendlyException("Thiếu mã đề hoặc phiên bản dữ liệu. Tải lại danh sách đề.");
            await _admin.DeleteDraftAsync(input);
            TempData["ReadingMessage"]="Đã xóa bản nháp và nội dung của bản nháp. Bạn có thể nhập lại file Word.";
        }
        catch (UserFriendlyException e) { TempData["ReadingError"]=e.Message; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> Archive(ManageReadingExamInput input)
    {
        try
        {
            if (!ModelState.IsValid) throw new UserFriendlyException("Thiếu mã đề hoặc phiên bản dữ liệu. Tải lại danh sách đề.");
            await _admin.ArchiveAsync(input);
            TempData["ReadingMessage"]="Đã lưu trữ đề. Sinh viên không thể bắt đầu lượt mới; bài đang làm và kết quả cũ vẫn được giữ. Bạn có thể nhập lại file Word thành bản nháp mới.";
        }
        catch (UserFriendlyException e) { TempData["ReadingError"]=e.Message; }
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Preview(int id, int number = 101)
    {
        var model = await _admin.GetPreviewAsync(id);
        ViewBag.Number=model.Questions.Any(q => q.Question.OriginalNumber == number) ? number : 101;
        return View(model);
    }
    [HttpPost] public async Task<IActionResult> Edit(EditQuestionInput input)
    {
        await _admin.EditAsync(input);
        TempData["ReadingMessage"]="Đã lưu nội dung và kiểm tra hợp lệ. Nội dung sửa chỉ thuộc bản nháp này.";
        return RedirectToAction(nameof(Preview),new { id=input.ExamId,number=Request.Form["ReturnNumber"].ToString() });
    }
    [HttpPost] public async Task<IActionResult> Validate(int id)
    {
        await _admin.ValidateAsync(id); TempData["ReadingMessage"]="Đề hợp lệ về cấu trúc. Việc này chưa xác nhận tính đúng của đáp án.";
        return RedirectToAction(nameof(Preview),new { id });
    }
    [HttpPost, RequestSizeLimit(12*1024*1024)] public async Task<IActionResult> EditBlock(int examId,int blockId,string examRowVersion,string textContent,int returnNumber,IFormFile image)
    {
        byte[] data=null;
        if(image!=null)
        {
            if(image.Length>10*1024*1024) throw new UserFriendlyException("Ảnh vượt giới hạn 10 MB.");
            using var stream=new MemoryStream();await image.CopyToAsync(stream);data=stream.ToArray();
        }
        await _admin.EditBlockAsync(new EditReadingBlockInput { ExamId=examId,BlockId=blockId,ExamRowVersion=examRowVersion,TextContent=textContent,ImageData=data });
        TempData["ReadingMessage"]="Đã cập nhật bài đọc. Các đáp án cùng nhóm cần được xác nhận lại.";
        return RedirectToAction(nameof(Preview),new { id=examId,number=returnNumber });
    }
    [HttpPost] public async Task<IActionResult> Review(ReviewInput input)
    {
        await _admin.ReviewAsync(input); TempData["ReadingMessage"]="Đã ghi nhận xác nhận 100 đáp án bởi tài khoản giảng viên hiện tại.";
        return RedirectToAction(nameof(Preview),new { id=input.ExamId });
    }
    [HttpPost] public async Task<IActionResult> Publish(ReviewInput input)
    {
        await _admin.PublishAsync(input); TempData["ReadingMessage"]="Đề đã xuất bản. Sinh viên có thể bắt đầu làm bài.";
        return RedirectToAction(nameof(Preview),new { id=input.ExamId });
    }
    public async Task<IActionResult> Statistics(int? id, int? part)
    {
        var exams=await _admin.GetExamsAsync(); ViewBag.Exams=exams; ViewBag.Part=part;
        if (exams.Count == 0) return View((ExamStatistics)null);
        return View(await _admin.GetStatisticsAsync(id ?? exams[0].Id,part));
    }
}
