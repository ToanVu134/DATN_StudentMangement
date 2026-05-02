using DATN_StudentMangement.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DATN_StudentMangement.Controllers
{
    public class LopHPController : Controller
    {
        private QuanLiSinhVien_SenikaaEntities db = new QuanLiSinhVien_SenikaaEntities();

        public ActionResult Index(string namHoc, string hocKi)
        {
            if (Session["Quyen"] == null) return RedirectToAction("DangNhap", "HeThong");

            AutoLockOldClasses();

            string quyen = Session["Quyen"].ToString();
            var query = db.LopHPs.AsQueryable();

            if (quyen == "GiangVien")
            {
                string maGV = Session["MaGV"].ToString();
                query = query.Where(l => l.MaGV == maGV);
            }

            ViewBag.DSNamHoc = db.LopHPs.Select(l => l.NamHoc).Distinct().OrderByDescending(n => n).ToList();
            ViewBag.DSHocKi = new List<string> { "Kỳ 1", "Kỳ Xuân", "Kỳ 2", "Kỳ Hè" };
            ViewBag.DSMonHoc = db.Monhocs.ToList();
            ViewBag.DSGiangVien = db.GiangViens.ToList();

            var current = GetCurrentAcademicInfo();
            ViewBag.CurrentNamHoc = current.NamHoc;
            ViewBag.CurrentHocKi = current.HocKi;
            ViewBag.DefaultMaLHP = GenerateMaLHP(current.NamHoc, current.HocKi);

            if (!string.IsNullOrEmpty(namHoc)) query = query.Where(l => l.NamHoc == namHoc);
            if (!string.IsNullOrEmpty(hocKi)) query = query.Where(l => l.HocKi == hocKi);

            return View(query.ToList());
        }

        [HttpPost]
        public ActionResult LuuThongTin(LopHP model)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin") return RedirectToAction("DangNhap", "HeThong");

            var lhp = db.LopHPs.Find(model.MaLHP);
            if (lhp != null)
            {
                if (lhp.TrangThai != "Đã khóa")
                {
                    lhp.MaMH = model.MaMH;
                    lhp.MaGV = model.MaGV;
                    lhp.NamHoc = model.NamHoc;
                    lhp.HocKi = model.HocKi;
                    lhp.Thu = model.Thu;
                    lhp.Buoi = model.Buoi;
                    lhp.SiSoToiDa = model.SiSoToiDa;
                }
            }
            else
            {
                model.TrangThai = "Mở đăng ký";
                db.LopHPs.Add(model);
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Xoa(string id)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin") return RedirectToAction("DangNhap", "HeThong");

            var lhp = db.LopHPs.Find(id);
            if (lhp != null && lhp.TrangThai != "Đã khóa")
            {
                try
                {
                    db.LopHPs.Remove(lhp);
                    db.SaveChanges();
                    TempData["ThongBao"] = "Xóa lớp học phần thành công.";
                }
                catch (Exception)
                {
                    TempData["Error"] = "Không thể xóa lớp này vì đã có dữ liệu sinh viên hoặc điểm số.";
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult DanhSachSinhVien(string id, string search)
        {
            if (Session["Quyen"] == null) return RedirectToAction("DangNhap", "HeThong");

            var lhp = db.LopHPs.Find(id);
            if (lhp == null) return HttpNotFound();

            if (Session["Quyen"].ToString() == "GiangVien" && lhp.MaGV != Session["MaGV"].ToString())
            {
                return new HttpStatusCodeResult(403, "Bạn không có quyền truy cập lớp này.");
            }

            ViewBag.LopHP = lhp;
            var query = db.ChiTietLopHPs.Where(k => k.MaLHP == id).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.Sinhvien.HoTen.Contains(search) || s.MaSV.Contains(search));
            }

            var dsSinhVienTrongLop = query.ToList();

            int tongSinhVien = dsSinhVienTrongLop.Count;
            double diemTrungBinh = tongSinhVien > 0 ? dsSinhVienTrongLop.Average(s => s.DiemTongKet ?? 0) : 0;
            int soSinhVienDat = dsSinhVienTrongLop.Count(s => (s.DiemTongKet ?? 0) >= 4.0);
            double tyLeDat = tongSinhVien > 0 ? ((double)soSinhVienDat / tongSinhVien) * 100 : 0;

            ViewBag.TongSinhVien = tongSinhVien;
            ViewBag.DiemTrungBinh = Math.Round(diemTrungBinh, 2);
            ViewBag.TyLeDat = Math.Round(tyLeDat, 1);

            var maSVTrongLop = dsSinhVienTrongLop.Select(k => k.MaSV).ToList();
            ViewBag.DSSinhVienNgoaiLop = db.Sinhviens.Where(s => !maSVTrongLop.Contains(s.MaSV)).ToList();

            return View(dsSinhVienTrongLop);
        }

        [HttpPost]
        public ActionResult ThemNhieuSinhVien(string maLHP, string[] selectedMaSV)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin")
                return RedirectToAction("DangNhap", "HeThong");

            var targetClass = db.LopHPs.Find(maLHP);
            if (targetClass != null && targetClass.TrangThai != "Đã khóa" && selectedMaSV != null && selectedMaSV.Length > 0)
            {
                int countSuccess = 0;
                int countConflict = 0;

                foreach (var maSV in selectedMaSV)
                {
                    bool daCoTrongLop = db.ChiTietLopHPs.Any(k => k.MaLHP == maLHP && k.MaSV == maSV);
                    if (!daCoTrongLop)
                    {
                        bool trungLich = db.ChiTietLopHPs.Any(c => c.MaSV == maSV &&
                            c.LopHP.NamHoc == targetClass.NamHoc &&
                            c.LopHP.HocKi == targetClass.HocKi &&
                            c.LopHP.Thu == targetClass.Thu &&
                            c.LopHP.Buoi == targetClass.Buoi);

                        if (!trungLich)
                        {
                            db.ChiTietLopHPs.Add(new ChiTietLopHP
                            {
                                MaLHP = maLHP,
                                MaSV = maSV,
                                DiemTX1 = 0,
                                DiemTX2 = 0,
                                DiemThi = 0,
                                DiemTongKet = 0,
                                SoBuoiVang = 0
                            });
                            countSuccess++;
                        }
                        else
                        {
                            countConflict++;
                        }
                    }
                }

                db.SaveChanges();

                TempData["ThongBao"] = $"Đã thêm thành công {countSuccess} sinh viên vào lớp.";
                if (countConflict > 0)
                {
                    TempData["Error"] = $"Có {countConflict} sinh viên bị bỏ qua do trùng lịch (cùng thứ, buổi, học kỳ, năm học).";
                }
            }
            return RedirectToAction("DanhSachSinhVien", new { id = maLHP });
        }

        [HttpPost]
        public ActionResult XoaSinhVienKhoiLop(string maLHP, string maSV)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin") return RedirectToAction("DangNhap", "HeThong");

            var lhp = db.LopHPs.Find(maLHP);
            if (lhp != null && lhp.TrangThai != "Đã khóa")
            {
                var ct = db.ChiTietLopHPs.FirstOrDefault(k => k.MaLHP == maLHP && k.MaSV == maSV);
                if (ct != null) { db.ChiTietLopHPs.Remove(ct); db.SaveChanges(); }
            }
            return RedirectToAction("DanhSachSinhVien", new { id = maLHP });
        }

        [HttpPost]
        public ActionResult CapNhatDiem(string maLHP, string maSV, double? diemTX1, double? diemTX2, double? diemThi, int? soBuoiVang)
        {
            if (Session["Quyen"] == null) return RedirectToAction("DangNhap", "HeThong");

            var lhp = db.LopHPs.Find(maLHP);
            if (lhp == null || lhp.TrangThai == "Đã khóa") return RedirectToAction("DanhSachSinhVien", new { id = maLHP });

            if (Session["Quyen"].ToString() == "GiangVien" && lhp.MaGV != Session["MaGV"].ToString())
                return new HttpStatusCodeResult(403);

            var ct = db.ChiTietLopHPs.FirstOrDefault(k => k.MaLHP == maLHP && k.MaSV == maSV);
            if (ct != null)
            {
                ct.DiemTX1 = Math.Max(0, Math.Min(10, diemTX1 ?? 0));
                ct.DiemTX2 = Math.Max(0, Math.Min(10, diemTX2 ?? 0));
                ct.DiemThi = Math.Max(0, Math.Min(10, diemThi ?? 0));
                ct.SoBuoiVang = soBuoiVang ?? 0;

                double t1 = lhp.Monhoc.TyLeTX1 ?? 15;
                double t2 = lhp.Monhoc.TyLeTX2 ?? 15;
                double thi = lhp.Monhoc.TyLeThi ?? 70;

                ct.DiemTongKet = Math.Round((ct.DiemTX1.Value * t1 / 100) + (ct.DiemTX2.Value * t2 / 100) + (ct.DiemThi.Value * thi / 100), 2);
                db.SaveChanges();
                TempData["ThongBao"] = "Cập nhật điểm thành công.";
            }
            return RedirectToAction("DanhSachSinhVien", new { id = maLHP });
        }

        [HttpGet]
        public JsonResult GetNextMaLHP(string namHoc, string hocKi)
        {
            return Json(new { code = GenerateMaLHP(namHoc, hocKi) }, JsonRequestBehavior.AllowGet);
        }

        private string GenerateMaLHP(string namHoc, string hocKi)
        {
            string[] parts = namHoc.Split('-');
            string yearPart = parts[0].Substring(2) + parts[1].Substring(2);
            string kiPart = hocKi == "Kỳ Xuân" ? "KX" : (hocKi == "Kỳ 2" ? "K2" : (hocKi == "Kỳ Hè" ? "KH" : "K1"));
            string prefix = $"LHP_{yearPart}_{kiPart}_";

            var lastLHP = db.LopHPs.Where(l => l.MaLHP.StartsWith(prefix)).OrderByDescending(l => l.MaLHP).FirstOrDefault();
            int nextNum = 1;
            if (lastLHP != null)
            {
                string lastPart = lastLHP.MaLHP.Substring(lastLHP.MaLHP.LastIndexOf('_') + 1);
                if (int.TryParse(lastPart, out int lastNum)) nextNum = lastNum + 1;
            }
            return prefix + nextNum.ToString("D3");
        }

        private void AutoLockOldClasses()
        {
            var current = GetCurrentAcademicInfo();
            var classes = db.LopHPs.Where(l => l.TrangThai != "Đã khóa").ToList();
            foreach (var item in classes)
            {
                if (IsPastSemester(item.NamHoc, item.HocKi, current.NamHoc, current.HocKi)) item.TrangThai = "Đã khóa";
            }
            db.SaveChanges();
        }

        private AcademicInfo GetCurrentAcademicInfo()
        {
            int month = DateTime.Now.Month; int year = DateTime.Now.Year;
            if (month >= 9) return new AcademicInfo { NamHoc = $"{year}-{year + 1}", HocKi = "Kỳ 1" };
            if (month <= 2) return new AcademicInfo { NamHoc = $"{year - 1}-{year}", HocKi = "Kỳ Xuân" };
            if (month <= 6) return new AcademicInfo { NamHoc = $"{year - 1}-{year}", HocKi = "Kỳ 2" };
            return new AcademicInfo { NamHoc = $"{year - 1}-{year}", HocKi = "Kỳ Hè" };
        }

        public class AcademicInfo { public string NamHoc { get; set; } public string HocKi { get; set; } }

        private bool IsPastSemester(string checkNam, string checkKi, string currNam, string currKi)
        {
            if (checkNam != currNam) { return int.Parse(checkNam.Split('-')[0]) < int.Parse(currNam.Split('-')[0]); }
            var order = new List<string> { "Kỳ 1", "Kỳ Xuân", "Kỳ 2", "Kỳ Hè" };
            return order.IndexOf(checkKi) < order.IndexOf(currKi);
        }

        [HttpGet]
        public ActionResult XuatExcelSinhVien(string id)
        {
            if (Session["Quyen"] == null) return RedirectToAction("DangNhap", "HeThong");

            var dsSinhVien = db.ChiTietLopHPs.Where(c => c.MaLHP == id).ToList();
            ExcelPackage.License.SetNonCommercialPersonal("DATN_Senikaa");
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("DSSV_" + id);
                string[] headers = { "Mã SV", "Họ tên", "Điểm TX1", "Điểm TX2", "Điểm Thi", "Tổng kết" };
                for (int i = 0; i < headers.Length; i++) { worksheet.Cells[1, i + 1].Value = headers[i]; worksheet.Cells[1, i + 1].Style.Font.Bold = true; }
                int row = 2;
                foreach (var item in dsSinhVien)
                {
                    worksheet.Cells[row, 1].Value = item.MaSV;
                    worksheet.Cells[row, 2].Value = item.Sinhvien?.HoTen;
                    worksheet.Cells[row, 3].Value = item.DiemTX1;
                    worksheet.Cells[row, 4].Value = item.DiemTX2;
                    worksheet.Cells[row, 5].Value = item.DiemThi;
                    worksheet.Cells[row, 6].Value = item.DiemTongKet;
                    row++;
                }
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DSSV_{id}_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }
        }

        [HttpPost]
        public ActionResult ImportExcelSinhVien(string maLHP, HttpPostedFileBase fileExcel)
        {
            if (Session["Quyen"] == null || Session["Quyen"].ToString() != "Admin") return RedirectToAction("DangNhap", "HeThong");

            int countSuccess = 0;
            int countConflict = 0;
            var targetClass = db.LopHPs.Find(maLHP);

            if (targetClass != null && fileExcel != null && fileExcel.ContentLength > 0)
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
                            string maSV = worksheet.Cells[row, 1].Text?.Trim();
                            if (!string.IsNullOrEmpty(maSV))
                            {
                                bool svTonTai = db.Sinhviens.Any(s => s.MaSV == maSV);
                                bool daCoTrongLop = db.ChiTietLopHPs.Any(c => c.MaLHP == maLHP && c.MaSV == maSV);

                                if (svTonTai && !daCoTrongLop)
                                {
                                    bool trungLich = db.ChiTietLopHPs.Any(c => c.MaSV == maSV &&
                                        c.LopHP.NamHoc == targetClass.NamHoc &&
                                        c.LopHP.HocKi == targetClass.HocKi &&
                                        c.LopHP.Thu == targetClass.Thu &&
                                        c.LopHP.Buoi == targetClass.Buoi);

                                    if (!trungLich)
                                    {
                                        db.ChiTietLopHPs.Add(new ChiTietLopHP
                                        {
                                            MaLHP = maLHP,
                                            MaSV = maSV,
                                            DiemTX1 = 0,
                                            DiemTX2 = 0,
                                            DiemThi = 0,
                                            DiemTongKet = 0,
                                            SoBuoiVang = 0
                                        });
                                        countSuccess++;
                                    }
                                    else
                                    {
                                        countConflict++;
                                    }
                                }
                            }
                        }
                        db.SaveChanges();
                    }
                }

                TempData["ThongBao"] = $"Đã thêm {countSuccess} sinh viên vào lớp từ tệp Excel.";
                if (countConflict > 0)
                {
                    TempData["Error"] = $"Có {countConflict} sinh viên trong tệp Excel bị bỏ qua do trùng lịch học.";
                }
            }
            return RedirectToAction("DanhSachSinhVien", new { id = maLHP });
        }
    }
}