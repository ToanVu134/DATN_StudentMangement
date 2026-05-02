using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DATN_StudentMangement.Models;
using OfficeOpenXml;
using System.Text.RegularExpressions;

namespace DATN_StudentMangement.Controllers
{
    public class GiangVienController : Controller
    {
        private QuanLiSinhVien_SenikaaEntities db = new QuanLiSinhVien_SenikaaEntities();

        public ActionResult Index(string hocHam, string hocVi)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
                return RedirectToAction("DangNhap", "HeThong");

            // Truyền danh sách cho bộ lọc
            ViewBag.DSHocHam = db.GiangViens.Select(g => g.HocHam).Distinct().Where(h => !string.IsNullOrEmpty(h)).ToList();
            ViewBag.DSHocVi = db.GiangViens.Select(g => g.HocVi).Distinct().Where(h => !string.IsNullOrEmpty(h)).ToList();

            // Truyền mã GV mới cho Modal thêm
            ViewBag.NextMaGV = GenerateNewMaGV();

            var query = db.GiangViens.AsQueryable();
            if (!string.IsNullOrEmpty(hocHam)) query = query.Where(g => g.HocHam == hocHam);
            if (!string.IsNullOrEmpty(hocVi)) query = query.Where(g => g.HocVi == hocVi);

            return View(query.ToList());
        }

        private string GenerateNewMaGV()
        {
            var lastGV = db.GiangViens
                .Where(g => g.MaGV.StartsWith("GV"))
                .OrderByDescending(g => g.MaGV)
                .FirstOrDefault();

            int nextNum = 1;
            if (lastGV != null)
            {
                string lastPart = lastGV.MaGV.Replace("GV", "");
                if (int.TryParse(lastPart, out int lastNum))
                {
                    nextNum = lastNum + 1;
                }
            }
            return "GV" + nextNum.ToString("D3");
        }

        [HttpPost]
        public ActionResult LuuThongTin(GiangVien model, HttpPostedFileBase fileAnh, string MatKhau)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
                return RedirectToAction("DangNhap", "HeThong");

            if (fileAnh != null && fileAnh.ContentLength > 0)
            {
                string path = Server.MapPath("~/Content/Uploads/Avatars/");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string fileName = "GV_" + DateTime.Now.Ticks + Path.GetExtension(fileAnh.FileName);
                fileAnh.SaveAs(Path.Combine(path, fileName));
                model.AnhDaiDien = "/Content/Uploads/Avatars/" + fileName;
            }

            var gv = db.GiangViens.Find(model.MaGV);
            if (gv != null)
            {
                gv.HoTen = model.HoTen;
                gv.GioiTinh = model.GioiTinh;
                gv.NgaySinh = model.NgaySinh;
                gv.SoDT = model.SoDT;
                gv.Email = model.Email;
                gv.ChuyenNganh = model.ChuyenNganh;
                gv.HocHam = model.HocHam;
                gv.HocVi = model.HocVi;
                if (model.AnhDaiDien != null) gv.AnhDaiDien = model.AnhDaiDien;

                // Nếu có nhập mật khẩu mới thì cập nhật luôn bảng TaiKhoan
                if (!string.IsNullOrEmpty(MatKhau) && gv.MaTK.HasValue)
                {
                    var tk = db.TaiKhoans.Find(gv.MaTK.Value);
                    if (tk != null)
                    {
                        tk.MatKhau = HeThongController.GetSHA256(MatKhau);
                    }
                }
                TempData["ThongBao"] = "Cập nhật giảng viên thành công.";
            }
            else
            {
                // Thêm mới tài khoản
                TaiKhoan tk = new TaiKhoan();
                tk.TenDangNhap = model.MaGV;

                // Mã hóa mật khẩu nhập vào, nếu không nhập mặc định là 123456
                string rawPassword = string.IsNullOrEmpty(MatKhau) ? "123456" : MatKhau;
                tk.MatKhau = HeThongController.GetSHA256(rawPassword);
                tk.Quyen = "GiangVien";

                db.TaiKhoans.Add(tk);
                db.SaveChanges(); // Lưu để lấy tk.Id

                model.MaTK = tk.Id;
                db.GiangViens.Add(model);
                TempData["ThongBao"] = "Thêm mới giảng viên thành công.";
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Xoa(string id)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin") return RedirectToAction("DangNhap", "HeThong");
            try
            {
                var gv = db.GiangViens.Find(id);
                if (gv != null)
                {
                    if (gv.MaTK.HasValue)
                    {
                        var tk = db.TaiKhoans.Find(gv.MaTK.Value);
                        if (tk != null) db.TaiKhoans.Remove(tk);
                    }
                    db.GiangViens.Remove(gv); db.SaveChanges(); TempData["ThongBao"] = "Xóa giảng viên thành công.";
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Không thể xóa vì giảng viên đang phụ trách lớp học phần.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult XuatExcel()
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
                return RedirectToAction("DangNhap", "HeThong");

            var dsGiangVien = db.GiangViens.ToList();

            ExcelPackage.License.SetNonCommercialPersonal("DATN_Senikaa");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("DanhSachGiangVien");

                worksheet.Cells[1, 1].Value = "Mã GV";
                worksheet.Cells[1, 2].Value = "Họ và tên";
                worksheet.Cells[1, 3].Value = "Giới tính";
                worksheet.Cells[1, 4].Value = "Ngày sinh";
                worksheet.Cells[1, 5].Value = "Số điện thoại";
                worksheet.Cells[1, 6].Value = "Email";
                worksheet.Cells[1, 7].Value = "Học hàm";
                worksheet.Cells[1, 8].Value = "Học vị";
                worksheet.Cells[1, 9].Value = "Chuyên ngành";

                int row = 2;
                foreach (var g in dsGiangVien)
                {
                    worksheet.Cells[row, 1].Value = g.MaGV;
                    worksheet.Cells[row, 2].Value = g.HoTen;
                    worksheet.Cells[row, 3].Value = g.GioiTinh;
                    worksheet.Cells[row, 4].Value = g.NgaySinh.HasValue ? g.NgaySinh.Value.ToString("dd/MM/yyyy") : "";
                    worksheet.Cells[row, 5].Value = g.SoDT;
                    worksheet.Cells[row, 6].Value = g.Email;
                    worksheet.Cells[row, 7].Value = g.HocHam;
                    worksheet.Cells[row, 8].Value = g.HocVi;
                    worksheet.Cells[row, 9].Value = g.ChuyenNganh;
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                string fileName = $"DanhSachGiangVien_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}