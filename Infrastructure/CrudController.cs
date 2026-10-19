using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;
using QuanLyLichHoc.Models;
using QuanLyLichHoc.Services;

namespace QuanLyLichHoc.Infrastructure
{
    /// <summary>
    /// Lớp cơ sở cho các màn hình danh mục: danh sách (lọc + phân trang bằng AJAX),
    /// thêm / sửa trong hộp thoại modal, xóa. Các View cần có: _Table.cshtml và _Form.cshtml.
    /// </summary>
    [Authorize(Roles = "GiaoVu,Admin")]
    public abstract class CrudController<T> : Controller where T : class, new()
    {
        protected readonly AppDbContext Db;
        protected readonly AuditService Audit;
        protected const int PageSize = 10;

        protected CrudController(AppDbContext db, AuditService audit) { Db = db; Audit = audit; }

        protected abstract string Title { get; }
        protected abstract IQueryable<T> Items(string q, IQueryCollection f);
        protected abstract Task<T> FindAsync(string id);
        protected abstract string KeyOf(T model);

        protected virtual string FilterPartial => null;
        protected virtual Task PrepareIndexAsync() => Task.CompletedTask;
        protected virtual Task PrepareFormAsync(T model, bool isNew) => Task.CompletedTask;
        protected virtual Task<List<string>> ValidateAsync(T model, bool isNew) => Task.FromResult(new List<string>());
        protected virtual Task BeforeCreateAsync(T model) => Task.CompletedTask;
        protected virtual Task AfterCreateAsync(T model) => Task.CompletedTask;
        protected virtual void CopyValues(T existing, T model) => Db.Entry(existing).CurrentValues.SetValues(model);
        protected virtual Task<string> CanDeleteAsync(T entity) => Task.FromResult<string>(null);
        protected virtual string Describe(T model) => typeof(T).Name + " " + KeyOf(model);

        protected static int I(string s) => int.TryParse(s, out var n) ? n : 0;
        protected bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        protected static SelectList Sel<TItem>(IEnumerable<TItem> items, string value, string text, object selected = null) =>
            new SelectList(items, value, text, selected);

        [HttpGet]
        public virtual async Task<IActionResult> Index(string q, int page = 1)
        {
            await PrepareIndexAsync();
            var list = await PaginatedList<T>.CreateAsync(Items(q, Request.Query), page, PageSize);
            if (IsAjax) return PartialView("_Table", list);
            ViewData["Title"] = Title;
            ViewData["Controller"] = ControllerContext.ActionDescriptor.ControllerName;
            ViewData["FilterPartial"] = FilterPartial;
            ViewData["Q"] = q;
            return View("~/Views/Shared/CrudIndex.cshtml", list);
        }

        [HttpGet]
        public async Task<IActionResult> Form(string id)
        {
            bool isNew = string.IsNullOrEmpty(id);
            var model = isNew ? new T() : await FindAsync(id);
            if (model == null) return NotFound();
            ViewData["IsNew"] = isNew;
            await PrepareFormAsync(model, isNew);
            return PartialView("_Form", model);
        }

        [HttpPost]
        public async Task<IActionResult> Save(T model, bool isNew)
        {
            var errors = new List<string>();
            if (!ModelState.IsValid)
                errors.AddRange(ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Dữ liệu nhập không hợp lệ." : e.ErrorMessage));
            if (errors.Count == 0) errors.AddRange(await ValidateAsync(model, isNew));
            if (errors.Count > 0) return Json(new { ok = false, errors });

            try
            {
                if (isNew)
                {
                    await BeforeCreateAsync(model);
                    Db.Add(model);
                    await Db.SaveChangesAsync();
                    await AfterCreateAsync(model);
                }
                else
                {
                    var existing = await FindAsync(KeyOf(model));
                    if (existing == null) return Json(new { ok = false, errors = new[] { "Không tìm thấy bản ghi cần sửa." } });
                    CopyValues(existing, model);
                    await Db.SaveChangesAsync();
                }
            }
            catch (DbUpdateException)
            {
                return Json(new { ok = false, errors = new[] { "Không thể lưu: dữ liệu bị trùng hoặc vi phạm ràng buộc." } });
            }
            await Audit.LogAsync(User.Identity?.Name, isNew ? "Thêm" : "Sửa", Describe(model));
            return Json(new { ok = true });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var entity = await FindAsync(id);
            if (entity == null) return Json(new { ok = false, errors = new[] { "Không tìm thấy bản ghi." } });
            var block = await CanDeleteAsync(entity);
            if (block != null) return Json(new { ok = false, errors = new[] { block } });
            Db.Remove(entity);
            try { await Db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                return Json(new { ok = false, errors = new[] { "Không thể xóa vì bản ghi đang được sử dụng ở nơi khác." } });
            }
            await Audit.LogAsync(User.Identity?.Name, "Xóa", Describe(entity));
            return Json(new { ok = true });
        }
    }
}
