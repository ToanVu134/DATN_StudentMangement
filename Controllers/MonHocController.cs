using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DATN_StudentMangement.Models;
using OfficeOpenXml;

namespace DATN_StudentMangement.Controllers
{
    public class MonHocController : BaseController
    {
        private QuanLiSinhVien_SenikaaEntities db = new QuanLiSinhVien_SenikaaEntities();

        public ActionResult Index()
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin") return RedirectToAction("DangNhap", "HeThong");

            ViewBag.NextMaMH = GenerateNewMaMH();
            return View(db.Monhocs.ToList());
        }

        private string GenerateNewMaMH()
        {
            var lastMonHoc = db.Monhocs.OrderByDescending(m => m.MaMH).FirstOrDefault();
            if (lastMonHoc == null || string.IsNullOrEmpty(lastMonHoc.MaMH)) return "MH001";
            string digits = new string(lastMonHoc.MaMH.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits)) return lastMonHoc.MaMH + "1";
            string prefix = lastMonHoc.MaMH.Substring(0, lastMonHoc.MaMH.Length - digits.Length);
            if (int.TryParse(digits, out int number)) return prefix + (number + 1).ToString("D" + digits.Length);
            return lastMonHoc.MaMH + "_new";
        }

        [HttpPost]
        public ActionResult LuuThongTin(Monhoc model)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin") return RedirectToAction("DangNhap", "HeThong");

            var monHoc = db.Monhocs.FirstOrDefault(m => m.MaMH == model.MaMH);
            if (monHoc != null)
            {
                monHoc.TenMH = model.TenMH;
                monHoc.SoTinChi = model.SoTinChi;
                monHoc.TyLeTX1 = model.TyLeTX1;
                monHoc.TyLeTX2 = model.TyLeTX2;
                monHoc.TyLeThi = model.TyLeThi;
                TempData["ThongBao"] = "Cập nhật môn học thành công.";
            }
            else
            {
                model.MaMH = GenerateNewMaMH();
                db.Monhocs.Add(model);
                TempData["ThongBao"] = "Thêm mới môn học thành công.";
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
                            string tenMH = worksheet.Cells[row, 1].Text?.Trim();
                            string soTCStr = worksheet.Cells[row, 2].Text?.Trim();
                            string tyLeTX1Str = worksheet.Cells[row, 3].Text?.Trim();
                            string tyLeTX2Str = worksheet.Cells[row, 4].Text?.Trim();
                            string tyLeThiStr = worksheet.Cells[row, 5].Text?.Trim();

                            if (!string.IsNullOrEmpty(tenMH))
                            {
                                var mh = new Monhoc();
                                mh.MaMH = GenerateNewMaMH();
                                mh.TenMH = tenMH;
                                mh.SoTinChi = int.TryParse(soTCStr, out int tc) ? tc : 3;
                                mh.TyLeTX1 = int.TryParse(tyLeTX1Str, out int t1) ? t1 : 15;
                                mh.TyLeTX2 = int.TryParse(tyLeTX2Str, out int t2) ? t2 : 15;
                                mh.TyLeThi = int.TryParse(tyLeThiStr, out int tt) ? tt : 70;

                                db.Monhocs.Add(mh);
                                db.SaveChanges();
                                soLuongThanhCong++;
                            }
                        }
                    }
                }
                TempData["ThongBao"] = $"Đã xử lý thành công {soLuongThanhCong} môn học từ tệp Excel.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult XuatExcel()
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
                return RedirectToAction("DangNhap", "HeThong");

            var dsMonHoc = db.Monhocs.ToList();

            ExcelPackage.License.SetNonCommercialPersonal("DATN_Senikaa");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("DanhSachMonHoc");

                worksheet.Cells[1, 1].Value = "Mã môn học";
                worksheet.Cells[1, 2].Value = "Tên môn học";
                worksheet.Cells[1, 3].Value = "Số tín chỉ";
                worksheet.Cells[1, 4].Value = "Tỷ lệ TX1 (%)";
                worksheet.Cells[1, 5].Value = "Tỷ lệ TX2 (%)";
                worksheet.Cells[1, 6].Value = "Tỷ lệ Thi (%)";

                int row = 2;
                foreach (var mh in dsMonHoc)
                {
                    worksheet.Cells[row, 1].Value = mh.MaMH;
                    worksheet.Cells[row, 2].Value = mh.TenMH;
                    worksheet.Cells[row, 3].Value = mh.SoTinChi;
                    worksheet.Cells[row, 4].Value = mh.TyLeTX1;
                    worksheet.Cells[row, 5].Value = mh.TyLeTX2;
                    worksheet.Cells[row, 6].Value = mh.TyLeThi;
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                string fileName = $"DanhSachMonHoc_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        public ActionResult Xoa(string id)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin") return RedirectToAction("DangNhap", "HeThong");
            try
            {
                var mh = db.Monhocs.Find(id);
                if (mh != null) { db.Monhocs.Remove(mh); db.SaveChanges(); TempData["ThongBao"] = "Xóa môn học thành công."; }
            }
            catch (Exception)
            {
                TempData["Error"] = "Không thể xóa vì môn học đang có lớp học phần mở.";
            }
            return RedirectToAction("Index");
        }
    }
}