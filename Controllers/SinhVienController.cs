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
    public class SinhVienController : Controller
    {
        private QuanLiSinhVien_SenikaaEntities db = new QuanLiSinhVien_SenikaaEntities();

        public ActionResult Index(string khoa, string nganh, string trangThai)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
                return RedirectToAction("DangNhap", "HeThong");

            ViewBag.DSKhoa = db.Sinhviens.Select(s => s.KhoaNhapHoc).Distinct().Where(k => !string.IsNullOrEmpty(k)).ToList();
            ViewBag.DSNganh = db.Sinhviens.Select(s => s.NganhHoc).Distinct().Where(n => !string.IsNullOrEmpty(n)).ToList();
            ViewBag.DSTrangThai = new List<string> { "Đang học", "Đã tốt nghiệp", "Bảo lưu", "Đình chỉ" };

            var query = db.Sinhviens.AsQueryable();

            if (!string.IsNullOrEmpty(khoa)) query = query.Where(s => s.KhoaNhapHoc == khoa);
            if (!string.IsNullOrEmpty(nganh)) query = query.Where(s => s.NganhHoc == nganh);
            if (!string.IsNullOrEmpty(trangThai)) query = query.Where(s => s.TrangThaiHocTap == trangThai);

            var dsSinhVien = query.ToList();

            ViewBag.TongSinhVien = dsSinhVien.Count;
            ViewBag.DangHoc = dsSinhVien.Count(s => s.TrangThaiHocTap == "Đang học");
            ViewBag.DaTotNghiep = dsSinhVien.Count(s => s.TrangThaiHocTap == "Đã tốt nghiệp");
            ViewBag.BaoLuu = dsSinhVien.Count(s => s.TrangThaiHocTap == "Bảo lưu");
            ViewBag.DinhChi = dsSinhVien.Count(s => s.TrangThaiHocTap == "Đình chỉ");

            return View(dsSinhVien);
        }

        [HttpGet]
        public JsonResult GetNextMaSV(string khoa)
        {
            return Json(new { code = GenerateNewMaSV(khoa) }, JsonRequestBehavior.AllowGet);
        }

        private string GenerateNewMaSV(string khoa)
        {
            string soKhoa = Regex.Replace(khoa ?? "", @"\D+", "");
            if (string.IsNullOrEmpty(soKhoa)) soKhoa = "00";

            string prefix = "SV" + soKhoa;

            var lastSV = db.Sinhviens
                .Where(s => s.MaSV.StartsWith(prefix))
                .OrderByDescending(s => s.MaSV)
                .FirstOrDefault();

            int nextNum = 1;
            if (lastSV != null)
            {
                string lastPart = lastSV.MaSV.Replace(prefix, "");
                if (int.TryParse(lastPart, out int lastNum))
                {
                    nextNum = lastNum + 1;
                }
            }

            return prefix + nextNum.ToString("D3");
        }

        [HttpPost]
        public ActionResult LuuThongTin(Sinhvien model, HttpPostedFileBase fileAnh)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
                return RedirectToAction("DangNhap", "HeThong");

            if (fileAnh != null && fileAnh.ContentLength > 0)
            {
                string path = Server.MapPath("~/Content/Uploads/Avatars/");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string fileName = "SV_" + DateTime.Now.Ticks + Path.GetExtension(fileAnh.FileName);
                fileAnh.SaveAs(Path.Combine(path, fileName));
                model.AnhDaiDien = "/Content/Uploads/Avatars/" + fileName;
            }

            var sv = db.Sinhviens.Find(model.MaSV);
            if (sv != null)
            {
                sv.HoTen = model.HoTen;
                sv.GioiTinh = model.GioiTinh;
                sv.NgaySinh = model.NgaySinh;
                sv.SoDT = model.SoDT;
                sv.Email = model.Email;
                sv.KhoaNhapHoc = model.KhoaNhapHoc;
                sv.NganhHoc = model.NganhHoc;
                sv.TrangThaiHocTap = model.TrangThaiHocTap;
                if (model.AnhDaiDien != null) sv.AnhDaiDien = model.AnhDaiDien;
                TempData["ThongBao"] = "Cập nhật thông tin sinh viên thành công.";
            }
            else
            {
                model.MaSV = GenerateNewMaSV(model.KhoaNhapHoc);
                db.Sinhviens.Add(model);
                TempData["ThongBao"] = "Thêm mới sinh viên thành công.";
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult ImportExcel(HttpPostedFileBase fileExcel)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
                return RedirectToAction("DangNhap", "HeThong");

            int soLuongThanhCong = 0;

            if (fileExcel != null && fileExcel.ContentLength > 0)
            {
                ExcelPackage.License.SetNonCommercialPersonal("DATN_Senikaa");

                using (var package = new ExcelPackage(fileExcel.InputStream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet != null)
                    {
                        int rowCount = worksheet.Dimension.Rows;
                        for (int row = 2; row <= rowCount; row++)
                        {
                            string hoTen = worksheet.Cells[row, 1].Text?.Trim();
                            string gioiTinh = worksheet.Cells[row, 2].Text?.Trim();
                            string ngaySinhStr = worksheet.Cells[row, 3].Text?.Trim();
                            string soDT = worksheet.Cells[row, 4].Text?.Trim();
                            string email = worksheet.Cells[row, 5].Text?.Trim();
                            string khoa = worksheet.Cells[row, 6].Text?.Trim();
                            string nganh = worksheet.Cells[row, 7].Text?.Trim();
                            string trangThai = worksheet.Cells[row, 8].Text?.Trim();

                            if (!string.IsNullOrEmpty(hoTen) && !string.IsNullOrEmpty(khoa))
                            {
                                var sv = new Sinhvien
                                {
                                    MaSV = GenerateNewMaSV(khoa),
                                    HoTen = hoTen,
                                    GioiTinh = gioiTinh,
                                    SoDT = soDT,
                                    Email = email,
                                    KhoaNhapHoc = khoa,
                                    NganhHoc = nganh,
                                    TrangThaiHocTap = string.IsNullOrEmpty(trangThai) ? "Đang học" : trangThai
                                };

                                if (DateTime.TryParse(ngaySinhStr, out DateTime ns))
                                {
                                    sv.NgaySinh = ns;
                                }

                                db.Sinhviens.Add(sv);
                                db.SaveChanges();
                                soLuongThanhCong++;
                            }
                        }
                    }
                }
                TempData["ThongBao"] = $"Đã thêm thành công {soLuongThanhCong} sinh viên từ tệp Excel.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult XuatExcel(string khoa, string nganh, string trangThai)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
                return RedirectToAction("DangNhap", "HeThong");

            var query = db.Sinhviens.AsQueryable();
            if (!string.IsNullOrEmpty(khoa)) query = query.Where(s => s.KhoaNhapHoc == khoa);
            if (!string.IsNullOrEmpty(nganh)) query = query.Where(s => s.NganhHoc == nganh);
            if (!string.IsNullOrEmpty(trangThai)) query = query.Where(s => s.TrangThaiHocTap == trangThai);

            var dsSinhVien = query.ToList();

            ExcelPackage.License.SetNonCommercialPersonal("DATN_Senikaa");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("DanhSachSinhVien");

                worksheet.Cells[1, 1].Value = "Mã SV";
                worksheet.Cells[1, 2].Value = "Họ và tên";
                worksheet.Cells[1, 3].Value = "Giới tính";
                worksheet.Cells[1, 4].Value = "Ngày sinh";
                worksheet.Cells[1, 5].Value = "Số điện thoại";
                worksheet.Cells[1, 6].Value = "Email";
                worksheet.Cells[1, 7].Value = "Khóa";
                worksheet.Cells[1, 8].Value = "Ngành học";
                worksheet.Cells[1, 9].Value = "Trạng thái";

                int row = 2;
                foreach (var sv in dsSinhVien)
                {
                    worksheet.Cells[row, 1].Value = sv.MaSV;
                    worksheet.Cells[row, 2].Value = sv.HoTen;
                    worksheet.Cells[row, 3].Value = sv.GioiTinh;
                    worksheet.Cells[row, 4].Value = sv.NgaySinh.HasValue ? sv.NgaySinh.Value.ToString("dd/MM/yyyy") : "";
                    worksheet.Cells[row, 5].Value = sv.SoDT;
                    worksheet.Cells[row, 6].Value = sv.Email;
                    worksheet.Cells[row, 7].Value = sv.KhoaNhapHoc;
                    worksheet.Cells[row, 8].Value = sv.NganhHoc;
                    worksheet.Cells[row, 9].Value = sv.TrangThaiHocTap;
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream(package.GetAsByteArray());
                string fileName = $"DanhSachSinhVien_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        public ActionResult Xoa(string id)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin") return RedirectToAction("DangNhap", "HeThong");
            try
            {
                var sv = db.Sinhviens.Find(id);
                if (sv != null) { db.Sinhviens.Remove(sv); db.SaveChanges(); TempData["ThongBao"] = "Xóa sinh viên thành công."; }
            }
            catch (Exception)
            {
                TempData["Error"] = "Không thể xóa vì sinh viên đã tham gia lớp học phần.";
            }
            return RedirectToAction("Index");
        }
    }
}